import { useParams, Link } from "react-router-dom";
import {
  ArrowRight,
  MessageSquare,
  FileCheck,
  CreditCard,
  Receipt,
  Calendar,
  MapPin,
  ExternalLink,
  Clock,
  Plus,
  ShieldCheck,
  BookOpen,
  Scale,
  Lock,
} from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { BackLink } from "@/components/shared/BackLink";
import { Loader } from "@/components/shared/Loader";
import { EmptyState } from "@/components/shared/EmptyState";
import { DealTimeline } from "@/features/deals/components/DealTimeline";
import { DealPhaseBadge } from "@/features/deals/components/DealPhaseBadge";
import { DepositReturnPanel } from "@/features/deals/components/DepositReturnPanel";
import { DealStayAccessCard } from "@/features/deals/components/DealStayAccessCard";
import { EndingSoonBadge } from "@/features/deals/components/BookingAttentionBanner";
import { LeaveStayReviewPanel } from "@/features/reviews/components/LeaveStayReviewPanel";
import { useMyDeals } from "@/features/deals/hooks/useDeals";
import { useSnapshotByDealId } from "@/features/truth-surface/hooks/useTruthSurface";
import { LeaseAgreementDownloadButton } from "@/features/truth-surface/components/LeaseAgreementDownloadButton";
import { useAuthStore } from "@/app/auth/authStore";
import { tierLabel } from "@/features/applications/lib/bookingConsent";
import { getEndingSoon } from "@/features/deals/utils/bookingAttention";
import { formatDate, formatMoney } from "@/utils/format";
import type { DealSummaryDto, TruthSurfaceDto } from "@/api/types";

type PerspectiveProps = {
  deal: DealSummaryDto;
  isLandlord: boolean;
  isTenant: boolean;
  /**
   * Phase 16.4 — Truth Surface for this deal, if one already exists.
   * Under V2 the snapshot is auto-created at host-approval time, so any
   * CTA that says "create" must defer to "review" once a snapshot
   * exists. `undefined` means "still loading"; `null` means "checked,
   * none yet".
   */
  truthSurface?: TruthSurfaceDto | null;
};

function nextStepMessage(
  deal: Pick<
    DealSummaryDto,
    | "dealPhase"
    | "hostConfirmedDepositReturnedAt"
    | "tenantConfirmedDepositReceivedAt"
    | "depositReturnSettledAt"
  >,
  isLandlord: boolean,
  isTenant: boolean,
): string {
  switch (deal.dealPhase) {
    case "TruthSurface":
      return "Both parties need to confirm the truth surface to proceed.";
    case "Checkout":
      if (isLandlord) return "The tenant is completing payment.";
      if (isTenant) return "Finish your payment to proceed.";
      return "The tenant is completing payment.";
    case "Active":
      return "This deal is active. You can view billing details and invoices.";
    case "AwaitingDepositReturn":
      if (deal.depositReturnSettledAt) {
        return "Deposit return is complete. This booking is finishing.";
      }
      if (isLandlord) {
        if (deal.hostConfirmedDepositReturnedAt) {
          return deal.tenantConfirmedDepositReceivedAt
            ? "The guest confirmed they received the deposit. This booking is finishing."
            : "You reported returning the deposit. Waiting for the guest to confirm receipt.";
        }
        return "The stay has ended. Return the deposit (or an itemized statement of deductions) within 21 days of move-out, then confirm it below. If you return less than the full amount, include a reason and a damage photo.";
      }
      if (isTenant) {
        if (deal.tenantConfirmedDepositReceivedAt) {
          return "You confirmed receipt of your deposit. This booking is finishing.";
        }
        if (deal.hostConfirmedDepositReturnedAt) {
          return "Your host reported returning the deposit. Confirm you received it below — or open a dispute if you didn't.";
        }
        return "The stay has ended. Confirm you received your deposit back to complete the deal — or open a dispute if you didn't. Hosts generally have 21 days to return the deposit.";
      }
      return "The stay has ended and the deposit return is being confirmed.";
    case "PaymentFailed":
      if (isTenant)
        return "Your agreement is sealed, but the deposit payment failed. Update your card to finish activating the booking.";
      if (isLandlord)
        return "The agreement is sealed, but the guest's deposit payment failed. They've been asked to update their card.";
      return "The deposit payment failed and is awaiting a retry.";
    case "Closed":
      return "This deal has been completed.";
    case "Cancelled":
      return "This deal was cancelled.";
    default:
      return "";
  }
}

