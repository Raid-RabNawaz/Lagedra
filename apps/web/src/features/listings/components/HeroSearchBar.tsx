import { useEffect, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { Search, MapPin, Calendar as CalendarIcon, SlidersHorizontal } from "lucide-react";
import { DateRangeCalendar, type DateRange } from "@/features/listings/components/DateRangeCalendar";
import { cn } from "@/lib/utils";

type Segment = "where" | "checkin" | "checkout" | "filter" | null;

const POPULAR_DESTINATIONS = [
  { label: "New York, NY", hint: "United States" },
  { label: "San Francisco, CA", hint: "United States" },
  { label: "Austin, TX", hint: "United States" },
  { label: "Miami, FL", hint: "United States" },
  { label: "Los Angeles, CA", hint: "United States" },
  { label: "Seattle, WA", hint: "United States" },
];

function formatDateShort(d: Date | null) {
  if (!d) return null;
  return d.toLocaleDateString(undefined, { month: "short", day: "numeric" });
}

function toIsoDate(d: Date) {
  // Local-timezone safe YYYY-MM-DD (avoids UTC off-by-one)
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, "0");
  const day = String(d.getDate()).padStart(2, "0");
  return `${y}-${m}-${day}`;
}

export function HeroSearchBar() {
  const navigate = useNavigate();
  const [where, setWhere] = useState("");
  const [range, setRange] = useState<DateRange>({ start: null, end: null });
  const [active, setActive] = useState<Segment>(null);

  const containerRef = useRef<HTMLDivElement | null>(null);
  const whereInputRef = useRef<HTMLInputElement | null>(null);

  // Close popover on outside click
  useEffect(() => {
    if (!active) return;
    const onDown = (e: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setActive(null);
      }
    };
    document.addEventListener("mousedown", onDown);
    return () => document.removeEventListener("mousedown", onDown);
  }, [active]);

  // Close popover on Escape
  useEffect(() => {
    if (!active) return;
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") setActive(null);
    };
    document.addEventListener("keydown", onKey);
    return () => document.removeEventListener("keydown", onKey);
  }, [active]);

  // Auto-focus the Where input when its segment becomes active
  useEffect(() => {
    if (active === "where") {
      setTimeout(() => whereInputRef.current?.focus(), 0);
    }
  }, [active]);

  const submit = () => {
    const params = new URLSearchParams();
    if (where.trim()) params.set("keyword", where.trim());
    if (range.start) params.set("availableFrom", toIsoDate(range.start));
    if (range.end) params.set("availableTo", toIsoDate(range.end));
    const qs = params.toString();
    navigate(qs ? `/listings/search?${qs}` : "/listings/search");
  };

  const handleRangeChange = (next: DateRange) => {
    setRange(next);
    if (active === "checkin" && next.start && !next.end) {
      setActive("checkout");
    } else if (active === "checkout" && next.start && next.end) {
      setActive(null);
    }
  };

  // ─────── Mobile: simple compact pill (kept from prior design) ───────
  // For a fully Airbnb-like mobile flow we'd open a full-screen sheet — keeping
  // it scope-tight here: tapping the pill jumps to the search page where the
  // user can refine.
  return (
    <div ref={containerRef} className="relative">
      {/* Mobile pill */}
      <form
        className="flex items-stretch overflow-hidden rounded-full bg-background shadow-[0_12px_40px_-16px_rgba(15,23,42,0.35)] ring-1 ring-border sm:hidden"
        onSubmit={(e) => {
          e.preventDefault();
          submit();
        }}
      >
        <div className="flex flex-1 items-center gap-2 px-4 py-2.5">
          <MapPin className="h-4 w-4 text-muted-foreground" />
          <input
            type="text"
            value={where}
            onChange={(e) => setWhere(e.target.value)}
            placeholder="Where to?"
            className="w-full bg-transparent text-sm text-foreground outline-none placeholder:text-muted-foreground"
          />
        </div>
        <button
          type="submit"
          aria-label="Search"
          className="flex items-center justify-center bg-primary px-5 text-primary-foreground transition-colors hover:bg-primary/90"
        >
          <Search className="h-4 w-4" />
        </button>
      </form>

      {/* Desktop segmented bar */}
      <div
        className={cn(
          "relative hidden items-stretch overflow-visible rounded-full p-1.5 shadow-[0_20px_60px_-20px_rgba(15,23,42,0.35)] ring-1 transition-colors sm:flex",
          active ? "bg-secondary ring-border" : "bg-background ring-border",
        )}
      >
        <SegmentButton
          isActive={active === "where"}
          dimmed={Boolean(active) && active !== "where"}
          label="Where"
          value={where || undefined}
          placeholder="Anywhere"
          onClick={() => setActive(active === "where" ? null : "where")}
          className="flex-[1.4] rounded-l-full"
          rounded="left"
        />

        <SegmentButton
          isActive={active === "checkin"}
          dimmed={Boolean(active) && active !== "checkin"}
          label="Check-in"
          value={formatDateShort(range.start) ?? undefined}
          placeholder="Add dates"
          onClick={() => setActive(active === "checkin" ? null : "checkin")}
          className="flex-1"
          rounded="none"
        />

        <SegmentButton
          isActive={active === "checkout"}
          dimmed={Boolean(active) && active !== "checkout"}
          label="Check-out"
          value={formatDateShort(range.end) ?? undefined}
          placeholder="Add dates"
          onClick={() => setActive(active === "checkout" ? null : "checkout")}
          className="flex-1"
          rounded="none"
        />

        <SegmentButton
          isActive={active === "filter"}
          dimmed={Boolean(active) && active !== "filter"}
          label="Filter"
          value="All filters"
          onClick={() => setActive(active === "filter" ? null : "filter")}
          className="flex-1"
          rounded="right"
          icon={<SlidersHorizontal className="h-3.5 w-3.5" />}
        />

        {/* Submit */}
        <div className="flex items-center pl-1.5">
          <button
            type="button"
            onClick={submit}
            aria-label="Search"
            className="flex h-12 items-center justify-center gap-2 rounded-full bg-primary px-5 text-primary-foreground transition-all hover:bg-primary/90 hover:shadow-md"
          >
            <Search className="h-4 w-4" />
            {(where || range.start || active) && (
              <span className="hidden text-sm font-semibold lg:inline">Search</span>
            )}
          </button>
        </div>
      </div>

      {/* ───── Popover panels (desktop only) ───── */}
      {active && (
        <div
          className="absolute left-0 right-0 top-full z-30 mt-3 hidden sm:block"
          role="dialog"
        >
          <div className="rounded-3xl border border-border bg-background p-5 shadow-[0_24px_60px_-20px_rgba(15,23,42,0.35)]">
            {active === "where" && (
              <WherePanel
                value={where}
                inputRef={whereInputRef}
                onChange={setWhere}
                onPick={(v) => {
                  setWhere(v);
                  setActive("checkin");
                }}
              />
            )}
            {(active === "checkin" || active === "checkout") && (
              <DateRangeCalendar range={range} onChange={handleRangeChange} />
            )}
            {active === "filter" && <FilterPanel onNavigate={submit} />}
          </div>
        </div>
      )}
    </div>
  );
}

