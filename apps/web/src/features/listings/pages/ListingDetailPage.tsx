import { useParams, Link } from "react-router-dom";
import {
  Bed,
  Bath,
  Ruler,
  Calendar,
  ShieldCheck,
  CheckCircle2,
  MailCheck,
  MapPin,
  Share2,
  Zap,
  ImageOff,
  ChevronLeft,
  ChevronRight,
  ExternalLink,
  Maximize2,
  Images,
  Languages,
  Briefcase,
  Clock,
  TrendingUp,
  ArrowUpRight,
  Star,
} from "lucide-react";
import { useState, useRef, useLayoutEffect, lazy, Suspense } from "react";
import { useListingDetail, useSimilarListings } from "@/features/listings/hooks/useListings";
import { usePublicProfile } from "@/features/auth/hooks/usePublicProfile";
import { useListingReviews, useUserReputation } from "@/features/reviews/hooks/useReviews";
import { StarRatingDisplay } from "@/features/reviews/components/StarRating";
import { ReputationPreview } from "@/features/reviews/components/ReputationPreview";
import { SaveButton } from "@/features/listings/components/SaveButton";
import { BookingPanel } from "@/features/listings/components/BookingPanel";
import { useAuthStore } from "@/app/auth/authStore";
import { AmenityGrid } from "@/features/listings/components/AmenityGrid";
import { SafetyDeviceList } from "@/features/listings/components/SafetyDeviceList";
import { ConsiderationList } from "@/features/listings/components/ConsiderationList";
import { HouseRulesSection } from "@/features/listings/components/HouseRulesSection";
import { CancellationPolicySummary } from "@/features/listings/components/CancellationPolicySummary";
import { ListingCard } from "@/features/listings/components/ListingCard";
import { PhotoLightbox } from "@/features/listings/components/PhotoLightbox";
import { PhotoGalleryModal } from "@/features/listings/components/PhotoGalleryModal";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { buttonVariants } from "@/components/ui/button-variants";
import { Separator } from "@/components/ui/separator";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { BackLink } from "@/components/shared/BackLink";
import { Loader } from "@/components/shared/Loader";
import { formatMoney, formatDate } from "@/utils/format";
import { cn } from "@/lib/utils";

const ListingApproxMap = lazy(() =>
  import("@/features/listings/components/ListingApproxMap").then((m) => ({
    default: m.ListingApproxMap,
  })),
);

