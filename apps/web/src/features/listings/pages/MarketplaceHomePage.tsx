import { useMemo, useRef, useState } from "react";
import { Link } from "react-router-dom";
import {
  ChevronLeft,
  ChevronRight,
  ShieldCheck,
  ArrowRight,
  Building2,
  Home,
  Castle,
  Warehouse,
  Tent,
  TreePine,
  Compass,
  Sparkles,
} from "lucide-react";
import { useListings } from "@/features/listings/hooks/useListings";
import { useGeolocation } from "@/features/listings/hooks/useGeolocation";
import { ListingCard } from "@/features/listings/components/ListingCard";
import { HeroSearchBar } from "@/features/listings/components/HeroSearchBar";
import { LocationPermissionPrompt } from "@/features/listings/components/LocationPermissionPrompt";
import type {
  ListingSummaryDto,
  PropertyType,
  SearchListingsParams,
} from "@/api/types";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";

/**
 * Default catchment radius (km) when querying the "near you" carousel.
 * Mid-term renters routinely consider listings up to ~50km from their
 * search anchor — a tighter radius produces empty carousels in low-
 * density markets, a wider one stops feeling "near you".
 */
const NEAR_YOU_RADIUS_KM = 50;

type Category = {
  id: PropertyType | "All";
  label: string;
  icon: typeof Compass;
};

const categories: Category[] = [
  { id: "All", label: "Explore", icon: Compass },
  { id: "Apartment", label: "Apartments", icon: Building2 },
  { id: "House", label: "Houses", icon: Home },
  { id: "Villa", label: "Villas", icon: Castle },
  { id: "Studio", label: "Studios", icon: Warehouse },
  { id: "Loft", label: "Lofts", icon: Sparkles },
  { id: "Cabin", label: "Cabins", icon: TreePine },
  { id: "Cottage", label: "Cottages", icon: Tent },
];

const HERO_IMG =
  "https://images.unsplash.com/photo-1600566753190-17f0baa2a6c3?auto=format&fit=crop&w=1280&q=70";

