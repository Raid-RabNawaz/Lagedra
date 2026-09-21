import type { SearchListingsParams } from "@/api/types";
import type { MapBounds } from "@/features/listings/components/ListingMap";

export type GeocodedPlace = {
  lat: number;
  lng: number;
  bounds: MapBounds;
};

export type PlaceSuggestion = {
  id: string;
  /** Primary line shown in the typeahead, and the value written into the box. */
  label: string;
  /** Secondary line (City / Address / Neighborhood / State, or a region). */
  hint: string;
  /** Canonical string to put in the search field. */
  query: string;
  place: GeocodedPlace | null;
};

type NominatimAddress = {
  house_number?: string;
  road?: string;
  pedestrian?: string;
  neighbourhood?: string;
  suburb?: string;
  city?: string;
  town?: string;
  village?: string;
  hamlet?: string;
  municipality?: string;
  county?: string;
  state?: string;
  postcode?: string;
  country?: string;
};

type NominatimHit = {
  lat?: string;
  lon?: string;
  boundingbox?: string[];
  display_name?: string;
  addresstype?: string;
  type?: string;
  class?: string;
  osm_id?: number | string;
  osm_type?: string;
  address?: NominatimAddress;
};

const US_STATE_ABBR: Record<string, string> = {
  Alabama: "AL",
  Alaska: "AK",
  Arizona: "AZ",
  Arkansas: "AR",
  California: "CA",
  Colorado: "CO",
  Connecticut: "CT",
  Delaware: "DE",
  Florida: "FL",
  Georgia: "GA",
  Hawaii: "HI",
  Idaho: "ID",
  Illinois: "IL",
  Indiana: "IN",
  Iowa: "IA",
  Kansas: "KS",
  Kentucky: "KY",
  Louisiana: "LA",
  Maine: "ME",
  Maryland: "MD",
  Massachusetts: "MA",
  Michigan: "MI",
  Minnesota: "MN",
  Mississippi: "MS",
  Missouri: "MO",
  Montana: "MT",
  Nebraska: "NE",
  Nevada: "NV",
  "New Hampshire": "NH",
  "New Jersey": "NJ",
  "New Mexico": "NM",
  "New York": "NY",
  "North Carolina": "NC",
  "North Dakota": "ND",
  Ohio: "OH",
  Oklahoma: "OK",
  Oregon: "OR",
  Pennsylvania: "PA",
  "Rhode Island": "RI",
  "South Carolina": "SC",
  "South Dakota": "SD",
  Tennessee: "TN",
  Texas: "TX",
  Utah: "UT",
  Vermont: "VT",
  Virginia: "VA",
  Washington: "WA",
  "West Virginia": "WV",
  Wisconsin: "WI",
  Wyoming: "WY",
  "District of Columbia": "DC",
};

/** Bias suggestions toward California without blocking other US places. */
const CA_VIEWBOX = "-124.48,42.01,-114.13,32.53";

function localityName(addr: NominatimAddress): string {
  return (
    addr.city ??
    addr.town ??
    addr.village ??
    addr.hamlet ??
    addr.municipality ??
    ""
  );
}

function stateAbbr(state?: string): string {
  if (!state) return "";
  if (state.length === 2) return state.toUpperCase();
  return US_STATE_ABBR[state] ?? state;
}

function cityStateLine(addr: NominatimAddress): string {
  const city = localityName(addr);
  const state = stateAbbr(addr.state);
  return [city, state].filter(Boolean).join(", ");
}

function placeKindHint(kind: string): string {
  if (kind === "state") return "State";
  if (kind === "county") return "County";
  if (kind === "postcode") return "ZIP code";
  if (kind === "house" || kind === "building") return "Address";
  if (kind === "road" || kind === "highway") return "Street";
  if (kind === "neighbourhood" || kind === "suburb" || kind === "quarter") return "Neighborhood";
  if (kind === "city" || kind === "town" || kind === "village" || kind === "municipality") {
    return "City";
  }
  return "Place";
}

/**
 * Turn a Nominatim hit into a short typeahead row (city, address, neighborhood, or state).
 */
export function nominatimToSuggestion(hit: NominatimHit): PlaceSuggestion | null {
  const place = nominatimToPlace(hit);
  if (!place) return null;

  const addr = hit.address ?? {};
  const kind = (hit.addresstype ?? hit.type ?? "").toLowerCase();
  const cityState = cityStateLine(addr);
  const road = addr.road ?? addr.pedestrian ?? "";
  const id = `${hit.osm_type ?? "place"}-${hit.osm_id ?? hit.display_name ?? `${place.lat},${place.lng}`}`;

  let label = "";
  let hint = placeKindHint(kind);

  if (addr.house_number && road) {
    label = `${addr.house_number} ${road}`;
    hint = cityState || "Address";
  } else if ((kind === "road" || kind === "highway") && road) {
    label = road;
    hint = cityState || "Street";
  } else if (kind === "neighbourhood" || kind === "suburb" || kind === "quarter") {
    label = addr.neighbourhood ?? addr.suburb ?? "";
    hint = cityState || "Neighborhood";
  } else if (kind === "state" || (addr.state && !localityName(addr) && !addr.county && !addr.postcode)) {
    label = addr.state ?? "";
    hint = addr.country ?? "United States";
  } else if (kind === "postcode" && addr.postcode) {
    label = addr.postcode;
    hint = cityState || "ZIP code";
  } else if (kind === "county" && addr.county) {
    const state = stateAbbr(addr.state);
    label = state ? `${addr.county}, ${state}` : addr.county;
    hint = "County";
  } else if (localityName(addr)) {
    const city = localityName(addr);
    const state = stateAbbr(addr.state);
    label = state ? `${city}, ${state}` : city;
    hint = "City";
  }

  if (!label) {
    const parts = (hit.display_name ?? "")
      .split(",")
      .map((p) => p.trim())
      .filter(Boolean);
    label = parts.slice(0, 2).join(", ");
  }

  if (!label) return null;

  return { id, label, hint, query: label, place };
}

