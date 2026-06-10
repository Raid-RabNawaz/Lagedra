import { useEffect } from "react";
import { useParams, Link, useNavigate, Navigate } from "react-router-dom";
import {
  ArrowLeft,
  FileCheck,
  Calendar,
  DollarSign,
  MapPin,
  Shield,
  AlertTriangle,
} from "lucide-react";
import { useMyDeals } from "@/features/deals/hooks/useDeals";
import { useSnapshotByDealId, useCreateFromDeal } from "@/features/truth-surface/hooks/useTruthSurface";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Alert } from "@/components/ui/alert";
import { Separator } from "@/components/ui/separator";
import { Loader } from "@/components/shared/Loader";
import { EmptyState } from "@/components/shared/EmptyState";
import { formatDate, formatMoney } from "@/utils/format";
import type { DealSummaryDto } from "@/api/types";

function TermRow({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div className="flex items-center justify-between py-2.5 border-b border-border/50 last:border-0">
      <span className="text-sm text-muted-foreground">{label}</span>
      <span className="text-sm font-medium text-right">{value}</span>
    </div>
  );
}

function DealTermsPreview({ deal }: { deal: DealSummaryDto }) {
  const totalDue =
    (deal.monthlyRentCents ?? 0) +
    (deal.depositAmountCents ?? 0) +
    (deal.totalAmountCents ? deal.totalAmountCents - (deal.monthlyRentCents ?? 0) - (deal.depositAmountCents ?? 0) : 0);

  return (
    <div className="space-y-4">
      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-base flex items-center gap-2">
            <MapPin className="h-4 w-4" />
            Listing
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="flex gap-3">
            {deal.listingCoverPhotoUri && (
              <div className="w-16 h-16 rounded-lg overflow-hidden bg-muted shrink-0">
                <img
                  src={deal.listingCoverPhotoUri}
                  alt={deal.listingTitle}
                  className="h-full w-full object-cover"
                />
              </div>
            )}
            <div>
              <p className="font-medium text-sm">{deal.listingTitle}</p>
              {deal.listingCity && (
                <p className="text-xs text-muted-foreground mt-0.5">{deal.listingCity}</p>
              )}
            </div>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-base flex items-center gap-2">
            <Calendar className="h-4 w-4" />
            Stay Details
          </CardTitle>
        </CardHeader>
        <CardContent>
          <TermRow label="Check-in" value={formatDate(deal.requestedCheckIn)} />
          <TermRow label="Check-out" value={formatDate(deal.requestedCheckOut)} />
          <TermRow label="Duration" value={`${deal.stayDurationDays} days`} />
        </CardContent>
      </Card>

      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-base flex items-center gap-2">
            <DollarSign className="h-4 w-4" />
            Financial Terms
          </CardTitle>
        </CardHeader>
        <CardContent>
          {deal.monthlyRentCents != null && (
            <TermRow label="Monthly Rent" value={formatMoney(deal.monthlyRentCents)} />
          )}
          {deal.depositAmountCents != null && deal.depositAmountCents > 0 && (
            <TermRow label="Security Deposit" value={formatMoney(deal.depositAmountCents)} />
          )}
          {deal.totalAmountCents != null && (
            <>
              <Separator className="my-1" />
              <TermRow
                label="Total Due at Checkout"
                value={
                  <span className="font-semibold">{formatMoney(totalDue > 0 ? totalDue : deal.totalAmountCents)}</span>
                }
              />
            </>
          )}
        </CardContent>
      </Card>
    </div>
  );
}

