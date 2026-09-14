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
  // Compact stay label so the meta row never wraps and misaligns prices.
  const stayRange =
    listing.minStayDays && listing.maxStayDays
      ? `${listing.minStayDays}–${listing.maxStayDays}d`
      : listing.minStayDays
        ? `${listing.minStayDays}+d`
        : null;

  const bedsLabel =
    listing.bedrooms === 0
      ? "Studio"
      : `${listing.bedrooms} bed${listing.bedrooms === 1 ? "" : "s"}`;

  const reviewCount = listing.hostReviewCount ?? 0;
  const average = listing.hostAverageRating;
  const hasRating = reviewCount > 0 && average != null;

  return (
    <Link
      to={`/listings/${listing.id}`}
      className={cn("group block", className)}
    >
      {/*
        Photo keeps its own aspect; the footer sizes to its text so beds,
        baths, and stay are never clipped. A fixed 4/5 card with an 80/20
        split hid that row on 2-col mobile. Meta may wrap on narrow cards.
      */}
      <div className="relative overflow-hidden rounded-2xl bg-card ring-1 ring-border/60 transition-all duration-200 hover:-translate-y-0.5 hover:shadow-[var(--shadow-soft)] hover:ring-border">
        {/*
          Photo is clipped in its own stacking context so a wide/tall cover
          (or hover scale) cannot paint over the heart. Chrome lives on the
          card, not on the image pixels.
        */}
        <div className="relative isolate z-0 aspect-[4/3] overflow-hidden bg-muted">
          {listing.coverPhotoUrl ? (
            <img
              src={listing.coverPhotoUrl}
              alt={listing.title}
              className="absolute inset-0 z-0 h-full w-full object-cover object-center transition-transform duration-300 group-hover:scale-105"
              loading="lazy"
            />
          ) : (
            <div className="absolute inset-0 z-0 flex items-center justify-center">
              <ImageOff className="h-10 w-10 text-muted-foreground/40" />
            </div>
          )}
        </div>

        <Badge
          className="pointer-events-none absolute left-2.5 top-2.5 z-20 rounded-full bg-background text-[10px] font-semibold text-foreground shadow-sm sm:left-3 sm:top-3"
          variant="secondary"
        >
          {propertyTypeLabels[listing.propertyType] ?? listing.propertyType}
        </Badge>
        <div className="absolute right-2.5 top-2.5 z-20 sm:right-3 sm:top-3">
          <SaveButton
            listingId={listing.id}
            className="shadow-sm ring-1 ring-black/10"
          />
        </div>

        <div className="flex flex-col gap-1.5 px-2.5 py-3 sm:px-3">
          <div className="flex min-h-5 items-center gap-2">
            <h3 className="min-w-0 flex-1 truncate text-sm font-semibold leading-5 text-foreground transition-colors group-hover:text-primary">
              {listing.title}
            </h3>
            {hasRating && (
              <span className="inline-flex shrink-0 items-center gap-0.5 text-xs font-medium leading-5 tabular-nums text-foreground">
                <Star className="h-3 w-3 fill-amber-500 text-amber-500" />
                {average.toFixed(1)}
                <span className="font-normal text-muted-foreground">
                  ({reviewCount})
                </span>
              </span>
            )}
          </div>

          <div className="flex flex-wrap items-center gap-x-2 gap-y-0.5 text-xs leading-4 text-muted-foreground sm:gap-x-2.5">
            <span className="inline-flex items-center gap-1">
              <Bed className="h-3 w-3 shrink-0" />
              {bedsLabel}
            </span>
            <span className="inline-flex items-center gap-1">
              <Bath className="h-3 w-3 shrink-0" />
              {listing.bathrooms} bath
            </span>
            {stayRange && (
              <span className="inline-flex items-center gap-1">
                <Calendar className="h-3 w-3 shrink-0" />
                {stayRange}
              </span>
            )}
          </div>

          <div className="flex min-h-5 items-baseline gap-1">
            <span className="text-base font-bold leading-5 tabular-nums">
              {formatMoney(listing.monthlyRentCents)}
            </span>
            <span className="text-xs leading-4 text-muted-foreground">/ month</span>
          </div>
        </div>
      </div>
    </Link>
  );
}
