import { useEffect, useId, useRef, useState } from "react";
import { MapPin } from "lucide-react";
import { usePlaceSuggestions } from "@/features/listings/hooks/usePlaceSuggestions";
import type { PlaceSuggestion } from "@/features/listings/lib/placeSearch";
import { Input } from "@/components/ui/input";
import { cn } from "@/lib/utils";

type PlaceSearchFieldProps = {
  value: string;
  onChange: (value: string) => void;
  onSelect: (suggestion: PlaceSuggestion) => void;
  placeholder?: string;
  inputRef?: React.RefObject<HTMLInputElement | null>;
  /** Overlay list under the input (listings search). Panel list sits in-flow (hero Where). */
  variant?: "dropdown" | "panel";
  showPopularWhenEmpty?: boolean;
  leadingIcon?: React.ReactNode;
  /** Rendered inside the input row (e.g. the compact search submit button). */
  trailing?: React.ReactNode;
  /** Classes for the input row, so a rounded pill can clip the trailing button. */
  fieldClassName?: string;
  inputClassName?: string;
  className?: string;
};

export function PlaceSearchField({
  value,
  onChange,
  onSelect,
  placeholder = "Search a city, address, or state",
  inputRef,
  variant = "dropdown",
  showPopularWhenEmpty = false,
  leadingIcon,
  trailing,
  fieldClassName,
  inputClassName,
  className,
}: PlaceSearchFieldProps) {
  const listId = useId();
  const rootRef = useRef<HTMLDivElement | null>(null);
  const innerInputRef = useRef<HTMLInputElement | null>(null);
  const [open, setOpen] = useState(false);
  const [activeIndex, setActiveIndex] = useState(0);

  const { suggestions, loading } = usePlaceSuggestions(value, {
    enabled: open || variant === "panel",
    includePopularWhenEmpty: showPopularWhenEmpty,
  });

  const setInputRef = (node: HTMLInputElement | null) => {
    innerInputRef.current = node;
    if (inputRef) {
      (inputRef as React.MutableRefObject<HTMLInputElement | null>).current = node;
    }
  };

  useEffect(() => {
    setActiveIndex(0);
  }, [suggestions]);

  useEffect(() => {
    if (!open) return;
    const onDown = (e: MouseEvent) => {
      if (rootRef.current && !rootRef.current.contains(e.target as Node)) {
        setOpen(false);
      }
    };
    document.addEventListener("mousedown", onDown);
    return () => document.removeEventListener("mousedown", onDown);
  }, [open]);

  const pick = (suggestion: PlaceSuggestion) => {
    onChange(suggestion.query);
    onSelect(suggestion);
    setOpen(false);
  };

  const showList =
    (variant === "panel" && (showPopularWhenEmpty || value.trim().length >= 2 || open)) ||
    (variant === "dropdown" &&
      open &&
      (suggestions.length > 0 ||
        loading ||
        value.trim().length >= 2 ||
        showPopularWhenEmpty));

  const emptyQuery = value.trim().length === 0;
  const heading =
    emptyQuery && showPopularWhenEmpty ? "Popular in California" : "Suggestions";

  const onKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === "ArrowDown") {
      e.preventDefault();
      setOpen(true);
      setActiveIndex((i) => Math.min(suggestions.length - 1, i + 1));
      return;
    }
    if (e.key === "ArrowUp") {
      e.preventDefault();
      setActiveIndex((i) => Math.max(0, i - 1));
      return;
    }
    if (e.key === "Escape") {
      setOpen(false);
      return;
    }
    if (e.key === "Enter" && open && suggestions[activeIndex]) {
      e.preventDefault();
      pick(suggestions[activeIndex]);
    }
  };

  const list = (
    <div
      className={cn(
        variant === "dropdown" &&
          "absolute left-0 right-0 top-full z-50 mt-1.5 overflow-hidden rounded-xl border border-border bg-background text-foreground shadow-[0_16px_40px_-16px_rgba(15,23,42,0.35)]",
        variant === "panel" && "mt-5",
      )}
    >
      {variant === "panel" && (
        <p className="mb-2 text-[11px] font-semibold uppercase tracking-wide text-muted-foreground">
          {heading}
        </p>
      )}
      {loading && suggestions.length === 0 && (
        <p className="px-3 py-2 text-sm text-muted-foreground">Looking up places…</p>
      )}
      {!loading && suggestions.length === 0 && value.trim().length >= 2 && (
        <p className="px-3 py-2 text-sm text-muted-foreground">No matching places</p>
      )}
      {suggestions.length > 0 && (
        <ul
          id={listId}
          role="listbox"
          className={cn(
            variant === "panel" && emptyQuery && showPopularWhenEmpty
              ? "grid grid-cols-1 gap-1 sm:grid-cols-2"
              : "flex flex-col py-1",
          )}
        >
          {suggestions.map((suggestion, index) => (
            <li key={suggestion.id} role="presentation">
              <button
                type="button"
                id={`${listId}-${index}`}
                role="option"
                aria-selected={index === activeIndex}
                onMouseDown={(e) => {
                  e.preventDefault();
                  pick(suggestion);
                }}
                onMouseEnter={() => setActiveIndex(index)}
                className={cn(
                  "flex w-full items-center gap-3 px-3 py-2 text-left transition-colors",
                  variant === "panel" ? "rounded-xl" : "rounded-none",
                  index === activeIndex ? "bg-secondary" : "hover:bg-secondary",
                )}
              >
                <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-secondary">
                  <MapPin className="h-4 w-4 text-muted-foreground" />
                </span>
                <span className="min-w-0">
                  <span className="block truncate text-sm font-medium text-foreground">
                    {suggestion.label}
                  </span>
                  <span className="block truncate text-xs text-muted-foreground">
                    {suggestion.hint}
                  </span>
                </span>
              </button>
            </li>
          ))}
        </ul>
      )}
      {loading && suggestions.length > 0 && (
        <p className="border-t border-border px-3 py-1.5 text-[11px] text-muted-foreground">
          Updating suggestions…
        </p>
      )}
    </div>
  );

  return (
    <div ref={rootRef} className={cn("relative", className)}>
      <div
        className={cn(
          variant === "panel" &&
            "flex items-center gap-3 rounded-2xl border border-border bg-background px-4 py-3 ring-1 ring-transparent focus-within:ring-primary/30",
          variant === "dropdown" && "relative",
          fieldClassName,
        )}
      >
        {leadingIcon}
        {variant === "panel" && !leadingIcon && (
          <MapPin className="h-4 w-4 shrink-0 text-muted-foreground" />
        )}
        <Input
          ref={setInputRef}
          type="text"
          value={value}
          role="combobox"
          aria-expanded={showList}
          aria-controls={listId}
          aria-autocomplete="list"
          aria-activedescendant={
            showList && suggestions[activeIndex] ? `${listId}-${activeIndex}` : undefined
          }
          autoComplete="off"
          placeholder={placeholder}
          onChange={(e) => {
            onChange(e.target.value);
            setOpen(true);
          }}
          onFocus={() => setOpen(true)}
          onClick={() => setOpen(true)}
          onKeyDown={onKeyDown}
          className={cn(
            variant === "panel" &&
              "h-auto border-0 bg-transparent p-0 shadow-none focus-visible:ring-0 focus-visible:ring-offset-0",
            trailing && "min-w-0 flex-1",
            inputClassName,
          )}
        />
        {trailing}
      </div>
      {showList && list}
    </div>
  );
}