export const MarketplaceHomePage = () => {
  const [activeCategory, setActiveCategory] = useState<Category["id"]>("All");
  const [dismissedLocationPrompt, setDismissedLocationPrompt] = useState(false);

  // Geolocation drives the first carousel's labelling and filtering. We
  // never auto-prompt — `useGeolocation` only fires the dialog when the
  // user clicks the "Enable location" CTA, or when a previous session
  // had already granted permission.
  const geo = useGeolocation();
  const hasCoords = geo.coords != null;

  // Build the "near you" query: when we have coordinates, ask the API
  // to filter by radius AND sort by distance so the carousel actually
  // earns its name. Without coordinates we fall back to "Featured rentals"
  // (Newest sort), but ONLY if the user hasn't explicitly denied — denied
  // users get an alternative section title and we keep the carousel
  // useful without misrepresenting it.
  const nearYouParams = useMemo<SearchListingsParams>(() => {
    if (geo.coords) {
      return {
        page: 1,
        pageSize: 8,
        sortBy: "Distance",
        latitude: geo.coords.latitude,
        longitude: geo.coords.longitude,
        radiusKm: NEAR_YOU_RADIUS_KM,
      };
    }
    return { page: 1, pageSize: 8, sortBy: "Newest" };
  }, [geo.coords]);

  const nearYou = useListings(nearYouParams);

  const mostFamous = useListings({ page: 1, pageSize: 8, sortBy: "PriceDesc" });
  const gridParams = useMemo(
    () => ({
      page: 1,
      pageSize: 12,
      sortBy: "Newest" as const,
      ...(activeCategory !== "All" ? { propertyType: activeCategory as PropertyType } : {}),
    }),
    [activeCategory],
  );
  const grid = useListings(gridParams);

  // Section label + "view all" link adapt to geolocation reality:
  //   - granted/cached coords  → "Rentals near you" (sortBy=Distance)
  //   - everything else        → "Featured rentals" (sortBy=Newest)
  // This honours the rule that we never claim "near you" when we can't
  // back the claim up with actual coordinates.
  const carouselTitle = hasCoords ? "Rentals near you" : "Featured rentals";
  const carouselViewAllHref = hasCoords
    ? `/listings/search?sortBy=Distance&latitude=${geo.coords!.latitude}&longitude=${geo.coords!.longitude}&radiusKm=${NEAR_YOU_RADIUS_KM}`
    : "/listings/search?sortBy=Newest";

  // Show the prompt only when there's a useful action the user can take.
  // The "denied" state still surfaces a short explainer banner so the
  // section title swap doesn't feel arbitrary — but the user dismissed
  // it once we hide it for the rest of the session.
  const showLocationPrompt =
    !dismissedLocationPrompt &&
    !hasCoords &&
    geo.permission !== "granted" &&
    geo.permission !== "unknown";

  return (
    <div className="bg-background">
      {/* ─── Hero ──────────────────────────────────────────── */}
      <section className="mx-auto max-w-7xl px-4 pt-6 sm:px-6 lg:px-8">
        {/*
          The search bar must live OUTSIDE the hero card's `overflow-hidden`,
          otherwise its date-picker popover gets clipped by the rounded mask.
          So: hero card + search bar are siblings inside a `relative` wrapper.
        */}
        <div className="relative">
          <div className="relative overflow-hidden rounded-3xl bg-brand-deep">
            <img
              src={HERO_IMG}
              alt=""
              decoding="async"
              fetchPriority="high"
              className="absolute inset-0 h-full w-full object-cover opacity-90"
            />
            <div className="absolute inset-0 bg-gradient-to-r from-brand-deep/85 via-brand-deep/40 to-transparent" />

            <div className="relative px-6 pt-14 pb-32 sm:px-12 sm:pt-20 sm:pb-40 md:pt-28 md:pb-48">
              <h1 className="max-w-xl text-4xl font-extrabold leading-[1.05] text-white sm:text-5xl md:text-6xl">
                Move In
                <br />
                Settle Down
              </h1>
              <p className="mt-4 max-w-md text-sm text-white/80 sm:text-base">
                Mid-term rentals you can trust. Verified hosts, transparent pricing,
                and a protocol that protects every move.
              </p>
            </div>
          </div>

          {/* Lifted out of the clipping container */}
          <div className="absolute inset-x-4 -bottom-8 z-30 sm:inset-x-12 sm:-bottom-10">
            <HeroSearchBar />
          </div>
        </div>
        {/* Spacer so the overhanging search bar doesn't collide with the next section */}
        <div className="h-14 sm:h-16" aria-hidden />
      </section>

      {/* ─── Location prompt (only when actionable) ────────── */}
      {showLocationPrompt && (
        <section className="mx-auto max-w-7xl px-4 pt-8 sm:px-6 lg:px-8">
          <LocationPermissionPrompt
            permission={geo.permission}
            loading={geo.loading}
            error={geo.error}
            onEnable={geo.requestLocation}
            onDismiss={() => setDismissedLocationPrompt(true)}
          />
        </section>
      )}

      {/* ─── Featured / nearby carousel ─────────────────────── */}
      <Carousel
        title={carouselTitle}
        viewAllHref={carouselViewAllHref}
        loading={nearYou.isLoading}
        items={nearYou.data?.items ?? []}
      />

      {/* ─── Find your perfect place — category grid ───────── */}
      <section className="mx-auto max-w-7xl px-4 py-10 sm:px-6 lg:px-8">
        <div className="flex items-end justify-between gap-4">
          <h2 className="text-2xl font-bold tracking-tight sm:text-3xl">
            Find your perfect place to stay
          </h2>
          <Link
            to="/listings/search"
            className="hidden shrink-0 items-center gap-1 text-sm font-semibold text-primary hover:underline sm:inline-flex"
          >
            View all
            <ArrowRight className="h-4 w-4" />
          </Link>
        </div>

        <div className="mt-6 flex gap-2 overflow-x-auto pb-2 no-scrollbar">
          {categories.map((cat) => {
            const Icon = cat.icon;
            const active = activeCategory === cat.id;
            return (
              <button
                key={cat.id}
                onClick={() => setActiveCategory(cat.id)}
                className={cn(
                  "inline-flex shrink-0 items-center gap-2 rounded-full border px-4 py-2 text-sm font-semibold transition-all",
                  active
                    ? "border-foreground bg-foreground text-background shadow-sm"
                    : "border-border bg-background text-muted-foreground hover:border-foreground/40 hover:text-foreground",
                )}
              >
                <Icon className="h-4 w-4" />
                {cat.label}
              </button>
            );
          })}
        </div>

        <div className="mt-8 grid grid-cols-2 gap-5 sm:grid-cols-3 lg:grid-cols-4">
          {grid.isLoading
            ? Array.from({ length: 8 }).map((_, i) => <CardSkeleton key={i} />)
            : grid.data?.items.map((listing) => (
                <ListingCard key={listing.id} listing={listing} />
              ))}
        </div>

        {!grid.isLoading && (grid.data?.items.length ?? 0) === 0 && (
          <p className="mt-8 text-center text-sm text-muted-foreground">
            No listings yet for this category. Check back soon.
          </p>
        )}
      </section>

      {/* ─── Most famous (carousel) ────────────────────────── */}
      <Carousel
        title="Most famous"
        viewAllHref="/listings/search"
        loading={mostFamous.isLoading}
        items={mostFamous.data?.items ?? []}
      />

      {/* ─── Trust strip ───────────────────────────────────── */}
      <section className="mx-auto max-w-7xl px-4 py-6 sm:px-6 lg:px-8">
        <div className="flex flex-col items-start gap-4 rounded-2xl bg-brand-deep px-6 py-5 text-white sm:flex-row sm:items-center sm:justify-between sm:px-8">
          <div className="flex items-center gap-3">
            <span className="flex h-10 w-10 items-center justify-center rounded-full bg-white/10 ring-1 ring-white/20">
              <ShieldCheck className="h-5 w-5" />
            </span>
            <p className="text-sm font-medium sm:text-base">
              Future-proof your next move with the Lagedra Trust Protocol.
            </p>
          </div>
          <div className="flex items-center gap-2">
            <Link
              to="/how-it-works"
              className="rounded-full px-4 py-2 text-sm font-semibold text-white/85 hover:text-white"
            >
              How it works
            </Link>
            <Link
              to="/join"
              className="rounded-full bg-white px-5 py-2 text-sm font-semibold text-foreground transition-transform hover:scale-[1.02]"
            >
              Join now
            </Link>
          </div>
        </div>
      </section>

      <div className="h-8" />
    </div>
  );
};

