import type { AppMode } from "./modeStore";

type PathRule = {
  match: (pathname: string) => boolean;
  /**
   * Where to send the user when switching into guest / travelling mode.
   * `null` means the current path is already valid for guest mode.
   */
  guestTarget: string | null;
  /**
   * Where to send the user when switching into host / hosting mode.
   * `null` means the current path is already valid for host mode
   * (e.g. stay on `/app/listings/new` instead of collapsing to `/app/listings`).
   */
  hostTarget: string | null;
};

/**
 * Routes that only make sense in one dashboard mode. When the user flips
 * the Travelling ↔ Hosting switch (or lands with a mismatched persisted
 * mode), we bounce them to the closest equivalent surface rather than
 * leaving them on a page whose sidebar link just vanished.
 *
 * Shared routes like `/app/deals` intentionally omit rules here — those
 * pages read `useModeStore` and adapt in place.
 */
const MODE_SCOPED_ROUTES: PathRule[] = [
  {
    match: (p) => p === "/app/applications",
    guestTarget: "/app/my-applications",
    hostTarget: "/app/applications",
  },
  {
    match: (p) => p === "/app/my-applications",
    guestTarget: "/app/my-applications",
    hostTarget: "/app/applications",
  },
  {
    match: (p) => p === "/app/inquiries",
    guestTarget: "/app/my-inquiries",
    hostTarget: "/app/inquiries",
  },
  {
    match: (p) => p === "/app/my-inquiries",
    guestTarget: "/app/my-inquiries",
    hostTarget: "/app/inquiries",
  },
  {
    // Entire hosting listings tree (list, create, detail, edit). Guest mode
    // leaves this area; host mode must keep the exact URL.
    match: (p) => p.startsWith("/app/listings"),
    guestTarget: "/app",
    hostTarget: null,
  },
  {
    match: (p) => p === "/app/channels",
    guestTarget: "/app",
    hostTarget: "/app/channels",
  },
  {
    match: (p) => p === "/app/payout-setup" || p === "/app/stripe-onboarding",
    guestTarget: "/app/profile",
    hostTarget: "/app/payout-setup",
  },
];

/**
 * Returns a replacement path when `pathname` belongs to the *other* mode,
 * or `null` when the current URL is fine for `mode`.
 */
export function resolveModeSwitchRedirect(
  pathname: string,
  mode: AppMode,
): string | null {
  for (const rule of MODE_SCOPED_ROUTES) {
    if (!rule.match(pathname)) continue;
    const target = mode === "guest" ? rule.guestTarget : rule.hostTarget;
    if (target == null || target === pathname) return null;
    return target;
  }
  return null;
}
