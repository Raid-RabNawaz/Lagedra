import { lazy, Suspense, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  ArrowLeft,
  Pencil,
  ExternalLink,
  Rocket,
  Ban,
  Bed,
  Bath,
  Ruler,
  Calendar,
  ImageOff,
  MapPin,
  Lock,
  Sparkles,
  Shield,
  Zap,
  CheckCircle2,
  AlertCircle,
  Image as ImageIcon,
  Film,
} from "lucide-react";
import { useListingDetail } from "@/features/listings/hooks/useListings";
import { listingApi } from "@/features/listings/services/listingApi";
import { useAuthStore } from "@/app/auth/authStore";
import { roles } from "@/app/auth/roles";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button, buttonVariants } from "@/components/ui/button";
import { Separator } from "@/components/ui/separator";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Loader } from "@/components/shared/Loader";
import { formatMoney, formatDate } from "@/utils/format";
import { cn } from "@/lib/utils";

const ListingApproxMap = lazy(() =>
  import("@/features/listings/components/ListingApproxMap").then((m) => ({
    default: m.ListingApproxMap,
  })),
);

const statusVariant: Record<string, "secondary" | "success" | "accent" | "outline"> = {
  Draft: "secondary",
  Published: "success",
  Activated: "accent",
  Closed: "outline",
};

