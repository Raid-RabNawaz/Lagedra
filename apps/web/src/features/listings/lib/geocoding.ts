/**
 * Thin client over the OpenStreetMap Nominatim geocoding service plus a few
 * helpers for keeping a map pin and a structured postal address in sync.
 *
 * Nominatim is what `EditListingPage` already uses for "Look up address",
 * so reusing it keeps round-trip behaviour identical (no new API key, no
 * surprise rate limits) and means hosts get consistent results regardless
 * of which direction they edit from.
 *
 * Nominatim's usage policy caps callers at ~1 request per second per IP.
 * We do not implement a global queue here because the call sites in this
 * file are all behind user gestures or 700ms debounces, which keeps us well
 * under the limit in practice.
 */
export type ParsedAddress = {
  street: string;
  city: string;
  state: string;
  zipCode: string;
  country: string;
};

export type GeocodeResult = {
  latitude: number;
  longitude: number;
  /**
   * Parsed structured address. `null` when Nominatim could not break the
   * result down into something resembling a street address (e.g. the user
   * dropped the pin in the middle of a lake).
   */
  address: ParsedAddress | null;
  /** Untrimmed `display_name` from Nominatim, useful for UI captions. */
  displayName: string;
};

/** Raw Nominatim address payload — only the fields we care about. */
type NominatimAddress = {
  house_number?: string;
  road?: string;
  pedestrian?: string;
  footway?: string;
  cycleway?: string;
  path?: string;
  suburb?: string;
  neighbourhood?: string;
  city?: string;
  town?: string;
  village?: string;
  hamlet?: string;
  municipality?: string;
  county?: string;
  state?: string;
  region?: string;
  province?: string;
  postcode?: string;
  country?: string;
  country_code?: string;
};

type NominatimItem = {
  lat: string;
  lon: string;
  display_name?: string;
  address?: NominatimAddress;
};

const NOMINATIM_BASE = "https://nominatim.openstreetmap.org";

/**
 * Map Nominatim's wildly varying address keys into our flat
 * { street, city, state, zip, country } shape. Nominatim returns *different*
 * keys depending on whether the pin landed in a city, a village, a borough,
 * or in the middle of a national park, so we cascade through likely
 * alternatives before giving up.
 */
function parseNominatimAddress(addr: NominatimAddress | undefined): ParsedAddress | null {
  if (!addr) return null;

  // Build "house_number road" for street; fall back to a pedestrian way name
  // when the pin sits on a path rather than a numbered road.
  const roadName =
    addr.road ?? addr.pedestrian ?? addr.footway ?? addr.cycleway ?? addr.path ?? "";
  const street = [addr.house_number, roadName].filter(Boolean).join(" ").trim();

  // "City" in OSM-land can be city, town, village, hamlet, or municipality.
  // Suburb/neighbourhood are last-ditch fallbacks for dense urban pins.
  const city =
    addr.city ??
    addr.town ??
    addr.village ??
    addr.hamlet ??
    addr.municipality ??
    addr.suburb ??
    addr.neighbourhood ??
    addr.county ??
    "";

  // Some countries use `region` or `province` instead of `state`.
  const state = addr.state ?? addr.region ?? addr.province ?? "";

  const zipCode = addr.postcode ?? "";

  // Prefer the ISO-2 code (matches the existing "US" placeholder in the form)
  // but fall back to the human country name when Nominatim omits the code.
  const country = (addr.country_code ?? "").toUpperCase() || (addr.country ?? "");

  // If we couldn't extract *any* of the four critical fields, treat the
  // whole parse as a miss so callers don't pre-fill garbage.
  if (!street && !city && !state && !zipCode) return null;

  return { street, city, state, zipCode, country };
}

/**
 * Resolve a free-form address string into a coordinate pair and (when
 * available) a structured address. Returns `null` if Nominatim could not
 * locate the query at all.
 */
