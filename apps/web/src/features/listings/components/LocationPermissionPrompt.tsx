import { MapPin, Loader2, X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import type {
  GeolocationError,
  GeolocationPermissionState,
} from "@/features/listings/hooks/useGeolocation";

type Props = {
  permission: GeolocationPermissionState;
  loading: boolean;
  error: GeolocationError | null;
  onEnable: () => void;
  onDismiss?: () => void;
  className?: string;
};

/**
 * Inline banner that asks the user to share their location so we can
 * surface nearby rentals. Designed to live just above the "Featured
 * rentals" carousel on the marketplace home page — it stays out of the
 * way once permission is granted (caller hides it) and never bypasses
 * the browser's own prompt.
 *
 * Renders three distinct states:
 *   - "prompt" / "unknown" / "unsupported" → friendly opt-in
 *   - "denied"                              → explain why the section is
 *                                             showing featured listings
 *                                             instead, with a hint to
 *                                             re-enable from browser
 *                                             settings
 *   - "unavailable"                         → quiet message, no CTA
 *                                             (nothing the user can do
 *                                             from inside the page)
 *
 * The "granted" state is intentionally NOT rendered — the caller should
 * unmount the banner once it has coordinates so the home page doesn't
 * carry a permanent "we're using your location" strip. Browser address-
 * bar UI already signals that.
 */
export function LocationPermissionPrompt({
  permission,
  loading,
  error,
  onEnable,
  onDismiss,
  className,
}: Props) {
  // Nothing to show when the user has already shared (caller unmounts us
  // in that case but be defensive in case of stale props).
  if (permission === "granted") return null;

  const isDenied = permission === "denied" || error?.kind === "denied";
  const isUnavailable = permission === "unavailable" || error?.kind === "unavailable";

  // Pick copy that matches the user's actual state. The tone shifts from
  // inviting ("see nearby rentals") to explanatory ("we can't show
  // nearby rentals") so we never claim something the data can't back up.
  const { title, body, cta } = (() => {
    if (isDenied) {
      return {
        title: "Showing featured rentals",
        body:
          "Location is blocked, so we can't sort by what's near you. " +
          "Turn it on from the browser's address bar to see nearby places.",
        cta: null as null | string,
      };
    }
    if (isUnavailable) {
      return {
        title: "Showing featured rentals",
        body: "Location isn't available on this device — showing top picks instead.",
        cta: null,
      };
    }
    return {
      title: "See rentals near you",
      body:
        "Share your location so we can surface listings within driving " +
        "distance. We never store it on your account.",
      cta: "Enable location" as const,
    };
  })();

  return (
    <div
      className={cn(
        "flex flex-col gap-3 rounded-2xl border bg-surface px-4 py-3 sm:flex-row sm:items-center sm:justify-between sm:px-5 sm:py-4",
        className,
      )}
      role="region"
      aria-label="Location preference"
    >
      <div className="flex items-start gap-3 min-w-0">
        <span
          className={cn(
            "mt-0.5 flex h-9 w-9 shrink-0 items-center justify-center rounded-full",
            isDenied || isUnavailable
              ? "bg-muted text-muted-foreground"
              : "bg-primary/10 text-primary",
          )}
        >
          <MapPin className="h-4 w-4" />
        </span>
        <div className="min-w-0">
          <p className="text-sm font-semibold leading-tight">{title}</p>
          <p className="mt-0.5 text-xs text-muted-foreground leading-snug">
            {body}
          </p>
        </div>
      </div>

      <div className="flex items-center gap-2 self-end sm:self-auto shrink-0">
        {cta && (
          <Button
            type="button"
            size="sm"
            onClick={onEnable}
            disabled={loading}
            className="gap-1.5"
          >
            {loading ? (
              <Loader2 className="h-3.5 w-3.5 animate-spin" />
            ) : (
              <MapPin className="h-3.5 w-3.5" />
            )}
            {loading ? "Locating…" : cta}
          </Button>
        )}
        {onDismiss && (
          <Button
            type="button"
            size="icon"
            variant="ghost"
            className="h-8 w-8 rounded-full"
            onClick={onDismiss}
            aria-label="Dismiss"
            title="Dismiss"
          >
            <X className="h-4 w-4" />
          </Button>
        )}
      </div>
    </div>
  );
}
