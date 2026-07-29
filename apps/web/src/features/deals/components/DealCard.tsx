import { Link } from "react-router-dom";
import { Calendar, MapPin, ArrowRight } from "lucide-react";
import { Card, CardContent } from "@/components/ui/card";
import { DealPhaseBadge } from "./DealPhaseBadge";
import { EndingSoonBadge } from "./BookingAttentionBanner";
import { formatDate, formatMoney } from "@/utils/format";
import { useAuthStore } from "@/app/auth/authStore";
import {
  getDealIssue,
  getEndingSoon,
  isCriticalDealIssue,
} from "@/features/deals/utils/bookingAttention";
import type { DealSummaryDto } from "@/api/types";
import { cn } from "@/lib/utils";

function actionLabel(
  deal: Pick<
    DealSummaryDto,
    | "dealPhase"
    | "hostConfirmedDepositReturnedAt"
    | "tenantConfirmedDepositReceivedAt"
    | "depositReturnSettledAt"
  >,
  isLandlord: boolean,
): string {
  switch (deal.dealPhase) {
    case "TruthSurface":
      return "Confirm truth surface";
    case "Checkout":
      return isLandlord ? "View payment" : "Complete payment";
    case "Active":
      return "View billing";
    case "AwaitingDepositReturn":
      if (deal.depositReturnSettledAt) return "View details";
      if (isLandlord) {
        if (deal.hostConfirmedDepositReturnedAt) {
          return deal.tenantConfirmedDepositReceivedAt
            ? "View details"
            : "Waiting on guest";
        }
        return "Return deposit";
      }
      if (deal.tenantConfirmedDepositReceivedAt) return "View details";
      return deal.hostConfirmedDepositReturnedAt
        ? "Confirm deposit"
        : "Awaiting host";
    case "PaymentFailed":
      return isLandlord ? "Resolve payment" : "Update payment";
    case "Closed":
    case "Cancelled":
      return "View details";
    default:
      return "View";
  }
}

export function DealCard({ deal }: { deal: DealSummaryDto }) {
  const user = useAuthStore((s) => s.user);
  const isLandlord = user?.userId === deal.landlordUserId;
  const perspective = isLandlord ? "host" : "guest";
  const issue = getDealIssue(deal, perspective);
  const ending = getEndingSoon(deal);
  const critical = issue ? isCriticalDealIssue(issue.kind) : false;

  return (
    <Link to={`/app/deals/${deal.dealId}`} className="block group">
      <Card
        className={cn(
          "overflow-hidden transition hover:shadow-md",
          critical && "border-destructive/50 ring-1 ring-destructive/20",
          !critical && ending && "border-amber-300 ring-1 ring-amber-200",
          !critical &&
            issue?.kind === "AwaitingDepositReturn" &&
            "border-amber-300 ring-1 ring-amber-200",
        )}
      >
        <div className="flex">
          <div className="relative w-36 min-h-[140px] shrink-0 bg-muted">
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

          <CardContent className="flex flex-1 flex-col justify-between p-4">
            <div>
              <div className="flex items-start justify-between gap-2 mb-1">
                <h3 className="font-semibold text-base leading-snug line-clamp-1 group-hover:underline">
                  {deal.listingTitle}
                </h3>
                <div className="flex shrink-0 flex-col items-end gap-1">
                  <DealPhaseBadge phase={deal.dealPhase} />
                  {ending && <EndingSoonBadge ending={ending} />}
                </div>
              </div>

              {deal.listingCity && (
                <p className="text-sm text-muted-foreground flex items-center gap-1 mb-2">
                  <MapPin className="h-3.5 w-3.5" />
                  {deal.listingCity}
                </p>
              )}

              <div className="flex items-center gap-1 text-sm text-muted-foreground">
                <Calendar className="h-3.5 w-3.5" />
                <span>{formatDate(deal.requestedCheckIn)}</span>
                <span>–</span>
                <span>{formatDate(deal.requestedCheckOut)}</span>
              </div>

              {issue && (
                <p
                  className={cn(
                    "mt-2 text-xs leading-relaxed",
                    critical ? "text-destructive" : "text-amber-900",
                  )}
                >
                  {issue.problem}
                </p>
              )}
            </div>

            <div className="flex items-end justify-between mt-3 pt-2 border-t">
              <div className="text-sm">
                {deal.monthlyRentCents != null && (
                  <span className="font-medium">
                    {formatMoney(deal.monthlyRentCents)}/mo
                  </span>
                )}
              </div>
              <span
                className={cn(
                  "text-sm font-medium flex items-center gap-1",
                  critical ? "text-destructive" : "text-primary",
                )}
              >
                {actionLabel(deal, isLandlord)}
                <ArrowRight className="h-3.5 w-3.5" />
              </span>
            </div>
          </CardContent>
        </div>

        {issue && (
          <div
            className={cn(
              "border-t px-4 py-2.5 text-xs",
              critical
                ? "border-destructive/20 bg-destructive/5 text-destructive"
                : "border-amber-200 bg-amber-50 text-amber-950",
            )}
          >
            <span className="font-medium">How to resolve: </span>
            {issue.resolution}
          </div>
        )}
      </Card>
    </Link>
  );
}