export async function forwardGeocode(
  query: string,
  signal?: AbortSignal,
): Promise<GeocodeResult | null> {
  const q = query.trim();
  if (!q) return null;

  const url = `${NOMINATIM_BASE}/search?q=${encodeURIComponent(q)}&format=json&limit=1&addressdetails=1`;
  const res = await fetch(url, {
    headers: { "Accept-Language": "en" },
    signal,
  });
  if (!res.ok) throw new Error(`Geocoding failed: ${res.status}`);

  const data = (await res.json()) as NominatimItem[];
  if (!data.length) return null;

  const hit = data[0];
  return {
    latitude: Number(hit.lat),
    longitude: Number(hit.lon),
    address: parseNominatimAddress(hit.address),
    displayName: hit.display_name ?? "",
  };
}

/**
 * Resolve a coordinate pair into a structured address. Returns `null` if the
 * pin lands somewhere Nominatim can't name (open ocean, polar regions).
 */
export async function reverseGeocode(
  latitude: number,
  longitude: number,
  signal?: AbortSignal,
): Promise<GeocodeResult | null> {
  if (!Number.isFinite(latitude) || !Number.isFinite(longitude)) return null;

  const url = `${NOMINATIM_BASE}/reverse?lat=${latitude}&lon=${longitude}&format=json&addressdetails=1`;
  const res = await fetch(url, {
    headers: { "Accept-Language": "en" },
    signal,
  });
  if (!res.ok) throw new Error(`Reverse geocoding failed: ${res.status}`);

  const hit = (await res.json()) as NominatimItem | { error?: string };
  if (!hit || "error" in hit || !("lat" in hit)) return null;

  const parsed = parseNominatimAddress(hit.address);
  return {
    latitude: Number(hit.lat),
    longitude: Number(hit.lon),
    address: parsed,
    displayName: hit.display_name ?? "",
  };
}

/**
 * Great-circle distance between two coordinates in kilometres. Used to
 * decide whether the host's typed address and the dropped pin are close
 * enough to belong to the same listing or whether we should warn.
 *
 * Uses the haversine formula. Good for any distance scale we care about
 * here — within a single city's worth of error we'd never warn anyway.
 */
export function haversineKm(
  a: { latitude: number; longitude: number },
  b: { latitude: number; longitude: number },
): number {
  const R = 6_371; // mean Earth radius (km)
  const toRad = (deg: number) => (deg * Math.PI) / 180;
  const dLat = toRad(b.latitude - a.latitude);
  const dLon = toRad(b.longitude - a.longitude);
  const lat1 = toRad(a.latitude);
  const lat2 = toRad(b.latitude);
  const h =
    Math.sin(dLat / 2) ** 2 +
    Math.cos(lat1) * Math.cos(lat2) * Math.sin(dLon / 2) ** 2;
  return 2 * R * Math.asin(Math.min(1, Math.sqrt(h)));
}

/** Build a single-line query string from a partially-filled structured address. */
export function structuredAddressToQuery(parts: Partial<ParsedAddress>): string {
  return [parts.street, parts.city, parts.state, parts.zipCode, parts.country]
    .map((p) => p?.trim() ?? "")
    .filter(Boolean)
    .join(", ");
}

/**
 * "Looks complete enough to be worth geocoding" — at least city + state OR
 * city + country, plus a postal code or a street. This stops us from
 * forward-geocoding every keystroke while the host is still typing.
 */
export function isAddressGeocodable(parts: Partial<ParsedAddress>): boolean {
  const street = parts.street?.trim() ?? "";
  const city = parts.city?.trim() ?? "";
  const state = parts.state?.trim() ?? "";
  const country = parts.country?.trim() ?? "";
  const zip = parts.zipCode?.trim() ?? "";

  const hasCityOrZip = (city.length > 0) || (zip.length > 0);
  const hasRegionOrCountry = (state.length > 0) || (country.length > 0);
  const hasStreet = street.length > 0;

  return hasCityOrZip && hasRegionOrCountry && hasStreet;
}
