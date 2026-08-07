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
        Fixed card aspect with an 80/20 image→info split so every card in a
        row shares the same silhouette. Info rows use fixed heights + nowrap
        so titles / meta / prices line up across cards even when a rating is
        missing or the stay range is longer.
      */}
      <div className="relative grid aspect-[4/5] grid-rows-[4fr_1fr] overflow-hidden rounded-2xl bg-card ring-1 ring-border/60 transition-all duration-200 hover:-translate-y-0.5 hover:shadow-[var(--shadow-soft)] hover:ring-border">
        {/*
          Image is absolute-filled inside a fixed cell so its intrinsic
          width/height never shifts layout. Overlays live in a separate
          z-10 layer pinned to the cell corners — not to the photo pixels —
          so wide/tall covers (and hover scale) can't cover the heart.
        */}
        <div className="relative min-h-0 overflow-hidden bg-muted">
          {listing.coverPhotoUrl ? (
            <img
              src={listing.coverPhotoUrl}
              alt={listing.title}
              className="absolute inset-0 h-full w-full object-cover transition-transform duration-300 group-hover:scale-105"
              loading="lazy"
            />
          ) : (
            <div className="absolute inset-0 flex items-center justify-center">
              <ImageOff className="h-10 w-10 text-muted-foreground/40" />
            </div>
          )}

          <div className="pointer-events-none absolute inset-0 z-10">
            <Badge
              className="pointer-events-none absolute left-3 top-3 rounded-full bg-background text-[10px] font-semibold text-foreground shadow-sm"
              variant="secondary"
            >
              {propertyTypeLabels[listing.propertyType] ?? listing.propertyType}
            </Badge>
            <div className="pointer-events-auto absolute right-3 top-3">
              <SaveButton
                listingId={listing.id}
                className="shadow-sm ring-1 ring-black/10"
              />
            </div>
          </div>
        </div>

        <div className="flex min-h-0 flex-col justify-between gap-1 px-3 py-2.5">
          <div className="flex h-5 items-center gap-2">
            <h3 className="min-w-0 flex-1 truncate text-sm font-semibold leading-none text-foreground transition-colors group-hover:text-primary">
              {listing.title}
            </h3>
            {hasRating && (
              <span className="inline-flex shrink-0 items-center gap-0.5 text-xs font-medium tabular-nums text-foreground">
                <Star className="h-3 w-3 fill-amber-500 text-amber-500" />
                {average.toFixed(1)}
                <span className="font-normal text-muted-foreground">
                  ({reviewCount})
                </span>
              </span>
            )}
          </div>

          <div className="flex h-4 items-center gap-2.5 overflow-hidden whitespace-nowrap text-xs text-muted-foreground">
            <span className="inline-flex items-center gap-1">
              <Bed className="h-3 w-3 shrink-0" />
              {bedsLabel}
            </span>
            <span className="inline-flex items-center gap-1">
              <Bath className="h-3 w-3 shrink-0" />
              {listing.bathrooms} bath
            </span>
            {stayRange && (
              <span className="inline-flex min-w-0 items-center gap-1 truncate">
                <Calendar className="h-3 w-3 shrink-0" />
                {stayRange}
              </span>
            )}
          </div>

          <div className="flex h-5 items-baseline gap-1">
            <span className="text-base font-bold leading-none tabular-nums">
              {formatMoney(listing.monthlyRentCents)}
            </span>
            <span className="text-xs text-muted-foreground">/ month</span>
          </div>
        </div>
      </div>
    </Link>
  );
}
