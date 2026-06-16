import { useEffect, useLayoutEffect, useMemo, useRef, useState } from "react";
import { Calendar as CalendarIcon, X } from "lucide-react";
import { DateRangeCalendar, type DateRange } from "@/features/listings/components/DateRangeCalendar";
import { cn } from "@/lib/utils";

/**
 * Width target for the popover in pixels. Matches the `38rem` clamp the
 * popover renders at on roomy viewports (Tailwind defaults: 1rem = 16px).
 * We use it purely for the right-overflow heuristic — the popover itself
 * still caps itself with `min(38rem, calc(100vw - 2rem))` so it can
 * always physically fit even if our pre-paint guess is off.
 */
const POPOVER_TARGET_WIDTH_PX = 38 * 16;
const VIEWPORT_PADDING_PX = 16;

export type DateRangeFieldValue = {
  /** YYYY-MM-DD (local time safe) or empty string. */
  checkIn: string;
  /** YYYY-MM-DD (local time safe) or empty string. */
  checkOut: string;
};

type Props = {
  value: DateRangeFieldValue;
  onChange: (next: DateRangeFieldValue) => void;
  /** Minimum stay (days). Used for the helper hint only. */
  minStayDays?: number;
  /** Maximum stay (days). Used for the helper hint only. */
  maxStayDays?: number;
  /** Optional layout hint — "stacked" renders one button, "twin" renders
   *  two segmented buttons styled like a check-in/check-out pair. */
  layout?: "stacked" | "twin";
  /**
   * When true, the field renders in an error state — destructive border,
   * destructive label colour on whichever segment is empty, and (if set)
   * an `errorMessage` underneath. Used by the booking + apply flows to
   * highlight the field after the host clicks the CTA without picking
   * dates first.
   */
  error?: boolean;
  /**
   * Optional message rendered in destructive text below the field. Only
   * shown when `error` is true. Keep short — long messages collide with
   * the "Stay range" hint on the same row.
   */
  errorMessage?: string;
  /** Optional id linking the field to an external label / aria-describedby. */
  id?: string;
  className?: string;
};

/**
 * Drop-in replacement for the previous twin `<input type="date">` pair
 * that lived inside `BookingPanel` and `ApplyDialog`. Surfaces the
 * existing `DateRangeCalendar` (a two-month, range-aware grid used by
 * the marketplace hero search) inside a click-to-open popover so the
 * booking flow shares the same picker the search bar already uses.
 *
 * The value is exchanged as `YYYY-MM-DD` strings (local-timezone safe)
 * so it slots straight into existing apply/quote API requests without
 * any Date<->string juggling at the call site.
 */
