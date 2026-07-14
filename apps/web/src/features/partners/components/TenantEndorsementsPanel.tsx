import { useEffect, useState } from "react";
import { Plus, ShieldCheck, Loader2, Building2, Search } from "lucide-react";
import { partnerApi } from "@/features/partners/services/partnerApi";
import { extractErrorMessage } from "@/lib/errors";
import { EndorsementStatusBadge } from "@/features/partners/components/EndorsementStatusBadge";
import type { DiscoveredPartnerDto, PartnerEndorsementDto } from "@/api/types";
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
import { Loader } from "@/components/shared/Loader";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { FormError } from "@/components/shared/FormError";
import { PartnerServiceReviewPanel } from "@/features/reviews/components/PartnerServiceReviewPanel";

const formatDate = (iso: string | null) =>
  iso ? new Date(iso).toLocaleDateString(undefined, { dateStyle: "medium" }) : "—";

/**
 * Self-contained read-only panel a tenant can drop into their Profile or Verification page.
 * Lists their endorsements and exposes a "Request endorsement from a partner" CTA.
 */
export function TenantEndorsementsPanel() {
  const [items, setItems] = useState<PartnerEndorsementDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<unknown>(null);
  const [requestOpen, setRequestOpen] = useState(false);

  const load = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const data = await partnerApi.listMyEndorsements();
      setItems(data);
    } catch (err) {
      setError(err);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    void load();
  }, []);

  const activeCount = items.filter((i) => i.status === "Approved").length;

  return (
    <Card>
      <CardHeader className="pb-3">
        <div className="flex items-start justify-between gap-3">
          <div>
            <CardTitle className="flex items-center gap-2 text-lg">
              <ShieldCheck className="h-5 w-5 text-muted-foreground" />
              Institutional endorsements
            </CardTitle>
            <CardDescription>
              {activeCount > 0
                ? `${activeCount} active endorsement${activeCount === 1 ? "" : "s"} reduce${activeCount === 1 ? "s" : ""} your security deposit on new applications.`
                : "Verified Lagedra partners can vouch for you to lower your security deposit."}
            </CardDescription>
          </div>
          <Button variant="outline" size="sm" onClick={() => setRequestOpen(true)}>
            <Plus className="h-4 w-4" />
            Request
          </Button>
        </div>
      </CardHeader>
      <CardContent className="space-y-3">
        {isLoading ? (
          <Loader label="Loading endorsements..." />
        ) : error ? (
          <Alert variant="destructive">
            <AlertDescription>{extractErrorMessage(error)}</AlertDescription>
          </Alert>
        ) : items.length === 0 ? (
          <p className="rounded-md border border-dashed p-4 text-center text-sm text-muted-foreground">
            You don't have any endorsements yet. Click <strong>Request</strong> to ask a partner
            organization to endorse you.
          </p>
        ) : (
          <ul className="space-y-2">
            {items.map((e) => (
              <li
                key={e.id}
                className="flex items-start justify-between gap-3 rounded-md border p-3"
              >
                <div className="min-w-0 flex-1">
                  <p className="flex items-center gap-2 font-medium">
                    <Building2 className="h-4 w-4 text-muted-foreground" />
                    {e.organizationName}
                  </p>
                  <p className="mt-0.5 text-xs text-muted-foreground">
                    Requested {formatDate(e.requestedAt)}
                    {e.approvedAt && ` • approved ${formatDate(e.approvedAt)}`}
                    {e.expiresAt && ` • expires ${formatDate(e.expiresAt)}`}
                    {e.revokedAt && ` • revoked ${formatDate(e.revokedAt)}`}
                  </p>
                  {e.note && (
                    <p className="mt-1 text-xs italic text-muted-foreground line-clamp-2">
                      "{e.note}"
                    </p>
                  )}
                  {(e.status === "Approved" ||
                    e.status === "Revoked" ||
                    e.status === "Expired") && (
                    <PartnerServiceReviewPanel
                      organizationId={e.organizationId}
                      organizationName={e.organizationName}
                    />
                  )}
                </div>
                <EndorsementStatusBadge status={e.status} />
              </li>
            ))}
          </ul>
        )}
        <p className="text-[11px] text-muted-foreground">
          <strong>Note:</strong> Partner-Backed Protection is a verification status, not an
          insurance policy. Lagedra does not pay claims under this tier; eviction-related disputes
          follow the standard Lagedra arbitration process.
        </p>
      </CardContent>

      <RequestEndorsementDialog
        open={requestOpen}
        onOpenChange={setRequestOpen}
        onSuccess={() => void load()}
      />
    </Card>
  );
}

