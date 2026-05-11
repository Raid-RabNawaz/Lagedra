import { useParams, Link } from "react-router-dom";
import {
  ArrowLeft,
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
} from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Loader } from "@/components/shared/Loader";
import { EmptyState } from "@/components/shared/EmptyState";
import { DealTimeline } from "@/features/deals/components/DealTimeline";
import { DealPhaseBadge } from "@/features/deals/components/DealPhaseBadge";
import { useMyDeals } from "@/features/deals/hooks/useDeals";
import { useAuthStore } from "@/app/auth/authStore";
import { formatDate, formatMoney } from "@/utils/format";
import type { DealSummaryDto, DealPhase } from "@/api/types";

type PerspectiveProps = {
  deal: DealSummaryDto;
  isLandlord: boolean;
  isTenant: boolean;
};

function nextStepMessage(
  phase: DealPhase,
  isLandlord: boolean,
  isTenant: boolean,
): string {
  switch (phase) {
    case "Inquiry":
      if (isLandlord) {
        return "A tenant is asking questions about the listing. Respond to their inquiry.";
      }
      if (isTenant) {
        return "Ask the landlord any questions you have before proceeding.";
      }
      return "The tenant and landlord are still discussing the listing.";
    case "TruthSurface":
      return "Both parties need to confirm the truth surface to proceed.";
    case "AwaitingPayment":
      if (isLandlord) return "Waiting for the tenant to complete payment.";
      if (isTenant) return "Complete your payment to activate this deal.";
      return "Waiting for the tenant to complete payment.";
    case "Checkout":
      if (isLandlord) return "The tenant is completing checkout.";
      if (isTenant) return "Finish your checkout to proceed.";
      return "The tenant is completing checkout.";
    case "Active":
      return "This deal is active. You can view billing details and invoices.";
    case "Closed":
      return "This deal has been completed.";
    case "Cancelled":
      return "This deal was cancelled.";
    default:
      return "";
  }
}

function StatusAndNextSteps({ deal, isLandlord, isTenant }: PerspectiveProps) {
  return (
    <Card>
      <CardHeader className="pb-3">
        <CardTitle className="text-base">Status & Next Steps</CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        <DealTimeline currentPhase={deal.dealPhase} />
        <p className="text-sm text-muted-foreground">
          {nextStepMessage(deal.dealPhase, isLandlord, isTenant)}
        </p>
        <PrimaryAction deal={deal} isLandlord={isLandlord} isTenant={isTenant} />
      </CardContent>
    </Card>
  );
}

function PrimaryAction({ deal, isLandlord, isTenant }: PerspectiveProps) {
  switch (deal.dealPhase) {
    case "Inquiry":
      if (isLandlord) {
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
        <div className="flex items-center gap-2 rounded-lg border border-amber-200 bg-amber-50 px-4 py-3">
          <Clock className="h-4 w-4 text-amber-600 shrink-0" />
          <p className="text-sm text-amber-800">
            {isTenant
              ? "The landlord will create the truth surface once the inquiry is complete."
              : "Waiting for the landlord to create the truth surface."}
          </p>
        </div>
      );

    case "TruthSurface":
      if (!isLandlord && !isTenant) {
        return null;
      }
      return (
        <Link to={`/app/deals/${deal.dealId}/truth-surface`}>
          <Button className="w-full gap-2">
            <FileCheck className="h-4 w-4" />
            Review & Confirm Truth Surface
          </Button>
        </Link>
      );

    case "AwaitingPayment":
    case "Checkout":
      if (isTenant) {
        return (
          <Link to={`/app/deals/${deal.dealId}/checkout`}>
            <Button className="w-full gap-2">
              <CreditCard className="h-4 w-4" />
              Go to Checkout
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
        <Link to="/app/deals">
          <Button variant="outline" size="sm">
            <ArrowLeft className="mr-2 h-4 w-4" />
            Back to deals
          </Button>
        </Link>
      </EmptyState>
    );
  }

  const showInquiry = ["Inquiry", "TruthSurface", "AwaitingPayment", "Checkout", "Active", "Closed"].includes(deal.dealPhase);
  const showPayment = ["AwaitingPayment", "Checkout", "Active", "Closed"].includes(deal.dealPhase);
  const showBilling = ["Active", "Closed"].includes(deal.dealPhase);
  const showCompliance = ["Active", "Closed"].includes(deal.dealPhase);

  return (
    <div className="mx-auto max-w-3xl space-y-6">
      <Link
        to="/app/deals"
        className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground transition-colors"
      >
        <ArrowLeft className="h-4 w-4" />
        {isLandlord ? "My Deals" : "My Reservations"}
      </Link>

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
            <DealPhaseBadge phase={deal.dealPhase} />
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
        </div>
      </div>

      <StatusAndNextSteps deal={deal} isLandlord={isLandlord} isTenant={isTenant} />

      {/* Section links */}
      <div className="grid gap-3">
        {showInquiry && (
          <SectionLink
            to={`/app/deals/${deal.dealId}/inquiry`}
            icon={<MessageSquare className="h-5 w-5" />}
            title="Inquiry"
            description="View questions and answers about this listing"
            badge={
              deal.dealPhase === "Inquiry" ? (
                <Badge variant="accent" className="text-[10px] px-1.5 py-0">
                  Current
                </Badge>
              ) : undefined
            }
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
              deal.dealPhase === "AwaitingPayment" || deal.dealPhase === "Checkout" ? (
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
                </div>
              )}
              <div>
                <p className="text-muted-foreground">Stay Duration</p>
                <p className="font-medium">{deal.stayDurationDays} days</p>
              </div>
              <div>
                <p className="text-muted-foreground">Total Due at Checkout</p>
                <p className="font-medium">{formatMoney(deal.totalAmountCents)}</p>
              </div>
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
