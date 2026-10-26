import { useCallback, useEffect, useMemo, useRef, useState, lazy, Suspense } from "react";
import { useSearchParams } from "react-router-dom";
import {
  Search,
  SlidersHorizontal,
  X,
  ChevronLeft,
  ChevronRight,
  Map,
  List,
  DollarSign,
  Bed,
  Bath,
  Users,
  Home,
  Calendar,
  Sparkles,
  ShieldCheck,
  Info,
} from "lucide-react";
import { useListings } from "@/features/listings/hooks/useListings";
import { useListingDefinitions } from "@/features/listings/hooks/useListingDefinitions";
import { ListingCard } from "@/features/listings/components/ListingCard";
import { PlaceSearchField } from "@/features/listings/components/PlaceSearchField";
import type { PlaceSuggestion } from "@/features/listings/lib/placeSearch";
import type { MapBounds } from "@/features/listings/components/ListingMap";
import {
  applyLocationToSearchParams,
  geocodePlace,
  radiusBounds,
  type GeocodedPlace,
} from "@/features/listings/lib/placeSearch";
import type {
  PropertyType,
  SearchListingsParams,
  SearchListingsSortBy,
} from "@/api/types";
import { Button } from "@/components/ui/button";
import { DatePicker } from "@/components/ui/date-picker";
import { NumberInput } from "@/components/ui/number-input";
import { Select } from "@/components/ui/select";
import { Badge } from "@/components/ui/badge";
import { Separator } from "@/components/ui/separator";
import { Loader } from "@/components/shared/Loader";
import { EmptyState } from "@/components/shared/EmptyState";
import { DynamicIcon } from "@/features/listings/components/DynamicIcon";
import { cn } from "@/lib/utils";

const ListingMap = lazy(() =>
  import("@/features/listings/components/ListingMap").then((m) => ({
    default: m.ListingMap,
  })),
);

const PAGE_SIZE = 20;
const MAP_PAGE_SIZE = 100;

const propertyTypes: PropertyType[] = [
  "Apartment",
  "House",
  "Condo",
  "Townhouse",
  "Studio",
  "Loft",
  "Villa",
  "Cottage",
  "Cabin",
];

const sortOptions: { value: SearchListingsSortBy; label: string }[] = [
  { value: "Newest", label: "Newest first" },
  { value: "PriceAsc", label: "Price: low to high" },
  { value: "PriceDesc", label: "Price: high to low" },
  { value: "Distance", label: "Distance" },
];

type FlyToTarget = { lat: number; lng: number; zoom?: number; bounds?: MapBounds };