export function mergePlaceSuggestions(
  local: PlaceSuggestion[],
  remote: PlaceSuggestion[],
  limit = 8,
): PlaceSuggestion[] {
  const seen = new Set<string>();
  const out: PlaceSuggestion[] = [];
  for (const suggestion of [...local, ...remote]) {
    const key = suggestion.label.toLowerCase();
    if (seen.has(key)) continue;
    seen.add(key);
    out.push(suggestion);
    if (out.length >= limit) break;
  }
  return out;
}

export async function suggestPlaces(
  query: string,
  signal?: AbortSignal,
): Promise<PlaceSuggestion[]> {
  const term = query.trim();
  if (term.length < 2) return [];
  try {
    const params = new URLSearchParams({
      q: term,
      format: "json",
      addressdetails: "1",
      limit: "8",
      countrycodes: "us",
      viewbox: CA_VIEWBOX,
      bounded: "0",
      dedupe: "1",
    });
    const res = await fetch(`https://nominatim.openstreetmap.org/search?${params}`, {
      headers: { "Accept-Language": "en" },
      signal,
    });
    if (!res.ok) return [];
    const data: unknown = await res.json();
    if (!Array.isArray(data)) return [];

    const seen = new Set<string>();
    const out: PlaceSuggestion[] = [];
    for (const raw of data) {
      const suggestion = nominatimToSuggestion(raw as NominatimHit);
      if (!suggestion) continue;
      const key = suggestion.label.toLowerCase();
      if (seen.has(key)) continue;
      seen.add(key);
      out.push(suggestion);
    }
    return out;
  } catch {
    return [];
  }
}

/** Nominatim boundingbox is south, north, west, east. */
export function nominatimToPlace(hit: NominatimHit): GeocodedPlace | null {
  const lat = Number(hit.lat);
  const lng = Number(hit.lon);
  if (!Number.isFinite(lat) || !Number.isFinite(lng)) return null;

  const bbox = hit.boundingbox;
  if (bbox && bbox.length >= 4) {
    const swLat = Number(bbox[0]);
    const neLat = Number(bbox[1]);
    const swLng = Number(bbox[2]);
    const neLng = Number(bbox[3]);
    if (
      Number.isFinite(swLat)
      && Number.isFinite(neLat)
      && Number.isFinite(swLng)
      && Number.isFinite(neLng)
    ) {
      return {
        lat,
        lng,
        bounds: { swLat, swLng, neLat, neLng, centerLat: lat, centerLng: lng },
      };
    }
  }

  return { lat, lng, bounds: radiusBounds(lat, lng, 20) };
}

/** Rough bounding box around a point so a place search works before the map moves. */
export function radiusBounds(lat: number, lng: number, radiusKm: number): MapBounds {
  const kmPerDegLat = 111;
  const kmPerDegLng = 111 * Math.cos((lat * Math.PI) / 180);
  const dLat = radiusKm / kmPerDegLat;
  const dLng = kmPerDegLng === 0 ? radiusKm / kmPerDegLat : radiusKm / kmPerDegLng;
  return {
    swLat: lat - dLat,
    swLng: lng - dLng,
    neLat: lat + dLat,
    neLng: lng + dLng,
    centerLat: lat,
    centerLng: lng,
  };
}

export async function geocodePlace(query: string): Promise<GeocodedPlace | null> {
  const term = query.trim();
  if (!term) return null;
  try {
    const url = `https://nominatim.openstreetmap.org/search?q=${encodeURIComponent(term)}&format=json&limit=1`;
    const res = await fetch(url, { headers: { "Accept-Language": "en" } });
    const data: unknown = await res.json();
    if (!Array.isArray(data) || data.length === 0) return null;
    return nominatimToPlace(data[0] as NominatimHit);
  } catch {
    return null;
  }
}

/**
 * A geocoded "Where" is a map area, not a title keyword. Keep the typed
 * string as a keyword only when Nominatim could not resolve a place.
 */
export function applyLocationToSearchParams(
  params: SearchListingsParams,
  options: {
    keyword: string;
    placeResolved: boolean;
    userMapBounds: MapBounds | null;
    placeBounds: MapBounds | null;
    radiusAnchor: { lat: number; lng: number; radiusKm: number } | null;
  },
): SearchListingsParams {
  const next = { ...params };
  const keyword = options.keyword.trim();
  if (keyword && !options.placeResolved) {
    next.keyword = keyword;
  } else {
    delete next.keyword;
  }

  const bounds = options.userMapBounds ?? options.placeBounds;
  if (bounds) {
    next.swLat = bounds.swLat;
    next.swLng = bounds.swLng;
    next.neLat = bounds.neLat;
    next.neLng = bounds.neLng;
    next.latitude = bounds.centerLat;
    next.longitude = bounds.centerLng;
    delete next.radiusKm;
    return next;
  }

  if (options.radiusAnchor) {
    next.latitude = options.radiusAnchor.lat;
    next.longitude = options.radiusAnchor.lng;
    next.radiusKm = options.radiusAnchor.radiusKm;
  }

  return next;
}
