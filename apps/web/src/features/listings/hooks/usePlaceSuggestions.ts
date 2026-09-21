import { useEffect, useMemo, useState } from "react";
import { matchPopularDestinations } from "@/features/listings/lib/popularDestinations";
import {
  mergePlaceSuggestions,
  suggestPlaces,
  type PlaceSuggestion,
} from "@/features/listings/lib/placeSearch";

const DEBOUNCE_MS = 350;
const MIN_QUERY_LENGTH = 2;

type Options = {
  enabled?: boolean;
  /** When the box is empty, return the California popular set. */
  includePopularWhenEmpty?: boolean;
};

export function usePlaceSuggestions(
  query: string,
  options: Options = {},
): { suggestions: PlaceSuggestion[]; loading: boolean } {
  const { enabled = true, includePopularWhenEmpty = false } = options;
  const [remote, setRemote] = useState<PlaceSuggestion[]>([]);
  const [loading, setLoading] = useState(false);

  const trimmed = query.trim();
  const local = useMemo(() => {
    if (!enabled) return [];
    if (!trimmed) return includePopularWhenEmpty ? matchPopularDestinations("") : [];
    return matchPopularDestinations(trimmed);
  }, [enabled, includePopularWhenEmpty, trimmed]);

  useEffect(() => {
    if (!enabled || trimmed.length < MIN_QUERY_LENGTH) {
      setRemote([]);
      setLoading(false);
      return;
    }

    const controller = new AbortController();
    const timer = window.setTimeout(() => {
      setLoading(true);
      void suggestPlaces(trimmed, controller.signal).then((hits) => {
        if (controller.signal.aborted) return;
        setRemote(hits);
        setLoading(false);
      });
    }, DEBOUNCE_MS);

    return () => {
      window.clearTimeout(timer);
      controller.abort();
    };
  }, [enabled, trimmed]);

  const suggestions = useMemo(
    () => mergePlaceSuggestions(local, remote),
    [local, remote],
  );

  return { suggestions, loading };
}