export const LandlordListingDetailPage = () => {
  const { id } = useParams<{ id: string }>();
  const { data: listing, isLoading, isError } = useListingDetail(id);
  const queryClient = useQueryClient();
  const user = useAuthStore((s) => s.user);
  const [actionError, setActionError] = useState<string | null>(null);

  const publishMutation = useMutation({
    mutationFn: () => listingApi.publish(id!),
    onSuccess: () => {
      setActionError(null);
      void queryClient.invalidateQueries({ queryKey: ["listing", id] });
      void queryClient.invalidateQueries({ queryKey: ["listings", "mine"] });
    },
    onError: (err: unknown) => {
      const detail =
        (err as { response?: { data?: { detail?: string } } })?.response?.data?.detail ??
        (err instanceof Error ? err.message : "Failed to publish listing.");
      setActionError(detail);
    },
  });

  const closeMutation = useMutation({
    mutationFn: () => listingApi.close(id!),
    onSuccess: () => {
      setActionError(null);
      void queryClient.invalidateQueries({ queryKey: ["listing", id] });
      void queryClient.invalidateQueries({ queryKey: ["listings", "mine"] });
    },
    onError: (err: unknown) => {
      const detail =
        (err as { response?: { data?: { detail?: string } } })?.response?.data?.detail ??
        (err instanceof Error ? err.message : "Failed to close listing.");
      setActionError(detail);
    },
  });

  if (isLoading) return <Loader fullPage label="Loading listing..." />;

  if (isError || !listing) {
    return (
      <div className="text-center py-16">
        <p className="text-destructive font-medium">Listing not found or failed to load.</p>
        <Link to="/app/listings" className={cn(buttonVariants({ variant: "outline" }), "mt-4")}>
          <ArrowLeft className="h-4 w-4" />
          Back to my listings
        </Link>
      </div>
    );
  }

  const isOwner = user?.userId === listing.landlordUserId;
  const isAdmin = user?.role === roles.platformAdmin;
  const canManage = isOwner || isAdmin;

  const cover =
    listing.photos.find((p) => p.isCover && p.url) ??
    [...listing.photos].filter((p) => p.url).sort((a, b) => a.sortOrder - b.sortOrder)[0];
  const otherPhotos = listing.photos
    .filter((p) => p.url && p.id !== cover?.id)
    .sort((a, b) => a.sortOrder - b.sortOrder)
    .slice(0, 5);
  const photoCount = listing.photos.filter((p) => p.url).length;

  const stayRange =
    listing.minStayDays && listing.maxStayDays
      ? `${listing.minStayDays}–${listing.maxStayDays} days`
      : listing.minStayDays
        ? `${listing.minStayDays}+ days`
        : "—";

  const locationLine = listing.preciseAddress
    ? [
        listing.preciseAddress.street,
        listing.preciseAddress.city,
        listing.preciseAddress.state,
        listing.preciseAddress.country,
      ]
        .filter(Boolean)
        .join(", ")
    : null;

  const hasCoords = listing.latitude != null && listing.longitude != null;
  const isAddressLocked = Boolean(listing.preciseAddress);

  const canPublish = listing.status === "Draft";
  const canClose = listing.status === "Published" || listing.status === "Activated";
  const isMutating = publishMutation.isPending || closeMutation.isPending;

  return (
    <div className="space-y-6">
      <div>
        <Link
          to="/app/listings"
          className="inline-flex items-center gap-1.5 text-sm text-muted-foreground hover:text-foreground"
        >
          <ArrowLeft className="h-4 w-4" />
          Back to my listings
        </Link>
      </div>

      <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
        <div className="min-w-0">
          <div className="flex flex-wrap items-center gap-2 mb-2">
            <Badge variant={statusVariant[listing.status] ?? "secondary"}>{listing.status}</Badge>
            <Badge variant="outline">{listing.propertyType}</Badge>
            {listing.instantBookingEnabled && (
              <Badge variant="accent" className="gap-1">
                <Zap className="h-3 w-3" />
                Instant book
              </Badge>
            )}
            {listing.insuranceRequired && (
              <Badge variant="default" className="gap-1">
                <Shield className="h-3 w-3" />
                Insurance required
              </Badge>
            )}
          </div>
          <h1 className="text-2xl font-bold tracking-tight sm:text-3xl truncate">{listing.title}</h1>
          <p className="mt-1 text-sm text-muted-foreground">
            Created {formatDate(listing.createdAt)} · Last updated {formatDate(listing.updatedAt)}
          </p>
        </div>

        {canManage && (
          <div className="flex flex-wrap gap-2 shrink-0">
            <Link
              to={`/listings/${listing.id}`}
              target="_blank"
              rel="noopener noreferrer"
              className={cn(buttonVariants({ variant: "outline" }))}
            >
              <ExternalLink className="h-4 w-4" />
              View public page
            </Link>
            <Link
              to={`/app/listings/${listing.id}/edit`}
              className={cn(buttonVariants({ variant: "default" }))}
            >
              <Pencil className="h-4 w-4" />
              Edit listing
            </Link>
            {canPublish && (
              <Button
                variant="accent"
                onClick={() => publishMutation.mutate()}
                disabled={isMutating}
              >
                <Rocket className="h-4 w-4" />
                {publishMutation.isPending ? "Publishing..." : "Publish"}
              </Button>
            )}
            {canClose && (
              <Button
                variant="outline"
                onClick={() => {
                  if (window.confirm("Close this listing? It will no longer appear in search.")) {
                    closeMutation.mutate();
                  }
                }}
                disabled={isMutating}
              >
                <Ban className="h-4 w-4" />
                {closeMutation.isPending ? "Closing..." : "Close"}
              </Button>
            )}
          </div>
        )}
      </div>

      {actionError && (
        <Alert variant="destructive">
          <AlertDescription>{actionError}</AlertDescription>
        </Alert>
      )}

      {/* Quick stats */}
      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
        <StatCard label="Monthly rent" value={formatMoney(listing.monthlyRentCents)} />
        <StatCard
          label="Suggested deposit"
          value={
            listing.suggestedDepositLowCents != null
              ? `${formatMoney(listing.suggestedDepositLowCents)}${
                  listing.suggestedDepositHighCents &&
                  listing.suggestedDepositHighCents !== listing.suggestedDepositLowCents
                    ? `–${formatMoney(listing.suggestedDepositHighCents)}`
                    : ""
                }`
              : `Up to ${formatMoney(listing.maxDepositCents)}`
          }
        />
        <StatCard
          label="Quality score"
          value={listing.qualityScore != null ? Math.round(listing.qualityScore).toString() : "—"}
          icon={<Sparkles className="h-4 w-4" />}
        />
        <StatCard
          label="Photos"
          value={photoCount.toString()}
          icon={<ImageIcon className="h-4 w-4" />}
        />
      </div>

      <div className="grid gap-6 lg:grid-cols-3">
        <div className="space-y-6 lg:col-span-2">
          {/* Photos preview */}
          <Card>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-3">
              <CardTitle className="text-lg flex items-center gap-2">
                <ImageIcon className="h-5 w-5" />
                Photos
                <Badge variant="secondary" className="ml-1">
                  {photoCount}
                </Badge>
              </CardTitle>
              {canManage && (
                <Link
                  to={`/app/listings/${listing.id}/edit`}
                  className="text-xs text-muted-foreground hover:text-foreground"
                >
                  Manage photos
                </Link>
              )}
            </CardHeader>
            <CardContent className="space-y-3">
              {photoCount === 0 ? (
                <div className="flex aspect-[16/9] items-center justify-center rounded-lg bg-muted">
                  <div className="text-center">
                    <ImageOff className="mx-auto h-10 w-10 text-muted-foreground/40" />
                    <p className="mt-2 text-sm text-muted-foreground">No photos yet</p>
                    {canManage && (
                      <Link
                        to={`/app/listings/${listing.id}/edit`}
                        className={cn(buttonVariants({ variant: "outline", size: "sm" }), "mt-3")}
                      >
                        Add photos
                      </Link>
                    )}
                  </div>
                </div>
              ) : (
                <>
                  {cover && (
                    <Link
                      to={`/listings/${listing.id}`}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="block aspect-[16/9] overflow-hidden rounded-lg bg-muted relative group"
                    >
                      <img
                        src={cover.url ?? ""}
                        alt={cover.caption ?? listing.title}
                        className="h-full w-full object-cover transition-transform group-hover:scale-[1.02]"
                      />
                      <div className="absolute left-3 top-3">
                        <Badge variant="default">Cover</Badge>
                      </div>
                    </Link>
                  )}
                  {otherPhotos.length > 0 && (
                    <div className="grid grid-cols-5 gap-2">
                      {otherPhotos.map((p) => (
                        <div
                          key={p.id}
                          className="aspect-square overflow-hidden rounded-md bg-muted"
                        >
                          <img
                            src={p.url ?? ""}
                            alt={p.caption ?? ""}
                            className="h-full w-full object-cover"
                            loading="lazy"
                          />
                        </div>
                      ))}
                    </div>
                  )}
                </>
              )}
            </CardContent>
          </Card>

          {/* Description */}
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-lg">Description</CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-sm leading-relaxed text-muted-foreground whitespace-pre-line">
                {listing.description}
              </p>
            </CardContent>
          </Card>

          {/* Property facts */}
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-lg">Property facts</CardTitle>
            </CardHeader>
            <CardContent>
              <dl className="grid grid-cols-2 gap-x-6 gap-y-3 sm:grid-cols-4 text-sm">
                <FactItem
                  icon={<Bed className="h-4 w-4" />}
                  label="Bedrooms"
                  value={listing.bedrooms === 0 ? "Studio" : listing.bedrooms.toString()}
                />
                <FactItem
                  icon={<Bath className="h-4 w-4" />}
                  label="Bathrooms"
                  value={listing.bathrooms.toString()}
                />
                {listing.squareFootage != null && (
                  <FactItem
                    icon={<Ruler className="h-4 w-4" />}
                    label="Square footage"
                    value={`${listing.squareFootage.toLocaleString()} sq ft`}
                  />
                )}
                <FactItem
                  icon={<Calendar className="h-4 w-4" />}
                  label="Stay length"
                  value={stayRange}
                />
              </dl>

              <Separator className="my-4" />

              <div className="grid gap-3 sm:grid-cols-3">
                <SummaryChip
                  count={listing.amenities.length}
                  label={`amenit${listing.amenities.length === 1 ? "y" : "ies"}`}
                />
                <SummaryChip
                  count={listing.safetyDevices.length}
                  label={`safety device${listing.safetyDevices.length === 1 ? "" : "s"}`}
                />
                <SummaryChip
                  count={listing.considerations.length}
                  label={`consideration${listing.considerations.length === 1 ? "" : "s"}`}
                />
              </div>

              {listing.virtualTourUrl && (
                <>
                  <Separator className="my-4" />
                  <a
                    href={listing.virtualTourUrl}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="inline-flex items-center gap-1.5 text-sm text-foreground hover:underline"
                  >
                    <Film className="h-4 w-4" />
                    Virtual tour available
                    <ExternalLink className="h-3 w-3" />
                  </a>
                </>
              )}
            </CardContent>
          </Card>
        </div>

        <div className="space-y-6">
          {/* Location card */}
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-lg flex items-center gap-2">
                <MapPin className="h-5 w-5" />
                Location
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              {hasCoords ? (
                <div className="aspect-[4/3] overflow-hidden rounded-lg border">
                  <Suspense
                    fallback={
                      <div className="flex h-full w-full items-center justify-center bg-muted">
                        <Loader />
                      </div>
                    }
                  >
                    <ListingApproxMap
                      latitude={listing.latitude!}
                      longitude={listing.longitude!}
                      showMarker={isAddressLocked}
                      privacyRadiusMeters={isAddressLocked ? undefined : 350}
                    />
                  </Suspense>
                </div>
              ) : (
                <div className="flex aspect-[4/3] items-center justify-center rounded-lg border bg-muted text-center text-sm text-muted-foreground">
                  <div>
                    <MapPin className="mx-auto h-8 w-8 text-muted-foreground/50 mb-2" />
                    No location set
                  </div>
                </div>
              )}

              <ul className="space-y-2 text-sm">
                <li className="flex items-start gap-2">
                  {hasCoords ? (
                    <CheckCircle2 className="h-4 w-4 text-success mt-0.5 shrink-0" />
                  ) : (
                    <AlertCircle className="h-4 w-4 text-amber-500 mt-0.5 shrink-0" />
                  )}
                  <div>
                    <p className="font-medium">Approximate location</p>
                    {hasCoords ? (
                      <p className="text-xs text-muted-foreground">
                        {listing.latitude!.toFixed(4)}, {listing.longitude!.toFixed(4)}
                      </p>
                    ) : (
                      <p className="text-xs text-muted-foreground">Set a pin so renters can find you on the map.</p>
                    )}
                  </div>
                </li>
                <li className="flex items-start gap-2">
                  {isAddressLocked ? (
                    <Lock className="h-4 w-4 text-success mt-0.5 shrink-0" />
                  ) : (
                    <AlertCircle className="h-4 w-4 text-amber-500 mt-0.5 shrink-0" />
                  )}
                  <div>
                    <p className="font-medium">Precise address</p>
                    {locationLine ? (
                      <p className="text-xs text-muted-foreground">{locationLine}</p>
                    ) : (
                      <p className="text-xs text-muted-foreground">Lock the precise address before publishing.</p>
                    )}
                  </div>
                </li>
              </ul>

              {canManage && (
                <Link
                  to={`/app/listings/${listing.id}/edit`}
                  className={cn(buttonVariants({ variant: "outline", size: "sm" }), "w-full")}
                >
                  <Pencil className="h-4 w-4" />
                  {hasCoords && isAddressLocked ? "Edit location" : "Set location"}
                </Link>
              )}
            </CardContent>
          </Card>

          {/* House rules summary */}
          {listing.houseRules && (
            <Card>
              <CardHeader className="pb-3">
                <CardTitle className="text-lg">House rules</CardTitle>
              </CardHeader>
              <CardContent className="space-y-2 text-sm">
                <RuleRow label="Check-in" value={listing.houseRules.checkInTime ?? "—"} />
                <RuleRow label="Check-out" value={listing.houseRules.checkOutTime ?? "—"} />
                <RuleRow label="Max guests" value={listing.houseRules.maxGuests?.toString() ?? "—"} />
                <RuleRow
                  label="Pets"
                  value={listing.houseRules.petsAllowed ? "Allowed" : "Not allowed"}
                />
                <RuleRow
                  label="Smoking"
                  value={listing.houseRules.smokingAllowed ? "Allowed" : "Not allowed"}
                />
                <RuleRow
                  label="Parties"
                  value={listing.houseRules.partiesAllowed ? "Allowed" : "Not allowed"}
                />
              </CardContent>
            </Card>
          )}
        </div>
      </div>
    </div>
  );
};

