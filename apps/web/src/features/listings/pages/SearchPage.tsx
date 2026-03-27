import { useState, useMemo, useCallback, useRef, lazy, Suspense } from "react";
import {
  Search,
  SlidersHorizontal,
  X,
  ChevronLeft,
  ChevronRight,
  Home,
  Building2,
  Landmark,
  Map,
  List,
} from "lucide-react";
import { useListings } from "@/features/listings/hooks/useListings";
import { ListingCard } from "@/features/listings/components/ListingCard";
import type { MapBounds } from "@/features/listings/components/ListingMap";
import type { PropertyType, SearchListingsParams, SearchListingsSortBy } from "@/api/types";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Select } from "@/components/ui/select";
import { Badge } from "@/components/ui/badge";
import { Separator } from "@/components/ui/separator";
import { Loader } from "@/components/shared/Loader";
import { EmptyState } from "@/components/shared/EmptyState";
import { cn } from "@/lib/utils";

const ListingMap = lazy(() =>
  import("@/features/listings/components/ListingMap").then((m) => ({
    default: m.ListingMap,
  })),
);

const PAGE_SIZE = 20;
const MAP_PAGE_SIZE = 100;

const propertyTypes: { value: PropertyType; label: string; icon: typeof Home }[] = [
  { value: "Apartment", label: "Apartment", icon: Building2 },
  { value: "House", label: "House", icon: Home },
  { value: "Condo", label: "Condo", icon: Landmark },
  { value: "Townhouse", label: "Townhouse", icon: Building2 },
  { value: "Studio", label: "Studio", icon: Home },
  { value: "Loft", label: "Loft", icon: Building2 },
  { value: "Villa", label: "Villa", icon: Home },
  { value: "Cottage", label: "Cottage", icon: Home },
  { value: "Cabin", label: "Cabin", icon: Home },
];

const sortOptions: { value: SearchListingsSortBy; label: string }[] = [
  { value: "Newest", label: "Newest first" },
  { value: "PriceAsc", label: "Price: low to high" },
  { value: "PriceDesc", label: "Price: high to low" },
  { value: "Distance", label: "Distance" },
];

async function geocodeCity(query: string): Promise<{ lat: number; lng: number } | null> {
  try {
    const url = `https://nominatim.openstreetmap.org/search?q=${encodeURIComponent(query)}&format=json&limit=1`;
    const res = await fetch(url, {
      headers: { "Accept-Language": "en" },
    });
    const data = await res.json();
    if (data.length > 0) {
      return { lat: parseFloat(data[0].lat), lng: parseFloat(data[0].lon) };
    }
  } catch {
    /* geocoding is best-effort */
  }
  return null;
}

