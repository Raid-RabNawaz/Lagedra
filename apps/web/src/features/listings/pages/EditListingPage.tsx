import { useParams, Link } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useCallback, useState, lazy, Suspense } from "react";
import {
  ArrowLeft,
  MapPin,
  Lock,
  ImagePlus,
  Trash2,
  Star,
  Send,
  Ban,
  AlertTriangle,
  Clock,
  GripVertical,
  ChevronUp,
  ChevronDown,
  Search,
  Upload,
  Film,
  Loader2,
  CheckCircle2,
  Circle,
  ArrowRight,
} from "lucide-react";

const LocationPickerMap = lazy(() =>
  import("@/features/listings/components/LocationPickerMap").then((m) => ({
    default: m.LocationPickerMap,
  })),
);
import { useAuthStore } from "@/app/auth/authStore";
import { roles } from "@/app/auth/roles";
import { listingApi } from "@/features/listings/services/listingApi";
import { ListingForm } from "@/features/listings/components/ListingForm";
import { useListingDefinitions } from "@/features/listings/hooks/useListingDefinitions";
import { listingDetailsToFormValues } from "@/features/listings/lib/mapListingToForm";
import { toUpdateListingRequest } from "@/features/listings/lib/toListingRequests";
import type { ListingFormValues } from "@/features/listings/lib/listingFormSchema";
import { getApiErrorMessage } from "@/api/errors";
import { Loader } from "@/components/shared/Loader";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Separator } from "@/components/ui/separator";
import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";

