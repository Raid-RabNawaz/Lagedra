import { useCallback, useEffect, useState } from "react";

/**
 * Possible states of the browser's Permissions API for `geolocation`.
 *
 * `"unsupported"` covers two distinct realities:
 *   - The user is on a browser too old to expose `navigator.permissions`
 *     (Safari < 16, some embedded WebViews).
 *   - The browser exposes it but doesn't list `geolocation` as a queryable
 *     permission name (rare, but seen on some Linux Firefox builds).
 * In both cases we still have a working `navigator.geolocation`, so the
 * UI should let the user *try* — we just can't tell them up front whether
 * a prompt is coming.
 *
 * `"unknown"` is the transient initial state before our first probe.
 *
 * `"unavailable"` means there is no geolocation API at all — typically a
 * non-secure context (HTTP page) or a browser with the feature disabled.
 */
export type GeolocationPermissionState =
  | "unknown"
  | "prompt"
  | "granted"
  | "denied"
  | "unsupported"
  | "unavailable";

export type GeolocationCoords = {
  latitude: number;
  longitude: number;
  /** Reported accuracy of the fix in metres. Useful for radius sizing. */
  accuracyMeters: number;
  /** Epoch ms when the fix was captured. */
  capturedAt: number;
};

export type GeolocationError =
  | { kind: "denied"; message: string }
  | { kind: "unavailable"; message: string }
  | { kind: "timeout"; message: string }
  | { kind: "unknown"; message: string };

type UseGeolocationOptions = {
  /**
   * If true, the hook fires `navigator.geolocation.getCurrentPosition`
   * immediately on mount IF the user has already granted permission in
   * a previous session. We deliberately do NOT auto-prompt — surfacing
   * a permission dialog without user intent is hostile UX and gets the
   * site blacklisted in Brave / shields-up browsers.
   *
   * Defaults to true: granted users get a seamless experience, prompt
   * users see no popup until they click a CTA.
   */
  autoFetchWhenGranted?: boolean;

  /**
   * Soft TTL on cached coordinates (ms). Inside this window, a re-mount
   * (e.g. navigating between marketplace pages) reuses the last fix from
   * `sessionStorage` instead of hitting the GPS again. Tuned conservatively
   * — mid-term rentals are city-scoped, the user rarely moves more than a
   * few km between page loads.
   *
   * Defaults to 30 minutes.
   */
  maxAgeMs?: number;
};

type UseGeolocationReturn = {
  /** Last known coordinates or `null` when we don't have a fix. */
  coords: GeolocationCoords | null;
  /** Current best-effort view of the permission status. */
  permission: GeolocationPermissionState;
  /** True while a `getCurrentPosition` call is in flight. */
  loading: boolean;
  /** Last error from the geolocation API, or `null` on success / idle. */
  error: GeolocationError | null;
  /**
   * Imperative trigger — call this from a user gesture (button click) to
   * request a fresh fix. Will prompt for permission the first time. Safe
   * to call when already granted; behaves like a refresh.
   */
  requestLocation: () => void;
  /**
   * Wipe local state so the user can re-prompt later (e.g. they previously
   * dismissed a banner and want to try again). Does NOT revoke browser
   * permission — that's the browser's job via the address-bar UI.
   */
  clear: () => void;
};

const SESSION_KEY = "lagedra.geolocation";

function readCached(maxAgeMs: number): GeolocationCoords | null {
  if (typeof window === "undefined") return null;
  try {
    const raw = window.sessionStorage.getItem(SESSION_KEY);
    if (!raw) return null;
    const parsed = JSON.parse(raw) as GeolocationCoords;
    if (
      typeof parsed?.latitude !== "number" ||
      typeof parsed?.longitude !== "number" ||
      typeof parsed?.capturedAt !== "number"
    ) {
      return null;
    }
    if (Date.now() - parsed.capturedAt > maxAgeMs) {
      return null;
    }
    return parsed;
  } catch {
    return null;
  }
}

function writeCached(coords: GeolocationCoords) {
  if (typeof window === "undefined") return;
  try {
    window.sessionStorage.setItem(SESSION_KEY, JSON.stringify(coords));
  } catch {
    /* private mode / quota — silently ignore, the in-memory state still works */
  }
}

function clearCached() {
  if (typeof window === "undefined") return;
  try {
    window.sessionStorage.removeItem(SESSION_KEY);
  } catch {
    /* ignore */
  }
}

