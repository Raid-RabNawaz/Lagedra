import { useEffect, useLayoutEffect, useMemo, useRef, useState } from "react";
import { Calendar as CalendarIcon, ChevronLeft, ChevronRight, X } from "lucide-react";
import { cn } from "@/lib/utils";

/**
 * Single-date picker used across the app in place of native
 * `<input type="date">` (which renders as an inconsistent, unstyled browser
 * control). Values are exchanged as `YYYY-MM-DD` strings — the same wire
 * format the native input used — so call sites keep their existing state
 * and API plumbing.
 *
 * The header's month + year dropdowns allow jumping decades in two clicks,
 * which matters for date-of-birth fields where arrow-paging 300 months back
 * is hostile.
 */
type DatePickerProps = {
  /** Selected date as `YYYY-MM-DD`, or empty string for none. */
  value: string;
  onChange: (value: string) => void;
  id?: string;
  placeholder?: string;
  /** Earliest selectable date (`YYYY-MM-DD`), inclusive. */
  min?: string;
  /** Latest selectable date (`YYYY-MM-DD`), inclusive. */
  max?: string;
  disabled?: boolean;
  /** Show the inline clear affordance when a date is selected. Default true. */
  clearable?: boolean;
  /** Extra classes for the trigger button (e.g. `h-9` for compact filters). */
  className?: string;
  "aria-label"?: string;
};

const WEEKDAYS = ["Su", "Mo", "Tu", "We", "Th", "Fr", "Sa"];
const MONTHS = [
  "January", "February", "March", "April", "May", "June",
  "July", "August", "September", "October", "November", "December",
];
const POPOVER_WIDTH_PX = 312; // w-[19.5rem]
const VIEWPORT_PADDING_PX = 16;

