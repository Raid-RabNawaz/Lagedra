import { useEffect, useState } from "react";
import { Plus, Users, Loader2 } from "lucide-react";
import { partnerApi } from "@/features/partners/services/partnerApi";
import { usePartnerMembership } from "@/features/partners/hooks/usePartnerMembership";
import { extractErrorMessage } from "@/lib/errors";
import type { PartnerMemberDto, PartnerMemberRole } from "@/api/types";
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
import { Loader } from "@/components/shared/Loader";
import { ErrorState } from "@/components/shared/ErrorState";
import { EmptyState } from "@/components/shared/EmptyState";
import { FormError } from "@/components/shared/FormError";

const formatDate = (iso: string) =>
  new Date(iso).toLocaleDateString(undefined, { dateStyle: "medium" });

export const PartnerMembersPage = () => {
  const { membership, isLoading: membershipLoading, error: membershipError, refresh } =
    usePartnerMembership();

  const [members, setMembers] = useState<PartnerMemberDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<unknown>(null);
  const [dialogOpen, setDialogOpen] = useState(false);

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
      <div className="flex items-start justify-between gap-3">
        <div>
          <h1 className="flex items-center gap-2 text-3xl font-bold tracking-tight">
            <Users className="h-7 w-7 text-muted-foreground" />
            Members
          </h1>
          <p className="mt-1 text-muted-foreground">
            People in <strong>{membership.organization.name}</strong> who can act on the
            organization's behalf.
          </p>
        </div>
        {isAdmin && (
          <Button onClick={() => setDialogOpen(true)}>
            <Plus className="h-4 w-4" />
            Add member
          </Button>
        )}
      </div>

      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-lg">Team</CardTitle>
          <CardDescription>
            {members.length} member{members.length === 1 ? "" : "s"}
          </CardDescription>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <Loader label="Loading members..." />
          ) : error ? (
            <ErrorState error={error} onRetry={() => void loadMembers()} />
          ) : members.length === 0 ? (
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
                  <TableHead>User ID</TableHead>
                  <TableHead>Role</TableHead>
                  <TableHead className="hidden md:table-cell">Joined</TableHead>
                  <TableHead className="hidden lg:table-cell">Invited by</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {members.map((m) => (
                  <TableRow key={m.id}>
                    <TableCell className="font-mono text-xs" title={m.userId}>
                      {m.userId.slice(0, 12)}…
                    </TableCell>
                    <TableCell>
                      <Badge variant={m.memberRole === "Admin" ? "accent" : "secondary"}>
                        {m.memberRole}
                      </Badge>
                    </TableCell>
                    <TableCell className="hidden md:table-cell text-sm text-muted-foreground">
                      {formatDate(m.joinedAt)}
                    </TableCell>
                    <TableCell className="hidden lg:table-cell font-mono text-xs text-muted-foreground">
                      {m.invitedBy ? `${m.invitedBy.slice(0, 8)}…` : "—"}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      {orgId && (
        <AddMemberDialog
          open={dialogOpen}
          onOpenChange={setDialogOpen}
          orgId={orgId}
          onSuccess={() => void loadMembers()}
        />
      )}
    </div>
  );
};

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
  const [userId, setUserId] = useState("");
  const [role, setRole] = useState<PartnerMemberRole>("Member");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const reset = () => {
    setUserId("");
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
    if (!userId.trim()) return;
    setSubmitting(true);
    setError(null);
    try {
      await partnerApi.addMember(orgId, { userId: userId.trim(), role });
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
            Add an existing Lagedra user to your partner organization. Use their user ID — you can
            ask them to copy it from their Profile page.
          </DialogDescription>
        </DialogHeader>
        <form onSubmit={(e) => void handleSubmit(e)} className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="member-user-id">User ID</Label>
            <Input
              id="member-user-id"
              value={userId}
              onChange={(e) => setUserId(e.target.value)}
              placeholder="00000000-0000-0000-0000-000000000000"
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