/**
 * Thin wrapper over `navigator.geolocation` + the Permissions API designed
 * for marketplace "near you" features. Three things make this hook
 * different from a naive `getCurrentPosition` call:
 *
 *   1. It never prompts implicitly. The permission dialog only opens
 *      when `requestLocation()` is called from a user gesture, or when
 *      a prior session has already granted permission and `autoFetchWhenGranted`
 *      is left at its default.
 *   2. It exposes a discriminated `permission` value so callers can branch
 *      between "ask the user", "show 'near you'", and "fall back to
 *      Featured" without ad-hoc try/catch chains.
 *   3. It caches the last fix in `sessionStorage` so route changes inside
 *      the SPA don't re-hit GPS / IP-geo. Tuned to 30 min by default.
 */
export function useGeolocation(
  options: UseGeolocationOptions = {},
): UseGeolocationReturn {
  const { autoFetchWhenGranted = true, maxAgeMs = 30 * 60 * 1000 } = options;

  const [permission, setPermission] = useState<GeolocationPermissionState>(
    () => {
      if (typeof navigator === "undefined") return "unavailable";
      if (!("geolocation" in navigator)) return "unavailable";
      return "unknown";
    },
  );
  const [coords, setCoords] = useState<GeolocationCoords | null>(() =>
    readCached(maxAgeMs),
  );
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<GeolocationError | null>(null);

  // Probe the Permissions API once on mount. This is the *only* way to
  // distinguish "user hasn't decided yet" from "user explicitly denied"
  // without showing a prompt — both look identical to getCurrentPosition.
  useEffect(() => {
    let cancelled = false;
    if (typeof navigator === "undefined") return;
    if (!("permissions" in navigator) || !navigator.permissions?.query) {
      setPermission((prev) => (prev === "unknown" ? "unsupported" : prev));
      return;
    }

    navigator.permissions
      .query({ name: "geolocation" as PermissionName })
      .then((status) => {
        if (cancelled) return;
        setPermission(status.state as GeolocationPermissionState);
        // Browsers fire `change` when the user flips the choice from the
        // URL-bar UI. Keep the hook reactive to that without forcing a
        // page reload.
        status.onchange = () => {
          if (cancelled) return;
          setPermission(status.state as GeolocationPermissionState);
          if (status.state === "denied") {
            setCoords(null);
            clearCached();
          }
        };
      })
      .catch(() => {
        if (cancelled) return;
        setPermission("unsupported");
      });

    return () => {
      cancelled = true;
    };
  }, []);

  const fetchPosition = useCallback(() => {
    if (typeof navigator === "undefined" || !("geolocation" in navigator)) {
      setPermission("unavailable");
      setError({
        kind: "unavailable",
        message: "Geolocation is not available in this browser.",
      });
      return;
    }

    setLoading(true);
    setError(null);
    navigator.geolocation.getCurrentPosition(
      (pos) => {
        const next: GeolocationCoords = {
          latitude: pos.coords.latitude,
          longitude: pos.coords.longitude,
          accuracyMeters: pos.coords.accuracy,
          capturedAt: Date.now(),
        };
        setCoords(next);
        writeCached(next);
        setPermission("granted");
        setLoading(false);
      },
      (err) => {
        setLoading(false);
        if (err.code === err.PERMISSION_DENIED) {
          setPermission("denied");
          setCoords(null);
          clearCached();
          setError({ kind: "denied", message: err.message || "Permission denied." });
        } else if (err.code === err.POSITION_UNAVAILABLE) {
          setError({
            kind: "unavailable",
            message: err.message || "Position unavailable.",
          });
        } else if (err.code === err.TIMEOUT) {
          setError({ kind: "timeout", message: err.message || "Request timed out." });
        } else {
          setError({ kind: "unknown", message: err.message || "Unknown geolocation error." });
        }
      },
      {
        // 8s is a balance between "fast lock indoors over Wi-Fi" and "give
        // the GPS chip enough time on mobile cold start". Anything shorter
        // tends to time out before iOS finishes its first fix.
        timeout: 8_000,
        // Accept anything cached by the OS up to a minute old — keeps the
        // hook responsive even when the underlying location service is
        // slow to repond.
        maximumAge: 60_000,
        enableHighAccuracy: false,
      },
    );
  }, []);

  // Auto-fetch when permission is already granted from a previous session.
  // We deliberately key this on a fresh permission probe to avoid kicking
  // off a fetch on every re-render.
  useEffect(() => {
    if (!autoFetchWhenGranted) return;
    if (permission !== "granted") return;
    if (coords) return;
    fetchPosition();
  }, [autoFetchWhenGranted, permission, coords, fetchPosition]);

  const clear = useCallback(() => {
    setCoords(null);
    setError(null);
    clearCached();
  }, []);

  return {
    coords,
    permission,
    loading,
    error,
    requestLocation: fetchPosition,
    clear,
  };
}