export const SearchPage = () => {
  const [keyword, setKeyword] = useState("");
  const [searchKeyword, setSearchKeyword] = useState("");
  const [propertyType, setPropertyType] = useState<PropertyType | "">("");
  const [minBedrooms, setMinBedrooms] = useState<string>("");
  const [minBathrooms, setMinBathrooms] = useState<string>("");
  const [minPrice, setMinPrice] = useState<string>("");
  const [maxPrice, setMaxPrice] = useState<string>("");
  const [minStayDays, setMinStayDays] = useState<string>("");
  const [maxStayDays, setMaxStayDays] = useState<string>("");
  const [sortBy, setSortBy] = useState<SearchListingsSortBy>("Newest");
  const [page, setPage] = useState(1);
  const [showFilters, setShowFilters] = useState(false);

  const [showMap, setShowMap] = useState(false);
  const [searchOnMove, setSearchOnMove] = useState(true);
  const [mapBounds, setMapBounds] = useState<MapBounds | null>(null);
  const [flyTo, setFlyTo] = useState<{ lat: number; lng: number; zoom?: number } | null>(null);
  const [highlightedId, setHighlightedId] = useState<string | null>(null);

  const cardRefs = useRef<Record<string, HTMLDivElement | null>>({});

  const pageSize = showMap ? MAP_PAGE_SIZE : PAGE_SIZE;

  const params = useMemo<SearchListingsParams>(() => {
    const p: SearchListingsParams = {
      page,
      pageSize,
      sortBy,
    };
    if (searchKeyword.trim()) p.keyword = searchKeyword.trim();
    if (propertyType) p.propertyType = propertyType as PropertyType;
    if (minBedrooms) p.minBedrooms = Number(minBedrooms);
    if (minBathrooms) p.minBathrooms = Number(minBathrooms);
    if (minPrice) p.minPriceCents = Number(minPrice) * 100;
    if (maxPrice) p.maxPriceCents = Number(maxPrice) * 100;
    if (minStayDays) p.minStayDays = Number(minStayDays);
    if (maxStayDays) p.maxStayDays = Number(maxStayDays);
    if (showMap && mapBounds) {
      p.swLat = mapBounds.swLat;
      p.swLng = mapBounds.swLng;
      p.neLat = mapBounds.neLat;
      p.neLng = mapBounds.neLng;
      p.latitude = mapBounds.centerLat;
      p.longitude = mapBounds.centerLng;
    }
    return p;
  }, [searchKeyword, propertyType, minBedrooms, minBathrooms, minPrice, maxPrice, minStayDays, maxStayDays, sortBy, page, pageSize, showMap, mapBounds]);

  const { data, isLoading, isError } = useListings(params);

  const handleSearch = useCallback(
    async (e: React.FormEvent) => {
      e.preventDefault();
      const term = keyword.trim();
      setSearchKeyword(term);
      setPage(1);

      if (term && showMap) {
        const geo = await geocodeCity(term);
        if (geo) {
          setFlyTo({ lat: geo.lat, lng: geo.lng, zoom: 12 });
        }
      }
    },
    [keyword, showMap],
  );

  const handleBoundsChange = useCallback(
    (bounds: MapBounds) => {
      setMapBounds(bounds);
      setPage(1);
    },
    [],
  );

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
      }
      return !v;
    });
  }, []);

  const activeFilterCount = [propertyType, minBedrooms, minBathrooms, minPrice, maxPrice, minStayDays, maxStayDays].filter(Boolean).length;
  const totalCount = data?.totalCount ?? 0;
  const totalPages = Math.ceil(totalCount / pageSize);
  const fromItem = totalCount > 0 ? (page - 1) * pageSize + 1 : 0;
  const toItem = Math.min(page * pageSize, totalCount);

  const clearFilters = () => {
    setPropertyType("");
    setMinBedrooms("");
    setMinBathrooms("");
    setMinPrice("");
    setMaxPrice("");
    setMinStayDays("");
    setMaxStayDays("");
    setPage(1);
  };

  const listings = data?.items ?? [];

  const listContent = (
    <>
      {isLoading ? (
        <Loader label="Searching listings..." />
      ) : isError ? (
        <div className="py-16 text-center">
          <p className="text-destructive font-medium">Failed to load listings. Please try again.</p>
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
                  if (totalPages <= 5) {
                    pageNum = i + 1;
                  } else if (page <= 3) {
                    pageNum = i + 1;
                  } else if (page >= totalPages - 2) {
                    pageNum = totalPages - 4 + i;
                  } else {
                    pageNum = page - 2 + i;
                  }
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
          title="No listings found"
          description="Try adjusting your search or filters to find what you're looking for."
        />
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
            <div className="relative flex-1">
              <Search className="absolute left-3.5 top-1/2 h-5 w-5 -translate-y-1/2 text-muted-foreground" />
              <Input
                value={keyword}
                onChange={(e) => setKeyword(e.target.value)}
                placeholder="Search by city, neighborhood, or keyword..."
                className="h-11 pl-11 bg-background text-foreground border-0 rounded-xl text-base"
              />
            </div>
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
                  <Badge variant="accent" className="ml-1 h-5 w-5 rounded-full p-0 text-[10px] flex items-center justify-center">
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

              {searchKeyword && (
                <Badge variant="secondary" className="gap-1">
                  &ldquo;{searchKeyword}&rdquo;
                  <button
                    onClick={() => { setKeyword(""); setSearchKeyword(""); setPage(1); }}
                    className="ml-0.5 cursor-pointer"
                  >
                    <X className="h-3 w-3" />
                  </button>
                </Badge>
              )}

              {propertyType && (
                <Badge variant="secondary" className="gap-1">
                  {propertyType}
                  <button onClick={() => { setPropertyType(""); setPage(1); }} className="ml-0.5 cursor-pointer">
                    <X className="h-3 w-3" />
                  </button>
                </Badge>
              )}

              {activeFilterCount > 0 && (
                <button onClick={clearFilters} className="text-xs text-muted-foreground hover:text-foreground cursor-pointer">
                  Clear all
                </button>
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
                onChange={(e) => { setSortBy(e.target.value as SearchListingsSortBy); setPage(1); }}
                className="w-40 h-9"
              >
                {sortOptions.map((opt) => (
                  <option key={opt.value} value={opt.value}>{opt.label}</option>
                ))}
              </Select>
            </div>
          </div>

          {showFilters && (
            <>
              <Separator className="my-3" />
              <div className="grid grid-cols-2 gap-3 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 pb-2">
                <div className="space-y-1.5">
                  <label className="text-xs font-medium text-muted-foreground">Property type</label>
                  <Select
                    value={propertyType}
                    onChange={(e) => { setPropertyType(e.target.value as PropertyType | ""); setPage(1); }}
                    className="h-9"
                  >
                    <option value="">All types</option>
                    {propertyTypes.map((pt) => (
                      <option key={pt.value} value={pt.value}>{pt.label}</option>
                    ))}
                  </Select>
                </div>

                <div className="space-y-1.5">
                  <label className="text-xs font-medium text-muted-foreground">Min bedrooms</label>
                  <Select
                    value={minBedrooms}
                    onChange={(e) => { setMinBedrooms(e.target.value); setPage(1); }}
                    className="h-9"
                  >
                    <option value="">Any</option>
                    <option value="0">Studio</option>
                    <option value="1">1+</option>
                    <option value="2">2+</option>
                    <option value="3">3+</option>
                    <option value="4">4+</option>
                  </Select>
                </div>

                <div className="space-y-1.5">
                  <label className="text-xs font-medium text-muted-foreground">Min bathrooms</label>
                  <Select
                    value={minBathrooms}
                    onChange={(e) => { setMinBathrooms(e.target.value); setPage(1); }}
                    className="h-9"
                  >
                    <option value="">Any</option>
                    <option value="1">1+</option>
                    <option value="1.5">1.5+</option>
                    <option value="2">2+</option>
                    <option value="2.5">2.5+</option>
                    <option value="3">3+</option>
                  </Select>
                </div>

                <div className="space-y-1.5">
                  <label className="text-xs font-medium text-muted-foreground">Min price / month</label>
                  <Select
                    value={minPrice}
                    onChange={(e) => { setMinPrice(e.target.value); setPage(1); }}
                    className="h-9"
                  >
                    <option value="">Any</option>
                    <option value="500">$500</option>
                    <option value="1000">$1,000</option>
                    <option value="1500">$1,500</option>
                    <option value="2000">$2,000</option>
                    <option value="3000">$3,000</option>
                    <option value="5000">$5,000</option>
                  </Select>
                </div>

                <div className="space-y-1.5">
                  <label className="text-xs font-medium text-muted-foreground">Max price / month</label>
                  <Select
                    value={maxPrice}
                    onChange={(e) => { setMaxPrice(e.target.value); setPage(1); }}
                    className="h-9"
                  >
                    <option value="">Any</option>
                    <option value="1000">$1,000</option>
                    <option value="2000">$2,000</option>
                    <option value="3000">$3,000</option>
                    <option value="5000">$5,000</option>
                    <option value="7500">$7,500</option>
                    <option value="10000">$10,000</option>
                  </Select>
                </div>

                <div className="space-y-1.5">
                  <label className="text-xs font-medium text-muted-foreground">Min stay (days)</label>
                  <Select
                    value={minStayDays}
                    onChange={(e) => { setMinStayDays(e.target.value); setPage(1); }}
                    className="h-9"
                  >
                    <option value="">Any</option>
                    <option value="30">30+</option>
                    <option value="60">60+</option>
                    <option value="90">90+</option>
                  </Select>
                </div>

                <div className="space-y-1.5">
                  <label className="text-xs font-medium text-muted-foreground">Max stay (days)</label>
                  <Select
                    value={maxStayDays}
                    onChange={(e) => { setMaxStayDays(e.target.value); setPage(1); }}
                    className="h-9"
                  >
                    <option value="">Any</option>
                    <option value="90">&le; 90</option>
                    <option value="120">&le; 120</option>
                    <option value="180">&le; 180</option>
                  </Select>
                </div>

                <div className="flex items-end">
                  <Button variant="outline" size="sm" onClick={clearFilters} className="w-full h-9">
                    Reset filters
                  </Button>
                </div>
              </div>
            </>
          )}
        </div>
      </div>

      {/* Content area */}
      {showMap ? (
        <div className="flex flex-1 min-h-0">
          {/* List panel — hidden on mobile when map is showing, visible on lg+ */}
          <div className="hidden lg:block lg:w-[55%] xl:w-[50%] overflow-y-auto border-r p-4">
            {listContent}
          </div>

          {/* Map panel */}
          <div className="flex-1 relative">
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
              />
            </Suspense>

            {/* Mobile: floating "Show list" button */}
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
                  <Badge variant="secondary" className="ml-1.5 text-[10px]">{totalCount}</Badge>
                )}
              </Button>
            </div>
          </div>
        </div>
      ) : (
        <div className="flex-1 overflow-y-auto">
          <div className="mx-auto max-w-7xl px-4 py-6 sm:px-6 lg:px-8">
            {listContent}
          </div>
        </div>
      )}
    </div>
  );
};
