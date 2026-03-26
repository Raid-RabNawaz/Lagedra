import { useParams, Link } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState, lazy, Suspense } from "react";
import {
  ArrowLeft,
  MapPin,
  Lock,
  ImagePlus,
  Trash2,
  Star,
  Rocket,
  Ban,
  AlertTriangle,
  GripVertical,
  ChevronUp,
  ChevronDown,
  Search,
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

  const publishMutation = useMutation({
    mutationFn: () => listingApi.publish(id!),
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
          {listing.status === "Draft" && (
            <Button
              variant="accent"
              disabled={publishMutation.isPending}
              onClick={() => publishMutation.mutate()}
            >
              <Rocket className="h-4 w-4" />
              {publishMutation.isPending ? "Publishing..." : "Publish"}
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

      {updateMutation.isError && (
        <Alert variant="destructive">
          <AlertDescription>Failed to save changes. Try again.</AlertDescription>
        </Alert>
      )}
      {publishMutation.isError && (
        <Alert variant="destructive">
          <AlertDescription>
            {(publishMutation.error as any)?.response?.data?.detail ?? "Failed to publish listing."}
          </AlertDescription>
        </Alert>
      )}
      {closeMutation.isError && (
        <Alert variant="destructive">
          <AlertDescription>
            {(closeMutation.error as any)?.response?.data?.detail ?? "Failed to close listing."}
          </AlertDescription>
        </Alert>
      )}
      {lockAddressMutation.isError && (
        <Alert variant="destructive">
          <AlertDescription>
            {(lockAddressMutation.error as any)?.response?.data?.detail ??
              (lockAddressMutation.error as Error).message ??
              "Failed to lock address."}
          </AlertDescription>
        </Alert>
      )}

      <ListingForm
        key={listing.updatedAt}
        definitions={defs.data}
        defaultValues={defaultValues}
        submitLabel="Save changes"
        onSubmit={async (values) => {
          await updateMutation.mutateAsync(values);
        }}
      />

      <Separator />

      <div className="grid gap-6 lg:grid-cols-2">
        <Card className="lg:col-span-2">
          <CardHeader>
            <CardTitle className="text-lg flex items-center gap-2">
              <MapPin className="h-5 w-5" />
              Approximate location
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
          <Card>
            <CardHeader>
              <CardTitle className="text-lg flex items-center gap-2">
                <Lock className="h-5 w-5" />
                Precise address
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

        <Card>
          <CardHeader>
            <CardTitle className="text-lg flex items-center gap-2">
              <ImagePlus className="h-5 w-5" />
              Photos
            </CardTitle>
            <CardDescription>Add images via public URL (e.g. CDN). First photo can be set as cover.</CardDescription>
          </CardHeader>
          <CardContent className="space-y-3">
            <div className="space-y-1.5">
              <Label htmlFor="photoUrl">Image URL</Label>
              <Input
                id="photoUrl"
                value={photoUrl}
                onChange={(e) => setPhotoUrl(e.target.value)}
                placeholder="https://..."
              />
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="caption">Caption (optional)</Label>
              <Input id="caption" value={photoCaption} onChange={(e) => setPhotoCaption(e.target.value)} />
            </div>
            {addPhotoMutation.isError && (
              <p className="text-sm text-destructive">{(addPhotoMutation.error as Error).message}</p>
            )}
            <Button
              type="button"
              variant="secondary"
              disabled={addPhotoMutation.isPending}
              onClick={() => addPhotoMutation.mutate()}
            >
              {addPhotoMutation.isPending ? "Adding..." : "Add photo"}
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
    </div>
  );
};
