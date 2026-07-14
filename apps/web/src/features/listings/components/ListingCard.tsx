import { Link } from "react-router-dom";
import { Bed, Bath, Calendar, ImageOff, Star } from "lucide-react";
import type { ListingSummaryDto } from "@/api/types";
import { Badge } from "@/components/ui/badge";
import { SaveButton } from "@/features/listings/components/SaveButton";
import { formatMoney } from "@/utils/format";
import { cn } from "@/lib/utils";

const propertyTypeLabels: Record<string, string> = {
  Apartment: "Apartment",
  House: "House",
  Condo: "Condo",
  Townhouse: "Townhouse",
  Studio: "Studio",
  Loft: "Loft",
  Villa: "Villa",
  Cottage: "Cottage",
  Cabin: "Cabin",
  Other: "Other",
};

type ListingCardProps = {
  listing: ListingSummaryDto;
  className?: string;
};

export function ListingCard({ listing, className }: ListingCardProps) {
  const stayRange =
    listing.minStayDays && listing.maxStayDays
      ? `${listing.minStayDays}–${listing.maxStayDays} days`
      : listing.minStayDays
        ? `${listing.minStayDays}+ days`
        : null;

  const reviewCount = listing.hostReviewCount ?? 0;
  const average = listing.hostAverageRating;

  return (
    <Link
      to={`/listings/${listing.id}`}
      className={cn("group block", className)}
    >
      <div className="overflow-hidden rounded-2xl bg-card ring-1 ring-border/60 transition-all duration-200 hover:-translate-y-0.5 hover:shadow-[var(--shadow-soft)] hover:ring-border">
        <div className="relative aspect-[4/3] overflow-hidden bg-muted">
          {listing.coverPhotoUrl ? (
            <img
              src={listing.coverPhotoUrl}
              alt={listing.title}
              className="h-full w-full object-cover transition-transform duration-300 group-hover:scale-105"
              loading="lazy"
            />
          ) : (
            <div className="flex h-full w-full items-center justify-center">
              <ImageOff className="h-10 w-10 text-muted-foreground/40" />
            </div>
          )}
          <Badge
            className="absolute left-3 top-3 rounded-full bg-background text-[10px] font-semibold text-foreground shadow-sm"
            variant="secondary"
          >
            {propertyTypeLabels[listing.propertyType] ?? listing.propertyType}
          </Badge>
          <div className="absolute right-3 top-3 flex items-center gap-1.5">
            <SaveButton
              listingId={listing.id}
              className="opacity-0 group-hover:opacity-100 transition-opacity sm:opacity-0 max-sm:opacity-100"
            />
          </div>
        </div>

        <div className="p-4">
          <div className="flex items-start justify-between gap-2">
            <h3 className="font-semibold leading-tight line-clamp-1 text-foreground transition-colors group-hover:text-primary">
              {listing.title}
            </h3>
            {reviewCount > 0 && average != null && (
              <span className="inline-flex shrink-0 items-center gap-0.5 text-xs font-medium tabular-nums text-foreground">
                <Star className="h-3.5 w-3.5 fill-amber-500 text-amber-500" />
                {average.toFixed(1)}
                <span className="font-normal text-muted-foreground">
                  ({reviewCount})
                </span>
              </span>
            )}
          </div>

          <div className="mt-2 flex items-center gap-3 text-sm text-muted-foreground">
            <span className="flex items-center gap-1">
              <Bed className="h-3.5 w-3.5" />
              {listing.bedrooms === 0
                ? "Studio"
                : `${listing.bedrooms} bed${listing.bedrooms > 1 ? "s" : ""}`}
            </span>
            <span className="flex items-center gap-1">
              <Bath className="h-3.5 w-3.5" />
              {listing.bathrooms} bath
            </span>
            {stayRange && (
              <span className="flex items-center gap-1">
                <Calendar className="h-3.5 w-3.5" />
                {stayRange}
              </span>
            )}
          </div>

          <div className="mt-3 flex items-baseline gap-1">
            <span className="text-lg font-bold">
              {formatMoney(listing.monthlyRentCents)}
            </span>
            <span className="text-sm text-muted-foreground">/ month</span>
          </div>
        </div>
      </div>
    </Link>
  );
}