function RequestEndorsementDialog({
  open,
  onOpenChange,
  onSuccess,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onSuccess: () => void;
}) {
  const [searchTerm, setSearchTerm] = useState("");
  const [appliedSearch, setAppliedSearch] = useState("");
  const [orgs, setOrgs] = useState<DiscoveredPartnerDto[]>([]);
  const [searching, setSearching] = useState(false);
  const [searchError, setSearchError] = useState<string | null>(null);
  const [selected, setSelected] = useState<DiscoveredPartnerDto | null>(null);
  const [note, setNote] = useState("");
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const reset = () => {
    setSearchTerm("");
    setAppliedSearch("");
    setOrgs([]);
    setSearching(false);
    setSearchError(null);
    setSelected(null);
    setNote("");
    setSubmitError(null);
    setSubmitting(false);
  };

  const handleClose = (next: boolean) => {
    if (!next) reset();
    onOpenChange(next);
  };

  const handleSearch = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!searchTerm.trim()) return;
    setSearching(true);
    setSearchError(null);
    setAppliedSearch(searchTerm);
    try {
      const data = await partnerApi.discoverVerifiedPartners(searchTerm.trim(), 25);
      setOrgs(data);
    } catch (err) {
      setSearchError(extractErrorMessage(err));
      setOrgs([]);
    } finally {
      setSearching(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selected) return;
    setSubmitting(true);
    setSubmitError(null);
    try {
      await partnerApi.requestEndorsementAsTenant({
        organizationId: selected.id,
        note: note.trim() || null,
      });
      onSuccess();
      handleClose(false);
    } catch (err) {
      setSubmitError(extractErrorMessage(err));
      setSubmitting(false);
    }
  };

  return (
    <Dialog open={open} onOpenChange={handleClose}>
      <DialogContent className="max-w-xl">
        <DialogHeader>
          <DialogTitle>Request a partner endorsement</DialogTitle>
          <DialogDescription>
            Find a verified Lagedra partner that knows you (your employer, university, or
            relocation provider) and ask them to endorse you.
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSearch} className="flex items-center gap-2">
          <Input
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            placeholder="Search verified partners by name..."
          />
          <Button type="submit" variant="outline" disabled={searching || !searchTerm.trim()}>
            <Search className="h-4 w-4" />
            Search
          </Button>
        </form>

        {searching ? (
          <Loader label="Searching..." />
        ) : searchError ? (
          <Alert variant="destructive">
            <AlertDescription>{searchError}</AlertDescription>
          </Alert>
        ) : orgs.length === 0 ? (
          appliedSearch ? (
            <p className="rounded-md border border-dashed p-4 text-center text-sm text-muted-foreground">
              No verified partners match "{appliedSearch}".
            </p>
          ) : null
        ) : (
          <ul className="max-h-[200px] space-y-1 overflow-y-auto rounded-md border p-2">
            {orgs.map((o) => (
              <li key={o.id}>
                <button
                  type="button"
                  onClick={() => setSelected(o)}
                  className={`flex w-full items-center justify-between rounded-md p-2 text-left text-sm hover:bg-accent/10 ${
                    selected?.id === o.id ? "bg-accent/10 ring-1 ring-accent" : ""
                  }`}
                >
                  <span className="flex items-center gap-2">
                    <Building2 className="h-4 w-4 text-muted-foreground" />
                    <span className="font-medium">{o.name}</span>
                  </span>
                  <span className="text-xs text-muted-foreground">{o.organizationType}</span>
                </button>
              </li>
            ))}
          </ul>
        )}

        {selected && (
          <form onSubmit={(e) => void handleSubmit(e)} className="space-y-3 border-t pt-3">
            <div className="rounded-md bg-muted/50 p-3 text-sm">
              You're requesting an endorsement from <strong>{selected.name}</strong>.
            </div>
            <div className="space-y-2">
              <Label htmlFor="tenant-endorse-note">Message to the partner (optional)</Label>
              <Textarea
                id="tenant-endorse-note"
                value={note}
                onChange={(e) => setNote(e.target.value)}
                rows={3}
                placeholder="e.g. I'm an employee at your company since 2024."
              />
            </div>
            {submitError && <FormError message={submitError} />}
            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => setSelected(null)}>
                Pick another
              </Button>
              <Button type="submit" disabled={submitting}>
                {submitting && <Loader2 className="h-4 w-4 animate-spin" />}
                Send request
              </Button>
            </DialogFooter>
          </form>
        )}
      </DialogContent>
    </Dialog>
  );
}
