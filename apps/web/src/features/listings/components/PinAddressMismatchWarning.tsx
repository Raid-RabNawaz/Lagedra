import { AlertTriangle, MapPin, Crosshair } from "lucide-react";
import { Button } from "@/components/ui/button";

type Props = {
  /** Computed great-circle distance between pin and geocoded address (km). */
  distanceKm: number;
  /** The pin's reverse-geocoded display name. Used to give the host context. */
  pinDisplayName: string | null;
  /** The structured address the host typed, summarised. */
  typedDisplayName: string;
  /** Forward-geocodes the typed address and moves the pin to match. */
  onMovePinToAddress: () => void;
  /** Reverse-geocodes the pin and overwrites the structured address fields. */
  onCopyAddressFromPin: () => void;
  /** True while either reconciliation action is in flight. */
  busy: boolean;
};

/**
 * Inline warning shown when the host's typed address and the dropped pin
 * resolve to coordinates that are too far apart to plausibly describe the
 * same listing. Surfaces two one-click fixes — "move pin to match address"
 * and "use pin's address" — so the host can resolve the mismatch without
 * leaving the page.
 *
 * Severity escalates with distance:
 *   - 5–25 km    → amber, informational ("the pin is a few towns over")
 *   - 25–100 km  → amber, with a stronger headline
 *   - > 100 km   → destructive styling (different city/state entirely)
 *
 * The parent component decides whether to *block* the save based on
 * `distanceKm`; this component only renders the visible message.
 */
export function PinAddressMismatchWarning({
  distanceKm,
  pinDisplayName,
  typedDisplayName,
  onMovePinToAddress,
  onCopyAddressFromPin,
  busy,
}: Props) {
  const severe = distanceKm > 100;
  const rounded =
    distanceKm < 10 ? distanceKm.toFixed(1) : Math.round(distanceKm).toString();

  return (
    <div
      role="alert"
      className={
        "rounded-lg border p-4 " +
        (severe
          ? "border-destructive/60 bg-destructive/5 text-destructive"
          : "border-amber-500/60 bg-amber-50 text-amber-900 dark:bg-amber-950/30 dark:text-amber-100")
      }
    >
      <div className="flex items-start gap-3">
        <AlertTriangle className="mt-0.5 h-5 w-5 shrink-0" />
        <div className="space-y-2 text-sm">
          <p className="font-semibold leading-tight">
            {severe
              ? `Pin is ${rounded} km from the typed address`
              : `Pin and address look a bit far apart (${rounded} km)`}
          </p>
          <p className="leading-snug opacity-90">
            Tenants rely on both the map pin and the written address. Pick one
            source of truth so a search for{" "}
            <span className="font-medium">{shortLabel(typedDisplayName)}</span>{" "}
            doesn't surface a listing pinned somewhere else.
          </p>
          {pinDisplayName && (
            <p className="text-xs leading-snug opacity-75">
              <span className="font-medium">Pin currently points at:</span>{" "}
              {shortLabel(pinDisplayName)}
            </p>
          )}
          <div className="flex flex-wrap gap-2 pt-1">
            <Button
              type="button"
              size="sm"
              variant="outline"
              onClick={onMovePinToAddress}
              disabled={busy}
              className="gap-1.5"
            >
              <MapPin className="h-3.5 w-3.5" />
              Move pin to address
            </Button>
            <Button
              type="button"
              size="sm"
              variant="outline"
              onClick={onCopyAddressFromPin}
              disabled={busy}
              className="gap-1.5"
            >
              <Crosshair className="h-3.5 w-3.5" />
              Use pin's address
            </Button>
          </div>
        </div>
      </div>
    </div>
  );
}

function shortLabel(s: string): string {
  if (!s) return "(empty)";
  // Display names from Nominatim can be 6+ comma-separated tokens including
  // country, county, postcode in awkward order. Trim to the first 4 parts so
  // the warning copy stays readable on a single line.
  const parts = s.split(",").map((p) => p.trim()).filter(Boolean);
  return parts.slice(0, 4).join(", ");
}
