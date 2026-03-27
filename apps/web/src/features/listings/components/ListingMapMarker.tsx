import { useMemo } from "react";
import { Marker, Popup } from "react-leaflet";
import L from "leaflet";
import { useNavigate } from "react-router-dom";
import type { ListingSummaryDto } from "@/api/types";
import { formatMoney } from "@/utils/format";

type ListingMapMarkerProps = {
  listing: ListingSummaryDto;
  isHighlighted: boolean;
  onHover: (id: string | null) => void;
};

function buildPriceIcon(price: string, highlighted: boolean) {
  const bg = highlighted ? "#222222" : "#ffffff";
  const color = highlighted ? "#ffffff" : "#222222";
  const shadow = highlighted
    ? "0 2px 8px rgba(0,0,0,0.4)"
    : "0 1px 4px rgba(0,0,0,0.18)";
  const scale = highlighted ? "scale(1.12)" : "scale(1)";
  const zIdx = highlighted ? 9999 : 1;

  return L.divIcon({
    className: "",
    html: `<div style="
      background:${bg};color:${color};
      font-weight:600;font-size:13px;font-family:Inter,sans-serif;
      padding:4px 10px;border-radius:20px;white-space:nowrap;
      box-shadow:${shadow};border:1.5px solid ${highlighted ? "#222" : "#ddd"};
      transform:${scale};transition:all 150ms ease;
      z-index:${zIdx};position:relative;cursor:pointer;
    ">${price}</div>`,
    iconSize: [0, 0],
    iconAnchor: [0, 0],
  });
}

export function ListingMapMarker({ listing, isHighlighted, onHover }: ListingMapMarkerProps) {
  const navigate = useNavigate();
  const price = formatMoney(listing.monthlyRentCents);

  const icon = useMemo(
    () => buildPriceIcon(price, isHighlighted),
    [price, isHighlighted],
  );

  if (listing.latitude == null || listing.longitude == null) return null;

  return (
    <Marker
      position={[listing.latitude, listing.longitude]}
      icon={icon}
      zIndexOffset={isHighlighted ? 1000 : 0}
      eventHandlers={{
        mouseover: () => onHover(listing.id),
        mouseout: () => onHover(null),
        click: () => navigate(`/listings/${listing.id}`),
      }}
    >
      <Popup closeButton={false} className="listing-popup" offset={[0, -4]}>
        <div
          style={{ width: 220, cursor: "pointer" }}
          onClick={() => navigate(`/listings/${listing.id}`)}
        >
          {listing.coverPhotoUrl && (
            <img
              src={listing.coverPhotoUrl}
              alt={listing.title}
              style={{
                width: "100%",
                height: 120,
                objectFit: "cover",
                borderRadius: "8px 8px 0 0",
              }}
            />
          )}
          <div style={{ padding: "8px 10px" }}>
            <div style={{ fontWeight: 600, fontSize: 14, lineHeight: 1.3, marginBottom: 4 }}>
              {listing.title}
            </div>
            <div style={{ fontSize: 13, color: "#717171" }}>
              {listing.bedrooms === 0 ? "Studio" : `${listing.bedrooms} bed`}
              {" · "}
              {listing.bathrooms} bath
            </div>
            <div style={{ fontWeight: 700, fontSize: 15, marginTop: 4 }}>
              {price}
              <span style={{ fontWeight: 400, fontSize: 13, color: "#717171" }}> / mo</span>
            </div>
          </div>
        </div>
      </Popup>
    </Marker>
  );
}
