import { describe, expect, it } from "vitest";
import { matchPopularDestinations } from "./popularDestinations";
import {
  applyLocationToSearchParams,
  mergePlaceSuggestions,
  nominatimToPlace,
  nominatimToSuggestion,
  radiusBounds,
} from "./placeSearch";

describe("nominatimToPlace", () => {
  it("uses Nominatim's south/north/west/east bounding box", () => {
    const place = nominatimToPlace({
      lat: "30.2672",
      lon: "-97.7431",
      boundingbox: ["30.098", "30.517", "-97.938", "-97.568"],
    });

    expect(place).toEqual({
      lat: 30.2672,
      lng: -97.7431,
      bounds: {
        swLat: 30.098,
        neLat: 30.517,
        swLng: -97.938,
        neLng: -97.568,
        centerLat: 30.2672,
        centerLng: -97.7431,
      },
    });
  });

  it("falls back to a radius box when Nominatim omits a bounding box", () => {
    const place = nominatimToPlace({ lat: "30.2672", lon: "-97.7431" });
    expect(place?.lat).toBeCloseTo(30.2672);
    expect(place?.bounds.swLat).toBeLessThan(30.2672);
    expect(place?.bounds.neLat).toBeGreaterThan(30.2672);
  });

  it("rejects unparseable hits", () => {
    expect(nominatimToPlace({})).toBeNull();
  });
});

describe("applyLocationToSearchParams", () => {
  it("drops the keyword once a place is resolved so city names are not title-matched", () => {
    const params = applyLocationToSearchParams(
      { page: 1, pageSize: 20 },
      {
        keyword: "Austin, TX",
        placeResolved: true,
        userMapBounds: null,
        placeBounds: radiusBounds(30.27, -97.74, 20),
        radiusAnchor: null,
      },
    );

    expect(params.keyword).toBeUndefined();
    expect(params.swLat).toBeDefined();
    expect(params.neLng).toBeDefined();
  });

  it("keeps the keyword when the term is not a place", () => {
    const params = applyLocationToSearchParams(
      { page: 1 },
      {
        keyword: "furnished loft",
        placeResolved: false,
        userMapBounds: null,
        placeBounds: null,
        radiusAnchor: null,
      },
    );

    expect(params.keyword).toBe("furnished loft");
    expect(params.swLat).toBeUndefined();
  });

  it("prefers the user's panned map bounds over the original place box", () => {
    const params = applyLocationToSearchParams(
      { page: 1 },
      {
        keyword: "Austin, TX",
        placeResolved: true,
        userMapBounds: {
          swLat: 30.2,
          swLng: -97.8,
          neLat: 30.3,
          neLng: -97.7,
          centerLat: 30.25,
          centerLng: -97.75,
        },
        placeBounds: radiusBounds(30.27, -97.74, 50),
        radiusAnchor: null,
      },
    );

    expect(params.swLat).toBe(30.2);
    expect(params.neLng).toBe(-97.7);
  });
});

describe("nominatimToSuggestion", () => {
  it("formats a California city as City, CA", () => {
    const suggestion = nominatimToSuggestion({
      lat: "34.0522",
      lon: "-118.2437",
      addresstype: "city",
      osm_type: "relation",
      osm_id: 1,
      address: { city: "Los Angeles", state: "California", country: "United States" },
    });

    expect(suggestion?.label).toBe("Los Angeles, CA");
    expect(suggestion?.hint).toBe("City");
    expect(suggestion?.query).toBe("Los Angeles, CA");
    expect(suggestion?.place?.lat).toBeCloseTo(34.0522);
  });

  it("formats a street address for autofill", () => {
    const suggestion = nominatimToSuggestion({
      lat: "37.3318",
      lon: "-122.0301",
      addresstype: "house",
      osm_id: 2,
      address: {
        house_number: "1",
        road: "Infinite Loop",
        city: "Cupertino",
        state: "California",
      },
    });

    expect(suggestion?.label).toBe("1 Infinite Loop");
    expect(suggestion?.hint).toBe("Cupertino, CA");
  });

  it("formats a neighborhood with its city", () => {
    const suggestion = nominatimToSuggestion({
      lat: "37.7599",
      lon: "-122.4148",
      addresstype: "neighbourhood",
      osm_id: 3,
      address: {
        neighbourhood: "Mission District",
        city: "San Francisco",
        state: "California",
      },
    });

    expect(suggestion?.label).toBe("Mission District");
    expect(suggestion?.hint).toBe("San Francisco, CA");
  });

  it("formats a state", () => {
    const suggestion = nominatimToSuggestion({
      lat: "37.2",
      lon: "-119.7",
      addresstype: "state",
      osm_id: 4,
      address: { state: "California", country: "United States" },
    });

    expect(suggestion?.label).toBe("California");
    expect(suggestion?.hint).toBe("United States");
  });
});

describe("mergePlaceSuggestions", () => {
  it("keeps local California matches first and drops duplicate labels", () => {
    const merged = mergePlaceSuggestions(
      [{ id: "p1", label: "Oakland, CA", hint: "Bay Area", query: "Oakland, CA", place: null }],
      [
        { id: "r1", label: "Oakland, CA", hint: "City", query: "Oakland, CA", place: null },
        { id: "r2", label: "Oakland Avenue", hint: "Pasadena, CA", query: "Oakland Avenue", place: null },
      ],
    );

    expect(merged.map((s) => s.id)).toEqual(["p1", "r2"]);
  });
});

describe("matchPopularDestinations", () => {
  it("filters the California set as the user types", () => {
    const hits = matchPopularDestinations("san");
    expect(hits.map((s) => s.label)).toEqual(
      expect.arrayContaining(["San Francisco, CA", "San Diego, CA", "San Jose, CA"]),
    );
    expect(hits.some((s) => s.label === "Los Angeles, CA")).toBe(false);
  });
});
