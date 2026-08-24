import { useEffect, useMemo, useState } from "react";
import { Plus, Link2, Copy, Check, Ban, Loader2 } from "lucide-react";
import { partnerApi } from "@/features/partners/services/partnerApi";
import { usePartnerMembership } from "@/features/partners/hooks/usePartnerMembership";
import { extractErrorMessage } from "@/lib/errors";
import type { ReferralLinkDto } from "@/api/types";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { DatePicker } from "@/components/ui/date-picker";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
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
import { Alert, AlertDescription } from "@/components/ui/alert";
import { FormError } from "@/components/shared/FormError";

const formatDate = (iso: string) =>
  new Date(iso).toLocaleDateString(undefined, { dateStyle: "medium" });

const buildRedemptionUrl = (code: string) => {
  const base = typeof window !== "undefined" ? window.location.origin : "";
  return `${base.replace(/\/$/, "")}/redeem/${code}`;
};

export const PartnerReferralsPage = () => {
  const { membership, isLoading: membershipLoading, error: membershipError, refresh } =
    usePartnerMembership();

  const [links, setLinks] = useState<ReferralLinkDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<unknown>(null);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [copiedCode, setCopiedCode] = useState<string | null>(null);

  const orgId = membership?.organization.id;
  const isAdmin = membership?.memberRole === "Admin";
  const isVerified = membership?.organization.status === "Verified";

  const loadLinks = async () => {
    if (!orgId) return;
    setIsLoading(true);
    setError(null);
    try {
      const data = await partnerApi.listReferralLinks(orgId);
      setLinks(data);
    } catch (err) {
      setError(err);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    if (orgId) void loadLinks();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [orgId]);

  const copyToClipboard = async (code: string) => {
    const url = buildRedemptionUrl(code);
    try {
      await navigator.clipboard.writeText(url);
      setCopiedCode(code);
      window.setTimeout(() => setCopiedCode((c) => (c === code ? null : c)), 2000);
    } catch {
      // best-effort; ignore clipboard failures (e.g. insecure context)
    }
  };

  const handleDeactivate = async (linkId: string) => {
    if (!orgId) return;
    if (!window.confirm("Deactivate this referral link? This cannot be undone.")) return;
    try {
      await partnerApi.deactivateReferralLink(orgId, linkId);
      await loadLinks();
    } catch (err) {
      window.alert(extractErrorMessage(err));
    }
  };

  if (membershipLoading) return <Loader label="Loading referral links..." />;
  if (membershipError) return <ErrorState error={membershipError} onRetry={() => void refresh()} />;
  if (!membership) return null;

  return (
    <div className="space-y-6">
      <div className="flex items-start justify-between gap-3">
        <div>
          <h1 className="flex items-center gap-2 text-3xl font-bold tracking-tight">
            <Link2 className="h-7 w-7 text-muted-foreground" />
            Referral links
          </h1>
          <p className="mt-1 text-muted-foreground">
            Share a referral link so members of your organization land in Lagedra with{" "}
            <strong>Partner-Backed Protection</strong> applied to their profile.
          </p>
        </div>
        {isAdmin && isVerified && (
          <Button onClick={() => setDialogOpen(true)}>
            <Plus className="h-4 w-4" />
            New link
          </Button>
        )}
      </div>

      {!isVerified && (
        <Alert>
          <AlertDescription>
            Referral links can only be created once your organization is verified. You can still
            view links that already exist.
          </AlertDescription>
        </Alert>
      )}

      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-lg">Active &amp; past links</CardTitle>
          <CardDescription>
            {links.length} link{links.length === 1 ? "" : "s"}
          </CardDescription>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <Loader label="Loading links..." />
          ) : error ? (
            <ErrorState error={error} onRetry={() => void loadLinks()} />
          ) : links.length === 0 ? (
            <EmptyState
              title="No referral links"
              description="Create a link to start onboarding members of your organization."
            >
              {isAdmin && isVerified && (
                <Button onClick={() => setDialogOpen(true)}>
                  <Plus className="h-4 w-4" />
                  New link
                </Button>
              )}
            </EmptyState>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Code</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Usage</TableHead>
                  <TableHead className="hidden md:table-cell">Expires</TableHead>
                  <TableHead className="hidden lg:table-cell">Created</TableHead>
                  <TableHead className="text-right">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {links.map((link) => {
                  const isExpired =
                    link.expiresAt != null && new Date(link.expiresAt).getTime() < Date.now();
                  const isExhausted = link.maxUses != null && link.usageCount >= link.maxUses;
                  return (
                    <TableRow key={link.id}>
                      <TableCell className="font-mono text-sm">{link.code}</TableCell>
                      <TableCell>
                        {!link.isActive ? (
                          <Badge variant="secondary">Deactivated</Badge>
                        ) : isExpired ? (
                          <Badge variant="destructive">Expired</Badge>
                        ) : isExhausted ? (
                          <Badge variant="destructive">Exhausted</Badge>
                        ) : (
                          <Badge variant="success">Active</Badge>
                        )}
                      </TableCell>
                      <TableCell className="text-sm">
                        {link.usageCount}
                        {link.maxUses != null ? ` / ${link.maxUses}` : ""}
                      </TableCell>
                      <TableCell className="hidden md:table-cell text-sm text-muted-foreground">
                        {link.expiresAt ? formatDate(link.expiresAt) : "Never"}
                      </TableCell>
                      <TableCell className="hidden lg:table-cell text-sm text-muted-foreground">
                        {formatDate(link.createdAt)}
                      </TableCell>
                      <TableCell className="text-right">
                        <div className="flex items-center justify-end gap-2">
                          <Button
                            variant="outline"
                            size="sm"
                            onClick={() => void copyToClipboard(link.code)}
                            title={buildRedemptionUrl(link.code)}
                          >
                            {copiedCode === link.code ? (
                              <>
                                <Check className="h-3 w-3" /> Copied
                              </>
                            ) : (
                              <>
                                <Copy className="h-3 w-3" /> Copy link
                              </>
                            )}
                          </Button>
                          {isAdmin && link.isActive && (
                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={() => void handleDeactivate(link.id)}
                            >
                              <Ban className="h-3 w-3" /> Deactivate
                            </Button>
                          )}
                        </div>
                      </TableCell>
                    </TableRow>
                  );
                })}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      {orgId && (
        <NewLinkDialog
          open={dialogOpen}
          onOpenChange={setDialogOpen}
          orgId={orgId}
          onSuccess={() => void loadLinks()}
        />
      )}
    </div>
  );
};

function NewLinkDialog({
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
  const [expiresAt, setExpiresAt] = useState("");
  const [maxUses, setMaxUses] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const minExpiry = useMemo(() => {
    const tomorrow = new Date();
    tomorrow.setDate(tomorrow.getDate() + 1);
    return tomorrow.toISOString().slice(0, 10);
  }, []);

  const reset = () => {
    setExpiresAt("");
    setMaxUses("");
    setError(null);
    setSubmitting(false);
  };

  const handleClose = (next: boolean) => {
    if (!next) reset();
    onOpenChange(next);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    let parsedMaxUses: number | null = null;
    if (maxUses.trim()) {
      const n = Number(maxUses);
      if (!Number.isInteger(n) || n <= 0) {
        setError("Max uses must be a positive whole number.");
        return;
      }
      parsedMaxUses = n;
    }

    let parsedExpiresAt: string | null = null;
    if (expiresAt.trim()) {
      const d = new Date(`${expiresAt}T23:59:59Z`);
      if (Number.isNaN(d.getTime())) {
        setError("Invalid expiry date.");
        return;
      }
      if (d.getTime() <= Date.now()) {
        setError("Expiry must be in the future.");
        return;
      }
      parsedExpiresAt = d.toISOString();
    }

    setSubmitting(true);
    try {
      await partnerApi.createReferralLink(orgId, {
        expiresAt: parsedExpiresAt,
        maxUses: parsedMaxUses,
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
          <DialogTitle>New referral link</DialogTitle>
          <DialogDescription>
            Both fields are optional. Leave them blank for a link that never expires and has no
            usage cap.
          </DialogDescription>
        </DialogHeader>
        <form onSubmit={(e) => void handleSubmit(e)} className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="link-expiry">Expires on (optional)</Label>
            <DatePicker
              id="link-expiry"
              min={minExpiry}
              value={expiresAt}
              onChange={setExpiresAt}
              placeholder="No expiry"
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="link-max-uses">Max uses (optional)</Label>
            <Input
              id="link-max-uses"
              type="number"
              min="1"
              value={maxUses}
              onChange={(e) => setMaxUses(e.target.value)}
              placeholder="e.g. 100"
            />
          </div>
          {error && <FormError message={error} />}
          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => handleClose(false)}>
              Cancel
            </Button>
            <Button type="submit" disabled={submitting}>
              {submitting && <Loader2 className="h-4 w-4 animate-spin" />}
              Create link
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
