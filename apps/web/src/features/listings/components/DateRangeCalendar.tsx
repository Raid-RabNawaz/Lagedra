import { useEffect, useMemo, useState } from "react";
import { ChevronLeft, ChevronRight } from "lucide-react";
import { cn } from "@/lib/utils";

export type DateRange = { start: Date | null; end: Date | null };

const WEEKDAYS = ["Su", "Mo", "Tu", "We", "Th", "Fr", "Sa"];

function startOfDay(d: Date) {
  const x = new Date(d);
  x.setHours(0, 0, 0, 0);
  return x;
}

function startOfMonth(d: Date) {
  return new Date(d.getFullYear(), d.getMonth(), 1);
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

function formatMonthLabel(d: Date) {
  return d.toLocaleDateString(undefined, { month: "long", year: "numeric" });
}

type Props = {
  range: DateRange;
  onChange: (next: DateRange) => void;
  /** Disables dates strictly before this. Defaults to today. */
  minDate?: Date;
};

export function DateRangeCalendar({ range, onChange, minDate }: Props) {
  const today = useMemo(() => startOfDay(new Date()), []);
  const min = minDate ? startOfDay(minDate) : today;

  // Anchor view month to the current selection or "today" on first open.
  const initialMonth = useMemo(
    () => startOfMonth(range.start ?? today),
    // intentionally compute once when the component mounts
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [],
  );
  const [viewMonth, setViewMonth] = useState<Date>(initialMonth);
  const [hovered, setHovered] = useState<Date | null>(null);

  // Don't allow paging earlier than the month containing `min`.
  const minMonth = startOfMonth(min);
  const canGoPrev = viewMonth.getTime() > minMonth.getTime();

  // If the user clears the range while open, snap the view back to today.
  useEffect(() => {
    if (!range.start && !range.end) setHovered(null);
  }, [range.start, range.end]);

  const handleDayClick = (date: Date) => {
    if (isBeforeDay(date, min)) return;
    const { start, end } = range;
    if (!start || (start && end)) {
      onChange({ start: date, end: null });
      return;
    }
    if (isSameDay(date, start)) {
      onChange({ start: null, end: null });
      return;
    }
    if (isBeforeDay(date, start)) {
      onChange({ start: date, end: null });
      return;
    }
    onChange({ start, end: date });
  };

  const monthA = viewMonth;
  const monthB = addMonths(viewMonth, 1);

  return (
    <div>
      {/* Month header: arrows + two month labels */}
      <div className="mb-4 flex items-center justify-between gap-4">
        <button
          type="button"
          aria-label="Previous month"
          disabled={!canGoPrev}
          onClick={() => canGoPrev && setViewMonth((m) => addMonths(m, -1))}
          className={cn(
            "flex h-8 w-8 shrink-0 items-center justify-center rounded-full border border-border text-foreground transition-colors hover:bg-secondary",
            !canGoPrev && "cursor-not-allowed opacity-30 hover:bg-transparent",
          )}
        >
          <ChevronLeft className="h-4 w-4" />
        </button>
        <div className="grid flex-1 grid-cols-1 gap-6 text-center text-sm font-semibold md:grid-cols-2">
          <span>{formatMonthLabel(monthA)}</span>
          <span className="hidden md:inline">{formatMonthLabel(monthB)}</span>
        </div>
        <button
          type="button"
          aria-label="Next month"
          onClick={() => setViewMonth((m) => addMonths(m, 1))}
          className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full border border-border text-foreground transition-colors hover:bg-secondary"
        >
          <ChevronRight className="h-4 w-4" />
        </button>
      </div>

      <div className="grid grid-cols-1 gap-6 md:grid-cols-2">
        <MonthGrid
          month={monthA}
          range={range}
          hovered={hovered}
          min={min}
          onHover={setHovered}
          onClick={handleDayClick}
        />
        <div className="hidden md:block">
          <MonthGrid
            month={monthB}
            range={range}
            hovered={hovered}
            min={min}
            onHover={setHovered}
            onClick={handleDayClick}
          />
        </div>
      </div>

      {/* Footer hint / clear */}
      <div className="mt-4 flex items-center justify-between border-t border-border pt-3 text-xs text-muted-foreground">
        <span>
          {range.start && range.end
            ? `${range.start.toLocaleDateString()} → ${range.end.toLocaleDateString()}`
            : range.start
              ? "Pick a check-out date"
              : "Pick a check-in date"}
        </span>
        {(range.start || range.end) && (
          <button
            type="button"
            onClick={() => onChange({ start: null, end: null })}
            className="rounded-md px-2 py-1 font-semibold text-foreground hover:bg-secondary"
          >
            Clear dates
          </button>
        )}
      </div>
    </div>
  );
}

/* ─────────── Month grid ─────────── */

function MonthGrid({
  month,
  range,
  hovered,
  min,
  onHover,
  onClick,
}: {
  month: Date;
  range: DateRange;
  hovered: Date | null;
  min: Date;
  onHover: (d: Date | null) => void;
  onClick: (d: Date) => void;
}) {
  const monthStart = startOfMonth(month);
  const startDow = monthStart.getDay();
  const daysInMonth = new Date(month.getFullYear(), month.getMonth() + 1, 0).getDate();

  // Build a flat array of `null` (leading blanks) + dates for every day.
  const cells: (Date | null)[] = [];
  for (let i = 0; i < startDow; i++) cells.push(null);
  for (let d = 1; d <= daysInMonth; d++) {
    cells.push(new Date(month.getFullYear(), month.getMonth(), d));
  }
  while (cells.length % 7 !== 0) cells.push(null);

  // The "effective end" used for highlighting includes the hovered cell when
  // the user has only chosen a start date.
  const effectiveEnd =
    range.end ??
    (range.start && hovered && !isBeforeDay(hovered, range.start) && !isSameDay(hovered, range.start)
      ? hovered
      : null);

  return (
    <div onMouseLeave={() => onHover(null)}>
      <div className="mb-1 grid grid-cols-7 text-center text-[11px] font-medium uppercase tracking-wide text-muted-foreground">
        {WEEKDAYS.map((w) => (
          <div key={w} className="py-1">
            {w}
          </div>
        ))}
      </div>
      <div className="grid grid-cols-7">
        {cells.map((cell, i) => (
          <DayCell
            key={i}
            date={cell}
            range={range}
            effectiveEnd={effectiveEnd}
            min={min}
            onHover={onHover}
            onClick={onClick}
          />
        ))}
      </div>
    </div>
  );
}

function DayCell({
  date,
  range,
  effectiveEnd,
  min,
  onHover,
  onClick,
}: {
  date: Date | null;
  range: DateRange;
  effectiveEnd: Date | null;
  min: Date;
  onHover: (d: Date | null) => void;
  onClick: (d: Date) => void;
}) {
  if (!date) return <div className="h-10 sm:h-11" />;

  const disabled = isBeforeDay(date, min);
  const start = range.start;
  const end = effectiveEnd;
  const isStart = !!(start && isSameDay(start, date));
  const isEnd = !!(end && isSameDay(end, date));
  const inBetween =
    !!(start && end && !isStart && !isEnd && !isBeforeDay(date, start) && isBeforeDay(date, end));
  const inRange = inBetween || isStart || isEnd;
  // When only a single date is selected (no end), give that single cell a
  // simple circular highlight instead of half-pill range chrome.
  const onlyStart = !!(start && !range.end && !effectiveEnd);

  return (
    <button
      type="button"
      disabled={disabled}
      onMouseEnter={() => !disabled && onHover(date)}
      onClick={() => onClick(date)}
      className={cn(
        "relative h-10 w-full text-sm font-medium transition-colors sm:h-11",
        disabled
          ? "cursor-not-allowed text-muted-foreground/40 line-through"
          : "text-foreground hover:[&_span]:bg-secondary",
      )}
    >
      {/* Range background — full-width strip when in-between, half-strip on the
          edges so the connection lines up with the next/previous cell. */}
      {inRange && !onlyStart && (
        <span
          aria-hidden
          className={cn(
            "absolute inset-y-1 bg-primary/10",
            isStart && !isEnd && "left-1/2 right-0",
            isEnd && !isStart && "left-0 right-1/2",
            inBetween && "left-0 right-0",
          )}
        />
      )}
      {/* Start/end accent: a filled circle on top of the range strip */}
      {(isStart || isEnd) && (
        <span
          aria-hidden
          className="absolute inset-1 rounded-full bg-primary"
        />
      )}
      <span
        className={cn(
          "relative z-10 mx-auto flex h-9 w-9 items-center justify-center rounded-full transition-colors",
          (isStart || isEnd) && "text-primary-foreground",
        )}
      >
        {date.getDate()}
      </span>
    </button>
  );
}
