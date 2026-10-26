import { useEffect, useRef, type RefObject } from "react";
import { MapContainer, TileLayer, useMap, useMapEvents } from "react-leaflet";
import type { Map as LeafletMap } from "leaflet";
import type { ListingSummaryDto } from "@/api/types";
import { ListingMapMarker } from "@/features/listings/components/ListingMapMarker";

export type MapBounds = {
  swLat: number;
  swLng: number;
  neLat: number;
  neLng: number;
  centerLat: number;
  centerLng: number;
};

type ListingMapProps = {
  listings: ListingSummaryDto[];
  highlightedId: string | null;
  onHover: (id: string | null) => void;
  onBoundsChange: (bounds: MapBounds) => void;
  flyTo?: { lat: number; lng: number; zoom?: number; bounds?: MapBounds } | null;
  searchOnMove: boolean;
  /**
   * While true, map move events are not reported. A place search sets this so
   * the default viewport cannot replace the city the user just picked.
   */
  suspendBoundsRef: RefObject<boolean>;
  /** Called after a programmatic fly/fit has finished (or timed out). */
  onFlySettled?: () => void;
};

const DEFAULT_CENTER: [number, number] = [39.8, -98.5];
const DEFAULT_ZOOM = 5;
const CITY_ZOOM = 11;
// If the visible listings span more than this many degrees of lat/lng, we
// avoid `fitBounds` (which would zoom out to country-level) and instead anchor
// the map at the result centroid at city zoom. Users can pan/zoom from there.
const FIT_MAX_SPAN_DEG = 1.5;

function extractBounds(map: LeafletMap): MapBounds {
  const b = map.getBounds();
  const c = map.getCenter();
  return {
    swLat: b.getSouthWest().lat,
    swLng: b.getSouthWest().lng,
    neLat: b.getNorthEast().lat,
    neLng: b.getNorthEast().lng,
    centerLat: c.lat,
    centerLng: c.lng,
  };
}

function MapEvents({
  onBoundsChange,
  searchOnMove,
  suspendBoundsRef,
}: {
  onBoundsChange: (bounds: MapBounds) => void;
  searchOnMove: boolean;
  suspendBoundsRef: RefObject<boolean>;
}) {
  const timerRef = useRef<ReturnType<typeof setTimeout>>(undefined);
  const onBoundsRef = useRef(onBoundsChange);
  const searchOnMoveRef = useRef(searchOnMove);
  const initialDispatched = useRef(false);

  // Keep callback refs in sync with the latest props without re-subscribing
  // to the leaflet event handlers below.
  useEffect(() => {
    onBoundsRef.current = onBoundsChange;
  }, [onBoundsChange]);
  useEffect(() => {
    searchOnMoveRef.current = searchOnMove;
  }, [searchOnMove]);

  const map = useMapEvents({
    moveend: () => {
      publishBounds();
    },
    zoomend: () => {
      publishBounds();
    },
  });

  function publishBounds() {
    if (suspendBoundsRef.current) return;
    if (!searchOnMoveRef.current) return;
    // The first settle is the default view, not a user pan. Sending those
    // bounds immediately re-filters the result set and hides pins.
    if (!initialDispatched.current) {
      initialDispatched.current = true;
      return;
    }
    if (timerRef.current) clearTimeout(timerRef.current);
    timerRef.current = setTimeout(() => {
      if (suspendBoundsRef.current) return;
      onBoundsRef.current(extractBounds(map));
    }, 400);
  }

  return null;
}

