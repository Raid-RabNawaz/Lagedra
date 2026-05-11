import { MapContainer, TileLayer, Marker, Circle } from "react-leaflet";
import L from "leaflet";

const pinIcon = L.icon({
  iconUrl: "https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png",
  iconRetinaUrl: "https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon-2x.png",
  shadowUrl: "https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png",
  iconSize: [25, 41],
  iconAnchor: [12, 41],
  popupAnchor: [1, -34],
  shadowSize: [41, 41],
});

type ListingApproxMapProps = {
  latitude: number;
  longitude: number;
  /** Privacy circle radius in metres. When set, hides the precise marker and shows a circle. */
  privacyRadiusMeters?: number;
  zoom?: number;
  showMarker?: boolean;
};

export function ListingApproxMap({
  latitude,
  longitude,
  privacyRadiusMeters,
  zoom = 14,
  showMarker = true,
}: ListingApproxMapProps) {
  return (
    <MapContainer
      center={[latitude, longitude]}
      zoom={zoom}
      className="h-full w-full"
      scrollWheelZoom={false}
      zoomControl={true}
    >
      <TileLayer
        attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
        url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
      />
      {privacyRadiusMeters ? (
        <Circle
          center={[latitude, longitude]}
          radius={privacyRadiusMeters}
          pathOptions={{
            color: "#22c55e",
            weight: 2,
            fillColor: "#22c55e",
            fillOpacity: 0.15,
          }}
        />
      ) : null}
      {showMarker && !privacyRadiusMeters && (
        <Marker position={[latitude, longitude]} icon={pinIcon} />
      )}
    </MapContainer>
  );
}
