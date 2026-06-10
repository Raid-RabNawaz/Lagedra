import { Link } from "react-router-dom";
import { Calendar, MapPin, ArrowRight } from "lucide-react";
import { Card, CardContent } from "@/components/ui/card";
import { DealPhaseBadge } from "./DealPhaseBadge";
import { formatDate, formatMoney } from "@/utils/format";
import { useAuthStore } from "@/app/auth/authStore";
import type { DealSummaryDto, DealPhase } from "@/api/types";

function actionLabel(phase: DealPhase, isLandlord: boolean): string {
  switch (phase) {
    case "TruthSurface":
      return "Confirm truth surface";
    case "Checkout":
      return isLandlord ? "View checkout" : "Complete checkout";
    case "Active":
      return "View billing";
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

  return (
    <Link to={`/app/deals/${deal.dealId}`} className="block group">
      <Card className="overflow-hidden transition hover:shadow-md">
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
                <DealPhaseBadge phase={deal.dealPhase} />
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
            </div>

            <div className="flex items-end justify-between mt-3 pt-2 border-t">
              <div className="text-sm">
                {deal.monthlyRentCents != null && (
                  <span className="font-medium">{formatMoney(deal.monthlyRentCents)}/mo</span>
                )}
              </div>
              <span className="text-sm font-medium text-primary flex items-center gap-1">
                {actionLabel(deal.dealPhase, isLandlord)}
                <ArrowRight className="h-3.5 w-3.5" />
              </span>
            </div>
          </CardContent>
        </div>
      </Card>
    </Link>
  );
}