function FlyToHandler({
  flyTo,
  onSettled,
}: {
  flyTo: ListingMapProps["flyTo"];
  onSettled?: () => void;
}) {
  const map = useMap();
  // Start unset so a fly target that exists on the first render still runs.
  // Seeding this with `flyTo` made the map stay on the US default and the
  // first search used that empty view.
  const prevFlyTo = useRef<ListingMapProps["flyTo"]>(undefined);
  const onSettledRef = useRef(onSettled);

  useEffect(() => {
    onSettledRef.current = onSettled;
  }, [onSettled]);

  useEffect(() => {
    if (!flyTo) return;
    const prev = prevFlyTo.current;
    const changed =
      !prev
      || flyTo.lat !== prev.lat
      || flyTo.lng !== prev.lng
      || flyTo.bounds !== prev.bounds;
    prevFlyTo.current = flyTo;
    if (!changed) return;

    let settled = false;
    const finish = () => {
      if (settled) return;
      settled = true;
      onSettledRef.current?.();
    };
    const timer = window.setTimeout(finish, 1600);
    const onEnd = () => {
      window.clearTimeout(timer);
      finish();
    };
    map.once("moveend", onEnd);

    if (flyTo.bounds) {
      map.fitBounds(
        [
          [flyTo.bounds.swLat, flyTo.bounds.swLng],
          [flyTo.bounds.neLat, flyTo.bounds.neLng],
        ],
        { padding: [40, 40], maxZoom: 13, animate: true },
      );
    } else {
      map.flyTo([flyTo.lat, flyTo.lng], flyTo.zoom ?? 12, { duration: 1.2 });
    }

    return () => {
      window.clearTimeout(timer);
      map.off("moveend", onEnd);
    };
  }, [flyTo, map]);

  return null;
}

function FitListings({
  listings,
  hasFlyTo,
}: {
  listings: ListingSummaryDto[];
  hasFlyTo: boolean;
}) {
  const map = useMap();
  const fitted = useRef(false);

  useEffect(() => {
    if (fitted.current) return;
    // An explicit flyTo (e.g. geocoded search city) wins — don't fight it by
    // refitting to the listing bounding box.
    if (hasFlyTo) {
      fitted.current = true;
      return;
    }
    const withCoords = listings.filter(
      (l) => l.latitude != null && l.longitude != null,
    );
    if (withCoords.length === 0) return;
    fitted.current = true;

    const lats = withCoords.map((l) => l.latitude!);
    const lngs = withCoords.map((l) => l.longitude!);
    const sw: [number, number] = [Math.min(...lats), Math.min(...lngs)];
    const ne: [number, number] = [Math.max(...lats), Math.max(...lngs)];

    if (sw[0] === ne[0] && sw[1] === ne[1]) {
      map.setView(sw, 14);
      return;
    }

    const latSpan = ne[0] - sw[0];
    const lngSpan = ne[1] - sw[1];
    if (latSpan > FIT_MAX_SPAN_DEG || lngSpan > FIT_MAX_SPAN_DEG) {
      // Results are scattered across regions — anchor at the centroid at city
      // zoom so the user lands on a recognisable map view, not a country.
      const center: [number, number] = [(sw[0] + ne[0]) / 2, (sw[1] + ne[1]) / 2];
      map.setView(center, CITY_ZOOM);
    } else {
      map.fitBounds([sw, ne], { padding: [40, 40], maxZoom: 14 });
    }
  }, [listings, map, hasFlyTo]);

  return null;
}

export function ListingMap({
  listings,
  highlightedId,
  onHover,
  onBoundsChange,
  flyTo,
  searchOnMove,
  suspendBoundsRef,
  onFlySettled,
}: ListingMapProps) {
  return (
    <MapContainer
      center={DEFAULT_CENTER}
      zoom={DEFAULT_ZOOM}
      className="h-full w-full"
      zoomControl={true}
      scrollWheelZoom={true}
    >
      <TileLayer
        attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
        url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
      />
      <MapEvents
        onBoundsChange={onBoundsChange}
        searchOnMove={searchOnMove}
        suspendBoundsRef={suspendBoundsRef}
      />
      <FlyToHandler flyTo={flyTo} onSettled={onFlySettled} />
      <FitListings listings={listings} hasFlyTo={Boolean(flyTo)} />
      {listings.map((listing) =>
        listing.latitude != null && listing.longitude != null ? (
          <ListingMapMarker
            key={listing.id}
            listing={listing}
            isHighlighted={highlightedId === listing.id}
            onHover={onHover}
          />
        ) : null,
      )}
    </MapContainer>
  );
}