export const SearchPage = () => {
  const [searchParams] = useSearchParams();
  const urlKeyword = searchParams.get("keyword")?.trim() ?? "";
  const urlLat = Number(searchParams.get("latitude"));
  const urlLng = Number(searchParams.get("longitude"));
  const urlRadius = Number(searchParams.get("radiusKm"));
  const urlSwLat = Number(searchParams.get("swLat"));
  const urlSwLng = Number(searchParams.get("swLng"));
  const urlNeLat = Number(searchParams.get("neLat"));
  const urlNeLng = Number(searchParams.get("neLng"));
  const urlHasBox = [urlSwLat, urlSwLng, urlNeLat, urlNeLng].every((n) => Number.isFinite(n));
  const urlHasCoords = Number.isFinite(urlLat) && Number.isFinite(urlLng);
  const urlKm = Number.isFinite(urlRadius) && urlRadius > 0 ? urlRadius : 25;
  const urlBounds: MapBounds | null = urlHasBox
    ? {
        swLat: urlSwLat,
        swLng: urlSwLng,
        neLat: urlNeLat,
        neLng: urlNeLng,
        centerLat: urlHasCoords ? urlLat : (urlSwLat + urlNeLat) / 2,
        centerLng: urlHasCoords ? urlLng : (urlSwLng + urlNeLng) / 2,
      }
    : urlHasCoords
      ? radiusBounds(urlLat, urlLng, urlKm)
      : null;

  const [keyword, setKeyword] = useState(urlKeyword);
  const [searchKeyword, setSearchKeyword] = useState(urlKeyword);
  const [propertyType, setPropertyType] = useState<PropertyType | "">("");
  const [minBedrooms, setMinBedrooms] = useState<string>("");
  const [minBathrooms, setMinBathrooms] = useState<string>("");
  const [guests, setGuests] = useState<string>("");
  const [minPrice, setMinPrice] = useState<string>("");
  const [maxPrice, setMaxPrice] = useState<string>("");
  const [minStayDays, setMinStayDays] = useState<string>("");
  const [maxStayDays, setMaxStayDays] = useState<string>("");
  const [availableFrom, setAvailableFrom] = useState<string>("");
  const [availableTo, setAvailableTo] = useState<string>("");
  const [amenityIds, setAmenityIds] = useState<string[]>([]);
  const [safetyDeviceIds, setSafetyDeviceIds] = useState<string[]>([]);
  const [considerationIds, setConsiderationIds] = useState<string[]>([]);
  const [sortBy, setSortBy] = useState<SearchListingsSortBy>("Newest");
  const [page, setPage] = useState(1);
  const [showFilters, setShowFilters] = useState(false);

  const [showMap, setShowMap] = useState(Boolean(urlKeyword) || urlBounds != null);
  const [searchOnMove, setSearchOnMove] = useState(true);
  const [mapBounds, setMapBounds] = useState<MapBounds | null>(null);
  const [placeBounds, setPlaceBounds] = useState<MapBounds | null>(urlBounds);
  const [placeResolved, setPlaceResolved] = useState(urlBounds != null);
  const [geoPending, setGeoPending] = useState(Boolean(urlKeyword) && urlBounds == null);
  const [radiusAnchor, setRadiusAnchor] = useState<{
    lat: number;
    lng: number;
    radiusKm: number;
  } | null>(urlHasCoords ? { lat: urlLat, lng: urlLng, radiusKm: urlKm } : null);
  const [flyTo, setFlyTo] = useState<FlyToTarget | null>(
    urlBounds
      ? {
          lat: urlHasCoords ? urlLat : urlBounds.centerLat,
          lng: urlHasCoords ? urlLng : urlBounds.centerLng,
          zoom: 12,
          bounds: urlBounds,
        }
      : null,
  );
  const [highlightedId, setHighlightedId] = useState<string | null>(null);

  const cardRefs = useRef<Record<string, HTMLDivElement | null>>({});
  // Ignore map move events until the camera finishes flying to the place.
  // The map otherwise reports its default US view first and that empty box
  // replaces the city, so the first search returns nothing.
  const suspendBoundsRef = useRef(Boolean(urlKeyword) || urlBounds != null);
  const searchGen = useRef(0);
  const defs = useListingDefinitions();

  const resumeMapBounds = useCallback(() => {
    suspendBoundsRef.current = false;
  }, []);

  const applyPlace = useCallback((place: GeocodedPlace) => {
    suspendBoundsRef.current = true;
    setMapBounds(null);
    setPlaceResolved(true);
    setPlaceBounds(place.bounds);
    setRadiusAnchor(null);
    setFlyTo({ lat: place.lat, lng: place.lng, zoom: 12, bounds: place.bounds });
    setShowMap(true);
  }, []);

  const urlPropertyType = searchParams.get("propertyType");
  const urlGuests = searchParams.get("guests");
  const urlFrom = searchParams.get("availableFrom");
  const urlTo = searchParams.get("availableTo");
  const urlSort = searchParams.get("sortBy");

  useEffect(() => {
    if (urlFrom) setAvailableFrom(urlFrom);
    if (urlTo) setAvailableTo(urlTo);
    if (urlSort && sortOptions.some((o) => o.value === urlSort)) {
      setSortBy(urlSort as SearchListingsSortBy);
    }
    if (urlPropertyType && propertyTypes.includes(urlPropertyType as PropertyType)) {
      setPropertyType(urlPropertyType as PropertyType);
    }
    const gNum = urlGuests ? Number(urlGuests) : NaN;
    if (Number.isInteger(gNum) && gNum > 0) {
      setGuests(String(gNum));
    }
  }, [urlFrom, urlTo, urlSort, urlPropertyType, urlGuests]);

  useEffect(() => {
    const hasBox = [urlSwLat, urlSwLng, urlNeLat, urlNeLng].every((n) => Number.isFinite(n));
    const hasCoords = Number.isFinite(urlLat) && Number.isFinite(urlLng);
    const bounds: MapBounds | null = hasBox
      ? {
          swLat: urlSwLat,
          swLng: urlSwLng,
          neLat: urlNeLat,
          neLng: urlNeLng,
          centerLat: hasCoords ? urlLat : (urlSwLat + urlNeLat) / 2,
          centerLng: hasCoords ? urlLng : (urlSwLng + urlNeLng) / 2,
        }
      : hasCoords
        ? radiusBounds(urlLat, urlLng, urlKm)
        : null;
    if (!urlKeyword && !bounds) return;

    const gen = ++searchGen.current;
    setKeyword(urlKeyword);
    setSearchKeyword(urlKeyword);
    setPage(1);
    setMapBounds(null);
    if (bounds) {
      suspendBoundsRef.current = true;
      setPlaceResolved(true);
      setPlaceBounds(bounds);
      setGeoPending(false);
      setShowMap(true);
      setFlyTo({
        lat: hasCoords ? urlLat : bounds.centerLat,
        lng: hasCoords ? urlLng : bounds.centerLng,
        zoom: 12,
        bounds,
      });
      return;
    }

    suspendBoundsRef.current = true;
    setPlaceResolved(false);
    setPlaceBounds(null);
    setGeoPending(true);
    setShowMap(true);
    let cancelled = false;
    void geocodePlace(urlKeyword).then((place) => {
      if (cancelled || gen !== searchGen.current) return;
      if (place) applyPlace(place);
      else suspendBoundsRef.current = false;
      setGeoPending(false);
    });
    return () => {
      cancelled = true;
    };
  }, [urlKeyword, urlLat, urlLng, urlKm, urlSwLat, urlSwLng, urlNeLat, urlNeLng, applyPlace]);

  const pageSize = showMap ? MAP_PAGE_SIZE : PAGE_SIZE;

  const params = useMemo<SearchListingsParams>(() => {
    const p: SearchListingsParams = { page, pageSize, sortBy };
    if (propertyType) p.propertyType = propertyType;
    if (minBedrooms) p.minBedrooms = Number(minBedrooms);
    if (minBathrooms) p.minBathrooms = Number(minBathrooms);
    if (guests) p.minGuests = Number(guests);
    const minPriceN = Number(minPrice);
    if (minPrice && !Number.isNaN(minPriceN) && minPriceN > 0) p.minPriceCents = minPriceN * 100;
    const maxPriceN = Number(maxPrice);
    if (maxPrice && !Number.isNaN(maxPriceN) && maxPriceN > 0) p.maxPriceCents = maxPriceN * 100;
    if (minStayDays) p.minStayDays = Number(minStayDays);
    if (maxStayDays) p.maxStayDays = Number(maxStayDays);
    if (availableFrom) p.availableFrom = availableFrom;
    if (availableTo) p.availableTo = availableTo;
    if (amenityIds.length > 0) p.amenityIds = amenityIds;
    if (safetyDeviceIds.length > 0) p.safetyDeviceIds = safetyDeviceIds;
    if (considerationIds.length > 0) p.considerationIds = considerationIds;
    return applyLocationToSearchParams(p, {
      keyword: searchKeyword,
      placeResolved,
      userMapBounds: mapBounds,
      placeBounds,
      radiusAnchor,
    });
  }, [
    searchKeyword,
    placeResolved,
    propertyType,
    minBedrooms,
    minBathrooms,
    guests,
    minPrice,
    maxPrice,
    minStayDays,
    maxStayDays,
    availableFrom,
    availableTo,
    amenityIds,
    safetyDeviceIds,
    considerationIds,
    sortBy,
    page,
    pageSize,
    mapBounds,
    placeBounds,
    radiusAnchor,
  ]);

  const { data, isLoading, isError } = useListings(params, { enabled: !geoPending });

  const runLocationSearch = useCallback(
    async (term: string, place?: GeocodedPlace | null) => {
      const trimmed = term.trim();
      const gen = ++searchGen.current;
      suspendBoundsRef.current = true;
      setKeyword(trimmed);
      setSearchKeyword(trimmed);
      setPage(1);
      setMapBounds(null);
      setPlaceResolved(false);
      setPlaceBounds(null);
      setRadiusAnchor(null);
      if (!showMap) setShowMap(true);
      if (!trimmed) {
        suspendBoundsRef.current = false;
        return;
      }
      if (place) {
        applyPlace(place);
        return;
      }
      setGeoPending(true);
      const resolved = await geocodePlace(trimmed);
      if (gen !== searchGen.current) return;
      if (resolved) applyPlace(resolved);
      else suspendBoundsRef.current = false;
      setGeoPending(false);
    },
    [showMap, applyPlace],
  );

  const handleSearch = useCallback(
    (e: React.FormEvent) => {
      e.preventDefault();
      void runLocationSearch(keyword);
    },
    [keyword, runLocationSearch],
  );

  const handlePlaceSelect = useCallback(
    (suggestion: PlaceSuggestion) => {
      void runLocationSearch(suggestion.query, suggestion.place);
    },
    [runLocationSearch],
  );

  const handleBoundsChange = useCallback((bounds: MapBounds) => {
    setMapBounds(bounds);
    setPage(1);
  }, []);

  const handleMarkerHover = useCallback((id: string | null) => {
    setHighlightedId(id);
    if (id && cardRefs.current[id]) {
      cardRefs.current[id]!.scrollIntoView({ behavior: "smooth", block: "nearest" });
    }
  }, []);

  const handleCardHover = useCallback((id: string | null) => {
    setHighlightedId(id);
  }, []);

  const toggleMap = useCallback(() => {
    setShowMap((v) => {
      if (!v) {
        setMapBounds(null);
        setPage(1);
        // If the user already searched for a city, jump back to it instead of
        // showing the country-wide default view.
        const term = searchKeyword.trim();
        if (term) {
          void geocodePlace(term).then((place) => {
            if (place) applyPlace(place);
          });
        }
      }
      return !v;
    });
  }, [searchKeyword, applyPlace]);

  const toggleAmenity = (id: string) => {
    setAmenityIds((prev) => (prev.includes(id) ? prev.filter((x) => x !== id) : [...prev, id]));
    setPage(1);
  };
  const toggleSafetyDevice = (id: string) => {
    setSafetyDeviceIds((prev) => (prev.includes(id) ? prev.filter((x) => x !== id) : [...prev, id]));
    setPage(1);
  };
  const toggleConsideration = (id: string) => {
    setConsiderationIds((prev) =>
      prev.includes(id) ? prev.filter((x) => x !== id) : [...prev, id],
    );
    setPage(1);
  };

  const activeFilterCount =
    (propertyType ? 1 : 0) +
    (minBedrooms ? 1 : 0) +
    (minBathrooms ? 1 : 0) +
    (guests ? 1 : 0) +
    (minPrice ? 1 : 0) +
    (maxPrice ? 1 : 0) +
    (minStayDays ? 1 : 0) +
    (maxStayDays ? 1 : 0) +
    (availableFrom ? 1 : 0) +
    (availableTo ? 1 : 0) +
    amenityIds.length +
    safetyDeviceIds.length +
    considerationIds.length;

  const totalCount = data?.totalCount ?? 0;
  const totalPages = Math.ceil(totalCount / pageSize);
  const fromItem = totalCount > 0 ? (page - 1) * pageSize + 1 : 0;
  const toItem = Math.min(page * pageSize, totalCount);

  const clearAllFilters = () => {
    setPropertyType("");
    setMinBedrooms("");
    setMinBathrooms("");
    setGuests("");
    setMinPrice("");
    setMaxPrice("");
    setMinStayDays("");
    setMaxStayDays("");
    setAvailableFrom("");
    setAvailableTo("");
    setAmenityIds([]);
    setSafetyDeviceIds([]);
    setConsiderationIds([]);
    setPage(1);
  };

  const listings = data?.items ?? [];

  // Build active filter chips
  const chips: { key: string; label: string; onClear: () => void }[] = [];
  if (searchKeyword) {
    chips.push({
      key: "kw",
      label: `“${searchKeyword}”`,
      onClear: () => {
        setKeyword("");
        setSearchKeyword("");
        setPlaceResolved(false);
        setPlaceBounds(null);
        setRadiusAnchor(null);
        setPage(1);
      },
    });
  }
  if (propertyType) {
    chips.push({
      key: "type",
      label: propertyType,
      onClear: () => {
        setPropertyType("");
        setPage(1);
      },
    });
  }
  if (minBedrooms) {
    chips.push({
      key: "beds",
      label: minBedrooms === "0" ? "Studio+" : `${minBedrooms}+ beds`,
      onClear: () => {
        setMinBedrooms("");
        setPage(1);
      },
    });
  }
  if (minBathrooms) {
    chips.push({
      key: "baths",
      label: `${minBathrooms}+ baths`,
      onClear: () => {
        setMinBathrooms("");
        setPage(1);
      },
    });
  }
  if (guests) {
    chips.push({
      key: "guests",
      label: `${guests} guest${guests === "1" ? "" : "s"}`,
      onClear: () => {
        setGuests("");
        setPage(1);
      },
    });
  }
  if (minPrice || maxPrice) {
    const label =
      minPrice && maxPrice
        ? `$${minPrice}–$${maxPrice}`
        : minPrice
          ? `≥ $${minPrice}`
          : `≤ $${maxPrice}`;
    chips.push({
      key: "price",
      label,
      onClear: () => {
        setMinPrice("");
        setMaxPrice("");
        setPage(1);
      },
    });
  }
  if (minStayDays || maxStayDays) {
    const label =
      minStayDays && maxStayDays
        ? `${minStayDays}–${maxStayDays} days`
        : minStayDays
          ? `≥ ${minStayDays} days`
          : `≤ ${maxStayDays} days`;
    chips.push({
      key: "stay",
      label,
      onClear: () => {
        setMinStayDays("");
        setMaxStayDays("");
        setPage(1);
      },
    });
  }
  if (availableFrom || availableTo) {
    const label =
      availableFrom && availableTo
        ? `${availableFrom} → ${availableTo}`
        : availableFrom
          ? `From ${availableFrom}`
          : `Until ${availableTo}`;
    chips.push({
      key: "dates",
      label,
      onClear: () => {
        setAvailableFrom("");
        setAvailableTo("");
        setPage(1);
      },
    });
  }
  if (amenityIds.length > 0) {
    chips.push({
      key: "amenities",
      label: `${amenityIds.length} amenit${amenityIds.length === 1 ? "y" : "ies"}`,
      onClear: () => {
        setAmenityIds([]);
        setPage(1);
      },
    });
  }
  if (safetyDeviceIds.length > 0) {
    chips.push({
      key: "safety",
      label: `${safetyDeviceIds.length} safety device${safetyDeviceIds.length === 1 ? "" : "s"}`,
      onClear: () => {
        setSafetyDeviceIds([]);
        setPage(1);
      },
    });
  }
  if (considerationIds.length > 0) {
    chips.push({
      key: "consid",
      label: `${considerationIds.length} consideration${considerationIds.length === 1 ? "" : "s"}`,
      onClear: () => {
        setConsiderationIds([]);
        setPage(1);
      },
    });
  }

  const listContent = (
    <>
      {isLoading || geoPending ? (
        <Loader label="Searching listings..." />
      ) : isError ? (
        <div className="py-16 text-center">
          <p className="text-destructive font-medium">
            Failed to load listings. Please try again.
          </p>
        </div>
      ) : listings.length > 0 ? (
        <>
          <div
            className={cn(
              "grid gap-4",
              showMap
                ? "grid-cols-1 sm:grid-cols-2"
                : "sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6",
            )}
          >
            {listings.map((listing) => (
              <div
                key={listing.id}
                ref={(el) => {
                  cardRefs.current[listing.id] = el;
                }}
                onMouseEnter={() => handleCardHover(listing.id)}
                onMouseLeave={() => handleCardHover(null)}
                className={cn(
                  "rounded-xl transition-shadow",
                  highlightedId === listing.id && "ring-2 ring-foreground/30 shadow-lg",
                )}
              >
                <ListingCard listing={listing} />
              </div>
            ))}
          </div>

          {totalPages > 1 && (
            <div className="mt-8 flex items-center justify-center gap-2">
              <Button
                variant="outline"
                size="sm"
                disabled={page <= 1}
                onClick={() => setPage((p) => Math.max(1, p - 1))}
              >
                <ChevronLeft className="h-4 w-4" />
                Previous
              </Button>
              <div className="flex items-center gap-1">
                {Array.from({ length: Math.min(totalPages, 5) }, (_, i) => {
                  let pageNum: number;
                  if (totalPages <= 5) pageNum = i + 1;
                  else if (page <= 3) pageNum = i + 1;
                  else if (page >= totalPages - 2) pageNum = totalPages - 4 + i;
                  else pageNum = page - 2 + i;
                  return (
                    <button
                      key={pageNum}
                      onClick={() => setPage(pageNum)}
                      className={cn(
                        "h-9 w-9 rounded-lg text-sm font-medium transition-colors cursor-pointer",
                        page === pageNum
                          ? "bg-foreground text-background"
                          : "hover:bg-secondary text-muted-foreground",
                      )}
                    >
                      {pageNum}
                    </button>
                  );
                })}
              </div>
              <Button
                variant="outline"
                size="sm"
                disabled={page >= totalPages}
                onClick={() => setPage((p) => p + 1)}
              >
                Next
                <ChevronRight className="h-4 w-4" />
              </Button>
            </div>
          )}
        </>
      ) : (
        <EmptyState
          title="No listings match your filters"
          description="Try widening your price range, removing a filter, or panning the map."
        >
          {activeFilterCount > 0 && (
            <Button variant="outline" size="sm" onClick={clearAllFilters}>
              Clear all filters
            </Button>
          )}
        </EmptyState>
      )}
    </>
  );

  return (
    <div className="flex flex-col flex-1 min-h-0">
      {/* Hero / Search bar */}
      <div className="bg-gradient-to-br from-foreground to-foreground/85 text-background shrink-0">
        <div className="mx-auto max-w-7xl px-4 py-6 sm:px-6 lg:px-8">
          <div className="flex items-center justify-between gap-4 flex-wrap">
            <div className="min-w-0">
              <h1 className="text-2xl font-bold tracking-tight sm:text-3xl">
                Find your next home
              </h1>
              <p className="mt-1 text-background/70 text-sm">
                Verified mid-term rentals with trust-first protection.
              </p>
            </div>
          </div>

          <form onSubmit={handleSearch} className="mt-4 flex gap-2 max-w-2xl">
            <PlaceSearchField
              className="flex-1"
              value={keyword}
              onChange={setKeyword}
              onSelect={handlePlaceSelect}
              showPopularWhenEmpty
              placeholder="Search by city, address, ZIP, or state..."
              leadingIcon={
                <Search className="pointer-events-none absolute left-3.5 top-1/2 z-10 h-5 w-5 -translate-y-1/2 text-muted-foreground" />
              }
              inputClassName="h-11 border-0 bg-background pl-11 text-base text-foreground rounded-xl"
            />
            <Button type="submit" variant="accent" size="lg" className="rounded-xl px-6 h-11">
              Search
            </Button>
          </form>
        </div>
      </div>

      {/* Toolbar */}
      <div className="shrink-0 border-b bg-background">
        <div className={cn("px-4 py-3 sm:px-6", !showMap && "mx-auto max-w-7xl lg:px-8")}>
          <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <div className="flex items-center gap-2 flex-wrap">
              <Button
                variant={showFilters ? "default" : "outline"}
                size="sm"
                onClick={() => setShowFilters((v) => !v)}
              >
                <SlidersHorizontal className="h-4 w-4" />
                Filters
                {activeFilterCount > 0 && (
                  <Badge
                    variant="accent"
                    className="ml-1 h-5 min-w-5 rounded-full px-1.5 text-[10px] flex items-center justify-center"
                  >
                    {activeFilterCount}
                  </Badge>
                )}
              </Button>

              <Button
                variant={showMap ? "default" : "outline"}
                size="sm"
                onClick={toggleMap}
              >
                {showMap ? <List className="h-4 w-4" /> : <Map className="h-4 w-4" />}
                {showMap ? "List only" : "Map"}
              </Button>

              {showMap && (
                <label className="flex items-center gap-1.5 text-xs text-muted-foreground cursor-pointer select-none">
                  <input
                    type="checkbox"
                    checked={searchOnMove}
                    onChange={(e) => setSearchOnMove(e.target.checked)}
                    className="accent-accent h-3.5 w-3.5 cursor-pointer"
                  />
                  Search as I move the map
                </label>
              )}
            </div>

            <div className="flex items-center gap-3">
              {totalCount > 0 && (
                <p className="text-sm text-muted-foreground whitespace-nowrap">
                  {fromItem}–{toItem} of {totalCount}
                </p>
              )}
              <Select
                value={sortBy}
                onChange={(e) => {
                  setSortBy(e.target.value as SearchListingsSortBy);
                  setPage(1);
                }}
                className="w-40 h-9"
              >
                {sortOptions.map((opt) => (
                  <option key={opt.value} value={opt.value}>
                    {opt.label}
                  </option>
                ))}
              </Select>
            </div>
          </div>

          {/* Active filter chips */}
          {chips.length > 0 && (
            <div className="mt-3 flex flex-wrap items-center gap-1.5">
              {chips.map((chip) => (
                <Badge key={chip.key} variant="secondary" className="gap-1 pr-1">
                  {chip.label}
                  <button
                    type="button"
                    aria-label={`Remove ${chip.label}`}
                    onClick={chip.onClear}
                    className="rounded-full p-0.5 hover:bg-foreground/10 cursor-pointer"
                  >
                    <X className="h-3 w-3" />
                  </button>
                </Badge>
              ))}
              <button
                type="button"
                onClick={() => {
                  clearAllFilters();
                  setKeyword("");
                  setSearchKeyword("");
                  setPlaceResolved(false);
                  setPlaceBounds(null);
                  setRadiusAnchor(null);
                  setMapBounds(null);
                }}
                className="text-xs text-muted-foreground hover:text-foreground underline-offset-2 hover:underline cursor-pointer"
              >
                Clear all
              </button>
            </div>
          )}

          {/* Filter panel */}
          {showFilters && (
            <>
              <Separator className="my-3" />

              {/* Group: Property */}
              <FilterSection icon={<Home className="h-3.5 w-3.5" />} title="Property">
                <div className="grid grid-cols-2 gap-3 sm:grid-cols-3">
                  <FilterField label="Property type">
                    <Select
                      value={propertyType}
                      onChange={(e) => {
                        setPropertyType(e.target.value as PropertyType | "");
                        setPage(1);
                      }}
                      className="h-9"
                    >
                      <option value="">All types</option>
                      {propertyTypes.map((pt) => (
                        <option key={pt} value={pt}>
                          {pt}
                        </option>
                      ))}
                    </Select>
                  </FilterField>
                  <FilterField label="Min bedrooms" icon={<Bed className="h-3 w-3" />}>
                    <Select
                      value={minBedrooms}
                      onChange={(e) => {
                        setMinBedrooms(e.target.value);
                        setPage(1);
                      }}
                      className="h-9"
                    >
                      <option value="">Any</option>
                      <option value="0">Studio</option>
                      <option value="1">1+</option>
                      <option value="2">2+</option>
                      <option value="3">3+</option>
                      <option value="4">4+</option>
                    </Select>
                  </FilterField>
                  <FilterField label="Min bathrooms" icon={<Bath className="h-3 w-3" />}>
                    <Select
                      value={minBathrooms}
                      onChange={(e) => {
                        setMinBathrooms(e.target.value);
                        setPage(1);
                      }}
                      className="h-9"
                    >
                      <option value="">Any</option>
                      <option value="1">1+</option>
                      <option value="1.5">1.5+</option>
                      <option value="2">2+</option>
                      <option value="2.5">2.5+</option>
                      <option value="3">3+</option>
                    </Select>
                  </FilterField>
                  <FilterField label="Guests" icon={<Users className="h-3 w-3" />}>
                    <Select
                      value={guests}
                      onChange={(e) => {
                        setGuests(e.target.value);
                        setPage(1);
                      }}
                      className="h-9"
                    >
                      <option value="">Any</option>
                      <option value="1">1 guest</option>
                      <option value="2">2 guests</option>
                      <option value="3">3 guests</option>
                      <option value="4">4 guests</option>
                      <option value="5">5 guests</option>
                      <option value="6">6 guests</option>
                      <option value="8">8+ guests</option>
                    </Select>
                  </FilterField>
                </div>
              </FilterSection>

              {/* Group: Price & stay */}
              <FilterSection icon={<DollarSign className="h-3.5 w-3.5" />} title="Price & stay length">
                <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
                  <FilterField label="Min price / mo (USD)">
                    <div className="relative">
                      <span className="absolute left-2.5 top-1/2 -translate-y-1/2 text-xs text-muted-foreground">$</span>
                      <NumberInput
                        min={0}
                        decimalPlaces={0}
                        placeholder="Any"
                        value={minPrice === "" ? null : Number(minPrice)}
                        onValueChange={(n) => {
                          setMinPrice(n == null ? "" : String(n));
                          setPage(1);
                        }}
                        className="h-9 pl-5"
                      />
                    </div>
                  </FilterField>
                  <FilterField label="Max price / mo (USD)">
                    <div className="relative">
                      <span className="absolute left-2.5 top-1/2 -translate-y-1/2 text-xs text-muted-foreground">$</span>
                      <NumberInput
                        min={0}
                        decimalPlaces={0}
                        placeholder="Any"
                        value={maxPrice === "" ? null : Number(maxPrice)}
                        onValueChange={(n) => {
                          setMaxPrice(n == null ? "" : String(n));
                          setPage(1);
                        }}
                        className="h-9 pl-5"
                      />
                    </div>
                  </FilterField>
                  <FilterField label="Min stay (days)">
                    <Select
                      value={minStayDays}
                      onChange={(e) => {
                        setMinStayDays(e.target.value);
                        setPage(1);
                      }}
                      className="h-9"
                    >
                      <option value="">Any</option>
                      <option value="30">30+</option>
                      <option value="60">60+</option>
                      <option value="90">90+</option>
                    </Select>
                  </FilterField>
                  <FilterField label="Max stay (days)">
                    <Select
                      value={maxStayDays}
                      onChange={(e) => {
                        setMaxStayDays(e.target.value);
                        setPage(1);
                      }}
                      className="h-9"
                    >
                      <option value="">Any</option>
                      <option value="90">≤ 90</option>
                      <option value="120">≤ 120</option>
                      <option value="180">≤ 180</option>
                    </Select>
                  </FilterField>
                </div>
              </FilterSection>

              {/* Group: Dates */}
              <FilterSection icon={<Calendar className="h-3.5 w-3.5" />} title="Stay window">
                <div className="grid grid-cols-2 gap-3 sm:grid-cols-3">
                  <FilterField label="Available from">
                    <DatePicker
                      value={availableFrom}
                      onChange={(v) => {
                        setAvailableFrom(v);
                        setPage(1);
                      }}
                      max={availableTo || undefined}
                      placeholder="Any date"
                      className="h-9"
                      aria-label="Available from"
                    />
                  </FilterField>
                  <FilterField label="Available until">
                    <DatePicker
                      value={availableTo}
                      min={availableFrom || undefined}
                      onChange={(v) => {
                        setAvailableTo(v);
                        setPage(1);
                      }}
                      placeholder="Any date"
                      className="h-9"
                      aria-label="Available until"
                    />
                  </FilterField>
                  <div className="flex items-end">
                    <p className="text-[11px] text-muted-foreground inline-flex items-start gap-1">
                      <Info className="h-3 w-3 mt-0.5 shrink-0" />
                      Hosts must accept stays inside this window.
                    </p>
                  </div>
                </div>
              </FilterSection>

              {/* Group: Amenities, safety, considerations */}
              <FilterSection icon={<Sparkles className="h-3.5 w-3.5" />} title="Amenities">
                <DefinitionChips
                  items={defs.data?.amenities ?? []}
                  selected={amenityIds}
                  onToggle={toggleAmenity}
                  isLoading={defs.isLoading}
                />
              </FilterSection>

              <FilterSection icon={<ShieldCheck className="h-3.5 w-3.5" />} title="Safety devices">
                <DefinitionChips
                  items={defs.data?.safetyDevices ?? []}
                  selected={safetyDeviceIds}
                  onToggle={toggleSafetyDevice}
                  isLoading={defs.isLoading}
                />
              </FilterSection>

              <FilterSection icon={<Info className="h-3.5 w-3.5" />} title="Considerations">
                <DefinitionChips
                  items={defs.data?.considerations ?? []}
                  selected={considerationIds}
                  onToggle={toggleConsideration}
                  isLoading={defs.isLoading}
                />
              </FilterSection>

              <div className="mt-3 flex items-center justify-end gap-2">
                <Button variant="ghost" size="sm" onClick={clearAllFilters}>
                  Reset all
                </Button>
                <Button variant="default" size="sm" onClick={() => setShowFilters(false)}>
                  Done
                </Button>
              </div>
            </>
          )}
        </div>
      </div>

      {/* Content area */}
      {showMap ? (
        <div className="flex flex-1 min-h-0">
          <div className="hidden lg:block lg:w-[55%] xl:w-[50%] overflow-y-auto border-r p-4">
            {listContent}
          </div>

          <div className="flex-1 relative min-h-[50vh]">
            <Suspense
              fallback={
                <div className="h-full flex items-center justify-center">
                  <Loader label="Loading map..." />
                </div>
              }
            >
              <ListingMap
                listings={listings}
                highlightedId={highlightedId}
                onHover={handleMarkerHover}
                onBoundsChange={handleBoundsChange}
                flyTo={flyTo}
                searchOnMove={searchOnMove}
                suspendBoundsRef={suspendBoundsRef}
                onFlySettled={resumeMapBounds}
              />
            </Suspense>

            <div className="lg:hidden absolute bottom-6 left-1/2 -translate-x-1/2 z-[1000]">
              <Button
                variant="default"
                size="sm"
                className="rounded-full shadow-lg px-5"
                onClick={toggleMap}
              >
                <List className="h-4 w-4" />
                Show list
                {totalCount > 0 && (
                  <Badge variant="secondary" className="ml-1.5 text-[10px]">
                    {totalCount}
                  </Badge>
                )}
              </Button>
            </div>
          </div>
        </div>
      ) : (
        <div className="flex-1 overflow-y-auto">
          <div className="mx-auto max-w-7xl px-4 py-6 sm:px-6 lg:px-8">{listContent}</div>
        </div>
      )}
    </div>
  );
};

