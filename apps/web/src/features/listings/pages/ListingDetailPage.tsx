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
import { ListingLeasePreviewCard } from "@/features/listings/components/ListingLeasePreviewCard";
import { listingHasLeaseAgreement } from "@/features/listings/lib/leaseAgreement";
import { ListingCard } from "@/features/listings/components/ListingCard";
import { PhotoLightbox } from "@/features/listings/components/PhotoLightbox";
import { PhotoGalleryModal } from "@/features/listings/components/PhotoGalleryModal";
import { Card, CardContent } from "@/components/ui/card";
import type {
  ListingDetailsDto,
  PublicUserProfileDto,
  StayReviewDto,
  UserReputationDto,
} from "@/api/types";
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
              {listing.stayProtectionEnabled !== false && (
                <Badge variant="outline" className="gap-1">
                  <ShieldCheck className="h-3 w-3" /> Stay protection
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

            {listing.hostProfile && (
              hostUserId ? (
                <Link
                  to={`/app/users/${hostUserId}`}
                  className="mt-3 inline-flex items-center gap-2 text-sm text-muted-foreground transition-colors hover:text-foreground"
                >
                  <Avatar className="h-7 w-7">
                    {listing.hostProfile.profilePhotoUrl ? (
                      <AvatarImage src={listing.hostProfile.profilePhotoUrl} alt={hostName} />
                    ) : null}
                    <AvatarFallback className="text-[10px]">{hostInitials}</AvatarFallback>
                  </Avatar>
                  Hosted by <span className="font-medium text-foreground">{hostName}</span>
                </Link>
              ) : (
                <p className="mt-3 inline-flex items-center gap-2 text-sm text-muted-foreground">
                  <Avatar className="h-7 w-7">
                    {listing.hostProfile.profilePhotoUrl ? (
                      <AvatarImage src={listing.hostProfile.profilePhotoUrl} alt={hostName} />
                    ) : null}
                    <AvatarFallback className="text-[10px]">{hostInitials}</AvatarFallback>
                  </Avatar>
                  Hosted by <span className="font-medium text-foreground">{hostName}</span>
                </p>
              )
            )}
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

          {listingHasLeaseAgreement(listing.leaseAgreementSource) && (
            <>
              <Separator />
              <section>
                <h2 className="text-lg font-semibold mb-3">Lease agreement</h2>
                <ListingLeasePreviewCard listing={listing} isSignedIn={Boolean(user)} />
              </section>
            </>
          )}

          {listing.hostProfile && (
            <>
              <Separator />
              <HostAboutSection
                listing={listing}
                hostName={hostName}
                hostInitials={hostInitials}
                hostUserId={hostUserId}
                hostPublic={hostPublic.data}
                hostReputation={hostReputation}
                listingReviews={listingReviews}
              />
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
        <div className="lg:col-span-1">
          <Card className="sticky top-24">
            <CardContent className="p-5">
              <div className="flex items-baseline gap-1 mb-3">
                <span className="text-2xl font-bold">{formatMoney(listing.monthlyRentCents)}</span>
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

function HostAboutSection({
  listing,
  hostName,
  hostInitials,
  hostUserId,
  hostPublic,
  hostReputation,
  listingReviews,
}: {
  listing: ListingDetailsDto;
  hostName: string;
  hostInitials: string;
  hostUserId?: string;
  hostPublic?: PublicUserProfileDto;
  hostReputation?: UserReputationDto;
  listingReviews?: StayReviewDto[];
}) {
  const host = listing.hostProfile;
  if (!host) return null;

  const location = [hostPublic?.city, hostPublic?.state, hostPublic?.country]
    .filter(Boolean)
    .join(", ");
  const badges = [
    host.isGovernmentIdVerified && {
      icon: CheckCircle2,
      label: host.hostKind === "PropertyManagementCompany" || hostPublic?.hostKind === "PropertyManagementCompany"
        ? "Business verified"
        : "Identity verified",
    },
    host.isPhoneVerified && { icon: CheckCircle2, label: "Phone verified" },
    hostPublic?.isEmailVerified && { icon: MailCheck, label: "Email verified" },
    listing.hostVerificationBadges?.isInsuranceActive && {
      icon: ShieldCheck,
      label: "Insurance active",
    },
    hostPublic?.languages && { icon: Languages, label: `Speaks ${hostPublic.languages}` },
  ].filter(Boolean) as { icon: typeof CheckCircle2; label: string }[];

  const reviewSnippets = listingReviews?.slice(0, 2) ?? [];
  const hasDetails = Boolean(
    hostPublic?.bio ||
      badges.length > 0 ||
      host.responseRatePercent != null ||
      host.responseTimeMinutes != null,
  );

  return (
    <section>
      <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
        <h2 className="text-lg font-semibold">Meet the host</h2>
        {hostUserId ? (
          <Link
            to={`/app/users/${hostUserId}`}
            className={cn(buttonVariants({ variant: "outline", size: "sm" }), "gap-1.5")}
          >
            View full profile
            <ArrowUpRight className="h-3.5 w-3.5" />
          </Link>
        ) : null}
      </div>

      <div className="rounded-2xl border bg-card p-5">
        <div
          className={cn(
            "flex flex-col gap-5",
            hasDetails && "sm:flex-row sm:items-start",
            !hasDetails && "sm:flex-row sm:items-center",
          )}
        >
          <div
            className={cn(
              "flex items-center gap-4",
              hasDetails && "sm:w-52 sm:shrink-0 sm:flex-col sm:text-center",
            )}
          >
            <Avatar className="h-16 w-16 sm:h-20 sm:w-20">
              {host.profilePhotoUrl ? (
                <AvatarImage src={host.profilePhotoUrl} alt={hostName} />
              ) : null}
              <AvatarFallback className="text-lg">{hostInitials}</AvatarFallback>
            </Avatar>
            <div className="min-w-0">
              <p className="truncate font-semibold">{hostName}</p>
              <p className="text-xs text-muted-foreground">
                Host · Member since {new Date(host.memberSince).getFullYear()}
              </p>
              {hostReputation && hostReputation.reviewCount > 0 && (
                <div className={cn("mt-1.5", hasDetails && "sm:flex sm:justify-center")}>
                  <StarRatingDisplay
                    average={hostReputation.averageOverall}
                    count={hostReputation.reviewCount}
                  />
                </div>
              )}
              {hostPublic?.occupation ? (
                <p className="mt-1.5 flex items-center gap-1 text-xs text-muted-foreground">
                  <Briefcase className="h-3 w-3 shrink-0" />
                  <span className="truncate">{hostPublic.occupation}</span>
                </p>
              ) : null}
              {location ? (
                <p className="mt-1 flex items-center gap-1 text-xs text-muted-foreground">
                  <MapPin className="h-3 w-3 shrink-0" />
                  <span className="truncate">{location}</span>
                </p>
              ) : null}
            </div>
          </div>

          {hasDetails && (
            <div className="min-w-0 flex-1 space-y-4 sm:border-l sm:pl-5">
              {hostPublic?.bio ? (
                <p className="text-sm leading-relaxed text-muted-foreground line-clamp-5 whitespace-pre-line">
                  {hostPublic.bio}
                </p>
              ) : null}

              {badges.length > 0 && (
                <ul className="flex flex-wrap gap-2">
                  {badges.map(({ icon: Icon, label }) => (
                    <li
                      key={label}
                      className="inline-flex items-center gap-1.5 rounded-full border bg-background px-2.5 py-1 text-xs"
                    >
                      <Icon className="h-3.5 w-3.5 text-success shrink-0" />
                      {label}
                    </li>
                  ))}
                </ul>
              )}

              {(host.responseRatePercent != null || host.responseTimeMinutes != null) && (
                <div
                  className={cn(
                    "grid gap-3 rounded-xl bg-secondary/70 p-3 text-center",
                    host.responseRatePercent != null && host.responseTimeMinutes != null
                      ? "grid-cols-2"
                      : "grid-cols-1 sm:max-w-xs",
                  )}
                >
                  {host.responseRatePercent != null && (
                    <div>
                      <p className="flex items-center justify-center gap-1 text-lg font-semibold">
                        <TrendingUp className="h-4 w-4 text-muted-foreground" />
                        {host.responseRatePercent}%
                      </p>
                      <p className="text-[10px] uppercase tracking-wide text-muted-foreground">
                        Response rate
                      </p>
                    </div>
                  )}
                  {host.responseTimeMinutes != null && (
                    <div>
                      <p className="flex items-center justify-center gap-1 text-lg font-semibold">
                        <Clock className="h-4 w-4 text-muted-foreground" />
                        {host.responseTimeMinutes < 60
                          ? `${host.responseTimeMinutes}m`
                          : `${Math.round(host.responseTimeMinutes / 60)}h`}
                      </p>
                      <p className="text-[10px] uppercase tracking-wide text-muted-foreground">
                        Response time
                      </p>
                    </div>
                  )}
                </div>
              )}
            </div>
          )}
        </div>

        {reviewSnippets.length > 0 && (
          <div className="mt-5 grid gap-3 border-t pt-5 sm:grid-cols-2">
            {reviewSnippets.map((r) => (
              <div key={r.id} className="rounded-xl bg-secondary/50 p-3 text-sm">
                <span className="inline-flex items-center gap-1 font-medium">
                  <Star className="h-3 w-3 fill-amber-500 text-amber-500" />
                  {r.overallRating}
                </span>
                <p className="mt-1.5 line-clamp-3 text-muted-foreground">{r.publicComment}</p>
              </div>
            ))}
          </div>
        )}
      </div>
    </section>
  );
}

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