/* ──────────── helper components (private) ──────────── */

function Carousel({
  title,
  viewAllHref,
  loading,
  items,
}: {
  title: string;
  viewAllHref: string;
  loading: boolean;
  items: ListingSummaryDto[];
}) {
  const trackRef = useRef<HTMLDivElement | null>(null);

  const scroll = (dir: "left" | "right") => {
    const el = trackRef.current;
    if (!el) return;
    const amount = el.clientWidth * 0.85 * (dir === "left" ? -1 : 1);
    el.scrollBy({ left: amount, behavior: "smooth" });
  };

  return (
    <section className="mx-auto max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
      <div className="flex items-end justify-between gap-4">
        <div className="flex items-center gap-3">
          <h2 className="text-2xl font-bold tracking-tight sm:text-3xl">{title}</h2>
          <Link
            to={viewAllHref}
            aria-label="View all"
            className="hidden h-7 w-7 items-center justify-center rounded-full bg-secondary text-muted-foreground transition-colors hover:bg-foreground hover:text-background sm:inline-flex"
          >
            <ArrowRight className="h-3.5 w-3.5" />
          </Link>
        </div>
        <div className="hidden items-center gap-2 sm:flex">
          <Button
            variant="outline"
            size="icon"
            className="h-9 w-9 rounded-full"
            onClick={() => scroll("left")}
            aria-label="Previous"
          >
            <ChevronLeft className="h-4 w-4" />
          </Button>
          <Button
            variant="outline"
            size="icon"
            className="h-9 w-9 rounded-full"
            onClick={() => scroll("right")}
            aria-label="Next"
          >
            <ChevronRight className="h-4 w-4" />
          </Button>
        </div>
      </div>

      <div
        ref={trackRef}
        className="mt-5 flex gap-5 overflow-x-auto pb-2 snap-x snap-mandatory no-scrollbar"
      >
        {loading
          ? Array.from({ length: 5 }).map((_, i) => (
              <div
                key={i}
                className="w-[260px] shrink-0 snap-start sm:w-[280px]"
              >
                <CardSkeleton />
              </div>
            ))
          : items.map((listing) => (
              <div
                key={listing.id}
                className="w-[260px] shrink-0 snap-start sm:w-[280px]"
              >
                <ListingCard listing={listing} />
              </div>
            ))}
        {!loading && items.length === 0 && (
          <p className="text-sm text-muted-foreground">
            No listings yet — be the first to publish one.
          </p>
        )}
      </div>
    </section>
  );
}

function CardSkeleton() {
  return (
    <div className="aspect-[4/5] overflow-hidden rounded-2xl ring-1 ring-border/60">
      <div className="grid h-full grid-rows-[4fr_1fr]">
        <div className="min-h-0 animate-pulse bg-muted" />
        <div className="space-y-2 px-3 py-2.5">
          <div className="h-4 w-3/4 animate-pulse rounded bg-muted" />
          <div className="h-3 w-1/2 animate-pulse rounded bg-muted" />
          <div className="h-4 w-1/3 animate-pulse rounded bg-muted" />
        </div>
      </div>
    </div>
  );
}