function StatusAndNextSteps({
  deal,
  isLandlord,
  isTenant,
  truthSurface,
}: PerspectiveProps) {
  return (
    <Card>
      <CardHeader className="pb-3">
        <CardTitle className="text-base">Status & Next Steps</CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        <DealTimeline currentPhase={deal.dealPhase} />
        <p className="text-sm text-muted-foreground">
          {nextStepMessage(deal, isLandlord, isTenant)}
        </p>
        <PrimaryAction
          deal={deal}
          isLandlord={isLandlord}
          isTenant={isTenant}
          truthSurface={truthSurface}
        />
      </CardContent>
    </Card>
  );
}

function PrimaryAction({ deal, isLandlord, isTenant, truthSurface }: PerspectiveProps) {
  // Phase 16.4 made the Truth Surface auto-create on host approval, so
  // *any* legacy CTA that points at /create-truth-surface must defer to
  // the existing snapshot's confirmation page once one exists. Without
  // this guard, the host can land on the legacy create page on a deal
  // that already has a snapshot and trigger a duplicate-create error.
  if (truthSurface && deal.dealPhase === "TruthSurface") {
    return (
      <Link to={`/app/truth-surface/${truthSurface.snapshotId}`}>
        <Button className="w-full gap-2">
          <FileCheck className="h-4 w-4" />
          Review & Confirm Truth Surface
        </Button>
      </Link>
    );
  }

  switch (deal.dealPhase) {
    case "TruthSurface":
      if (!isLandlord && !isTenant) {
        return null;
      }
      // Phase 17 — pre-V2 deals can land here without an auto-created
      // snapshot. Send the host to the legacy create page; the create
      // page itself now redirects to the snapshot if one was created in
      // a parallel tab (idempotent under the V2 endpoint guard).
      if (isLandlord && !truthSurface) {
        return (
          <Link to={`/app/deals/${deal.dealId}/create-truth-surface`}>
            <Button className="w-full gap-2">
              <Plus className="h-4 w-4" />
              Create Truth Surface
            </Button>
          </Link>
        );
      }
      return (
        <Link to={`/app/deals/${deal.dealId}/truth-surface`}>
          <Button className="w-full gap-2">
            <FileCheck className="h-4 w-4" />
            Review & Confirm Truth Surface
          </Button>
        </Link>
      );

    case "Checkout":
      if (isTenant) {
        return (
          <Link to={`/app/deals/${deal.dealId}/checkout`}>
            <Button className="w-full gap-2">
              <CreditCard className="h-4 w-4" />
              Go to Payment
            </Button>
          </Link>
        );
      }
      return (
        <div className="flex items-center gap-2 rounded-lg border border-amber-200 bg-amber-50 px-4 py-3">
          <Clock className="h-4 w-4 text-amber-600 shrink-0" />
          <p className="text-sm text-amber-800">Waiting for the tenant to complete payment.</p>
        </div>
      );

    case "Active":
      return (
        <Link to={`/app/deals/${deal.dealId}/billing`}>
          <Button variant="outline" className="w-full gap-2">
            <Receipt className="h-4 w-4" />
            View Billing Details
            <ArrowRight className="h-4 w-4 ml-auto" />
          </Button>
        </Link>
      );

    case "PaymentFailed":
      if (isTenant) {
        return (
          <Link to={`/app/deals/${deal.dealId}/checkout`}>
            <Button variant="destructive" className="w-full gap-2">
              <CreditCard className="h-4 w-4" />
              Update card & retry payment
            </Button>
          </Link>
        );
      }
      return (
        <div className="flex items-center gap-2 rounded-lg border border-destructive/30 bg-destructive/5 px-4 py-3">
          <Clock className="h-4 w-4 text-destructive shrink-0" />
          <p className="text-sm text-destructive">
            Waiting for the guest to update their card and retry payment.
          </p>
        </div>
      );

    default:
      return null;
  }
}

type SectionLinkProps = {
  to: string;
  icon: React.ReactNode;
  title: string;
  description: string;
  badge?: React.ReactNode;
};

function SectionLink({ to, icon, title, description, badge }: SectionLinkProps) {
  return (
    <Link to={to}>
      <Card className="transition hover:shadow-md group cursor-pointer">
        <CardContent className="flex items-center gap-4 p-4">
          <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-muted text-muted-foreground group-hover:bg-primary/10 group-hover:text-primary transition-colors">
            {icon}
          </div>
          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-2">
              <span className="font-medium text-sm">{title}</span>
              {badge}
            </div>
            <p className="text-xs text-muted-foreground mt-0.5">{description}</p>
          </div>
          <ExternalLink className="h-4 w-4 text-muted-foreground shrink-0" />
        </CardContent>
      </Card>
    </Link>
  );
}

