import { useEffect, useState } from "react";
import { Plus, Users, Loader2, Trash2 } from "lucide-react";
import { partnerApi } from "@/features/partners/services/partnerApi";
import { usePartnerMembership } from "@/features/partners/hooks/usePartnerMembership";
import { useAuthStore } from "@/app/auth/authStore";
import { extractErrorMessage } from "@/lib/errors";
import type { PartnerMemberDto, PartnerMemberRole } from "@/api/types";
import { PersonCell } from "@/features/partners/components/PersonCell";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select } from "@/components/ui/select";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { PageHeader } from "@/components/shared/PageHeader";
import { ListRowsSkeleton } from "@/components/shared/ListSkeleton";
import { Loader } from "@/components/shared/Loader";
import { ErrorState } from "@/components/shared/ErrorState";
import { EmptyState } from "@/components/shared/EmptyState";
import { FormError } from "@/components/shared/FormError";

const formatDate = (iso: string) =>
  new Date(iso).toLocaleDateString(undefined, { dateStyle: "medium" });

const memberLabel = (m: PartnerMemberDto) =>
  m.displayName?.trim() || m.email?.trim() || `${m.userId.slice(0, 8)}…`;

export const PartnerMembersPage = () => {
  const { membership, isLoading: membershipLoading, error: membershipError, refresh } =
    usePartnerMembership();

  const currentUserId = useAuthStore((s) => s.user?.userId);

  const [members, setMembers] = useState<PartnerMemberDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<unknown>(null);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [memberToRemove, setMemberToRemove] = useState<PartnerMemberDto | null>(null);

  const orgId = membership?.organization.id;
  const isAdmin = membership?.memberRole === "Admin";

  const loadMembers = async () => {
    if (!orgId) return;
    setIsLoading(true);
    setError(null);
    try {
      const data = await partnerApi.listMembers(orgId);
      setMembers(data);
    } catch (err) {
      setError(err);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    if (orgId) void loadMembers();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [orgId]);

  if (membershipLoading) return <Loader label="Loading members..." />;
  if (membershipError) return <ErrorState error={membershipError} onRetry={() => void refresh()} />;
  if (!membership) return null;

  return (
    <div className="space-y-6">
      <PageHeader
        icon={Users}
        title="Members"
        description={
          <>
            People in <strong>{membership.organization.name}</strong> who can act on the
            organization's behalf.
          </>
        }
      >
        {isAdmin && (
          <Button onClick={() => setDialogOpen(true)}>
            <Plus className="h-4 w-4" />
            Add member
          </Button>
        )}
      </PageHeader>

      {isLoading ? (
        <ListRowsSkeleton rows={3} />
      ) : error ? (
        <ErrorState error={error} onRetry={() => void loadMembers()} />
      ) : (
        <Card>
          <CardHeader className="pb-3">
            <CardTitle className="text-lg">Team</CardTitle>
            <CardDescription>
              {members.length} member{members.length === 1 ? "" : "s"}
            </CardDescription>
          </CardHeader>
          <CardContent>
            {members.length === 0 ? (
              <EmptyState
                title="No members yet"
                description="Add a teammate to give them access to this partner organization."
              >
                {isAdmin && (
                  <Button onClick={() => setDialogOpen(true)}>
                    <Plus className="h-4 w-4" />
                    Add member
                  </Button>
                )}
              </EmptyState>
            ) : (
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Member</TableHead>
                    <TableHead>Role</TableHead>
                    <TableHead className="hidden md:table-cell">Joined</TableHead>
                    <TableHead className="hidden lg:table-cell">Invited by</TableHead>
                    {isAdmin && <TableHead className="w-12" />}
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {members.map((m) => (
                    <TableRow key={m.id}>
                      <TableCell>
                        <PersonCell
                          displayName={m.displayName}
                          email={m.email}
                          userId={m.userId}
                        />
                      </TableCell>
                      <TableCell>
                        <Badge variant={m.memberRole === "Admin" ? "accent" : "secondary"}>
                          {m.memberRole}
                        </Badge>
                      </TableCell>
                      <TableCell className="hidden md:table-cell text-sm text-muted-foreground">
                        {formatDate(m.joinedAt)}
                      </TableCell>
                      <TableCell
                        className="hidden lg:table-cell text-sm text-muted-foreground"
                        title={m.invitedBy ?? undefined}
                      >
                        {m.invitedByDisplayName?.trim() ||
                          (m.invitedBy ? `${m.invitedBy.slice(0, 8)}…` : "—")}
                      </TableCell>
                      {isAdmin && (
                        <TableCell className="text-right">
                          {m.userId !== currentUserId && (
                            <Button
                              variant="ghost"
                              size="icon"
                              className="h-8 w-8 text-muted-foreground hover:text-destructive"
                              title="Remove member"
                              onClick={() => setMemberToRemove(m)}
                            >
                              <Trash2 className="h-4 w-4" />
                            </Button>
                          )}
                        </TableCell>
                      )}
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            )}
          </CardContent>
        </Card>
      )}

      {orgId && (
        <AddMemberDialog
          open={dialogOpen}
          onOpenChange={setDialogOpen}
          orgId={orgId}
          onSuccess={() => void loadMembers()}
        />
      )}

      {orgId && (
        <RemoveMemberDialog
          member={memberToRemove}
          orgId={orgId}
          onClose={() => setMemberToRemove(null)}
          onSuccess={() => void loadMembers()}
        />
      )}
    </div>
  );
};

function RemoveMemberDialog({
  member,
  orgId,
  onClose,
  onSuccess,
}: {
  member: PartnerMemberDto | null;
  orgId: string;
  onClose: () => void;
  onSuccess: () => void;
}) {
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const handleClose = (next: boolean) => {
    if (!next) {
      setError(null);
      setSubmitting(false);
      onClose();
    }
  };

  const handleConfirm = async () => {
    if (!member) return;
    setSubmitting(true);
    setError(null);
    try {
      await partnerApi.removeMember(orgId, member.id);
      onSuccess();
      handleClose(false);
    } catch (err) {
      setError(extractErrorMessage(err));
      setSubmitting(false);
    }
  };

  return (
    <Dialog open={member !== null} onOpenChange={handleClose}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Remove member</DialogTitle>
          <DialogDescription>
            {member ? (
              <>
                <strong>{memberLabel(member)}</strong> will lose access to this partner
                organization. This does not delete their Lagedra account.
              </>
            ) : null}
          </DialogDescription>
        </DialogHeader>
        {error && <FormError message={error} />}
        <DialogFooter>
          <Button type="button" variant="outline" onClick={() => handleClose(false)}>
            Cancel
          </Button>
          <Button
            type="button"
            variant="destructive"
            disabled={submitting}
            onClick={() => void handleConfirm()}
          >
            {submitting && <Loader2 className="h-4 w-4 animate-spin" />}
            Remove member
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

function AddMemberDialog({
  open,
  onOpenChange,
  orgId,
  onSuccess,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  orgId: string;
  onSuccess: () => void;
}) {
  const [email, setEmail] = useState("");
  const [role, setRole] = useState<PartnerMemberRole>("Member");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const reset = () => {
    setEmail("");
    setRole("Member");
    setError(null);
    setSubmitting(false);
  };

  const handleClose = (next: boolean) => {
    if (!next) reset();
    onOpenChange(next);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!email.trim()) return;
    setSubmitting(true);
    setError(null);
    try {
      await partnerApi.addMember(orgId, { email: email.trim(), role });
      onSuccess();
      handleClose(false);
    } catch (err) {
      setError(extractErrorMessage(err));
      setSubmitting(false);
    }
  };

  return (
    <Dialog open={open} onOpenChange={handleClose}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Add member</DialogTitle>
          <DialogDescription>
            Enter the email address of the teammate you want to add. They need an existing
            Lagedra account with that email.
          </DialogDescription>
        </DialogHeader>
        <form onSubmit={(e) => void handleSubmit(e)} className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="member-email">Email address</Label>
            <Input
              id="member-email"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="teammate@company.com"
              autoComplete="off"
              required
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="member-role">Role</Label>
            <Select
              id="member-role"
              value={role}
              onChange={(e) => setRole(e.target.value as PartnerMemberRole)}
            >
              <option value="Member">Member — can view, redeem referrals, request endorsements</option>
              <option value="Admin">Admin — can do everything (incl. invite, generate links)</option>
            </Select>
          </div>
          {error && <FormError message={error} />}
          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => handleClose(false)}>
              Cancel
            </Button>
            <Button type="submit" disabled={submitting}>
              {submitting && <Loader2 className="h-4 w-4 animate-spin" />}
              Add member
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
