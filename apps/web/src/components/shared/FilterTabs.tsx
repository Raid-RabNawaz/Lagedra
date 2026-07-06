import { cn } from "@/lib/utils";

export type FilterTabOption<T extends string> = {
  value: T;
  label: string;
  /** Optional count rendered as a pill badge after the label. */
  count?: number;
};

type FilterTabsProps<T extends string> = {
  options: FilterTabOption<T>[];
  value: T;
  onChange: (value: T) => void;
  /** Hide a count badge when its value is 0 (keeps inactive filters quiet). */
  hideZeroCounts?: boolean;
  className?: string;
  "aria-label"?: string;
};

/**
 * Wrapping pill-style filter row. Unlike a fixed-width segmented control, the
 * pills wrap onto multiple lines instead of forcing a horizontal scrollbar, so
 * every filter stays visible no matter how many there are or how narrow the
 * viewport is. Used across the listing/application inboxes for a consistent,
 * scannable filter affordance.
 */
export function FilterTabs<T extends string>({
  options,
  value,
  onChange,
  hideZeroCounts = false,
  className,
  "aria-label": ariaLabel,
}: FilterTabsProps<T>) {
  return (
    <div
      role="tablist"
      aria-label={ariaLabel}
      className={cn("flex flex-wrap items-center gap-2", className)}
    >
      {options.map((option) => {
        const isActive = option.value === value;
        const showCount =
          option.count != null && !(hideZeroCounts && option.count === 0);
        return (
          <button
            key={option.value}
            type="button"
            role="tab"
            aria-selected={isActive}
            onClick={() => onChange(option.value)}
            className={cn(
              "inline-flex items-center gap-1.5 rounded-full border px-3.5 py-1.5 text-sm font-medium transition-colors cursor-pointer focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 ring-offset-background",
              isActive
                ? "border-transparent bg-foreground text-background shadow-sm"
                : "border-border bg-background text-muted-foreground hover:bg-muted hover:text-foreground",
            )}
          >
            {option.label}
            {showCount && (
              <span
                className={cn(
                  "rounded-full px-1.5 text-[10px] font-semibold tabular-nums leading-5",
                  isActive
                    ? "bg-background/20 text-background"
                    : "bg-muted text-muted-foreground",
                )}
              >
                {option.count}
              </span>
            )}
          </button>
        );
      })}
    </div>
  );
}
