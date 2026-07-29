import { useEffect, useMemo, useState } from "react";
import { ShieldCheck, Plus, Loader2, Check, Ban } from "lucide-react";
import { partnerApi } from "@/features/partners/services/partnerApi";
import { usePartnerMembership } from "@/features/partners/hooks/usePartnerMembership";
import { extractErrorMessage } from "@/lib/errors";
import { EndorsementStatusBadge } from "@/features/partners/components/EndorsementStatusBadge";
import { PersonCell } from "@/features/partners/components/PersonCell";
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
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { PageHeader } from "@/components/shared/PageHeader";
import { FilterTabs, type FilterTabOption } from "@/components/shared/FilterTabs";
import { ListRowsSkeleton } from "@/components/shared/ListSkeleton";
import { Loader } from "@/components/shared/Loader";
import { ErrorState } from "@/components/shared/ErrorState";
import { EmptyState } from "@/components/shared/EmptyState";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { FormError } from "@/components/shared/FormError";

const formatDate = (iso: string | null) =>
  iso ? new Date(iso).toLocaleDateString(undefined, { dateStyle: "medium" }) : "—";

const STATUSES: PartnerEndorsementStatus[] = ["Requested", "Approved", "Revoked", "Expired"];

const EMPTY_COPY: Record<PartnerEndorsementStatus, { title: string; description?: string }> = {
  Requested: {
    title: "Nothing to review",
    description: "When a tenant requests an endorsement from your organization, it'll appear here.",
  },
  Approved: {
    title: "No active endorsements",
    description: "Approve a request or endorse a tenant directly to see them here.",
  },
  Revoked: { title: "No revoked endorsements" },
  Expired: { title: "No expired endorsements" },
};

