import { useEffect, useState } from "react";
import { ShieldCheck, Plus, Loader2, Check, Ban } from "lucide-react";
import { partnerApi } from "@/features/partners/services/partnerApi";
import { usePartnerMembership } from "@/features/partners/hooks/usePartnerMembership";
import { extractErrorMessage } from "@/lib/errors";
import { EndorsementStatusBadge } from "@/features/partners/components/EndorsementStatusBadge";
import type { PartnerEndorsementDto, PartnerEndorsementStatus } from "@/api/types";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Loader } from "@/components/shared/Loader";
import { ErrorState } from "@/components/shared/ErrorState";
import { EmptyState } from "@/components/shared/EmptyState";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { FormError } from "@/components/shared/FormError";

const formatDate = (iso: string | null) =>
  iso ? new Date(iso).toLocaleDateString(undefined, { dateStyle: "medium" }) : "—";

const truncate = (id: string) => `${id.slice(0, 8)}…`;

type Tab = "Requested" | "Approved" | "Revoked" | "Expired";

export const PartnerEndorsementsPage = () => {
  const { membership, isLoading: membershipLoading, error: membershipError, refresh } =
    usePartnerMembership();

  const [endorsements, setEndorsements] = useState<PartnerEndorsementDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<unknown>(null);
  const [tab, setTab] = useState<Tab>("Requested");
  const [requestDialogOpen, setRequestDialogOpen] = useState(false);
  const [revokeTarget, setRevokeTarget] = useState<PartnerEndorsementDto | null>(null);
  const [actionInFlight, setActionInFlight] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);

  const orgId = membership?.organization.id;
  const isAdmin = membership?.memberRole === "Admin";
  const isVerified = membership?.organization.status === "Verified";

  const loadEndorsements = async () => {
    if (!orgId) return;
    setIsLoading(true);
    setError(null);
    try {
      const data = await partnerApi.listEndorsements(orgId, {
        status: tab as PartnerEndorsementStatus,
      });
      setEndorsements(data);
    } catch (err) {
      setError(err);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    if (orgId) void loadEndorsements();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [orgId, tab]);

  const handleApprove = async (e: PartnerEndorsementDto) => {
    if (!orgId) return;
    setActionInFlight(e.id);
    setActionError(null);
    try {
      await partnerApi.approveEndorsement(orgId, e.id, { note: null });
      await loadEndorsements();
    } catch (err) {
      setActionError(extractErrorMessage(err));
    } finally {
      setActionInFlight(null);
    }
  };

  if (membershipLoading) return <Loader label="Loading endorsements..." />;
  if (membershipError) return <ErrorState error={membershipError} onRetry={() => void refresh()} />;
  if (!membership) return null;

  return (
    <div className="space-y-6">
      <div className="flex items-start justify-between gap-3">
        <div>
          <h1 className="flex items-center gap-2 text-3xl font-bold tracking-tight">
            <ShieldCheck className="h-7 w-7 text-muted-foreground" />
            Endorsements
          </h1>
          <p className="mt-1 text-muted-foreground">
            Vouch for tenants in your organization. An active endorsement gives them{" "}
            <strong>Partner-Backed Protection</strong> and a reduced security deposit.
          </p>
        </div>
        {isAdmin && isVerified && (
          <Button onClick={() => setRequestDialogOpen(true)}>
            <Plus className="h-4 w-4" />
            Request endorsement
          </Button>
        )}
      </div>

      {!isVerified && (
        <Alert>
          <AlertDescription>
            New endorsements can only be created once your organization is verified.
          </AlertDescription>
        </Alert>
      )}

      {actionError && (
        <Alert variant="destructive">
          <AlertDescription>{actionError}</AlertDescription>
        </Alert>
      )}

      <Tabs value={tab} onValueChange={(v) => setTab(v as Tab)}>
        <TabsList>
          <TabsTrigger value="Requested">Requested</TabsTrigger>
          <TabsTrigger value="Approved">Approved</TabsTrigger>
          <TabsTrigger value="Revoked">Revoked</TabsTrigger>
          <TabsTrigger value="Expired">Expired</TabsTrigger>
        </TabsList>
      </Tabs>

      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-lg">{tab} endorsements</CardTitle>
          <CardDescription>
            {endorsements.length} record{endorsements.length === 1 ? "" : "s"}
          </CardDescription>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <Loader label="Loading endorsements..." />
          ) : error ? (
            <ErrorState error={error} onRetry={() => void loadEndorsements()} />
          ) : endorsements.length === 0 ? (
            <EmptyState
              title={
                tab === "Requested"
                  ? "Nothing to review"
                  : tab === "Approved"
                    ? "No active endorsements"
                    : `No ${tab.toLowerCase()} endorsements`
              }
              description={
                tab === "Requested"
                  ? "When a tenant requests an endorsement from your organization, it'll appear here."
                  : undefined
              }
            />
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Tenant</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead className="hidden md:table-cell">Requested</TableHead>
                  <TableHead className="hidden lg:table-cell">Approved</TableHead>
                  <TableHead className="hidden lg:table-cell">Expires</TableHead>
                  <TableHead className="hidden xl:table-cell">Note</TableHead>
                  <TableHead className="text-right">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {endorsements.map((e) => (
                  <TableRow key={e.id}>
                    <TableCell className="font-mono text-xs" title={e.tenantUserId}>
                      {truncate(e.tenantUserId)}
                    </TableCell>
                    <TableCell>
                      <EndorsementStatusBadge status={e.status} />
                    </TableCell>
                    <TableCell className="hidden md:table-cell text-sm text-muted-foreground">
                      {formatDate(e.requestedAt)}
                    </TableCell>
                    <TableCell className="hidden lg:table-cell text-sm text-muted-foreground">
                      {formatDate(e.approvedAt)}
                    </TableCell>
                    <TableCell className="hidden lg:table-cell text-sm text-muted-foreground">
                      {formatDate(e.expiresAt)}
                    </TableCell>
                    <TableCell className="hidden xl:table-cell text-sm text-muted-foreground max-w-[200px] truncate">
                      {e.note ?? "—"}
                    </TableCell>
                    <TableCell className="text-right">
                      <div className="flex items-center justify-end gap-2">
                        {e.status === "Requested" && isAdmin && (
                          <Button
                            variant="outline"
                            size="sm"
                            onClick={() => void handleApprove(e)}
                            disabled={actionInFlight === e.id}
                          >
                            {actionInFlight === e.id ? (
                              <Loader2 className="h-3 w-3 animate-spin" />
                            ) : (
                              <Check className="h-3 w-3" />
                            )}
                            Approve
                          </Button>
                        )}
                        {(e.status === "Requested" || e.status === "Approved") && isAdmin && (
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => setRevokeTarget(e)}
                          >
                            <Ban className="h-3 w-3" /> Revoke
                          </Button>
                        )}
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      {orgId && (
        <RequestEndorsementDialog
          open={requestDialogOpen}
          onOpenChange={setRequestDialogOpen}
          orgId={orgId}
          onSuccess={() => {
            setTab("Approved");
            void loadEndorsements();
          }}
        />
      )}
      {orgId && revokeTarget && (
        <RevokeEndorsementDialog
          endorsement={revokeTarget}
          orgId={orgId}
          onClose={() => setRevokeTarget(null)}
          onSuccess={() => {
            setRevokeTarget(null);
            void loadEndorsements();
          }}
        />
      )}
    </div>
  );
};

function RequestEndorsementDialog({
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
  const [tenantUserId, setTenantUserId] = useState("");
  const [note, setNote] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const reset = () => {
    setTenantUserId("");
    setNote("");
    setError(null);
    setSubmitting(false);
  };

  const handleClose = (next: boolean) => {
    if (!next) reset();
    onOpenChange(next);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!tenantUserId.trim()) return;
    setSubmitting(true);
    setError(null);
    try {
      await partnerApi.requestEndorsement(orgId, {
        tenantUserId: tenantUserId.trim(),
        note: note.trim() || null,
      });
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
          <DialogTitle>Endorse a tenant</DialogTitle>
          <DialogDescription>
            Partner-initiated endorsements skip the request step and become Approved immediately.
          </DialogDescription>
        </DialogHeader>
        <form onSubmit={(e) => void handleSubmit(e)} className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="endorse-user-id">Tenant user ID</Label>
            <Input
              id="endorse-user-id"
              value={tenantUserId}
              onChange={(e) => setTenantUserId(e.target.value)}
              placeholder="00000000-0000-0000-0000-000000000000"
              required
            />
            <p className="text-xs text-muted-foreground">
              Ask the tenant to copy their user ID from their Profile page.
            </p>
          </div>
          <div className="space-y-2">
            <Label htmlFor="endorse-note">Note (optional)</Label>
            <Textarea
              id="endorse-note"
              value={note}
              onChange={(e) => setNote(e.target.value)}
              placeholder="Why are you endorsing this tenant?"
              rows={3}
            />
          </div>
          {error && <FormError message={error} />}
          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => handleClose(false)}>
              Cancel
            </Button>
            <Button type="submit" disabled={submitting}>
              {submitting && <Loader2 className="h-4 w-4 animate-spin" />}
              Endorse
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

function RevokeEndorsementDialog({
  endorsement,
  orgId,
  onClose,
  onSuccess,
}: {
  endorsement: PartnerEndorsementDto;
  orgId: string;
  onClose: () => void;
  onSuccess: () => void;
}) {
  const [reason, setReason] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!reason.trim()) {
      setError("A reason is required to revoke an endorsement.");
      return;
    }
    setSubmitting(true);
    setError(null);
    try {
      await partnerApi.revokeEndorsement(orgId, endorsement.id, { reason: reason.trim() });
      onSuccess();
    } catch (err) {
      setError(extractErrorMessage(err));
      setSubmitting(false);
    }
  };

  return (
    <Dialog open onOpenChange={(next) => !next && onClose()}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Revoke endorsement</DialogTitle>
          <DialogDescription>
            The tenant will lose Partner-Backed Protection from your organization. This is recorded
            in the audit log and is not reversible.
          </DialogDescription>
        </DialogHeader>
        <form onSubmit={(e) => void handleSubmit(e)} className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="revoke-reason">Reason</Label>
            <Textarea
              id="revoke-reason"
              value={reason}
              onChange={(e) => setReason(e.target.value)}
              placeholder="Tenant is no longer affiliated with our organization."
              rows={3}
              required
            />
          </div>
          {error && <FormError message={error} />}
          <DialogFooter>
            <Button type="button" variant="outline" onClick={onClose}>
              Cancel
            </Button>
            <Button type="submit" variant="destructive" disabled={submitting}>
              {submitting && <Loader2 className="h-4 w-4 animate-spin" />}
              Revoke
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
