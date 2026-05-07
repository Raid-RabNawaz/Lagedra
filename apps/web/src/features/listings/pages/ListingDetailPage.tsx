import { useParams, Link } from "react-router-dom";
import {
  ArrowLeft,
  Bed,
  Bath,
  Ruler,
  Calendar,
  Shield,
  ShieldCheck,
  CheckCircle2,
  MapPin,
  Share2,
  Zap,
  ImageOff,
  ChevronLeft,
  ChevronRight,
  ExternalLink,
  Maximize2,
  Images,
} from "lucide-react";
import { useState, lazy, Suspense } from "react";
import { useListingDetail, useSimilarListings } from "@/features/listings/hooks/useListings";
import { SaveButton } from "@/features/listings/components/SaveButton";
import { ApplyDialog } from "@/features/applications/components/ApplyDialog";
import { useAuthStore } from "@/app/auth/authStore";
import { AmenityGrid } from "@/features/listings/components/AmenityGrid";
import { SafetyDeviceList } from "@/features/listings/components/SafetyDeviceList";
import { ConsiderationList } from "@/features/listings/components/ConsiderationList";
import { HouseRulesSection } from "@/features/listings/components/HouseRulesSection";
import { CancellationPolicySummary } from "@/features/listings/components/CancellationPolicySummary";
import { ListingCard } from "@/features/listings/components/ListingCard";
import { PhotoLightbox } from "@/features/listings/components/PhotoLightbox";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button, buttonVariants } from "@/components/ui/button";
import { Separator } from "@/components/ui/separator";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
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
  const [currentPhoto, setCurrentPhoto] = useState(0);
  const [copied, setCopied] = useState(false);
  const [lightboxOpen, setLightboxOpen] = useState(false);
  const [lightboxIndex, setLightboxIndex] = useState(0);

  const openLightbox = (idx: number) => {
    setLightboxIndex(idx);
    setLightboxOpen(true);
  };

  if (isLoading) return <Loader fullPage label="Loading listing..." />;

  if (isError || !listing) {
    return (
      <div className="mx-auto max-w-7xl px-4 py-16 sm:px-6 lg:px-8 text-center">
        <p className="text-destructive font-medium">Listing not found or failed to load.</p>
        <Link to="/listings" className={cn(buttonVariants({ variant: "outline" }), "mt-4")}>
          <ArrowLeft className="h-4 w-4" />
          Back to listings
        </Link>
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

  const hostName = listing.hostProfile?.displayName ?? "Host";
  const hostInitials = hostName
    .split(" ")
    .slice(0, 2)
    .map((w) => w[0]?.toUpperCase())
    .join("");

  const address = listing.preciseAddress;
  const locationText = address
    ? [address.city, address.state, address.country].filter(Boolean).join(", ")
    : null;

  return (
    <div className="mx-auto max-w-7xl px-4 py-6 sm:px-6 lg:px-8">
      {/* Back link */}
      <Link
        to="/listings"
        className="inline-flex items-center gap-1.5 text-sm text-muted-foreground hover:text-foreground transition-colors mb-4"
      >
        <ArrowLeft className="h-4 w-4" />
        Back to listings
      </Link>

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
                    onClick={() => openLightbox(0)}
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
              {listing.insuranceRequired && (
                <Badge variant="default" className="gap-1">
                  <Shield className="h-3 w-3" /> Insurance required
                </Badge>
              )}
            </div>

            <h1 className="text-2xl font-bold tracking-tight sm:text-3xl">
              {listing.title}
            </h1>

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
            <p className="text-sm leading-relaxed text-muted-foreground whitespace-pre-line">
              {listing.description}
            </p>
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
                      privacyRadiusMeters={address ? undefined : 350}
                      showMarker={Boolean(address)}
                    />
                  </Suspense>
                </div>
                <p className="mt-2 text-xs text-muted-foreground">
                  {address
                    ? "Exact location is shown after your booking is confirmed."
                    : "Approximate location is shown until the host shares the precise address."}
                </p>
              </section>
            </>
          )}
        </div>

        {/* Sidebar */}
        <div className="lg:col-span-1 space-y-6">
          {/* Price card */}
          <Card className="sticky top-24">
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
                  <ApplyDialog listing={listing} />
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
              <CardContent>
                <div className="flex items-center gap-3 mb-3">
                  <Avatar className="h-12 w-12">
                    {listing.hostProfile.profilePhotoUrl ? (
                      <AvatarImage src={listing.hostProfile.profilePhotoUrl} alt={hostName} />
                    ) : null}
                    <AvatarFallback>{hostInitials}</AvatarFallback>
                  </Avatar>
                  <div>
                    <p className="font-medium">{hostName}</p>
                    <p className="text-xs text-muted-foreground">
                      Member since {new Date(listing.hostProfile.memberSince).getFullYear()}
                    </p>
                  </div>
                </div>

                <div className="space-y-2">
                  {listing.hostProfile.isGovernmentIdVerified && (
                    <div className="flex items-center gap-2 text-sm">
                      <CheckCircle2 className="h-4 w-4 text-success" />
                      Identity verified
                    </div>
                  )}
                  {listing.hostProfile.isPhoneVerified && (
                    <div className="flex items-center gap-2 text-sm">
                      <CheckCircle2 className="h-4 w-4 text-success" />
                      Phone verified
                    </div>
                  )}
                  {listing.hostVerificationBadges?.isInsuranceActive && (
                    <div className="flex items-center gap-2 text-sm">
                      <ShieldCheck className="h-4 w-4 text-success" />
                      Insurance active
                    </div>
                  )}
                </div>

                {listing.hostProfile.responseRatePercent != null && (
                  <div className="mt-3 grid grid-cols-2 gap-2 rounded-lg bg-secondary p-3 text-center">
                    <div>
                      <p className="text-lg font-semibold">{listing.hostProfile.responseRatePercent}%</p>
                      <p className="text-[10px] text-muted-foreground">Response rate</p>
                    </div>
                    {listing.hostProfile.responseTimeMinutes != null && (
                      <div>
                        <p className="text-lg font-semibold">
                          {listing.hostProfile.responseTimeMinutes < 60
                            ? `${listing.hostProfile.responseTimeMinutes}m`
                            : `${Math.round(listing.hostProfile.responseTimeMinutes / 60)}h`}
                        </p>
                        <p className="text-[10px] text-muted-foreground">Response time</p>
                      </div>
                    )}
                  </div>
                )}
              </CardContent>
            </Card>
          )}
        </div>
      </div>

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
