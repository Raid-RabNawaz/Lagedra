import { useEffect, useRef } from "react";
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
  flyTo?: { lat: number; lng: number; zoom?: number } | null;
  searchOnMove: boolean;
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
}: {
  onBoundsChange: (bounds: MapBounds) => void;
  searchOnMove: boolean;
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
      if (!searchOnMoveRef.current) return;
      if (timerRef.current) clearTimeout(timerRef.current);
      timerRef.current = setTimeout(() => {
        onBoundsRef.current(extractBounds(map));
      }, 400);
    },
    zoomend: () => {
      if (!searchOnMoveRef.current) return;
      if (timerRef.current) clearTimeout(timerRef.current);
      timerRef.current = setTimeout(() => {
        onBoundsRef.current(extractBounds(map));
      }, 400);
    },
  });

  useEffect(() => {
    if (initialDispatched.current) return;
    initialDispatched.current = true;
    // Defer to next tick so the map has settled its size/zoom.
    const handle = setTimeout(() => {
      onBoundsRef.current(extractBounds(map));
    }, 50);
    return () => clearTimeout(handle);
  }, [map]);

  return null;
}

function FlyToHandler({ flyTo }: { flyTo: ListingMapProps["flyTo"] }) {
  const map = useMap();
  const prevFlyTo = useRef(flyTo);

  useEffect(() => {
    if (
      flyTo &&
      (flyTo.lat !== prevFlyTo.current?.lat || flyTo.lng !== prevFlyTo.current?.lng)
    ) {
      map.flyTo([flyTo.lat, flyTo.lng], flyTo.zoom ?? 12, { duration: 1.2 });
    }
    prevFlyTo.current = flyTo;
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
      <MapEvents onBoundsChange={onBoundsChange} searchOnMove={searchOnMove} />
      <FlyToHandler flyTo={flyTo} />
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
