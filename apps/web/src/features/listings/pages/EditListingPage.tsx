import { useParams, Link, useNavigate } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useCallback, useEffect, useMemo, useRef, useState, lazy, Suspense } from "react";
import {
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
import { PinAddressMismatchWarning } from "@/features/listings/components/PinAddressMismatchWarning";
import {
  forwardGeocode,
  reverseGeocode,
  haversineKm,
  structuredAddressToQuery,
  isAddressGeocodable,
  type ParsedAddress,
} from "@/features/listings/lib/geocoding";

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
import { useAuthStore } from "@/app/auth/authStore";
import {
  computeProfileCompleteness,
  MIN_HOST_PROFILE_COMPLETENESS,
} from "@/features/auth/lib/profileCompleteness";
import { roles } from "@/app/auth/roles";
import { listingApi } from "@/features/listings/services/listingApi";
import { ListingForm } from "@/features/listings/components/ListingForm";
import { useListingDefinitions } from "@/features/listings/hooks/useListingDefinitions";
import { listingDetailsToFormValues } from "@/features/listings/lib/mapListingToForm";
import { toUpdateListingRequest } from "@/features/listings/lib/toListingRequests";
import type { ListingFormValues } from "@/features/listings/lib/listingFormSchema";
import { BackLink } from "@/components/shared/BackLink";
import { getApiErrorMessage } from "@/api/errors";
import { Loader } from "@/components/shared/Loader";
import { HostPayoutReadinessNotice } from "@/components/shared/HostPayoutReadinessNotice";
import { useHostPayoutReadiness } from "@/features/host-onboarding/hooks/useHostStripe";
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
  const navigate = useNavigate();
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
      // Same trust gate as Lock address: refuse to save a pin that
      // points to a wildly different place than the typed address
      // unless the host has explicitly overridden it.
      if (blockSaveForMismatch) {
        throw new Error(
          `Pin is ${Math.round(mismatchKm!)} km from the typed address. Move the pin closer or override the mismatch.`,
        );
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
      if (blockSaveForMismatch) {
        throw new Error(
          `Address is ${Math.round(mismatchKm!)} km from the pin. Move the pin to match or override the mismatch.`,
        );
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

  const { ready: payoutsReady, settled: payoutSettled } = useHostPayoutReadiness();

  // ── Derived state (safe before early returns; falls back when listing is null) ──
  const hasLocation = listing ? listing.latitude != null && listing.longitude != null : false;
  const hasPreciseAddress = listing ? Boolean(listing.preciseAddress) : false;
  const hasPhotos = listing ? listing.photos.length > 0 : false;
  const hasDescription = listing ? (listing.description ?? "").trim().length > 0 : false;
  const hasRent = listing ? (listing.monthlyRentCents ?? 0) > 0 : false;
  const isDraft = listing?.status === "Draft";
  const isDenied = listing?.status === "Denied";
  const isInReview = listing?.status === "InReview";
  const isEditable = isDraft || isDenied;

  // Mirror the server-side rule: SubmitForReview() requires Draft|Denied +
  // ApproxGeoPoint + a locked precise address (so the binding agreement never
  // seals with a blank city — Listing.PreciseAddressRequired), and the listing
  // can only go live once the host has a payout destination
  // (Listing.PayoutSetupRequired). Don't block while the payout lookup is
  // still in flight so we never flash a false "blocked" state.
  const payoutBlocks = payoutSettled && !payoutsReady;

  // Mirror the server-side host-profile gate: a faceless host can't go live,
  // because guests need to see who they're renting from before authorising a
  // payment (Listing.HostProfileIncomplete). Computed from the signed-in user's
  // profile with the same field set the backend enforces. Memoised so the
  // derived `canSubmit` stays referentially stable for the submit callback.
  const profileCompleteness = useMemo(
    () => computeProfileCompleteness(user),
    [user],
  );
  const profileComplete = profileCompleteness.meetsListingThreshold;

  const canSubmit =
    isEditable && hasLocation && hasPreciseAddress && !payoutBlocks && profileComplete;
  const submitLabel = isDenied ? "Resubmit for review" : "Submit for review";
  const submitBlockedReason = !listing
    ? ""
    : !isEditable
      ? isInReview
        ? "Listing is being reviewed by an admin."
        : `Already ${listing.status.toLowerCase()}.`
      : !hasLocation
        ? "Set the approximate location below before submitting."
        : !hasPreciseAddress
          ? "Add the full property address (including city) below before submitting."
          : payoutBlocks
            ? "Set up your payout details before submitting this listing."
            : !profileComplete
              ? `Complete at least ${MIN_HOST_PROFILE_COMPLETENESS}% of your host profile before submitting (you're at ${profileCompleteness.percent}%).`
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
        <BackLink fallbackTo="/app/listings" label="Back to my listings" />
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
          <BackLink fallbackTo="/app/listings" label="My listings" className="mb-2" />
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

      {isEditable && (
        <HostPayoutReadinessNotice
          message={
            <>
              You haven&apos;t set up payouts yet. Guests pay through Lagedra, so
              this listing can only go live once there&apos;s a payout
              destination for the rent and deposit.{" "}
              <Link to="/app/payout-setup" className="font-medium underline">
                Set up payouts
              </Link>{" "}
              first, then submit for review.
            </>
          }
        />
      )}

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
              done={hasPreciseAddress}
              required
              label="Add the full property address"
              detail="The city becomes part of the binding booking agreement, so the address (including city) is required. It stays private and is only shared with confirmed tenants."
              jumpLabel="Add address"
              onJump={() => scrollToId("precise-address")}
            />
            <ChecklistRow
              done={profileComplete}
              required
              label={`Complete your host profile (${profileCompleteness.percent}%)`}
              detail={
                profileComplete
                  ? "Guests can see who they're renting from."
                  : `Guests need to see who they're renting from before authorising a payment. Reach at least ${MIN_HOST_PROFILE_COMPLETENESS}%${
                      profileCompleteness.missing.length > 0
                        ? ` — add: ${profileCompleteness.missing.join(", ")}.`
                        : "."
                    }`
              }
              jumpLabel="Edit profile"
              onJump={() => navigate("/profile")}
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
              <p className="text-sm text-destructive">{(locationMutation.error as Error).message}</p>
            )}
            <Button
              type="button"
              variant="secondary"
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
              {locationMutation.isPending ? "Saving..." : "Save location"}
            </Button>
          </CardContent>
        </Card>

        {(isEditable || listing.status === "Published" || listing.preciseAddress) && (
          <Card id="precise-address" className="lg:col-span-2 scroll-mt-24">
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

              {/* Forward-geocode helper — keeps the host one click away
                  from realigning the pin to the typed address. We only
                  show it when the form has enough information to be worth
                  geocoding, otherwise it produces "no results" noise. */}
              {addressIsGeocodable && (
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
                </div>
              )}

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
                    I confirm this address belongs with the current pin and want
                    to lock it anyway.
                  </span>
                </label>
              )}

              {lockAddressMutation.isError && (
                <p className="text-sm text-destructive">{(lockAddressMutation.error as Error).message}</p>
              )}
              <Button
                type="button"
                variant="secondary"
                disabled={lockAddressMutation.isPending || blockSaveForMismatch}
                onClick={() => lockAddressMutation.mutate()}
                title={
                  blockSaveForMismatch
                    ? "Resolve the pin/address mismatch above first."
                    : undefined
                }
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