export function CreateTruthSurfacePage() {
  const { dealId } = useParams<{ dealId: string }>();
  const navigate = useNavigate();
  const { data: deals, isLoading: dealsLoading } = useMyDeals("all");
  const { data: existingSnapshot, isLoading: snapshotLoading } = useSnapshotByDealId(dealId);
  const createMutation = useCreateFromDeal();

  const deal = deals?.find((d) => d.dealId === dealId);
  const isLoading = dealsLoading || snapshotLoading;

  // Phase 16.4 — the host's manual create CTA is a legacy entry point.
  // Under V2 the snapshot is auto-created at approval time, so when this
  // page sees one already exists we silently redirect to its confirmation
  // page rather than showing a click-through. This avoids the broken UX
  // where the host clicks "Create" on a stale link and triggers a
  // duplicate-create error from the backend.
  useEffect(() => {
    if (existingSnapshot) {
      navigate(`/app/truth-surface/${existingSnapshot.snapshotId}`, {
        replace: true,
      });
    }
  }, [existingSnapshot, navigate]);

  const handleCreate = async () => {
    if (!dealId) return;
    // The endpoint is idempotent: if a snapshot was created concurrently
    // (e.g. AutoConfirmTruthSurfaceAsync just landed while this page
    // loaded), the API returns the existing one and we route to it.
    const result = await createMutation.mutateAsync(dealId);
    navigate(`/app/truth-surface/${result.snapshotId}`, { replace: true });
  };

  if (isLoading) {
    return <Loader fullPage label="Loading deal terms..." />;
  }

  // Render an immediate redirect if the effect hasn't fired yet on first
  // paint — e.g. when the snapshot was already in cache from a prior page.
  if (existingSnapshot) {
    return (
      <Navigate
        to={`/app/truth-surface/${existingSnapshot.snapshotId}`}
        replace
      />
    );
  }

  if (!deal) {
    return (
      <EmptyState
        title="Deal not found"
        description="This deal may no longer exist or you may not have access."
      >
        <Link to="/app/deals">
          <Button variant="outline" size="sm">
            <ArrowLeft className="mr-2 h-4 w-4" />
            Back to deals
          </Button>
        </Link>
      </EmptyState>
    );
  }

  return (
    <div className="mx-auto max-w-2xl px-4 py-8 sm:px-6 lg:px-8">
      <Link
        to={`/app/deals/${dealId}`}
        className="inline-flex items-center gap-1.5 text-sm text-muted-foreground hover:text-foreground transition-colors mb-6"
      >
        <ArrowLeft className="h-4 w-4" />
        Back to deal
      </Link>

      <div className="flex items-center gap-3 mb-2">
        <h1 className="text-2xl font-bold tracking-tight">Create Truth Surface</h1>
      </div>
      <p className="text-sm text-muted-foreground mb-6">
        Review the deal terms below. Once created, both you and the tenant must confirm
        the truth surface before payment can proceed.
      </p>

      <DealTermsPreview deal={deal} />

      <Card className="mt-4">
        <CardHeader className="pb-3">
          <CardTitle className="text-base flex items-center gap-2">
            <Shield className="h-4 w-4" />
            What Happens Next
          </CardTitle>
        </CardHeader>
        <CardContent className="text-sm text-muted-foreground space-y-2">
          <p>1. The truth surface will be created from these terms and submitted for confirmation.</p>
          <p>2. Both you (landlord) and the tenant will need to review and confirm each line item.</p>
          <p>3. Once both parties confirm, the snapshot is cryptographically sealed.</p>
          <p>4. The inquiry service will be automatically closed.</p>
          <p>5. The tenant can then proceed to checkout and make payment.</p>
        </CardContent>
      </Card>

      {createMutation.isError && (
        <Alert variant="destructive" className="mt-4">
          <AlertTriangle className="h-4 w-4" />
          <span className="ml-2 text-sm">
            {(createMutation.error as Error)?.message ?? "Failed to create truth surface. Please try again."}
          </span>
        </Alert>
      )}

      <div className="mt-6 flex justify-end">
        <Button
          onClick={handleCreate}
          disabled={createMutation.isPending}
          size="lg"
          className="gap-2"
        >
          <FileCheck className="h-4 w-4" />
          {createMutation.isPending ? "Creating..." : "Create Truth Surface"}
        </Button>
      </div>
    </div>
  );
}
