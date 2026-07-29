import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useCallback, useEffect, useMemo, useRef, useState, lazy, Suspense } from "react";
import { MapPin, Lock, Search, CheckCircle2 } from "lucide-react";
import { PinAddressMismatchWarning } from "@/features/listings/components/PinAddressMismatchWarning";
import {
  forwardGeocode,
  reverseGeocode,
  haversineKm,
  structuredAddressToQuery,
  isAddressGeocodable,
  type ParsedAddress,
} from "@/features/listings/lib/geocoding";
import { listingApi } from "@/features/listings/services/listingApi";
import type { ListingDetailsDto } from "@/api/types";
import { getApiErrorMessage } from "@/api/errors";
import { Loader } from "@/components/shared/Loader";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";

/**
 * When pin and typed address are this far apart we surface a warning. 5km
 * is roughly "same neighbourhood, maybe a borough over" — anything tighter
 * triggers on harmless reverse-geocode imprecision around city boundaries.
 */
const PIN_ADDRESS_WARN_KM = 5;

/**
 * Above this distance we block saves entirely until the host either
 * fixes the mismatch or explicitly overrides. 100km cleanly separates
 * "different neighbourhood" from "different city/state" (the Los Angeles
 * vs Oceanside scenario in the bug report is ~80mi ≈ 130km).
 */
const PIN_ADDRESS_BLOCK_KM = 100;

const LocationPickerMap = lazy(() =>
  import("@/features/listings/components/LocationPickerMap").then((m) => ({
    default: m.LocationPickerMap,
  })),
);

type ListingLocationEditorProps = {
  listing: ListingDetailsDto;
  /** Disables all save actions (e.g. platform admin inspecting a host listing). */
  readOnly?: boolean;
};

/**
 * Approximate-location (map pin) and precise-address cards. Each card saves
 * through its own endpoint and invalidates the shared ["listing", id] query,
 * so this component works both inside the create wizard and on the edit page.
 */
