import { Star } from "lucide-react";
import { cn } from "@/lib/utils";

type Props = {
  value: number;
  onChange?: (value: number) => void;
  size?: "sm" | "md";
  label?: string;
};

export function StarRatingInput({ value, onChange, size = "md", label }: Props) {
  const iconClass = size === "sm" ? "h-4 w-4" : "h-5 w-5";
  return (
    <div className="space-y-1">
      {label ? <p className="text-sm font-medium">{label}</p> : null}
      <div className="flex items-center gap-1">
        {[1, 2, 3, 4, 5].map((n) => (
          <button
            key={n}
            type="button"
            disabled={!onChange}
            className={cn(
              "rounded p-0.5 transition-colors",
              onChange ? "hover:text-amber-500" : "cursor-default",
              n <= value ? "text-amber-500" : "text-muted-foreground/40",
            )}
            onClick={() => onChange?.(n)}
            aria-label={`${n} star${n === 1 ? "" : "s"}`}
          >
            <Star className={cn(iconClass, n <= value && "fill-current")} />
          </button>
        ))}
      </div>
    </div>
  );
}

export function StarRatingDisplay({
  average,
  count,
  className,
}: {
  average: number;
  count: number;
  className?: string;
}) {
  if (count <= 0) {
    return (
      <span className={cn("text-sm text-muted-foreground", className)}>
        No reviews yet
      </span>
    );
  }

  return (
    <span className={cn("inline-flex items-center gap-1 text-sm", className)}>
      <Star className="h-3.5 w-3.5 fill-amber-500 text-amber-500" />
      <span className="font-medium">{average.toFixed(1)}</span>
      <span className="text-muted-foreground">({count})</span>
    </span>
  );
}
