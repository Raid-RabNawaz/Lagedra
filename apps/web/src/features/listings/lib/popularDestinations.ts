import { radiusBounds } from "@/features/listings/lib/placeSearch";
import type { PlaceSuggestion } from "@/features/listings/lib/placeSearch";

export type PopularDestination = {
  label: string;
  hint: string;
  lat: number;
  lng: number;
};

/** California-first launch set shown when the Where field is empty. */
export const POPULAR_DESTINATIONS: PopularDestination[] = [
  { label: "Los Angeles, CA", hint: "Southern California", lat: 34.0522, lng: -118.2437 },
  { label: "San Francisco, CA", hint: "Bay Area", lat: 37.7749, lng: -122.4194 },
  { label: "San Diego, CA", hint: "Southern California", lat: 32.7157, lng: -117.1611 },
  { label: "San Jose, CA", hint: "Bay Area", lat: 37.3382, lng: -121.8863 },
  { label: "Oakland, CA", hint: "Bay Area", lat: 37.8044, lng: -122.2712 },
  { label: "Irvine, CA", hint: "Orange County", lat: 33.6846, lng: -117.8265 },
  { label: "Sacramento, CA", hint: "Central Valley", lat: 38.5816, lng: -121.4944 },
  { label: "Santa Barbara, CA", hint: "Central Coast", lat: 34.4208, lng: -119.6982 },
];

export function popularToSuggestion(d: PopularDestination): PlaceSuggestion {
  return {
    id: `popular-${d.label}`,
    label: d.label,
    hint: d.hint,
    query: d.label,
    place: {
      lat: d.lat,
      lng: d.lng,
      bounds: radiusBounds(d.lat, d.lng, 20),
    },
  };
}

export function matchPopularDestinations(query: string): PlaceSuggestion[] {
  const term = query.trim().toLowerCase();
  if (!term) return POPULAR_DESTINATIONS.map(popularToSuggestion);
  return POPULAR_DESTINATIONS.filter(
    (d) => d.label.toLowerCase().includes(term) || d.hint.toLowerCase().includes(term),
  ).map(popularToSuggestion);
}