function StatCard({
  label,
  value,
  icon,
}: {
  label: string;
  value: string;
  icon?: React.ReactNode;
}) {
  return (
    <Card>
      <CardContent className="p-4">
        <p className="text-xs text-muted-foreground flex items-center gap-1.5">
          {icon}
          {label}
        </p>
        <p className="mt-1 text-xl font-semibold tabular-nums">{value}</p>
      </CardContent>
    </Card>
  );
}

function FactItem({
  icon,
  label,
  value,
}: {
  icon: React.ReactNode;
  label: string;
  value: string;
}) {
  return (
    <div>
      <dt className="flex items-center gap-1.5 text-xs text-muted-foreground">
        {icon}
        {label}
      </dt>
      <dd className="mt-0.5 font-medium">{value}</dd>
    </div>
  );
}

function SummaryChip({ count, label }: { count: number; label: string }) {
  return (
    <div className="rounded-lg border bg-muted/30 px-3 py-2 text-center">
      <p className="text-xl font-semibold tabular-nums">{count}</p>
      <p className="text-xs text-muted-foreground">{label}</p>
    </div>
  );
}

function RuleRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex items-center justify-between gap-4 border-b border-border/50 pb-1.5 last:border-0 last:pb-0">
      <span className="text-muted-foreground">{label}</span>
      <span className="font-medium">{value}</span>
    </div>
  );
}
