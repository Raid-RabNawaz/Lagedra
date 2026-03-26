import { useEffect, useRef } from "react";
import { MapContainer, TileLayer, Marker, useMap, useMapEvents } from "react-leaflet";
import L from "leaflet";

const DEFAULT_CENTER: [number, number] = [39.8, -98.5];
const DEFAULT_ZOOM = 4;
const PIN_ZOOM = 15;

const pinIcon = L.icon({
  iconUrl: "https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png",
  iconRetinaUrl: "https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon-2x.png",
  shadowUrl: "https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png",
  iconSize: [25, 41],
  iconAnchor: [12, 41],
  popupAnchor: [1, -34],
  shadowSize: [41, 41],
});

type LocationPickerMapProps = {
  latitude?: number;
  longitude?: number;
  onClick: (lat: number, lng: number) => void;
};

function ClickHandler({ onClick }: { onClick: (lat: number, lng: number) => void }) {
  useMapEvents({
    click: (e) => {
      onClick(e.latlng.lat, e.latlng.lng);
    },
  });
  return null;
}

function FlyToMarker({ lat, lng }: { lat: number; lng: number }) {
  const map = useMap();
  const prevPos = useRef<{ lat: number; lng: number } | null>(null);

  useEffect(() => {
    if (prevPos.current?.lat === lat && prevPos.current?.lng === lng) return;
    prevPos.current = { lat, lng };
    const currentZoom = map.getZoom();
    map.flyTo([lat, lng], Math.max(currentZoom, PIN_ZOOM), { duration: 0.8 });
  }, [lat, lng, map]);

  return null;
}

export function LocationPickerMap({ latitude, longitude, onClick }: LocationPickerMapProps) {
  const hasPin = latitude != null && longitude != null && !isNaN(latitude) && !isNaN(longitude);
  const center: [number, number] = hasPin ? [latitude!, longitude!] : DEFAULT_CENTER;
  const zoom = hasPin ? PIN_ZOOM : DEFAULT_ZOOM;

  return (
    <MapContainer
      center={center}
      zoom={zoom}
      className="h-full w-full"
      style={{ cursor: "crosshair" }}
      zoomControl={true}
      scrollWheelZoom={true}
    >
      <TileLayer
        attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
        url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
      />
      <ClickHandler onClick={onClick} />
      {hasPin && (
        <>
          <Marker position={[latitude!, longitude!]} icon={pinIcon} />
          <FlyToMarker lat={latitude!} lng={longitude!} />
        </>
      )}
    </MapContainer>
  );
}