// ── Filter UI helpers ─────────────────────────────────────────

function FilterSection({
  icon,
  title,
  children,
}: {
  icon: React.ReactNode;
  title: string;
  children: React.ReactNode;
}) {
  return (
    <div className="py-3 first:pt-0 border-t border-border/60 first:border-t-0">
      <h3 className="mb-2 flex items-center gap-1.5 text-xs font-semibold uppercase tracking-wide text-muted-foreground">
        {icon}
        {title}
      </h3>
      {children}
    </div>
  );
}

function FilterField({
  label,
  icon,
  children,
}: {
  label: string;
  icon?: React.ReactNode;
  children: React.ReactNode;
}) {
  return (
    <div className="space-y-1">
      <label className="flex items-center gap-1 text-xs font-medium text-muted-foreground">
        {icon}
        {label}
      </label>
      {children}
    </div>
  );
}

function DefinitionChips({
  items,
  selected,
  onToggle,
  isLoading,
}: {
  items: { id: string; name: string; iconKey: string }[];
  selected: string[];
  onToggle: (id: string) => void;
  isLoading: boolean;
}) {
  if (isLoading) {
    return <p className="text-xs text-muted-foreground">Loading...</p>;
  }
  if (items.length === 0) {
    return <p className="text-xs text-muted-foreground">No options available.</p>;
  }
  return (
    <div className="flex flex-wrap gap-1.5">
      {items.map((item) => {
        const checked = selected.includes(item.id);
        return (
          <button
            key={item.id}
            type="button"
            onClick={() => onToggle(item.id)}
            className={cn(
              "inline-flex items-center gap-1.5 rounded-full border px-2.5 py-1 text-xs transition-colors cursor-pointer",
              checked
                ? "border-foreground bg-foreground text-background"
                : "border-border bg-background hover:bg-muted/50",
            )}
          >
            <DynamicIcon iconKey={item.iconKey} className="h-3 w-3" />
            {item.name}
          </button>
        );
      })}
    </div>
  );
}
