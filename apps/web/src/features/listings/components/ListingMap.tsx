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
const DEFAULT_ZOOM = 4;

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
  const timerRef = useRef<ReturnType<typeof setTimeout>>();
  const onBoundsRef = useRef(onBoundsChange);
  const searchOnMoveRef = useRef(searchOnMove);
  onBoundsRef.current = onBoundsChange;
  searchOnMoveRef.current = searchOnMove;

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

function FitListings({ listings }: { listings: ListingSummaryDto[] }) {
  const map = useMap();
  const fitted = useRef(false);

  useEffect(() => {
    if (fitted.current) return;
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
      map.setView(sw, 13);
    } else {
      map.fitBounds([sw, ne], { padding: [40, 40], maxZoom: 15 });
    }
  }, [listings, map]);

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
      <FitListings listings={listings} />
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