export const EditListingPage = () => {
  const { id } = useParams<{ id: string }>();
  const user = useAuthStore((s) => s.user);
  const queryClient = useQueryClient();
  const defs = useListingDefinitions();

  const listingQuery = useQuery({
    queryKey: ["listing", id],
    queryFn: () => listingApi.getDetail(id!),
    enabled: Boolean(id),
  });

  const [lat, setLat] = useState("");
  const [lng, setLng] = useState("");
  const [addressQuery, setAddressQuery] = useState("");
  const [isGeocoding, setIsGeocoding] = useState(false);
  const [geocodeError, setGeocodeError] = useState("");
  const [street, setStreet] = useState("");
  const [city, setCity] = useState("");
  const [addrState, setAddrState] = useState("");
  const [zipCode, setZipCode] = useState("");
  const [country, setCountry] = useState("");
  const [jurisdictionCode, setJurisdictionCode] = useState("");
  const [photoUrl, setPhotoUrl] = useState("");
  const [photoCaption, setPhotoCaption] = useState("");
  const [dragIdx, setDragIdx] = useState<number | null>(null);
  const [mediaError, setMediaError] = useState<string | null>(null);

  const listing = listingQuery.data;

  const handleGeocode = async () => {
    const q = addressQuery.trim();
    if (!q) return;
    setIsGeocoding(true);
    setGeocodeError("");
    try {
      const res = await fetch(
        `https://nominatim.openstreetmap.org/search?q=${encodeURIComponent(q)}&format=json&limit=1`,
        { headers: { "Accept-Language": "en" } },
      );
      const data = await res.json();
      if (data.length > 0) {
        setLat(data[0].lat);
        setLng(data[0].lon);
      } else {
        setGeocodeError("No results found. Try a more specific address.");
      }
    } catch {
      setGeocodeError("Geocoding failed. Please try again.");
    } finally {
      setIsGeocoding(false);
    }
  };

  const handleMapClick = (latitude: number, longitude: number) => {
    setLat(latitude.toFixed(6));
    setLng(longitude.toFixed(6));
  };

  const updateMutation = useMutation({
    mutationFn: (values: ListingFormValues) =>
      listingApi.update(id!, toUpdateListingRequest(values)),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["listing", id] });
      void queryClient.invalidateQueries({ queryKey: ["listings", "mine"] });
    },
  });

  const locationMutation = useMutation({
    mutationFn: () => {
      const latitude = Number(lat);
      const longitude = Number(lng);
      if (Number.isNaN(latitude) || Number.isNaN(longitude)) {
        throw new Error("Enter valid latitude and longitude.");
      }
      return listingApi.setApproxLocation(id!, { latitude, longitude });
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["listing", id] });
    },
  });

  const lockAddressMutation = useMutation({
    mutationFn: () => {
      if (!street.trim() || !city.trim() || !addrState.trim() || !zipCode.trim() || !country.trim()) {
        throw new Error("All address fields are required.");
      }
      return listingApi.lockAddress(id!, {
        street: street.trim(),
        city: city.trim(),
        state: addrState.trim(),
        zipCode: zipCode.trim(),
        country: country.trim(),
        jurisdictionCode: jurisdictionCode.trim() || null,
      });
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["listing", id] });
    },
  });

  const addPhotoMutation = useMutation({
    mutationFn: () => {
      const url = photoUrl.trim();
      if (!url) throw new Error("Enter an image URL.");
      try {
        new URL(url);
      } catch {
        throw new Error("Enter a valid URL.");
      }
      const storageKey = `web/${crypto.randomUUID()}`;
      return listingApi.addPhoto(id!, { storageKey, url, caption: photoCaption.trim() || null });
    },
    onSuccess: () => {
      setPhotoUrl("");
      setPhotoCaption("");
      void queryClient.invalidateQueries({ queryKey: ["listing", id] });
    },
  });

  const uploadMediaMutation = useMutation({
    mutationFn: (params: { file: File; caption?: string | null }) =>
      listingApi.uploadMedia(id!, params.file, params.caption ?? null),
    onSuccess: () => {
      setPhotoCaption("");
      setMediaError(null);
      void queryClient.invalidateQueries({ queryKey: ["listing", id] });
    },
    onError: (error: unknown) => {
      const detail =
        (error as { response?: { data?: { detail?: string } } })?.response?.data?.detail ??
        (error instanceof Error ? error.message : "Upload failed.");
      setMediaError(detail);
    },
  });

  const removePhotoMutation = useMutation({
    mutationFn: (photoId: string) => listingApi.removePhoto(id!, photoId),
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: ["listing", id] }),
  });

  const coverMutation = useMutation({
    mutationFn: (photoId: string) => listingApi.setCoverPhoto(id!, photoId),
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: ["listing", id] }),
  });

  const reorderMutation = useMutation({
    mutationFn: (photoIds: string[]) => listingApi.reorderPhotos(id!, photoIds),
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: ["listing", id] }),
  });

  const submitMutation = useMutation({
    mutationFn: () => listingApi.submitForReview(id!),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["listing", id] });
      void queryClient.invalidateQueries({ queryKey: ["listings", "mine"] });
    },
  });

  const closeMutation = useMutation({
    mutationFn: () => listingApi.close(id!),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["listing", id] });
      void queryClient.invalidateQueries({ queryKey: ["listings", "mine"] });
    },
  });

  // ── Derived state (safe before early returns; falls back when listing is null) ──
  const hasLocation = listing ? listing.latitude != null && listing.longitude != null : false;
  const hasPhotos = listing ? listing.photos.length > 0 : false;
  const hasDescription = listing ? (listing.description ?? "").trim().length > 0 : false;
  const hasRent = listing ? (listing.monthlyRentCents ?? 0) > 0 : false;
  const isDraft = listing?.status === "Draft";
  const isDenied = listing?.status === "Denied";
  const isInReview = listing?.status === "InReview";
  const isEditable = isDraft || isDenied;

  // Mirror server-side rule (Listing.SubmitForReview() requires Draft|Denied + ApproxGeoPoint).
  const canSubmit = isEditable && hasLocation;
  const submitLabel = isDenied ? "Resubmit for review" : "Submit for review";
  const submitBlockedReason = !listing
    ? ""
    : !isEditable
      ? isInReview
        ? "Listing is being reviewed by an admin."
        : `Already ${listing.status.toLowerCase()}.`
      : !hasLocation
        ? "Set the approximate location below before submitting."
        : "";

  const scrollToId = useCallback((targetId: string) => {
    const el = document.getElementById(targetId);
    if (el) el.scrollIntoView({ behavior: "smooth", block: "start" });
  }, []);

  const handleSubmitClick = useCallback(() => {
    if (!canSubmit) return;
    if (!hasPhotos) {
      const ok = window.confirm(
        "This listing has no photos yet. Listings with photos perform much better — submit for review anyway?",
      );
      if (!ok) return;
    }
    submitMutation.mutate();
  }, [canSubmit, hasPhotos, submitMutation]);

  // ── Early returns (after all hooks) ─────────────────────────────────────
  if (!id) {
    return <p className="text-destructive">Missing listing id.</p>;
  }

  if (defs.isLoading || listingQuery.isLoading) {
    return <Loader fullPage label="Loading..." />;
  }

  if (defs.isError || !defs.data || listingQuery.isError || !listing) {
    return (
      <div className="space-y-4">
        <Alert variant="destructive">
          <AlertTriangle className="h-4 w-4" />
          <AlertDescription>Listing not found or failed to load.</AlertDescription>
        </Alert>
        <Link to="/app/listings" className="text-sm text-accent underline">
          Back to my listings
        </Link>
      </div>
    );
  }

  const isOwner = user && listing.landlordUserId === user.userId;
  const isPlatformAdmin = user && String(user.role) === roles.platformAdmin;
  if (user && !isOwner && !isPlatformAdmin) {
    return (
      <Alert variant="destructive">
        <AlertDescription>You do not have access to edit this listing.</AlertDescription>
      </Alert>
    );
  }

  const defaultValues = listingDetailsToFormValues(listing);

  return (
    <div className="space-y-8">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <Link
            to="/app/listings"
            className="inline-flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground mb-2"
          >
            <ArrowLeft className="h-4 w-4" />
            My listings
          </Link>
          <h1 className="text-3xl font-bold tracking-tight">Edit listing</h1>
          <div className="mt-2 flex flex-wrap items-center gap-2">
            <Badge variant="secondary">{listing.status}</Badge>
            <Link to={`/listings/${listing.id}`} className="text-sm text-accent hover:underline">
              View public page
            </Link>
          </div>
        </div>
        <div className="flex flex-wrap gap-2">
          {isEditable && (
            <Button
              variant="accent"
              disabled={submitMutation.isPending || !canSubmit}
              onClick={handleSubmitClick}
              title={canSubmit ? undefined : submitBlockedReason}
            >
              <Send className="h-4 w-4" />
              {submitMutation.isPending ? "Submitting..." : submitLabel}
            </Button>
          )}
          {(listing.status === "Published" || listing.status === "Activated") && (
            <Button
              variant="outline"
              disabled={closeMutation.isPending}
              onClick={() => {
                if (window.confirm("Close this listing? It will no longer appear in search.")) {
                  closeMutation.mutate();
                }
              }}
            >
              <Ban className="h-4 w-4" />
              Close listing
            </Button>
          )}
        </div>
      </div>

      {isInReview && (
        <Alert>
          <Clock className="h-4 w-4" />
          <AlertDescription>
            This listing is being reviewed by an admin and is read-only until the review completes.
            We&apos;ll notify you as soon as it&apos;s approved.
          </AlertDescription>
        </Alert>
      )}

      {isDenied && listing.rejectionReason && (
        <Alert variant="destructive">
          <AlertTriangle className="h-4 w-4" />
          <AlertDescription>
            <p className="font-medium">An admin asked you to update this listing before it can go live.</p>
            <p className="mt-1 text-sm">
              <span className="font-medium">Reason:</span> {listing.rejectionReason}
            </p>
            <p className="mt-2 text-sm">
              Make the changes below, then click <span className="font-medium">Resubmit for review</span> at the top.
            </p>
          </AlertDescription>
        </Alert>
      )}

      {updateMutation.isError && (
        <Alert variant="destructive">
          <AlertDescription>Failed to save changes. Try again.</AlertDescription>
        </Alert>
      )}
      {submitMutation.isError && (
        <Alert variant="destructive">
          <AlertDescription>
            {getApiErrorMessage(submitMutation.error, "Failed to submit listing for review.")}
          </AlertDescription>
        </Alert>
      )}
      {closeMutation.isError && (
        <Alert variant="destructive">
          <AlertDescription>
            {getApiErrorMessage(closeMutation.error, "Failed to close listing.")}
          </AlertDescription>
        </Alert>
      )}
      {lockAddressMutation.isError && (
        <Alert variant="destructive">
          <AlertDescription>
            {getApiErrorMessage(lockAddressMutation.error, "Failed to lock address.")}
          </AlertDescription>
        </Alert>
      )}

      {isEditable && (
        <Card className="border-accent/40 bg-accent/5">
          <CardHeader>
            <CardTitle className="text-lg flex items-center gap-2">
              <Send className="h-5 w-5 text-accent" />
              {isDenied ? "Address admin feedback and resubmit" : "Almost ready to submit for review"}
            </CardTitle>
            <CardDescription>
              {isDenied
                ? "Make the changes the admin requested, then resubmit. Once approved your listing will go live in the marketplace."
                : "Tenants can\u2019t see this listing until an admin approves it. Finish the items below, then submit for review."}
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-3">
            <ChecklistRow
              done={hasLocation}
              required
              label="Set the approximate location"
              detail="Drop a pin or look up an address so tenants can see the general area."
              jumpLabel="Add location"
              onJump={() => scrollToId("location")}
            />
            <ChecklistRow
              done={hasPhotos}
              required={false}
              label={`Add at least one photo${hasPhotos ? ` (${listing.photos.length} added)` : ""}`}
              detail="Listings with photos get up to 10× more applications. Strongly recommended."
              jumpLabel="Add photos"
              onJump={() => scrollToId("photos")}
            />
            <ChecklistRow
              done={hasDescription}
              required={false}
              label="Write a short description"
              detail="A short summary helps tenants understand what makes this place special."
              jumpLabel="Edit details"
              onJump={() => scrollToId("details")}
            />
            <ChecklistRow
              done={hasRent}
              required={false}
              label="Set the monthly rent"
              detail="Tenants filter by price — listings with $0 rent are usually hidden."
              jumpLabel="Edit details"
              onJump={() => scrollToId("details")}
            />

            <div className="flex flex-col gap-2 pt-2 sm:flex-row sm:items-center sm:justify-between">
              <p className="text-xs text-muted-foreground">
                {canSubmit
                  ? hasPhotos
                    ? "All set — submit for review whenever you're ready."
                    : "You can submit without photos, but we'll ask you to confirm."
                  : submitBlockedReason}
              </p>
              <Button
                variant="accent"
                disabled={submitMutation.isPending || !canSubmit}
                onClick={handleSubmitClick}
                title={canSubmit ? undefined : submitBlockedReason}
              >
                <Send className="h-4 w-4" />
                {submitMutation.isPending ? "Submitting..." : submitLabel}
              </Button>
            </div>
          </CardContent>
        </Card>
      )}

      <section className="space-y-3">
        <div>
          <h2 className="text-xl font-semibold tracking-tight">Step 1 — Location &amp; photos</h2>
          <p className="text-sm text-muted-foreground">
            These live outside the main form because each section saves on its own. Set them first so
            you don&apos;t hit a publish error later.
          </p>
        </div>

      <div className="grid gap-6 lg:grid-cols-2">
        <Card id="location" className="lg:col-span-2 scroll-mt-24">
          <CardHeader>
            <CardTitle className="text-lg flex items-center gap-2">
              <MapPin className="h-5 w-5" />
              Approximate location
              {hasLocation ? (
                <Badge variant="secondary" className="ml-2">
                  <CheckCircle2 className="h-3 w-3 mr-1" />
                  Set
                </Badge>
              ) : (
                <Badge variant="destructive" className="ml-2">Required</Badge>
              )}
            </CardTitle>
            <CardDescription>
              Search for an address, click on the map to drop a pin, or enter coordinates manually.
              Tenants see only the general area — the exact address is shared after activation.
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            {listing.latitude != null && listing.longitude != null && (
              <p className="text-sm text-muted-foreground">
                Current: {listing.latitude.toFixed(4)}, {listing.longitude.toFixed(4)}
              </p>
            )}

            <div className="space-y-1.5">
              <Label htmlFor="addressQuery">Look up address</Label>
              <div className="flex gap-2">
                <Input
                  id="addressQuery"
                  value={addressQuery}
                  onChange={(e) => setAddressQuery(e.target.value)}
                  onKeyDown={(e) => {
                    if (e.key === "Enter") {
                      e.preventDefault();
                      void handleGeocode();
                    }
                  }}
                  placeholder="e.g. 123 Main St, San Francisco, CA"
                  className="flex-1"
                />
                <Button
                  type="button"
                  variant="outline"
                  disabled={isGeocoding || !addressQuery.trim()}
                  onClick={() => void handleGeocode()}
                >
                  <Search className="h-4 w-4" />
                  {isGeocoding ? "Searching..." : "Look up"}
                </Button>
              </div>
              {geocodeError && (
                <p className="text-sm text-destructive">{geocodeError}</p>
              )}
            </div>

            <div className="rounded-lg border overflow-hidden" style={{ height: 300 }}>
              <Suspense
                fallback={
                  <div className="h-full flex items-center justify-center bg-muted">
                    <Loader label="Loading map..." />
                  </div>
                }
              >
                <LocationPickerMap
                  latitude={lat ? Number(lat) : listing.latitude ?? undefined}
                  longitude={lng ? Number(lng) : listing.longitude ?? undefined}
                  onClick={handleMapClick}
                />
              </Suspense>
            </div>

            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1.5">
                <Label htmlFor="lat">Latitude</Label>
                <Input id="lat" value={lat} onChange={(e) => setLat(e.target.value)} placeholder="37.7749" />
              </div>
              <div className="space-y-1.5">
                <Label htmlFor="lng">Longitude</Label>
                <Input id="lng" value={lng} onChange={(e) => setLng(e.target.value)} placeholder="-122.4194" />
              </div>
            </div>
            {locationMutation.isError && (
              <p className="text-sm text-destructive">{(locationMutation.error as Error).message}</p>
            )}
            <Button
              type="button"
              variant="secondary"
              disabled={locationMutation.isPending || !lat || !lng}
              onClick={() => locationMutation.mutate()}
            >
              {locationMutation.isPending ? "Saving..." : "Save location"}
            </Button>
          </CardContent>
        </Card>

        {(listing.status === "Published" || listing.preciseAddress) && (
          <Card className="lg:col-span-2 scroll-mt-24">
            <CardHeader>
              <CardTitle className="text-lg flex items-center gap-2">
                <Lock className="h-5 w-5" />
                Precise address
                {listing.preciseAddress && (
                  <Badge variant="secondary" className="ml-2">
                    <CheckCircle2 className="h-3 w-3 mr-1" />
                    Locked
                  </Badge>
                )}
              </CardTitle>
              <CardDescription>
                Lock the full address to proceed toward activation. This is shared only with confirmed tenants.
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-3">
              {listing.preciseAddress && (
                <p className="text-sm text-muted-foreground">
                  Current: {listing.preciseAddress.street}, {listing.preciseAddress.city},{" "}
                  {listing.preciseAddress.state} {listing.preciseAddress.zipCode},{" "}
                  {listing.preciseAddress.country}
                  {listing.jurisdictionCode && ` (${listing.jurisdictionCode})`}
                </p>
              )}
              <div className="grid grid-cols-2 gap-3">
                <div className="space-y-1.5">
                  <Label htmlFor="street">Street</Label>
                  <Input id="street" value={street} onChange={(e) => setStreet(e.target.value)} placeholder="123 Main St" />
                </div>
                <div className="space-y-1.5">
                  <Label htmlFor="city">City</Label>
                  <Input id="city" value={city} onChange={(e) => setCity(e.target.value)} placeholder="San Francisco" />
                </div>
                <div className="space-y-1.5">
                  <Label htmlFor="addrState">State</Label>
                  <Input id="addrState" value={addrState} onChange={(e) => setAddrState(e.target.value)} placeholder="CA" />
                </div>
                <div className="space-y-1.5">
                  <Label htmlFor="zipCode">ZIP Code</Label>
                  <Input id="zipCode" value={zipCode} onChange={(e) => setZipCode(e.target.value)} placeholder="94102" />
                </div>
                <div className="space-y-1.5">
                  <Label htmlFor="country">Country</Label>
                  <Input id="country" value={country} onChange={(e) => setCountry(e.target.value)} placeholder="US" />
                </div>
                <div className="space-y-1.5">
                  <Label htmlFor="jurisdictionCode">Jurisdiction code (optional)</Label>
                  <Input id="jurisdictionCode" value={jurisdictionCode} onChange={(e) => setJurisdictionCode(e.target.value)} placeholder="US-CA" />
                </div>
              </div>
              {lockAddressMutation.isError && (
                <p className="text-sm text-destructive">{(lockAddressMutation.error as Error).message}</p>
              )}
              <Button
                type="button"
                variant="secondary"
                disabled={lockAddressMutation.isPending}
                onClick={() => lockAddressMutation.mutate()}
              >
                {lockAddressMutation.isPending ? "Locking..." : "Lock address"}
              </Button>
            </CardContent>
          </Card>
        )}

        <Card id="photos" className="lg:col-span-2 scroll-mt-24">
          <CardHeader>
            <CardTitle className="text-lg flex items-center gap-2">
              <ImagePlus className="h-5 w-5" />
              Photos &amp; video
              {hasPhotos ? (
                <Badge variant="secondary" className="ml-2">
                  <CheckCircle2 className="h-3 w-3 mr-1" />
                  {listing.photos.length} added
                </Badge>
              ) : (
                <Badge variant="outline" className="ml-2">Recommended</Badge>
              )}
            </CardTitle>
            <CardDescription>
              Upload from your device (stored on Lagedra object storage) or add an existing image URL.
              First photo can be set as cover.
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="space-y-1.5">
              <Label htmlFor="caption">Caption (optional, applies to next upload)</Label>
              <Input
                id="caption"
                value={photoCaption}
                onChange={(e) => setPhotoCaption(e.target.value)}
                placeholder="e.g. Living room"
              />
            </div>

            <div className="rounded-lg border border-dashed p-4 space-y-3">
              <div className="flex flex-wrap items-center gap-3">
                <Button
                  type="button"
                  variant="secondary"
                  disabled={uploadMediaMutation.isPending}
                  className="relative"
                >
                  <label className="cursor-pointer flex items-center">
                    {uploadMediaMutation.isPending ? (
                      <Loader2 className="h-4 w-4 animate-spin mr-2" />
                    ) : (
                      <Upload className="h-4 w-4 mr-2" />
                    )}
                    {uploadMediaMutation.isPending ? "Uploading..." : "Upload photo"}
                    <input
                      type="file"
                      accept="image/jpeg,image/png,image/gif,image/webp,image/heic,image/heif"
                      className="absolute inset-0 opacity-0 cursor-pointer"
                      disabled={uploadMediaMutation.isPending}
                      onChange={(e) => {
                        const file = e.target.files?.[0];
                        const inputEl = e.target;
                        if (file) {
                          setMediaError(null);
                          uploadMediaMutation.mutate({ file, caption: photoCaption });
                        }
                        inputEl.value = "";
                      }}
                    />
                  </label>
                </Button>

                <Button
                  type="button"
                  variant="outline"
                  disabled={uploadMediaMutation.isPending}
                  className="relative"
                >
                  <label className="cursor-pointer flex items-center">
                    <Film className="h-4 w-4 mr-2" />
                    Upload virtual tour video
                    <input
                      type="file"
                      accept="video/mp4,video/quicktime,video/webm"
                      className="absolute inset-0 opacity-0 cursor-pointer"
                      disabled={uploadMediaMutation.isPending}
                      onChange={(e) => {
                        const file = e.target.files?.[0];
                        const inputEl = e.target;
                        if (file) {
                          setMediaError(null);
                          uploadMediaMutation.mutate({ file, caption: null });
                        }
                        inputEl.value = "";
                      }}
                    />
                  </label>
                </Button>
              </div>
              <p className="text-[11px] text-muted-foreground">
                Photos: JPEG, PNG, GIF, WebP, HEIC up to 15 MB. Videos: MP4, MOV, WebM up to 100 MB. A video
                replaces the listing&apos;s virtual tour URL.
              </p>
              {mediaError && <p className="text-sm text-destructive">{mediaError}</p>}
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="photoUrl">Or add an existing image URL</Label>
              <Input
                id="photoUrl"
                value={photoUrl}
                onChange={(e) => setPhotoUrl(e.target.value)}
                placeholder="https://..."
              />
            </div>
            {addPhotoMutation.isError && (
              <p className="text-sm text-destructive">{(addPhotoMutation.error as Error).message}</p>
            )}
            <Button
              type="button"
              variant="ghost"
              disabled={addPhotoMutation.isPending || !photoUrl.trim()}
              onClick={() => addPhotoMutation.mutate()}
            >
              {addPhotoMutation.isPending ? "Adding..." : "Add photo from URL"}
            </Button>

            {(() => {
              const sorted = listing.photos.slice().sort((a, b) => a.sortOrder - b.sortOrder);

              const movePhoto = (from: number, to: number) => {
                if (to < 0 || to >= sorted.length) return;
                const ids = sorted.map((p) => p.id);
                const [moved] = ids.splice(from, 1);
                ids.splice(to, 0, moved);
                reorderMutation.mutate(ids);
              };

              const handleDrop = (targetIdx: number) => {
                if (dragIdx === null || dragIdx === targetIdx) return;
                movePhoto(dragIdx, targetIdx);
                setDragIdx(null);
              };

              return (
                <ul className="space-y-2 pt-4 border-t">
                  {sorted.length === 0 ? (
                    <li className="text-sm text-muted-foreground">No photos yet.</li>
                  ) : (
                    sorted.map((p, idx) => (
                      <li
                        key={p.id}
                        draggable
                        onDragStart={() => setDragIdx(idx)}
                        onDragEnd={() => setDragIdx(null)}
                        onDragOver={(e) => e.preventDefault()}
                        onDrop={() => handleDrop(idx)}
                        className={cn(
                          "flex items-center gap-2 rounded-lg border p-2 text-sm transition-colors",
                          dragIdx === idx && "opacity-50",
                          dragIdx !== null && dragIdx !== idx && "border-dashed border-accent/40",
                        )}
                      >
                        <GripVertical className="h-4 w-4 shrink-0 text-muted-foreground cursor-grab" />

                        {p.url && (
                          <img
                            src={p.url}
                            alt={p.caption ?? ""}
                            className="h-10 w-10 rounded object-cover shrink-0"
                          />
                        )}

                        <span className="truncate flex-1 min-w-0">
                          {p.caption || p.url?.toString() || p.id}
                        </span>

                        <div className="flex items-center gap-0.5 shrink-0">
                          <Button
                            type="button"
                            variant="ghost"
                            size="icon"
                            className="h-7 w-7"
                            disabled={idx === 0 || reorderMutation.isPending}
                            onClick={() => movePhoto(idx, idx - 1)}
                          >
                            <ChevronUp className="h-3.5 w-3.5" />
                          </Button>
                          <Button
                            type="button"
                            variant="ghost"
                            size="icon"
                            className="h-7 w-7"
                            disabled={idx === sorted.length - 1 || reorderMutation.isPending}
                            onClick={() => movePhoto(idx, idx + 1)}
                          >
                            <ChevronDown className="h-3.5 w-3.5" />
                          </Button>

                          {p.isCover ? (
                            <Badge variant="accent" className="text-[10px] ml-1">
                              Cover
                            </Badge>
                          ) : (
                            <Button
                              type="button"
                              variant="ghost"
                              size="icon"
                              className="h-7 w-7"
                              onClick={() => coverMutation.mutate(p.id)}
                              disabled={coverMutation.isPending}
                            >
                              <Star className="h-3.5 w-3.5" />
                            </Button>
                          )}

                          <Button
                            type="button"
                            variant="ghost"
                            size="icon"
                            className="h-7 w-7 text-destructive"
                            onClick={() => removePhotoMutation.mutate(p.id)}
                            disabled={removePhotoMutation.isPending}
                          >
                            <Trash2 className="h-3.5 w-3.5" />
                          </Button>
                        </div>
                      </li>
                    ))
                  )}
                </ul>
              );
            })()}
          </CardContent>
        </Card>
      </div>
      </section>

      <Separator />

      <section id="details" className="space-y-3 scroll-mt-24">
        <div>
          <h2 className="text-xl font-semibold tracking-tight">Step 2 — Listing details</h2>
          <p className="text-sm text-muted-foreground">
            Title, description, pricing, amenities and house rules. Save changes when you&apos;re done
            editing this section.
          </p>
        </div>

        <ListingForm
          key={listing.updatedAt}
          definitions={defs.data}
          defaultValues={defaultValues}
          submitLabel="Save changes"
          onSubmit={async (values) => {
            await updateMutation.mutateAsync(values);
          }}
        />
      </section>
    </div>
  );
};