export const PartnerEndorsementsPage = () => {
  const { membership, isLoading: membershipLoading, error: membershipError, refresh } =
    usePartnerMembership();

  const [endorsements, setEndorsements] = useState<PartnerEndorsementDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<unknown>(null);
  const [tab, setTab] = useState<PartnerEndorsementStatus>("Requested");
  const [requestDialogOpen, setRequestDialogOpen] = useState(false);
  const [revokeTarget, setRevokeTarget] = useState<PartnerEndorsementDto | null>(null);
  const [actionInFlight, setActionInFlight] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);

  const orgId = membership?.organization.id;
  const isAdmin = membership?.memberRole === "Admin";
  const isVerified = membership?.organization.status === "Verified";

  const loadEndorsements = async (silent = false) => {
    if (!orgId) return;
    if (!silent) setIsLoading(true);
    setError(null);
    try {
      // Fetch every status once so tab switches are instant and counts stay
      // visible on the filter pills.
      const data = await partnerApi.listEndorsements(orgId, { take: 200 });
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
  }, [orgId]);

  const countByStatus = useMemo(() => {
    const counts = { Requested: 0, Approved: 0, Revoked: 0, Expired: 0 } as Record<
      PartnerEndorsementStatus,
      number
    >;
    for (const e of endorsements) counts[e.status] += 1;
    return counts;
  }, [endorsements]);

  const visible = useMemo(
    () => endorsements.filter((e) => e.status === tab),
    [endorsements, tab],
  );

  const tabOptions: FilterTabOption<PartnerEndorsementStatus>[] = STATUSES.map((s) => ({
    value: s,
    label: s,
    count: countByStatus[s],
  }));

  const handleApprove = async (e: PartnerEndorsementDto) => {
    if (!orgId) return;
    setActionInFlight(e.id);
    setActionError(null);
    try {
      await partnerApi.approveEndorsement(orgId, e.id, { note: null });
      await loadEndorsements(true);
    } catch (err) {
      setActionError(extractErrorMessage(err));
    } finally {
      setActionInFlight(null);
    }
  };

  if (membershipLoading) return <Loader label="Loading endorsements..." />;
  if (membershipError) return <ErrorState error={membershipError} onRetry={() => void refresh()} />;
  if (!membership) return null;

  const showApproved = tab === "Approved" || tab === "Expired";
  const showExpires = tab === "Approved" || tab === "Expired";
  const showRevoked = tab === "Revoked";
  const showActions = isAdmin && (tab === "Requested" || tab === "Approved");

  return (
    <div className="space-y-6">
      <PageHeader
        icon={ShieldCheck}
        title="Endorsements"
        description={
          <>
            Vouch for tenants in your organization. An active endorsement gives them{" "}
            <strong>Partner-Backed Protection</strong> and a reduced security deposit.
          </>
        }
      >
        {isAdmin && isVerified && (
          <Button onClick={() => setRequestDialogOpen(true)}>
            <Plus className="h-4 w-4" />
            Endorse a tenant
          </Button>
        )}
      </PageHeader>

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

      <FilterTabs
        aria-label="Filter endorsements by status"
        options={tabOptions}
        value={tab}
        onChange={setTab}
        hideZeroCounts
      />

      {isLoading ? (
        <ListRowsSkeleton rows={3} />
      ) : error ? (
        <ErrorState error={error} onRetry={() => void loadEndorsements()} />
      ) : (
        <Card>
          <CardHeader className="pb-3">
            <CardTitle className="text-lg">{tab} endorsements</CardTitle>
            <CardDescription>
              {visible.length} record{visible.length === 1 ? "" : "s"}
            </CardDescription>
          </CardHeader>
          <CardContent>
            {visible.length === 0 ? (
              <EmptyState title={EMPTY_COPY[tab].title} description={EMPTY_COPY[tab].description} />
            ) : (
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Tenant</TableHead>
                    <TableHead className="hidden sm:table-cell">Status</TableHead>
                    <TableHead className="hidden md:table-cell">Requested</TableHead>
                    {showApproved && (
                      <TableHead className="hidden lg:table-cell">Approved</TableHead>
                    )}
                    {showExpires && (
                      <TableHead className="hidden lg:table-cell">Expires</TableHead>
                    )}
                    {showRevoked && (
                      <>
                        <TableHead className="hidden md:table-cell">Revoked</TableHead>
                        <TableHead>Reason</TableHead>
                      </>
                    )}
                    {!showRevoked && <TableHead className="hidden xl:table-cell">Note</TableHead>}
                    {showActions && <TableHead className="text-right">Actions</TableHead>}
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {visible.map((e) => (
                    <TableRow key={e.id}>
                      <TableCell>
                        <PersonCell
                          displayName={e.tenantDisplayName}
                          email={e.tenantEmail}
                          userId={e.tenantUserId}
                        />
                      </TableCell>
                      <TableCell className="hidden sm:table-cell">
                        <EndorsementStatusBadge status={e.status} />
                      </TableCell>
                      <TableCell className="hidden md:table-cell text-sm text-muted-foreground">
                        {formatDate(e.requestedAt)}
                      </TableCell>
                      {showApproved && (
                        <TableCell className="hidden lg:table-cell text-sm text-muted-foreground">
                          {formatDate(e.approvedAt)}
                        </TableCell>
                      )}
                      {showExpires && (
                        <TableCell className="hidden lg:table-cell text-sm text-muted-foreground">
                          {formatDate(e.expiresAt)}
                        </TableCell>
                      )}
                      {showRevoked && (
                        <>
                          <TableCell className="hidden md:table-cell text-sm text-muted-foreground">
                            {formatDate(e.revokedAt)}
                          </TableCell>
                          <TableCell
                            className="text-sm text-muted-foreground max-w-[240px] truncate"
                            title={e.revokeReason ?? undefined}
                          >
                            {e.revokeReason ?? "—"}
                          </TableCell>
                        </>
                      )}
                      {!showRevoked && (
                        <TableCell
                          className="hidden xl:table-cell text-sm text-muted-foreground max-w-[200px] truncate"
                          title={e.note ?? undefined}
                        >
                          {e.note ?? "—"}
                        </TableCell>
                      )}
                      {showActions && (
                        <TableCell className="text-right">
                          <div className="flex items-center justify-end gap-2">
                            {e.status === "Requested" && (
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
                            <Button variant="ghost" size="sm" onClick={() => setRevokeTarget(e)}>
                              <Ban className="h-3 w-3" /> Revoke
                            </Button>
                          </div>
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
        <RequestEndorsementDialog
          open={requestDialogOpen}
          onOpenChange={setRequestDialogOpen}
          orgId={orgId}
          onSuccess={() => {
            setTab("Approved");
            void loadEndorsements(true);
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
            void loadEndorsements(true);
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

  const tenantLabel =
    endorsement.tenantDisplayName?.trim() || `tenant ${endorsement.tenantUserId.slice(0, 8)}…`;

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
            <strong>{tenantLabel}</strong> will lose Partner-Backed Protection from your
            organization. This is recorded in the audit log and is not reversible.
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