export function DatePicker({
  value,
  onChange,
  id,
  placeholder = "Pick a date",
  min,
  max,
  disabled = false,
  clearable = true,
  className,
  "aria-label": ariaLabel,
}: DatePickerProps) {
  const [open, setOpen] = useState(false);
  const [align, setAlign] = useState<"left" | "right">("left");
  const containerRef = useRef<HTMLDivElement | null>(null);

  const selected = useMemo(() => parseIsoDate(value), [value]);
  const minDate = useMemo(() => parseIsoDate(min ?? ""), [min]);
  const maxDate = useMemo(() => parseIsoDate(max ?? ""), [max]);

  const today = startOfDay(new Date());

  // Anchor the view on the selection; otherwise on today clamped into the
  // allowed window (so a "future dates only" picker opens on `min`, and a
  // date-of-birth picker capped at today opens on the current month).
  const [viewMonth, setViewMonth] = useState<Date>(() =>
    startOfMonth(selected ?? clampDate(today, minDate, maxDate)),
  );

  useEffect(() => {
    if (!open) return;
    setViewMonth(startOfMonth(selected ?? clampDate(today, minDate, maxDate)));
    // Re-anchor only when the popover opens.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open]);

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

  // Flip the popover to right-anchored when it would clip the viewport edge
  // (same heuristic as DateRangeField).
  useLayoutEffect(() => {
    if (!open) return;
    const recompute = () => {
      const node = containerRef.current;
      if (!node) return;
      const rect = node.getBoundingClientRect();
      const spaceOnRight = window.innerWidth - rect.left - VIEWPORT_PADDING_PX;
      const spaceOnLeft = rect.right - VIEWPORT_PADDING_PX;
      setAlign(spaceOnRight < POPOVER_WIDTH_PX && spaceOnLeft > spaceOnRight ? "right" : "left");
    };
    recompute();
    window.addEventListener("resize", recompute);
    return () => window.removeEventListener("resize", recompute);
  }, [open]);

  const isDayDisabled = (d: Date) =>
    (minDate !== null && isBeforeDay(d, minDate)) || (maxDate !== null && isBeforeDay(maxDate, d));

  const pick = (d: Date) => {
    if (isDayDisabled(d)) return;
    onChange(toIsoDate(d));
    setOpen(false);
  };

  const clear = (e: React.MouseEvent) => {
    e.preventDefault();
    e.stopPropagation();
    onChange("");
  };

  // Year dropdown range: cover min/max when provided, otherwise a window
  // wide enough for both birth dates and forward-looking expiry dates.
  const yearFloor = minDate?.getFullYear() ?? 1904;
  const yearCeil = maxDate?.getFullYear() ?? today.getFullYear() + 10;
  const years = useMemo(() => {
    const list: number[] = [];
    for (let y = yearCeil; y >= yearFloor; y--) list.push(y);
    return list;
  }, [yearFloor, yearCeil]);

  const prevMonth = addMonths(viewMonth, -1);
  const nextMonth = addMonths(viewMonth, 1);
  const canGoPrev = minDate === null || !isBeforeDay(endOfMonth(prevMonth), minDate);
  const canGoNext = maxDate === null || !isBeforeDay(maxDate, nextMonth);
  const todaySelectable = !isDayDisabled(today);

  return (
    <div ref={containerRef} className="relative">
      <button
        type="button"
        id={id}
        disabled={disabled}
        onClick={() => setOpen((v) => !v)}
        aria-haspopup="dialog"
        aria-expanded={open}
        aria-label={ariaLabel}
        className={cn(
          "flex h-11 w-full items-center gap-2 rounded-lg border border-input bg-background px-3 py-2 text-left text-sm ring-offset-background transition-colors",
          "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2",
          "disabled:cursor-not-allowed disabled:opacity-50",
          open && "ring-2 ring-ring ring-offset-2",
          className,
        )}
      >
        <CalendarIcon className="h-4 w-4 shrink-0 text-muted-foreground" />
        <span className={cn("flex-1 truncate", !selected && "text-muted-foreground")}>
          {selected ? formatDisplayDate(selected) : placeholder}
        </span>
        {clearable && selected && !disabled && (
          <span
            role="button"
            tabIndex={-1}
            aria-label="Clear date"
            onClick={clear}
            className="flex h-5 w-5 shrink-0 items-center justify-center rounded-full text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground"
          >
            <X className="h-3.5 w-3.5" />
          </span>
        )}
      </button>

      {open && (
        <div
          role="dialog"
          aria-label="Choose date"
          className={cn(
            "absolute top-full z-30 mt-2 w-[19.5rem] rounded-xl border bg-background p-3 shadow-[0_24px_60px_-20px_rgba(15,23,42,0.35)]",
            align === "right" ? "right-0" : "left-0",
          )}
        >
          {/* Header: month/year quick-jump + paging arrows */}
          <div className="mb-2 flex items-center gap-1.5">
            <select
              aria-label="Month"
              value={viewMonth.getMonth()}
              onChange={(e) => setViewMonth(new Date(viewMonth.getFullYear(), Number(e.target.value), 1))}
              className="h-8 flex-1 cursor-pointer rounded-md border border-input bg-background px-2 text-sm font-medium focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
            >
              {MONTHS.map((m, i) => (
                <option key={m} value={i}>{m}</option>
              ))}
            </select>
            <select
              aria-label="Year"
              value={viewMonth.getFullYear()}
              onChange={(e) => setViewMonth(new Date(Number(e.target.value), viewMonth.getMonth(), 1))}
              className="h-8 w-[4.75rem] cursor-pointer rounded-md border border-input bg-background px-2 text-sm font-medium focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
            >
              {/* Keep the current view year listed even when outside the range */}
              {!years.includes(viewMonth.getFullYear()) && (
                <option value={viewMonth.getFullYear()}>{viewMonth.getFullYear()}</option>
              )}
              {years.map((y) => (
                <option key={y} value={y}>{y}</option>
              ))}
            </select>
            <button
              type="button"
              aria-label="Previous month"
              disabled={!canGoPrev}
              onClick={() => canGoPrev && setViewMonth(prevMonth)}
              className={cn(
                "flex h-8 w-8 shrink-0 items-center justify-center rounded-full border border-border text-foreground transition-colors hover:bg-secondary",
                !canGoPrev && "cursor-not-allowed opacity-30 hover:bg-transparent",
              )}
            >
              <ChevronLeft className="h-4 w-4" />
            </button>
            <button
              type="button"
              aria-label="Next month"
              disabled={!canGoNext}
              onClick={() => canGoNext && setViewMonth(nextMonth)}
              className={cn(
                "flex h-8 w-8 shrink-0 items-center justify-center rounded-full border border-border text-foreground transition-colors hover:bg-secondary",
                !canGoNext && "cursor-not-allowed opacity-30 hover:bg-transparent",
              )}
            >
              <ChevronRight className="h-4 w-4" />
            </button>
          </div>

          {/* Weekday header */}
          <div className="grid grid-cols-7 text-center text-[11px] font-medium uppercase tracking-wide text-muted-foreground">
            {WEEKDAYS.map((w) => (
              <div key={w} className="py-1">{w}</div>
            ))}
          </div>

          {/* Day grid */}
          <div className="grid grid-cols-7">
            {buildMonthCells(viewMonth).map((cell, i) => {
              if (!cell) return <div key={i} className="h-9" />;
              const dayDisabled = isDayDisabled(cell);
              const isSelected = selected !== null && isSameDay(cell, selected);
              const isToday = isSameDay(cell, today);
              return (
                <button
                  key={i}
                  type="button"
                  disabled={dayDisabled}
                  aria-label={formatDisplayDate(cell)}
                  aria-pressed={isSelected}
                  onClick={() => pick(cell)}
                  className={cn(
                    "flex h-9 w-full items-center justify-center text-sm font-medium transition-colors",
                    dayDisabled && "cursor-not-allowed text-muted-foreground/40",
                  )}
                >
                  <span
                    className={cn(
                      "flex h-8 w-8 items-center justify-center rounded-full transition-colors",
                      isSelected
                        ? "bg-primary text-primary-foreground"
                        : !dayDisabled && "hover:bg-secondary",
                      !isSelected && isToday && "border border-primary/50 font-semibold",
                    )}
                  >
                    {cell.getDate()}
                  </span>
                </button>
              );
            })}
          </div>

          {/* Footer actions */}
          <div className="mt-2 flex items-center justify-between border-t border-border pt-2">
            <button
              type="button"
              disabled={!todaySelectable}
              onClick={() => pick(today)}
              className={cn(
                "rounded-md px-2 py-1 text-xs font-semibold text-foreground transition-colors hover:bg-secondary",
                !todaySelectable && "cursor-not-allowed opacity-40 hover:bg-transparent",
              )}
            >
              Today
            </button>
            {clearable && selected && (
              <button
                type="button"
                onClick={() => {
                  onChange("");
                  setOpen(false);
                }}
                className="rounded-md px-2 py-1 text-xs font-semibold text-muted-foreground transition-colors hover:bg-secondary hover:text-foreground"
              >
                Clear
              </button>
            )}
          </div>
        </div>
      )}
    </div>
  );
}

/* ─────────── date helpers (local-timezone safe) ─────────── */

function startOfDay(d: Date) {
  const x = new Date(d);
  x.setHours(0, 0, 0, 0);
  return x;
}

function startOfMonth(d: Date) {
  return new Date(d.getFullYear(), d.getMonth(), 1);
}

function endOfMonth(d: Date) {
  return new Date(d.getFullYear(), d.getMonth() + 1, 0);
}

function addMonths(d: Date, n: number) {
  return new Date(d.getFullYear(), d.getMonth() + n, 1);
}

function isSameDay(a: Date, b: Date) {
  return (
    a.getFullYear() === b.getFullYear() &&
    a.getMonth() === b.getMonth() &&
    a.getDate() === b.getDate()
  );
}

function isBeforeDay(a: Date, b: Date) {
  return startOfDay(a).getTime() < startOfDay(b).getTime();
}

function clampDate(d: Date, min: Date | null, max: Date | null) {
  if (min && isBeforeDay(d, min)) return min;
  if (max && isBeforeDay(max, d)) return max;
  return d;
}

function buildMonthCells(month: Date): (Date | null)[] {
  const cells: (Date | null)[] = [];
  const startDow = startOfMonth(month).getDay();
  const daysInMonth = endOfMonth(month).getDate();
  for (let i = 0; i < startDow; i++) cells.push(null);
  for (let d = 1; d <= daysInMonth; d++) {
    cells.push(new Date(month.getFullYear(), month.getMonth(), d));
  }
  while (cells.length % 7 !== 0) cells.push(null);
  return cells;
}

function toIsoDate(d: Date) {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, "0");
  const day = String(d.getDate()).padStart(2, "0");
  return `${y}-${m}-${day}`;
}

function parseIsoDate(iso: string): Date | null {
  const m = /^(\d{4})-(\d{2})-(\d{2})$/.exec(iso);
  if (!m) return null;
  return new Date(Number(m[1]), Number(m[2]) - 1, Number(m[3]));
}

function formatDisplayDate(d: Date): string {
  return d.toLocaleDateString(undefined, { month: "short", day: "numeric", year: "numeric" });
}