type ChecklistRowProps = {
  done: boolean;
  required: boolean;
  label: string;
  detail: string;
  jumpLabel: string;
  onJump: () => void;
};

const ChecklistRow = ({ done, required, label, detail, jumpLabel, onJump }: ChecklistRowProps) => {
  return (
    <div className="flex items-start gap-3 rounded-md border bg-background p-3">
      {done ? (
        <CheckCircle2 className="h-5 w-5 shrink-0 text-emerald-600 mt-0.5" />
      ) : required ? (
        <AlertTriangle className="h-5 w-5 shrink-0 text-destructive mt-0.5" />
      ) : (
        <Circle className="h-5 w-5 shrink-0 text-muted-foreground mt-0.5" />
      )}
      <div className="min-w-0 flex-1">
        <div className="flex flex-wrap items-center gap-2">
          <span className={cn("text-sm font-medium", done && "line-through text-muted-foreground")}>
            {label}
          </span>
          {!done &&
            (required ? (
              <Badge variant="destructive" className="text-[10px]">Required</Badge>
            ) : (
              <Badge variant="outline" className="text-[10px]">Recommended</Badge>
            ))}
        </div>
        <p className="text-xs text-muted-foreground mt-0.5">{detail}</p>
      </div>
      {!done && (
        <Button
          type="button"
          variant="ghost"
          size="sm"
          onClick={onJump}
          className="shrink-0 h-8 px-2 text-xs"
        >
          {jumpLabel}
          <ArrowRight className="h-3.5 w-3.5 ml-1" />
        </Button>
      )}
    </div>
  );
};