/* ─────────── Segment button ─────────── */

function SegmentButton({
  isActive,
  dimmed,
  label,
  value,
  placeholder,
  onClick,
  className,
  rounded,
  icon,
}: {
  isActive: boolean;
  dimmed: boolean;
  label: string;
  value?: string;
  placeholder?: string;
  onClick: () => void;
  className?: string;
  rounded: "left" | "right" | "none";
  icon?: React.ReactNode;
}) {
  const roundedCls =
    rounded === "left"
      ? "rounded-l-full"
      : rounded === "right"
        ? "rounded-r-full"
        : "rounded-full";

  return (
    <button
      type="button"
      onClick={onClick}
      className={cn(
        "group relative min-w-0 flex-col items-start gap-0.5 px-6 py-2.5 text-left transition-all",
        roundedCls,
        // Background: white pill with shadow when active; transparent + hover otherwise
        isActive && "bg-background shadow-[0_4px_12px_rgba(15,23,42,0.12)]",
        !isActive && !dimmed && "hover:bg-background/60",
        !isActive && dimmed && "hover:bg-background/40",
        "flex",
        className,
      )}
    >
      <span className="flex items-center gap-1.5 text-[11px] font-semibold uppercase tracking-wide text-muted-foreground">
        {icon}
        {label}
      </span>
      <span
        className={cn(
          "truncate text-sm",
          value ? "font-medium text-foreground" : "text-muted-foreground/80",
        )}
      >
        {value ?? placeholder}
      </span>
    </button>
  );
}