export function DateRangeField({
  value,
  onChange,
  minStayDays,
  maxStayDays,
  layout = "twin",
  error = false,
  errorMessage,
  id,
  className,
}: Props) {
  const [open, setOpen] = useState(false);
  const [activeSegment, setActiveSegment] = useState<"checkin" | "checkout">("checkin");
  // When the popover would overrun the right edge of the viewport (most
  // common with the BookingPanel rendered in the right sidebar on
  // desktop), we flip its anchor from the field's left edge to its
  // right edge so the calendar extends inward instead of clipping
  // off-screen. Recomputed on every open + on resize while open.
  const [align, setAlign] = useState<"left" | "right">("left");
  const containerRef = useRef<HTMLDivElement | null>(null);

  const range = useMemo<DateRange>(
    () => ({
      start: value.checkIn ? parseLocalDate(value.checkIn) : null,
      end: value.checkOut ? parseLocalDate(value.checkOut) : null,
    }),
    [value.checkIn, value.checkOut],
  );

  useEffect(() => {
    if (!open) return;
    const onDown = (e: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setOpen(false);
      }
    };
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") setOpen(false);
    };
    document.addEventListener("mousedown", onDown);
    document.addEventListener("keydown", onKey);
    return () => {
      document.removeEventListener("mousedown", onDown);
      document.removeEventListener("keydown", onKey);
    };
  }, [open]);

  // Re-anchor the popover before paint to dodge right-edge overflow.
  useLayoutEffect(() => {
    if (!open) return;
    const recompute = () => {
      const node = containerRef.current;
      if (!node) return;
      const rect = node.getBoundingClientRect();
      const viewportWidth = window.innerWidth;
      const spaceOnRight = viewportWidth - rect.left - VIEWPORT_PADDING_PX;
      const spaceOnLeft = rect.right - VIEWPORT_PADDING_PX;
      // Prefer left-anchored (extends right) when there is room.
      // Flip to right-anchored when the right-side overflow would be
      // worse than the left-side overflow — i.e. when the popover would
      // clip the viewport's right edge AND the field has more breathing
      // room to its left than its right.
      if (spaceOnRight < POPOVER_TARGET_WIDTH_PX && spaceOnLeft > spaceOnRight) {
        setAlign("right");
      } else {
        setAlign("left");
      }
    };
    recompute();
    window.addEventListener("resize", recompute);
    return () => window.removeEventListener("resize", recompute);
  }, [open]);

  const handleRangeChange = (next: DateRange) => {
    const nextValue: DateRangeFieldValue = {
      checkIn: next.start ? toIsoDate(next.start) : "",
      checkOut: next.end ? toIsoDate(next.end) : "",
    };
    onChange(nextValue);

    // Two-step UX: choosing the start advances the "active" segment so
    // the popover stays open and the user can keep tapping for the end
    // date; choosing the end closes the popover.
    if (next.start && !next.end) {
      setActiveSegment("checkout");
    } else if (next.start && next.end) {
      setOpen(false);
      setActiveSegment("checkin");
    }
  };

  const openTo = (segment: "checkin" | "checkout") => {
    setActiveSegment(segment);
    setOpen(true);
  };

  const clear = (e: React.MouseEvent) => {
    e.preventDefault();
    e.stopPropagation();
    onChange({ checkIn: "", checkOut: "" });
  };

  const checkInLabel = value.checkIn ? formatDateShort(value.checkIn) : "Add date";
  const checkOutLabel = value.checkOut ? formatDateShort(value.checkOut) : "Add date";
  const hasAny = Boolean(value.checkIn || value.checkOut);

  // Use the destructive-border-and-ring trick to show error state without
  // changing the layout. Per-segment colour adjusts below so that the
  // empty side of a "check-in picked but no check-out" combo is the one
  // tinted destructive — clearer than dyeing the whole field red.
  const errorRingClass = error
    ? "ring-2 ring-destructive/50 border-destructive"
    : "";
  const errorId = id ? `${id}-error` : undefined;

  return (
    <div
      ref={containerRef}
      className={cn("relative", className)}
      aria-invalid={error || undefined}
      aria-describedby={error && errorMessage ? errorId : undefined}
    >
      {layout === "twin" ? (
        <div
          className={cn(
            "grid grid-cols-2 overflow-hidden rounded-lg border bg-background transition-colors",
            errorRingClass,
          )}
        >
          <Segment
            label="Check-in"
            value={checkInLabel}
            placeholder="Add date"
            isFilled={Boolean(value.checkIn)}
            isActive={open && activeSegment === "checkin"}
            onClick={() => openTo("checkin")}
            hasError={error && !value.checkIn}
          />
          <Segment
            label="Check-out"
            value={checkOutLabel}
            placeholder="Add date"
            isFilled={Boolean(value.checkOut)}
            isActive={open && activeSegment === "checkout"}
            onClick={() => openTo("checkout")}
            hasError={error && !value.checkOut}
            withDivider
          />
        </div>
      ) : (
        <button
          type="button"
          onClick={() => openTo("checkin")}
          className={cn(
            "flex w-full items-center gap-3 rounded-lg border bg-background px-4 py-2.5 text-left transition-colors hover:bg-secondary",
            open && "ring-2 ring-primary",
            errorRingClass,
          )}
        >
          <CalendarIcon
            className={cn(
              "h-4 w-4",
              error ? "text-destructive" : "text-muted-foreground",
            )}
          />
          <span className="flex-1 text-sm">
            {hasAny ? (
              <>
                <span className="font-medium">{checkInLabel}</span>
                <span className="mx-2 text-muted-foreground">→</span>
                <span className="font-medium">{checkOutLabel}</span>
              </>
            ) : (
              <span
                className={cn(error ? "text-destructive" : "text-muted-foreground")}
              >
                Pick your dates
              </span>
            )}
          </span>
        </button>
      )}

      <div className="mt-2 flex items-center justify-between text-xs">
        {error && errorMessage ? (
          <span id={errorId} role="alert" className="text-destructive">
            {errorMessage}
          </span>
        ) : (
          <span className="text-muted-foreground">
            {minStayDays && maxStayDays
              ? `Stay range: ${minStayDays}–${maxStayDays} days`
              : minStayDays
                ? `Minimum stay: ${minStayDays} days`
                : ""}
          </span>
        )}
        {hasAny && (
          <button
            type="button"
            onClick={clear}
            className="inline-flex items-center gap-1 font-medium text-foreground hover:underline"
          >
            <X className="h-3 w-3" />
            Clear
          </button>
        )}
      </div>

      {open && (
        <div
          className={cn(
            "absolute top-full z-30 mt-2 w-[min(38rem,calc(100vw-2rem))]",
            align === "right" ? "right-0" : "left-0",
          )}
        >
          <div className="rounded-2xl border bg-background p-4 shadow-[0_24px_60px_-20px_rgba(15,23,42,0.35)]">
            <DateRangeCalendar range={range} onChange={handleRangeChange} />
          </div>
        </div>
      )}
    </div>
  );
}

function Segment({
  label,
  value,
  placeholder,
  isActive,
  isFilled,
  onClick,
  withDivider,
  hasError,
}: {
  label: string;
  value: string;
  placeholder: string;
  isActive: boolean;
  isFilled: boolean;
  onClick: () => void;
  withDivider?: boolean;
  /** Tint the segment destructive — used when the parent field is in error
   *  state AND this particular segment is the empty one. */
  hasError?: boolean;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      aria-invalid={hasError || undefined}
      className={cn(
        "group flex flex-col items-start gap-0.5 px-4 py-2.5 text-left transition-colors",
        withDivider && "border-l",
        isActive
          ? "bg-secondary"
          : "hover:bg-secondary/60",
      )}
    >
      <span
        className={cn(
          "text-[11px] font-semibold uppercase tracking-wide",
          hasError ? "text-destructive" : "text-muted-foreground",
        )}
      >
        {label}
      </span>
      <span
        className={cn(
          "truncate text-sm",
          isFilled
            ? "font-medium text-foreground"
            : hasError
              ? "text-destructive"
              : "text-muted-foreground/80",
        )}
      >
        {isFilled ? value : placeholder}
      </span>
    </button>
  );
}

function toIsoDate(d: Date) {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, "0");
  const day = String(d.getDate()).padStart(2, "0");
  return `${y}-${m}-${day}`;
}

function parseLocalDate(iso: string): Date | null {
  const m = /^(\d{4})-(\d{2})-(\d{2})$/.exec(iso);
  if (!m) return null;
  return new Date(Number(m[1]), Number(m[2]) - 1, Number(m[3]));
}

function formatDateShort(iso: string): string {
  const d = parseLocalDate(iso);
  if (!d) return iso;
  return d.toLocaleDateString(undefined, { month: "short", day: "numeric" });
}