export function ListingLocationEditor({
  listing,
  readOnly = false,
}: ListingLocationEditorProps) {
  const queryClient = useQueryClient();
  const id = listing.id;

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

  // ── Pin ↔ address reconciliation state ────────────────────────────────
  //
  // The map pin (`lat`/`lng`) and the structured precise-address fields
  // (`street`/`city`/...) can each be edited independently. Without the
  // pieces below they happily drift apart and the listing ends up
  // claiming "Los Angeles" while the pin lives in Oceanside. We:
  //
  //   1. Reverse-geocode the pin whenever it moves, so we can pre-fill
  //      empty address fields and show "Pin currently points at: …" in
  //      the mismatch warning.
  //   2. Forward-geocode the typed address whenever it changes, so we
  //      know where the *address* lives and can compare the two.
  //   3. Compute the haversine distance and either warn (>5 km) or block
  //      saves (>100 km, unless explicitly overridden).
  //
  // Both geocoders are debounced 700ms and use AbortController so a
  // rapidly-typing host doesn't pile up Nominatim requests.

  /** Last reverse-geocode of the dropped pin. Drives auto-fill + warning copy. */
  const [pinAddress, setPinAddress] = useState<{
    parsed: ParsedAddress | null;
    displayName: string;
    /** Coords the address was resolved for — used to detect stale results. */
    forLat: number;
    forLng: number;
  } | null>(null);

  /** Forward-geocode of the typed structured address. Used for mismatch math. */
  const [addressGeocode, setAddressGeocode] = useState<{
    latitude: number;
    longitude: number;
    /** The query string that was geocoded — used to detect stale results. */
    forQuery: string;
  } | null>(null);

  /**
   * Host clicked "Save anyway" on a >100km mismatch. Resets whenever the
   * pin or address changes so the override doesn't silently carry over
   * past edits the host might still want validated.
   */
  const [mismatchOverride, setMismatchOverride] = useState(false);

  const [isReverseGeocoding, setIsReverseGeocoding] = useState(false);
  const [isAddressGeocoding, setIsAddressGeocoding] = useState(false);

  const handleGeocode = useCallback(async () => {
    const q = addressQuery.trim();
    if (!q) return;
    setIsGeocoding(true);
    setGeocodeError("");
    try {
      const res = await forwardGeocode(q);
      if (!res) {
        setGeocodeError("No results found. Try a more specific address.");
        return;
      }
      setLat(res.latitude.toFixed(6));
      setLng(res.longitude.toFixed(6));
      // Fill the structured address from the same lookup so the pin and the
      // typed address can't drift apart from the moment they're first set.
      // Only overwrite empty fields — the host may have already partially
      // typed something they'd rather keep.
      if (res.address) {
        if (!street.trim()) setStreet(res.address.street);
        if (!city.trim()) setCity(res.address.city);
        if (!addrState.trim()) setAddrState(res.address.state);
        if (!zipCode.trim()) setZipCode(res.address.zipCode);
        if (!country.trim()) setCountry(res.address.country);
      }
    } catch {
      setGeocodeError("Geocoding failed. Please try again.");
    } finally {
      setIsGeocoding(false);
    }
  }, [addressQuery, street, city, addrState, zipCode, country]);

  const handleMapClick = useCallback((latitude: number, longitude: number) => {
    setLat(latitude.toFixed(6));
    setLng(longitude.toFixed(6));
    // Clearing the override on every pin change forces the host to re-confirm
    // any "I know it's far away, save anyway" decision after they've moved
    // the pin again.
    setMismatchOverride(false);
  }, []);

  // 1. Reverse-geocode the pin whenever it changes. Debounced so dragging
  //    the input or rapid clicks don't trigger N requests.
  useEffect(() => {
    const latitude = Number(lat);
    const longitude = Number(lng);
    if (!Number.isFinite(latitude) || !Number.isFinite(longitude) || !lat || !lng) {
      setPinAddress(null);
      return;
    }
    const controller = new AbortController();
    const timer = window.setTimeout(() => {
      setIsReverseGeocoding(true);
      reverseGeocode(latitude, longitude, controller.signal)
        .then((res) => {
          if (!res) {
            setPinAddress(null);
            return;
          }
          setPinAddress({
            parsed: res.address,
            displayName: res.displayName,
            forLat: latitude,
            forLng: longitude,
          });
          // Auto-fill structured address only when the host hasn't started
          // typing one yet. Reverse-geocode results are best-guess and we
          // don't want to overwrite the host's local knowledge of the
          // building name / suite number that OSM doesn't know about.
          const allEmpty =
            !street.trim() &&
            !city.trim() &&
            !addrState.trim() &&
            !zipCode.trim() &&
            !country.trim();
          if (allEmpty && res.address) {
            setStreet(res.address.street);
            setCity(res.address.city);
            setAddrState(res.address.state);
            setZipCode(res.address.zipCode);
            setCountry(res.address.country);
          }
        })
        .catch((err) => {
          if ((err as { name?: string })?.name === "AbortError") return;
          // Quiet failure: reverse-geocode is best-effort. The host can still
          // type the address manually and the mismatch warning falls back to
          // forward-geocoding.
        })
        .finally(() => setIsReverseGeocoding(false));
    }, 700);

    return () => {
      controller.abort();
      window.clearTimeout(timer);
    };
    // We intentionally do NOT depend on the address fields here — pulling
    // them in would cause a re-run on every keystroke.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [lat, lng]);

  // 2. Forward-geocode the structured address whenever it changes so we
  //    know where the *address* lives. Same debounce + abort pattern.
  const addressQueryForGeocoding = useMemo(
    () =>
      structuredAddressToQuery({
        street,
        city,
        state: addrState,
        zipCode,
        country,
      }),
    [street, city, addrState, zipCode, country],
  );
  const addressIsGeocodable = isAddressGeocodable({
    street,
    city,
    state: addrState,
    zipCode,
    country,
  });

  useEffect(() => {
    if (!addressIsGeocodable) {
      setAddressGeocode(null);
      return;
    }
    if (addressGeocode?.forQuery === addressQueryForGeocoding) {
      return; // already up to date
    }
    const controller = new AbortController();
    const timer = window.setTimeout(() => {
      setIsAddressGeocoding(true);
      forwardGeocode(addressQueryForGeocoding, controller.signal)
        .then((res) => {
          if (!res) {
            setAddressGeocode(null);
            return;
          }
          setAddressGeocode({
            latitude: res.latitude,
            longitude: res.longitude,
            forQuery: addressQueryForGeocoding,
          });
        })
        .catch((err) => {
          if ((err as { name?: string })?.name === "AbortError") return;
        })
        .finally(() => setIsAddressGeocoding(false));
    }, 700);

    return () => {
      controller.abort();
      window.clearTimeout(timer);
    };
  }, [addressIsGeocodable, addressQueryForGeocoding, addressGeocode?.forQuery]);

  // Pin↔address mismatch: only computable when we have both sides.
  const pinCoords = useMemo(() => {
    const latitude = Number(lat);
    const longitude = Number(lng);
    if (!Number.isFinite(latitude) || !Number.isFinite(longitude) || !lat || !lng) {
      return null;
    }
    return { latitude, longitude };
  }, [lat, lng]);

  const mismatchKm: number | null = useMemo(() => {
    if (!pinCoords || !addressGeocode) return null;
    return haversineKm(pinCoords, addressGeocode);
  }, [pinCoords, addressGeocode]);

  const showMismatchWarning =
    mismatchKm != null && mismatchKm > PIN_ADDRESS_WARN_KM;
  const blockSaveForMismatch =
    mismatchKm != null && mismatchKm > PIN_ADDRESS_BLOCK_KM && !mismatchOverride;

  /** Move the pin to the forward-geocoded coordinates of the typed address. */
  const movePinToAddress = useCallback(() => {
    if (!addressGeocode) return;
    setLat(addressGeocode.latitude.toFixed(6));
    setLng(addressGeocode.longitude.toFixed(6));
    setMismatchOverride(false);
  }, [addressGeocode]);

  /** Overwrite structured address with the reverse-geocode of the dropped pin. */
  const copyAddressFromPin = useCallback(() => {
    const parsed = pinAddress?.parsed;
    if (!parsed) return;
    setStreet(parsed.street);
    setCity(parsed.city);
    setAddrState(parsed.state);
    setZipCode(parsed.zipCode);
    setCountry(parsed.country);
    setMismatchOverride(false);
  }, [pinAddress]);

  // Reset override on any change to address fields too — same rationale as
  // the pin-change reset above.
  const addressFieldsKey = `${street}|${city}|${addrState}|${zipCode}|${country}`;
  const previousAddressFieldsKey = useRef(addressFieldsKey);
  useEffect(() => {
    if (previousAddressFieldsKey.current !== addressFieldsKey) {
      previousAddressFieldsKey.current = addressFieldsKey;
      setMismatchOverride(false);
    }
  }, [addressFieldsKey]);

  const locationMutation = useMutation({
    mutationFn: () => {
      const latitude = Number(lat);
      const longitude = Number(lng);
      if (Number.isNaN(latitude) || Number.isNaN(longitude)) {
        throw new Error("Enter valid latitude and longitude.");
      }
      // Same trust gate as Lock address: refuse to save a pin that
      // points to a wildly different place than the typed address
      // unless the host has explicitly overridden it.
      if (blockSaveForMismatch) {
        throw new Error(
          `Pin is ${Math.round(mismatchKm!)} km from the typed address. Move the pin closer or override the mismatch.`,
        );
      }
      return listingApi.setApproxLocation(id, { latitude, longitude });
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
      if (blockSaveForMismatch) {
        throw new Error(
          `Address is ${Math.round(mismatchKm!)} km from the pin. Move the pin to match or override the mismatch.`,
        );
      }
      return listingApi.lockAddress(id, {
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

  // Clear the "saved" confirmations as soon as the host edits again, so the
  // success state always refers to what's actually persisted.
  const resetLocationSuccess = locationMutation.reset;
  useEffect(() => {
    resetLocationSuccess();
  }, [lat, lng, resetLocationSuccess]);

  const resetLockSuccess = lockAddressMutation.reset;
  useEffect(() => {
    resetLockSuccess();
  }, [addressFieldsKey, jurisdictionCode, resetLockSuccess]);

  const hasLocation = listing.latitude != null && listing.longitude != null;
  const isEditable = listing.status === "Draft" || listing.status === "Denied";
  const canEditApproxLocation = !readOnly && isEditable;
  const canLockPreciseAddress =
    !readOnly && (isEditable || listing.status === "Published");
  const showPreciseAddressCard =
    isEditable || listing.status === "Published" || Boolean(listing.preciseAddress);

  return (
    <div className="grid gap-6">
      {lockAddressMutation.isError && (
        <Alert variant="destructive">
          <AlertDescription>
            {getApiErrorMessage(lockAddressMutation.error, "Failed to lock address.")}
          </AlertDescription>
        </Alert>
      )}

      <Card id="location" className="scroll-mt-24">
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
            Search for an address, click on the map to drop a pin, or enter
            coordinates manually. The pin and the precise address below stay
            in sync — we'll warn you if they drift apart so tenants always
            see a consistent location. Tenants see only the general area
            until the exact address is shared after activation.
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
                    if (canEditApproxLocation) void handleGeocode();
                  }
                }}
                placeholder="e.g. 123 Main St, San Francisco, CA"
                className="flex-1"
                disabled={!canEditApproxLocation}
              />
              <Button
                type="button"
                variant="outline"
                disabled={!canEditApproxLocation || isGeocoding || !addressQuery.trim()}
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
                onClick={canEditApproxLocation ? handleMapClick : () => {}}
              />
            </Suspense>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1.5">
              <Label htmlFor="lat">Latitude</Label>
              <Input
                id="lat"
                value={lat}
                onChange={(e) => setLat(e.target.value)}
                placeholder="37.7749"
                disabled={!canEditApproxLocation}
              />
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="lng">Longitude</Label>
              <Input
                id="lng"
                value={lng}
                onChange={(e) => setLng(e.target.value)}
                placeholder="-122.4194"
                disabled={!canEditApproxLocation}
              />
            </div>
          </div>

          {showMismatchWarning && (
            <PinAddressMismatchWarning
              distanceKm={mismatchKm!}
              pinDisplayName={pinAddress?.displayName ?? null}
              typedDisplayName={addressQueryForGeocoding}
              onMovePinToAddress={movePinToAddress}
              onCopyAddressFromPin={copyAddressFromPin}
              busy={isReverseGeocoding || isAddressGeocoding}
            />
          )}

          {blockSaveForMismatch && (
            <label className="flex items-start gap-2 text-xs text-muted-foreground">
              <input
                type="checkbox"
                className="mt-0.5"
                checked={mismatchOverride}
                onChange={(e) => setMismatchOverride(e.target.checked)}
              />
              <span>
                I confirm the pin and the typed address are intentionally far
                apart (e.g. compound spans multiple districts). I understand
                tenants will see both signals.
              </span>
            </label>
          )}

          {locationMutation.isError && (
            <p className="text-sm text-destructive">
              {getApiErrorMessage(locationMutation.error, "Failed to save location.")}
            </p>
          )}
          {locationMutation.isSuccess && (
            <div className="flex items-center gap-2 rounded-md border border-success/40 bg-success/10 p-3 text-sm font-medium text-success">
              <CheckCircle2 className="h-4 w-4 shrink-0" />
              Location saved — tenants will see this general area on the map.
            </div>
          )}
          {canEditApproxLocation && (
            <Button
              type="button"
              variant={locationMutation.isSuccess ? "outline" : "secondary"}
              disabled={
                locationMutation.isPending ||
                !lat ||
                !lng ||
                blockSaveForMismatch
              }
              onClick={() => locationMutation.mutate()}
              title={
                blockSaveForMismatch
                  ? "Resolve the pin/address mismatch above first."
                  : undefined
              }
            >
              {locationMutation.isPending ? (
                "Saving..."
              ) : locationMutation.isSuccess ? (
                <>
                  <CheckCircle2 className="h-4 w-4 text-success" />
                  Location saved
                </>
              ) : (
                "Save location"
              )}
            </Button>
          )}
        </CardContent>
      </Card>

      {showPreciseAddressCard && (
        <Card id="precise-address" className="scroll-mt-24">
          <CardHeader>
            <CardTitle className="text-lg flex items-center gap-2">
              <Lock className="h-5 w-5" />
              Precise address
              {listing.preciseAddress ? (
                <Badge variant="secondary" className="ml-2">
                  <CheckCircle2 className="h-3 w-3 mr-1" />
                  Locked
                </Badge>
              ) : (
                isEditable && (
                  <Badge variant="destructive" className="ml-2">Required</Badge>
                )
              )}
            </CardTitle>
            <CardDescription>
              Lock the full address before submitting for review. The city
              becomes part of the binding booking agreement, so it can&apos;t
              be left blank. The exact street address stays private and is
              shared only with confirmed tenants.
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
                <Input
                  id="street"
                  value={street}
                  onChange={(e) => setStreet(e.target.value)}
                  placeholder="123 Main St"
                  disabled={!canLockPreciseAddress}
                />
              </div>
              <div className="space-y-1.5">
                <Label htmlFor="city">City</Label>
                <Input
                  id="city"
                  value={city}
                  onChange={(e) => setCity(e.target.value)}
                  placeholder="San Francisco"
                  disabled={!canLockPreciseAddress}
                />
              </div>
              <div className="space-y-1.5">
                <Label htmlFor="addrState">State</Label>
                <Input
                  id="addrState"
                  value={addrState}
                  onChange={(e) => setAddrState(e.target.value)}
                  placeholder="CA"
                  disabled={!canLockPreciseAddress}
                />
              </div>
              <div className="space-y-1.5">
                <Label htmlFor="zipCode">ZIP Code</Label>
                <Input
                  id="zipCode"
                  value={zipCode}
                  onChange={(e) => setZipCode(e.target.value)}
                  placeholder="94102"
                  disabled={!canLockPreciseAddress}
                />
              </div>
              <div className="space-y-1.5">
                <Label htmlFor="country">Country</Label>
                <Input
                  id="country"
                  value={country}
                  onChange={(e) => setCountry(e.target.value)}
                  placeholder="US"
                  disabled={!canLockPreciseAddress}
                />
              </div>
              <div className="space-y-1.5">
                <Label htmlFor="jurisdictionCode">Jurisdiction code (optional)</Label>
                <Input
                  id="jurisdictionCode"
                  value={jurisdictionCode}
                  onChange={(e) => setJurisdictionCode(e.target.value)}
                  placeholder="US-CA"
                  disabled={!canLockPreciseAddress}
                />
              </div>
            </div>

            {/* Forward-geocode helper — keeps the host one click away
                from realigning the pin to the typed address. We only
                show it when the form has enough information to be worth
                geocoding, otherwise it produces "no results" noise. */}
            {canLockPreciseAddress && addressIsGeocodable && (
              <div className="flex flex-col gap-1 rounded-md border bg-muted/30 p-3 sm:flex-row sm:items-center sm:justify-between">
                <p className="text-xs text-muted-foreground">
                  {addressGeocode
                    ? mismatchKm == null || mismatchKm <= PIN_ADDRESS_WARN_KM
                      ? "Pin and address agree — looks good."
                      : `Pin is ${Math.round(mismatchKm)} km from this address.`
                    : isAddressGeocoding
                      ? "Resolving this address on the map…"
                      : "Couldn't locate this address on the map. Double-check the spelling."}
                </p>
                {canEditApproxLocation && (
                  <Button
                    type="button"
                    size="sm"
                    variant="outline"
                    disabled={!addressGeocode || isAddressGeocoding}
                    onClick={movePinToAddress}
                    className="self-start sm:self-auto"
                  >
                    <MapPin className="h-3.5 w-3.5" />
                    Set pin from this address
                  </Button>
                )}
              </div>
            )}

            {canLockPreciseAddress && showMismatchWarning && (
              <PinAddressMismatchWarning
                distanceKm={mismatchKm!}
                pinDisplayName={pinAddress?.displayName ?? null}
                typedDisplayName={addressQueryForGeocoding}
                onMovePinToAddress={movePinToAddress}
                onCopyAddressFromPin={copyAddressFromPin}
                busy={isReverseGeocoding || isAddressGeocoding}
              />
            )}

            {canLockPreciseAddress && blockSaveForMismatch && (
              <label className="flex items-start gap-2 text-xs text-muted-foreground">
                <input
                  type="checkbox"
                  className="mt-0.5"
                  checked={mismatchOverride}
                  onChange={(e) => setMismatchOverride(e.target.checked)}
                />
                <span>
                  I confirm this address belongs with the current pin and want
                  to lock it anyway.
                </span>
              </label>
            )}

            {lockAddressMutation.isError && (
              <p className="text-sm text-destructive">
                {getApiErrorMessage(lockAddressMutation.error, "Failed to lock address.")}
              </p>
            )}
            {lockAddressMutation.isSuccess && (
              <div className="flex items-center gap-2 rounded-md border border-success/40 bg-success/10 p-3 text-sm font-medium text-success">
                <CheckCircle2 className="h-4 w-4 shrink-0" />
                Address locked and saved. It stays private until a booking is confirmed.
              </div>
            )}
            {canLockPreciseAddress && (
              <Button
                type="button"
                variant={lockAddressMutation.isSuccess ? "outline" : "secondary"}
                disabled={lockAddressMutation.isPending || blockSaveForMismatch}
                onClick={() => lockAddressMutation.mutate()}
                title={
                  blockSaveForMismatch
                    ? "Resolve the pin/address mismatch above first."
                    : undefined
                }
              >
                {lockAddressMutation.isPending ? (
                  "Locking..."
                ) : lockAddressMutation.isSuccess ? (
                  <>
                    <CheckCircle2 className="h-4 w-4 text-success" />
                    Address locked
                  </>
                ) : (
                  "Lock address"
                )}
              </Button>
            )}
          </CardContent>
        </Card>
      )}
    </div>
  );
}