export function DealDetailPage() {
  const { dealId } = useParams<{ dealId: string }>();
  const { data: deals, isLoading, error } = useMyDeals("all");
  const user = useAuthStore((s) => s.user);
  // Phase 16.4 — fetch the auto-created Truth Surface (if any) so the
  // primary CTA can route the host to "Review & Confirm" instead of the
  // legacy create page. The hook 404s gracefully when no snapshot exists,
  // so `data` is null for pre-V2 deals and the legacy path still applies.
  const { data: truthSurface } = useSnapshotByDealId(dealId);

  const deal = deals?.find((d) => d.dealId === dealId);
  // Tenant/Landlord were merged into a single "Member" role; gate per-deal
  // controls by the actual participant identity. Platform admins or other
  // non-participants will see neither tenant nor landlord actions.
  const isLandlord = !!user && !!deal && user.userId === deal.landlordUserId;
  const isTenant = !!user && !!deal && user.userId === deal.tenantUserId;

  if (isLoading) {
    return <Loader label="Loading deal..." />;
  }

  if (error || !deal) {
    return (
      <EmptyState
        title="Deal not found"
        description="This deal may no longer exist or you may not have access."
      >
        <BackLink fallbackTo="/app/deals" variant="button" label="Back to deals" />
      </EmptyState>
    );
  }

  const showInquiry = ["TruthSurface", "Checkout", "Active", "PaymentFailed", "AwaitingDepositReturn", "Closed"].includes(deal.dealPhase);
  const showPayment = ["Checkout", "Active", "PaymentFailed", "AwaitingDepositReturn", "Closed"].includes(deal.dealPhase);
  const showBilling = ["Active", "AwaitingDepositReturn", "Closed"].includes(deal.dealPhase);
  const showCompliance = ["Active", "AwaitingDepositReturn", "Closed"].includes(deal.dealPhase);

  const truthSurfaceLocked = deal.truthSurfaceLocked ?? truthSurface?.isLocked ?? false;
  const ending = getEndingSoon(deal);

  return (
    <div className="mx-auto max-w-3xl space-y-6">
      <BackLink
        fallbackTo="/app/deals"
        label={isLandlord ? "My Deals" : "My Reservations"}
      />

      {/* Header */}
      <div className="flex gap-4">
        <div className="relative w-28 h-28 rounded-xl overflow-hidden bg-muted shrink-0">
          {deal.listingCoverPhotoUri ? (
            <img
              src={deal.listingCoverPhotoUri}
              alt={deal.listingTitle}
              className="h-full w-full object-cover"
            />
          ) : (
            <div className="flex h-full items-center justify-center text-muted-foreground text-xs">
              No photo
            </div>
          )}
        </div>
        <div className="flex-1 min-w-0">
          <div className="flex items-start justify-between gap-2">
            <h1 className="text-xl font-bold tracking-tight line-clamp-2">
              {deal.listingTitle}
            </h1>
            <div className="flex shrink-0 flex-col items-end gap-1">
              <DealPhaseBadge phase={deal.dealPhase} />
              {ending && <EndingSoonBadge ending={ending} />}
            </div>
          </div>
          {deal.listingCity && (
            <p className="text-sm text-muted-foreground flex items-center gap-1 mt-1">
              <MapPin className="h-3.5 w-3.5" />
              {deal.listingCity}
            </p>
          )}
          <div className="flex items-center gap-1 text-sm text-muted-foreground mt-1">
            <Calendar className="h-3.5 w-3.5" />
            {formatDate(deal.requestedCheckIn)} – {formatDate(deal.requestedCheckOut)}
          </div>
          <div className="flex gap-3 mt-2 text-sm">
            {deal.monthlyRentCents != null && (
              <span>
                <span className="font-medium">{formatMoney(deal.monthlyRentCents)}</span>
                <span className="text-muted-foreground">/mo</span>
              </span>
            )}
            {deal.depositAmountCents != null && deal.depositAmountCents > 0 && (
              <span className="text-muted-foreground">
                Deposit: {formatMoney(deal.depositAmountCents)}
              </span>
            )}
          </div>
          <div className="mt-2 flex flex-wrap items-center gap-1.5">
            {deal.tenantVerificationTier && (
              <Badge variant="secondary" className="gap-1 text-[10px]">
                <ShieldCheck className="h-3 w-3" />
                {tierLabel(deal.tenantVerificationTier)} tenant
              </Badge>
            )}
            {truthSurfaceLocked && (
              <Badge variant="outline" className="gap-1 text-[10px]">
                <Lock className="h-3 w-3" />
                Agreement sealed
              </Badge>
            )}
            {deal.paymentStatus && (
              <Badge
                variant={
                  deal.paymentStatus === "Confirmed"
                    ? "success"
                    : deal.paymentStatus === "Failed"
                      ? "destructive"
                      : "secondary"
                }
                className="text-[10px]"
              >
                Payment: {deal.paymentStatus}
              </Badge>
            )}
          </div>
        </div>
      </div>

      <StatusAndNextSteps
        deal={deal}
        isLandlord={isLandlord}
        isTenant={isTenant}
        truthSurface={truthSurface ?? null}
      />

      <DealStayAccessCard
        dealId={deal.dealId}
        dealPhase={deal.dealPhase}
        counterpartLabel={isTenant ? "Host" : "Guest"}
      />

      {(deal.dealPhase === "Active" ||
        deal.dealPhase === "AwaitingDepositReturn") && (
        <DepositReturnPanel
          deal={deal}
          isLandlord={isLandlord}
          isTenant={isTenant}
        />
      )}

      {deal.dealPhase === "Closed" && (
        <LeaveStayReviewPanel dealId={deal.dealId} />
      )}

      {/* Section links */}
      <div className="grid gap-3">
        {showInquiry && (
          <SectionLink
            to={`/app/deals/${deal.dealId}/inquiry`}
            icon={<MessageSquare className="h-5 w-5" />}
            title="Booking conversation"
            description="View the inquiry thread linked to this booking"
          />
        )}

        {showInquiry && (
          <SectionLink
            to={`/app/deals/${deal.dealId}/truth-surface`}
            icon={<FileCheck className="h-5 w-5" />}
            title="Truth Surface"
            description="Review and confirm the deal terms"
            badge={
              deal.dealPhase === "TruthSurface" ? (
                <Badge variant="accent" className="text-[10px] px-1.5 py-0">
                  Current
                </Badge>
              ) : undefined
            }
          />
        )}

        {truthSurface?.status === "Confirmed" && (isLandlord || isTenant) && (
          <div className="flex items-center justify-between gap-3 rounded-xl border p-4">
            <div className="min-w-0">
              <p className="text-sm font-medium">Lease agreement</p>
              <p className="text-xs text-muted-foreground">
                The signed lease generated from the sealed deal terms.
              </p>
            </div>
            <LeaseAgreementDownloadButton dealId={deal.dealId} size="sm" />
          </div>
        )}

        {showPayment && (
          <SectionLink
            to={`/app/deals/${deal.dealId}/checkout`}
            icon={<CreditCard className="h-5 w-5" />}
            title="Payment"
            description={
              deal.paymentStatus
                ? `Status: ${deal.paymentStatus}`
                : "Complete your payment"
            }
            badge={
              deal.dealPhase === "Checkout" ? (
                <Badge variant="accent" className="text-[10px] px-1.5 py-0">
                  Action needed
                </Badge>
              ) : undefined
            }
          />
        )}

        {showBilling && (
          <SectionLink
            to={`/app/deals/${deal.dealId}/billing`}
            icon={<Receipt className="h-5 w-5" />}
            title="Billing"
            description={
              deal.billingStatus
                ? `Billing: ${deal.billingStatus}`
                : "View billing details"
            }
          />
        )}

        {showCompliance && (
          <SectionLink
            to={`/app/deals/${deal.dealId}/compliance`}
            icon={<ShieldCheck className="h-5 w-5" />}
            title="Compliance"
            description="View compliance status and violations"
          />
        )}

        {showCompliance && (
          <SectionLink
            to={`/app/deals/${deal.dealId}/trust-ledger`}
            icon={<BookOpen className="h-5 w-5" />}
            title="Trust Ledger"
            description="Permanent record of trust-relevant events"
          />
        )}

        {showCompliance && (
          <SectionLink
            to={`/app/arbitration?dealId=${deal.dealId}`}
            icon={<Scale className="h-5 w-5" />}
            title="Arbitration"
            description="File a dispute or view existing cases"
          />
        )}
      </div>

      {/* Financials summary */}
      {deal.totalAmountCents != null && deal.totalAmountCents > 0 && (
        <Card>
          <CardHeader className="pb-3">
            <CardTitle className="text-base">Financial Summary</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="grid grid-cols-2 gap-4 text-sm">
              {deal.monthlyRentCents != null && (
                <div>
                  <p className="text-muted-foreground">Monthly Rent</p>
                  <p className="font-medium">{formatMoney(deal.monthlyRentCents)}</p>
                </div>
              )}
              {deal.depositAmountCents != null && deal.depositAmountCents > 0 && (
                <div>
                  <p className="text-muted-foreground">Security Deposit</p>
                  <p className="font-medium">{formatMoney(deal.depositAmountCents)}</p>
                  {deal.depositReason && (
                    <p className="text-xs text-muted-foreground mt-0.5">
                      {deal.depositReason}
                    </p>
                  )}
                </div>
              )}
              <div>
                <p className="text-muted-foreground">Stay Duration</p>
                <p className="font-medium">{deal.stayDurationDays} days</p>
              </div>
              <div>
                <p className="text-muted-foreground">Total Due Now</p>
                <p className="font-medium">{formatMoney(deal.totalAmountCents)}</p>
              </div>
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