const propertyTypeLabel: Record<string, string> = {
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

export const ListingDetailPage = () => {
  const { id } = useParams<{ id: string }>();
  const { data: listing, isLoading, isError } = useListingDetail(id);
  const { data: similar } = useSimilarListings(id);
  const user = useAuthStore((s) => s.user);
  // Pull richer host context (bio, location, languages, occupation,
  // email-verified flag) from the public-profile endpoint we ship for
  // application reviews. The detail view already exposes `hostProfile`
  // for trust badges, but it's deliberately sparse to stay light on
  // marketplace queries — the extra round-trip here is cheap (cached
  // 60s by usePublicProfile) and lets us render a real "About the
  // host" section instead of a name + verified row.
  const hostUserId = listing?.landlordUserId;
  const hostPublic = usePublicProfile(hostUserId);
  const { data: listingReviews } = useListingReviews(id);
  const { data: hostReputation } = useUserReputation(hostUserId);
  const [currentPhoto, setCurrentPhoto] = useState(0);
  const [copied, setCopied] = useState(false);
  const [lightboxOpen, setLightboxOpen] = useState(false);
  const [lightboxIndex, setLightboxIndex] = useState(0);
  const [galleryOpen, setGalleryOpen] = useState(false);

  const openLightbox = (idx: number) => {
    setLightboxIndex(idx);
    setLightboxOpen(true);
  };

  const openGallery = () => {
    setGalleryOpen(true);
  };

  if (isLoading) return <Loader fullPage label="Loading listing..." />;

  if (isError || !listing) {
    return (
      <div className="mx-auto max-w-7xl px-4 py-16 sm:px-6 lg:px-8 text-center">
        <p className="text-destructive font-medium">Listing not found or failed to load.</p>
        <div className="mt-4 flex justify-center">
          <BackLink fallbackTo="/listings" variant="button" label="Back to listings" />
        </div>
      </div>
    );
  }

  const photos = listing.photos.filter((p) => p.url).sort((a, b) => a.sortOrder - b.sortOrder);
  const stayRange =
    listing.minStayDays && listing.maxStayDays
      ? `${listing.minStayDays}–${listing.maxStayDays} days`
      : listing.minStayDays
        ? `${listing.minStayDays}+ days`
        : null;

  // Prefer the listing's own host snapshot, then the richer public profile
  // (display name, else first + last). Both read the same account record, so the
  // fallbacks just guard against one query loading before the other; "Host" is
  // only the last resort for a genuinely nameless account.
  const hostPublicName =
    hostPublic.data?.displayName ??
    [hostPublic.data?.firstName, hostPublic.data?.lastName].filter(Boolean).join(" ").trim();
  const hostName =
    listing.hostProfile?.displayName ||
    (hostPublicName && hostPublicName.length > 0 ? hostPublicName : null) ||
    "Host";
  const hostInitials = hostName
    .split(" ")
    .slice(0, 2)
    .map((w) => w[0]?.toUpperCase())
    .join("");

  const address = listing.preciseAddress;
  const hasExactStreet = Boolean(address?.street?.trim());
  const locationText = address
    ? [address.city, address.state, address.country].filter(Boolean).join(", ")
    : null;

  return (
    <div className="mx-auto max-w-7xl px-4 py-6 sm:px-6 lg:px-8">
      <BackLink fallbackTo="/listings" className="mb-4" />

      {/* Photo gallery */}
      <div className="mb-8 space-y-2">
        <div className="relative overflow-hidden rounded-2xl bg-muted aspect-[16/7] group">
          {photos.length > 0 ? (
            <>
              <button
                type="button"
                onClick={() => openLightbox(currentPhoto)}
                aria-label="Open full-screen gallery"
                className="block h-full w-full cursor-zoom-in"
              >
                <img
                  src={photos[currentPhoto]?.url ?? ""}
                  alt={photos[currentPhoto]?.caption ?? listing.title}
                  className="h-full w-full object-cover transition-transform group-hover:scale-[1.01]"
                />
              </button>
              {photos.length > 1 && (
                <>
                  <button
                    onClick={() =>
                      setCurrentPhoto((i) => (i === 0 ? photos.length - 1 : i - 1))
                    }
                    aria-label="Previous photo"
                    className="absolute left-3 top-1/2 -translate-y-1/2 flex h-10 w-10 items-center justify-center rounded-full bg-background/80 backdrop-blur hover:bg-background transition-colors cursor-pointer"
                  >
                    <ChevronLeft className="h-5 w-5" />
                  </button>
                  <button
                    onClick={() =>
                      setCurrentPhoto((i) => (i === photos.length - 1 ? 0 : i + 1))
                    }
                    aria-label="Next photo"
                    className="absolute right-3 top-1/2 -translate-y-1/2 flex h-10 w-10 items-center justify-center rounded-full bg-background/80 backdrop-blur hover:bg-background transition-colors cursor-pointer"
                  >
                    <ChevronRight className="h-5 w-5" />
                  </button>
                </>
              )}
              <div className="absolute right-3 bottom-3 flex items-center gap-2">
                <Button
                  variant="secondary"
                  size="sm"
                  onClick={() => openLightbox(currentPhoto)}
                  className="bg-background/85 backdrop-blur hover:bg-background"
                >
                  <Maximize2 className="h-4 w-4" />
                  Full screen
                </Button>
                {photos.length > 1 && (
                  <Button
                    variant="secondary"
                    size="sm"
                    onClick={openGallery}
                    className="bg-background/85 backdrop-blur hover:bg-background"
                  >
                    <Images className="h-4 w-4" />
                    All photos ({photos.length})
                  </Button>
                )}
              </div>
            </>
          ) : (
            <div className="flex h-full w-full items-center justify-center">
              <ImageOff className="h-16 w-16 text-muted-foreground/30" />
            </div>
          )}
        </div>

        {photos.length > 1 && (
          <div className="flex gap-2 overflow-x-auto pb-1">
            {photos.map((p, i) => (
              <button
                key={p.id}
                type="button"
                onClick={() => setCurrentPhoto(i)}
                aria-label={`Show photo ${i + 1}`}
                className={cn(
                  "relative h-16 w-24 shrink-0 overflow-hidden rounded-md border-2 transition-all cursor-pointer",
                  i === currentPhoto
                    ? "border-foreground opacity-100"
                    : "border-transparent opacity-70 hover:opacity-100",
                )}
              >
                {p.url && (
                  <img
                    src={p.url}
                    alt={p.caption ?? ""}
                    className="h-full w-full object-cover"
                    loading="lazy"
                  />
                )}
              </button>
            ))}
          </div>
        )}
      </div>

      <PhotoGalleryModal
        open={galleryOpen}
        photos={photos}
        onClose={() => setGalleryOpen(false)}
        onSelectPhoto={openLightbox}
      />

      <PhotoLightbox
        open={lightboxOpen}
        photos={photos}
        initialIndex={lightboxIndex}
        onClose={() => setLightboxOpen(false)}
      />

      <div className="grid gap-8 lg:grid-cols-3">
        {/* Main content */}
        <div className="lg:col-span-2 space-y-8">
          {/* Title & meta */}
          <div>
            <div className="flex flex-wrap items-center gap-2 mb-2">
              <Badge variant="secondary">
                {propertyTypeLabel[listing.propertyType] ?? listing.propertyType}
              </Badge>
              {listing.instantBookingEnabled && (
                <Badge variant="accent" className="gap-1">
                  <Zap className="h-3 w-3" /> Instant book
                </Badge>
              )}
            </div>

            <h1 className="text-2xl font-bold tracking-tight sm:text-3xl">
              {listing.title}
            </h1>

            {listingReviews && listingReviews.length > 0 && (
              <div className="mt-2">
                <StarRatingDisplay
                  average={
                    listingReviews.reduce((s, r) => s + r.overallRating, 0) /
                    listingReviews.length
                  }
                  count={listingReviews.length}
                />
              </div>
            )}

            {locationText && (
              <p className="mt-1 flex items-center gap-1 text-muted-foreground">
                <MapPin className="h-4 w-4" />
                {locationText}
              </p>
            )}

            <div className="mt-4 flex flex-wrap items-center gap-4 text-sm text-muted-foreground">
              <span className="flex items-center gap-1.5">
                <Bed className="h-4 w-4" />
                {listing.bedrooms === 0 ? "Studio" : `${listing.bedrooms} bedroom${listing.bedrooms > 1 ? "s" : ""}`}
              </span>
              <span className="flex items-center gap-1.5">
                <Bath className="h-4 w-4" />
                {listing.bathrooms} bathroom{listing.bathrooms > 1 ? "s" : ""}
              </span>
              {listing.squareFootage && (
                <span className="flex items-center gap-1.5">
                  <Ruler className="h-4 w-4" />
                  {listing.squareFootage.toLocaleString()} sq ft
                </span>
              )}
              {stayRange && (
                <span className="flex items-center gap-1.5">
                  <Calendar className="h-4 w-4" />
                  {stayRange}
                </span>
              )}
            </div>
          </div>

          <Separator />

          {/* Description */}
          <section>
            <h2 className="text-lg font-semibold mb-3">About this place</h2>
            <ExpandableDescription text={listing.description} />
          </section>

          {listing.virtualTourUrl && (
            <>
              <Separator />
              <section>
                <h2 className="text-lg font-semibold mb-3">Virtual tour</h2>
                <a
                  href={listing.virtualTourUrl}
                  target="_blank"
                  rel="noopener noreferrer"
                  className={cn(buttonVariants({ variant: "outline" }), "gap-2")}
                >
                  <ExternalLink className="h-4 w-4" />
                  Take a virtual tour
                </a>
              </section>
            </>
          )}

          {/* Amenities */}
          {listing.amenities.length > 0 && (
            <>
              <Separator />
              <section>
                <h2 className="text-lg font-semibold mb-4">What this place offers</h2>
                <AmenityGrid amenities={listing.amenities} />
              </section>
            </>
          )}

          {/* Safety */}
          {listing.safetyDevices.length > 0 && (
            <>
              <Separator />
              <section>
                <h2 className="text-lg font-semibold mb-3">Safety devices</h2>
                <SafetyDeviceList devices={listing.safetyDevices} />
              </section>
            </>
          )}

          {/* Considerations */}
          {listing.considerations.length > 0 && (
            <>
              <Separator />
              <section>
                <h2 className="text-lg font-semibold mb-3">Things to know</h2>
                <ConsiderationList considerations={listing.considerations} />
              </section>
            </>
          )}

          {/* House rules */}
          {listing.houseRules && (
            <>
              <Separator />
              <section>
                <h2 className="text-lg font-semibold mb-3">House rules</h2>
                <HouseRulesSection rules={listing.houseRules} />
              </section>
            </>
          )}

          {/* Cancellation policy */}
          {listing.cancellationPolicy && (
            <>
              <Separator />
              <section>
                <h2 className="text-lg font-semibold mb-3">Cancellation policy</h2>
                <CancellationPolicySummary policy={listing.cancellationPolicy} />
              </section>
            </>
          )}

          {/* Where you'll be */}
          {listing.latitude != null && listing.longitude != null && (
            <>
              <Separator />
              <section>
                <h2 className="text-lg font-semibold mb-3 flex items-center gap-2">
                  <MapPin className="h-5 w-5" />
                  Where you&apos;ll be
                </h2>
                <div className="overflow-hidden rounded-xl border aspect-[16/9]">
                  <Suspense
                    fallback={
                      <div className="flex h-full w-full items-center justify-center bg-muted">
                        <Loader />
                      </div>
                    }
                  >
                    <ListingApproxMap
                      latitude={listing.latitude}
                      longitude={listing.longitude}
                      privacyRadiusMeters={hasExactStreet ? undefined : 350}
                      showMarker={hasExactStreet}
                    />
                  </Suspense>
                </div>
                <p className="mt-2 text-xs text-muted-foreground">
                  {hasExactStreet
                    ? "Exact street address is shown for this listing."
                    : "Approximate area only — the full street address and host contact unlock after your booking is confirmed."}
                </p>
              </section>
            </>
          )}
        </div>

        {/* Sidebar */}
        <div className="lg:col-span-1 space-y-6">
          {/* Price card.
              `sticky` creates a new stacking context but defaults to
              `z-auto`, which puts the booking card *behind* the host
              card sibling below as soon as they overlap (most visible
              when the host card scrolls upward into the sticky card or
              when the date-picker popover opens). Pinning the card to
              `z-20` keeps the entire reservation widget — including
              its calendar popover — above the host card. The popover
              itself uses `z-30` so it still beats anything else inside
              this stacking context. */}
          <Card className="sticky top-24 z-20">
            <CardContent className="p-6">
              <div className="flex items-baseline gap-1 mb-4">
                <span className="text-3xl font-bold">{formatMoney(listing.monthlyRentCents)}</span>
                <span className="text-muted-foreground">/ month</span>
              </div>

              {(listing.suggestedDepositLowCents || listing.suggestedDepositHighCents) && (
                <p className="text-sm text-muted-foreground mb-3">
                  Deposit: {formatMoney(listing.suggestedDepositLowCents ?? listing.maxDepositCents)}
                  {listing.suggestedDepositHighCents && listing.suggestedDepositLowCents !== listing.suggestedDepositHighCents
                    ? ` – ${formatMoney(listing.suggestedDepositHighCents)}`
                    : ""}
                </p>
              )}

              <div className="mb-3">
                {!user ? (
                  <Link
                    to={`/auth/login?redirect=/listings/${listing.id}`}
                    className={cn(buttonVariants({ variant: "accent", size: "lg" }), "w-full")}
                  >
                    Sign in to book
                  </Link>
                ) : user.userId === listing.landlordUserId ? (
                  <Link
                    to={`/app/listings/${listing.id}`}
                    className={cn(buttonVariants({ variant: "accent", size: "lg" }), "w-full")}
                  >
                    Manage your listing
                  </Link>
                ) : (
                  <BookingPanel
                    listing={listing}
                    isProspectiveGuest={user.userId !== listing.landlordUserId}
                  />
                )}
              </div>
              <div className="flex gap-2">
                <Button
                  variant="outline"
                  className="flex-1 gap-2"
                  onClick={async () => {
                    const url = window.location.href;
                    if (navigator.share) {
                      try {
                        await navigator.share({ title: listing.title, url });
                      } catch { /* user cancelled */ }
                    } else {
                      await navigator.clipboard.writeText(url);
                      setCopied(true);
                      setTimeout(() => setCopied(false), 2000);
                    }
                  }}
                >
                  <Share2 className="h-4 w-4" />
                  {copied ? "Copied!" : "Share"}
                </Button>
                <SaveButton listingId={listing.id} className="shrink-0" />
              </div>

              <Separator className="my-4" />

              <p className="text-xs text-center text-muted-foreground">
                Listed on {formatDate(listing.createdAt)}
              </p>
            </CardContent>
          </Card>

          {/* Host card */}
          {listing.hostProfile && (
            <Card>
              <CardHeader className="pb-3">
                <CardTitle className="text-base">Hosted by</CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                {/* Identity row — avatar + name + member-since + view-profile link */}
                <div className="flex items-start gap-3">
                  <Avatar className="h-14 w-14">
                    {listing.hostProfile.profilePhotoUrl ? (
                      <AvatarImage src={listing.hostProfile.profilePhotoUrl} alt={hostName} />
                    ) : null}
                    <AvatarFallback className="text-base">
                      {hostInitials}
                    </AvatarFallback>
                  </Avatar>
                  <div className="min-w-0 flex-1">
                    <p className="font-semibold truncate">{hostName}</p>
                    <p className="text-xs text-muted-foreground">
                      Host · Member since{" "}
                      {new Date(listing.hostProfile.memberSince).getFullYear()}
                    </p>
                    {hostReputation && hostReputation.reviewCount > 0 && (
                      <div className="mt-1">
                        <StarRatingDisplay
                          average={hostReputation.averageOverall}
                          count={hostReputation.reviewCount}
                          className="text-xs"
                        />
                      </div>
                    )}
                    {/* Optional one-liner: occupation pulled from the
                        public profile, only rendered when populated so
                        we never show an empty subtitle row. */}
                    {hostPublic.data?.occupation ? (
                      <p className="mt-0.5 flex items-center gap-1 text-xs text-muted-foreground truncate">
                        <Briefcase className="h-3 w-3 shrink-0" />
                        {hostPublic.data.occupation}
                      </p>
                    ) : null}
                    {(hostPublic.data?.city || hostPublic.data?.country) && (
                      <p className="mt-0.5 flex items-center gap-1 text-xs text-muted-foreground truncate">
                        <MapPin className="h-3 w-3 shrink-0" />
                        {[
                          hostPublic.data?.city,
                          hostPublic.data?.state,
                          hostPublic.data?.country,
                        ]
                          .filter(Boolean)
                          .join(", ")}
                      </p>
                    )}
                  </div>
                </div>

                {/* Bio — collapsed to ~4 lines; viewing full text is
                    one click away on the public profile page. */}
                {hostPublic.data?.bio ? (
                  <p className="text-sm text-muted-foreground leading-relaxed line-clamp-4 whitespace-pre-line">
                    {hostPublic.data.bio}
                  </p>
                ) : null}

                {/* Verification chips */}
                <div className="space-y-1.5">
                  {listing.hostProfile.isGovernmentIdVerified && (
                    <div className="flex items-center gap-2 text-sm">
                      <CheckCircle2 className="h-4 w-4 text-success shrink-0" />
                      Identity verified
                    </div>
                  )}
                  {listing.hostProfile.isPhoneVerified && (
                    <div className="flex items-center gap-2 text-sm">
                      <CheckCircle2 className="h-4 w-4 text-success shrink-0" />
                      Phone verified
                    </div>
                  )}
                  {hostPublic.data?.isEmailVerified && (
                    <div className="flex items-center gap-2 text-sm">
                      <MailCheck className="h-4 w-4 text-success shrink-0" />
                      Email verified
                    </div>
                  )}
                  {listing.hostVerificationBadges?.isInsuranceActive && (
                    <div className="flex items-center gap-2 text-sm">
                      <ShieldCheck className="h-4 w-4 text-success shrink-0" />
                      Insurance active
                    </div>
                  )}
                  {hostPublic.data?.languages ? (
                    <div className="flex items-center gap-2 text-sm">
                      <Languages className="h-4 w-4 text-muted-foreground shrink-0" />
                      Speaks {hostPublic.data.languages}
                    </div>
                  ) : null}
                </div>

                {/* Response stats — bumped to a 2-col tile only when at
                    least one stat is available; we render a single tile
                    when only response rate is present. */}
                {listing.hostProfile.responseRatePercent != null && (
                  <div
                    className={cn(
                      "grid gap-2 rounded-lg bg-secondary p-3 text-center",
                      listing.hostProfile.responseTimeMinutes != null
                        ? "grid-cols-2"
                        : "grid-cols-1",
                    )}
                  >
                    <div>
                      <p className="text-lg font-semibold flex items-center justify-center gap-1">
                        <TrendingUp className="h-4 w-4 text-muted-foreground" />
                        {listing.hostProfile.responseRatePercent}%
                      </p>
                      <p className="text-[10px] text-muted-foreground uppercase tracking-wide">
                        Response rate
                      </p>
                    </div>
                    {listing.hostProfile.responseTimeMinutes != null && (
                      <div>
                        <p className="text-lg font-semibold flex items-center justify-center gap-1">
                          <Clock className="h-4 w-4 text-muted-foreground" />
                          {listing.hostProfile.responseTimeMinutes < 60
                            ? `${listing.hostProfile.responseTimeMinutes}m`
                            : `${Math.round(listing.hostProfile.responseTimeMinutes / 60)}h`}
                        </p>
                        <p className="text-[10px] text-muted-foreground uppercase tracking-wide">
                          Response time
                        </p>
                      </div>
                    )}
                  </div>
                )}

                {listingReviews && listingReviews.length > 0 && (
                  <div className="space-y-2 rounded-lg border p-3">
                    <StarRatingDisplay
                      average={
                        listingReviews.reduce((s, r) => s + r.overallRating, 0) /
                        listingReviews.length
                      }
                      count={listingReviews.length}
                    />
                    {listingReviews.slice(0, 2).map((r) => (
                      <div key={r.id} className="border-t pt-2 text-sm">
                        <span className="inline-flex items-center gap-1 font-medium">
                          <Star className="h-3 w-3 fill-amber-500 text-amber-500" />
                          {r.overallRating}
                        </span>
                        <p className="mt-1 text-muted-foreground line-clamp-2">
                          {r.publicComment}
                        </p>
                      </div>
                    ))}
                    <p className="text-[11px] text-muted-foreground">
                      See all guest reviews below
                    </p>
                  </div>
                )}

                {hostUserId ? (
                  <Link
                    to={`/app/users/${hostUserId}`}
                    className={cn(
                      buttonVariants({ variant: "outline", size: "sm" }),
                      "w-full gap-1.5",
                    )}
                  >
                    View full profile
                    <ArrowUpRight className="h-3.5 w-3.5" />
                  </Link>
                ) : null}
              </CardContent>
            </Card>
          )}
        </div>
      </div>

      {listingReviews && listingReviews.length > 0 && (
        <>
          <Separator className="my-10" />
          <section>
            <div className="mb-6 flex flex-wrap items-end justify-between gap-3">
              <div>
                <h2 className="text-xl font-semibold">Guest reviews</h2>
                <p className="mt-1 text-sm text-muted-foreground">
                  What guests said about stays at this listing
                </p>
              </div>
              <StarRatingDisplay
                average={
                  listingReviews.reduce((s, r) => s + r.overallRating, 0) /
                  listingReviews.length
                }
                count={listingReviews.length}
              />
            </div>
            <ReputationPreview
              reviews={listingReviews}
              maxReviews={12}
              showCategories
            />
          </section>
        </>
      )}

      {/* Similar listings */}
      {similar && similar.length > 0 && (
        <>
          <Separator className="my-10" />
          <section>
            <h2 className="text-xl font-semibold mb-6">Similar listings</h2>
            <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-4">
              {similar.slice(0, 4).map((item) => (
                <ListingCard key={item.id} listing={item} />
              ))}
            </div>
          </section>
        </>
      )}
    </div>
  );
};

/** Collapses long listing descriptions to 10 lines with a Show more control. */
function ExpandableDescription({ text }: { text: string }) {
  const [expanded, setExpanded] = useState(false);
  const [canExpand, setCanExpand] = useState(false);
  const ref = useRef<HTMLParagraphElement>(null);

  useLayoutEffect(() => {
    const el = ref.current;
    if (!el || expanded) return;
    setCanExpand(el.scrollHeight > el.clientHeight + 1);
  }, [text, expanded]);

  return (
    <div>
      <p
        ref={ref}
        className={cn(
          "text-sm leading-relaxed text-muted-foreground whitespace-pre-line",
          !expanded && "line-clamp-10",
        )}
      >
        {text}
      </p>
      {canExpand && (
        <button
          type="button"
          onClick={() => setExpanded((v) => !v)}
          className="mt-2 text-sm font-semibold text-foreground underline-offset-2 hover:underline cursor-pointer"
        >
          {expanded ? "Show less" : "Show more"}
        </button>
      )}
    </div>
  );
}
