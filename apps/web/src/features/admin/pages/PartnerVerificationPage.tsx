import { useEffect, useMemo, useState } from "react";
import {
  Building2,
  RefreshCw,
  Search,
  Check,
  Ban,
  Loader2,
  ExternalLink,
} from "lucide-react";
import { partnerApi } from "@/features/partners/services/partnerApi";
import { PartnerStatusBadge } from "@/features/partners/components/PartnerStatusBadge";
import { extractErrorMessage } from "@/lib/errors";
import type {
  PartnerOrganizationDto,
  PartnerOrganizationStatus,
} from "@/api/types";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Loader } from "@/components/shared/Loader";
import { ErrorState } from "@/components/shared/ErrorState";
import { EmptyState } from "@/components/shared/EmptyState";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { FormError } from "@/components/shared/FormError";

const formatDate = (iso: string | null) =>
  iso ? new Date(iso).toLocaleDateString(undefined, { dateStyle: "medium" }) : "—";

type Tab = "Pending" | "Verified" | "Suspended" | "All";

const tabToStatus: Record<Tab, PartnerOrganizationStatus | undefined> = {
  Pending: "PendingVerification",
  Verified: "Verified",
  Suspended: "Suspended",
  All: undefined,
};

export const PartnerVerificationPage = () => {
  const [orgs, setOrgs] = useState<PartnerOrganizationDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<unknown>(null);
  const [tab, setTab] = useState<Tab>("Pending");
  const [searchInput, setSearchInput] = useState("");
  const [appliedSearch, setAppliedSearch] = useState("");
  const [suspendTarget, setSuspendTarget] = useState<PartnerOrganizationDto | null>(null);
  const [verifyInFlight, setVerifyInFlight] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);

  const load = async () => {
    setIsLoading(true);
    setError(null);
    setActionError(null);
    try {
      const data = await partnerApi.listAllOrganizations({
        status: tabToStatus[tab],
        search: appliedSearch.trim() || undefined,
      });
      setOrgs(data);
    } catch (err) {
      setError(err);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [tab, appliedSearch]);

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    setAppliedSearch(searchInput);
  };

  const handleVerify = async (org: PartnerOrganizationDto) => {
    if (!window.confirm(`Verify "${org.name}"?`)) return;
    setVerifyInFlight(org.id);
    setActionError(null);
    try {
      await partnerApi.verifyOrganization(org.id);
      await load();
    } catch (err) {
      setActionError(extractErrorMessage(err));
    } finally {
      setVerifyInFlight(null);
    }
  };

  const counts = useMemo(() => {
    return {
      total: orgs.length,
      pending: orgs.filter((o) => o.status === "PendingVerification").length,
    };
  }, [orgs]);

  return (
    <div className="space-y-6">
      <div className="flex items-start justify-between gap-3">
        <div>
          <h1 className="flex items-center gap-2 text-3xl font-bold tracking-tight">
            <Building2 className="h-7 w-7 text-muted-foreground" />
            Partner organizations
          </h1>
          <p className="mt-1 text-muted-foreground">
            Verify, suspend, or audit institutional partners.
          </p>
        </div>
        <Button variant="outline" onClick={() => void load()} disabled={isLoading}>
          <RefreshCw className={`h-4 w-4 ${isLoading ? "animate-spin" : ""}`} />
          Refresh
        </Button>
      </div>

      {actionError && (
        <Alert variant="destructive">
          <AlertDescription>{actionError}</AlertDescription>
        </Alert>
      )}

      <Tabs value={tab} onValueChange={(v) => setTab(v as Tab)}>
        <TabsList>
          <TabsTrigger value="Pending">Pending</TabsTrigger>
          <TabsTrigger value="Verified">Verified</TabsTrigger>
          <TabsTrigger value="Suspended">Suspended</TabsTrigger>
          <TabsTrigger value="All">All</TabsTrigger>
        </TabsList>
      </Tabs>

      <Card>
        <CardHeader className="space-y-3 pb-3">
          <div className="flex items-start justify-between gap-3">
            <div>
              <CardTitle className="text-lg">{tab} organizations</CardTitle>
              <CardDescription>
                {counts.total} record{counts.total === 1 ? "" : "s"}
                {tab !== "Pending" && counts.pending > 0
                  ? ` (${counts.pending} still pending verification)`
                  : ""}
              </CardDescription>
            </div>
            <form onSubmit={handleSearch} className="flex w-full max-w-sm items-center gap-2">
              <Label htmlFor="org-search" className="sr-only">
                Search
              </Label>
              <Input
                id="org-search"
                value={searchInput}
                onChange={(e) => setSearchInput(e.target.value)}
                placeholder="Search by name, email, or tax ID..."
              />
              <Button type="submit" variant="outline" size="sm">
                <Search className="h-4 w-4" />
                Search
              </Button>
            </form>
          </div>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <Loader label="Loading partner organizations..." />
          ) : error ? (
            <ErrorState error={error} onRetry={() => void load()} />
          ) : orgs.length === 0 ? (
            <EmptyState
              title="No organizations"
              description={
                appliedSearch
                  ? `No matches for "${appliedSearch}".`
                  : tab === "Pending"
                    ? "There are no pending partner organizations."
                    : "Nothing here yet."
              }
            />
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Name</TableHead>
                  <TableHead>Type</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead className="hidden md:table-cell">Contact</TableHead>
                  <TableHead className="hidden lg:table-cell">Tax ID</TableHead>
                  <TableHead className="hidden lg:table-cell">Submitted</TableHead>
                  <TableHead className="hidden lg:table-cell">Verified</TableHead>
                  <TableHead className="text-right">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {orgs.map((o) => (
                  <TableRow key={o.id}>
                    <TableCell>
                      <div className="font-medium">{o.name}</div>
                      <div className="font-mono text-[10px] text-muted-foreground" title={o.id}>
                        {o.id.slice(0, 8)}…
                      </div>
                    </TableCell>
                    <TableCell className="text-sm">{o.organizationType}</TableCell>
                    <TableCell>
                      <PartnerStatusBadge status={o.status} />
                    </TableCell>
                    <TableCell className="hidden md:table-cell text-sm">
                      <a
                        href={`mailto:${o.contactEmail}`}
                        className="inline-flex items-center gap-1 hover:underline"
                      >
                        {o.contactEmail}
                        <ExternalLink className="h-3 w-3" />
                      </a>
                    </TableCell>
                    <TableCell className="hidden lg:table-cell text-sm font-mono text-muted-foreground">
                      {o.taxId ?? "—"}
                    </TableCell>
                    <TableCell className="hidden lg:table-cell text-sm text-muted-foreground">
                      {formatDate(o.createdAt)}
                    </TableCell>
                    <TableCell className="hidden lg:table-cell text-sm text-muted-foreground">
                      {formatDate(o.verifiedAt)}
                    </TableCell>
                    <TableCell className="text-right">
                      <div className="flex items-center justify-end gap-2">
                        {o.status === "PendingVerification" && (
                          <Button
                            variant="outline"
                            size="sm"
                            onClick={() => void handleVerify(o)}
                            disabled={verifyInFlight === o.id}
                          >
                            {verifyInFlight === o.id ? (
                              <Loader2 className="h-3 w-3 animate-spin" />
                            ) : (
                              <Check className="h-3 w-3" />
                            )}
                            Verify
                          </Button>
                        )}
                        {o.status !== "Suspended" && (
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => setSuspendTarget(o)}
                          >
                            <Ban className="h-3 w-3" /> Suspend
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

      {suspendTarget && (
        <SuspendDialog
          org={suspendTarget}
          onClose={() => setSuspendTarget(null)}
          onSuccess={() => {
            setSuspendTarget(null);
            void load();
          }}
        />
      )}
    </div>
  );
};

function SuspendDialog({
  org,
  onClose,
  onSuccess,
}: {
  org: PartnerOrganizationDto;
  onClose: () => void;
  onSuccess: () => void;
}) {
  const [reason, setReason] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!reason.trim()) {
      setError("Please describe why this organization is being suspended.");
      return;
    }
    setSubmitting(true);
    setError(null);
    try {
      await partnerApi.suspendOrganization(org.id, { reason: reason.trim() });
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
          <DialogTitle>Suspend {org.name}</DialogTitle>
          <DialogDescription>
            All members will lose the ability to generate referral links, create reservations, and
            issue endorsements. Existing endorsements remain valid until they expire or are
            revoked individually.
          </DialogDescription>
        </DialogHeader>
        <form onSubmit={(e) => void handleSubmit(e)} className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="suspend-reason">Reason</Label>
            <Textarea
              id="suspend-reason"
              value={reason}
              onChange={(e) => setReason(e.target.value)}
              rows={3}
              placeholder="e.g. Repeated abuse of referral program."
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
              Suspend organization
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