/* ─────────── Where panel ─────────── */

function WherePanel({
  value,
  inputRef,
  onChange,
  onPick,
}: {
  value: string;
  inputRef: React.RefObject<HTMLInputElement | null>;
  onChange: (v: string) => void;
  onPick: (v: string) => void;
}) {
  return (
    <div>
      <div className="flex items-center gap-3 rounded-2xl border border-border bg-background px-4 py-3 ring-1 ring-transparent focus-within:ring-primary/30">
        <MapPin className="h-4 w-4 text-muted-foreground" />
        <input
          ref={inputRef}
          type="text"
          value={value}
          onChange={(e) => onChange(e.target.value)}
          placeholder="Search a city, neighborhood, or address"
          className="w-full bg-transparent text-sm text-foreground outline-none placeholder:text-muted-foreground"
        />
      </div>
      <p className="mt-5 mb-2 text-[11px] font-semibold uppercase tracking-wide text-muted-foreground">
        Popular destinations
      </p>
      <ul className="grid grid-cols-1 gap-1 sm:grid-cols-2">
        {POPULAR_DESTINATIONS.map((d) => (
          <li key={d.label}>
            <button
              type="button"
              onClick={() => onPick(d.label)}
              className="flex w-full items-center gap-3 rounded-xl px-3 py-2 text-left transition-colors hover:bg-secondary"
            >
              <span className="flex h-9 w-9 items-center justify-center rounded-lg bg-secondary">
                <MapPin className="h-4 w-4 text-muted-foreground" />
              </span>
              <span className="min-w-0">
                <span className="block truncate text-sm font-medium text-foreground">
                  {d.label}
                </span>
                <span className="block truncate text-xs text-muted-foreground">
                  {d.hint}
                </span>
              </span>
            </button>
          </li>
        ))}
      </ul>
    </div>
  );
}

/* ─────────── Filter panel ─────────── */

function FilterPanel({ onNavigate }: { onNavigate: () => void }) {
  const quickTypes: { id: string; label: string }[] = [
    { id: "Apartment", label: "Apartments" },
    { id: "House", label: "Houses" },
    { id: "Studio", label: "Studios" },
    { id: "Loft", label: "Lofts" },
    { id: "Villa", label: "Villas" },
    { id: "Cabin", label: "Cabins" },
  ];
  return (
    <div>
      <p className="mb-2 text-[11px] font-semibold uppercase tracking-wide text-muted-foreground">
        Quick filters
      </p>
      <div className="flex flex-wrap gap-2">
        {quickTypes.map((t) => (
          <a
            key={t.id}
            href={`/listings/search?propertyType=${t.id}`}
            className="rounded-full border border-border bg-background px-4 py-1.5 text-sm font-medium text-foreground transition-colors hover:border-foreground/40 hover:bg-secondary"
          >
            {t.label}
          </a>
        ))}
      </div>
      <div className="mt-5 flex items-center justify-between border-t border-border pt-4">
        <span className="flex items-center gap-2 text-xs text-muted-foreground">
          <CalendarIcon className="h-4 w-4" />
          Pick dates above for date-aware results
        </span>
        <button
          type="button"
          onClick={onNavigate}
          className="rounded-full bg-primary px-5 py-2 text-sm font-semibold text-primary-foreground transition-colors hover:bg-primary/90"
        >
          More filters
        </button>
      </div>
    </div>
  );
}
