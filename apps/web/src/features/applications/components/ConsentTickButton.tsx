import type { ReactNode } from "react";
import { Check } from "lucide-react";
import { cn } from "@/lib/utils";

type Props = {
  checked: boolean;
  onCheckedChange: (checked: boolean) => void;
  children: ReactNode;
  className?: string;
  disabled?: boolean;
};

/**
 * Compact agree-control: a small tick button that turns green when selected.
 * Used for host Truth Surface consent and owner tenancy consent.
 */
export const ConsentTickButton = ({
  checked,
  onCheckedChange,
  children,
  className,
  disabled = false,
}: Props) => {
  return (
    <label
      className={cn(
        "flex cursor-pointer items-start gap-2.5 text-[11px] text-muted-foreground",
        disabled && "cursor-not-allowed opacity-60",
        className,
      )}
    >
      <button
        type="button"
        role="checkbox"
        aria-checked={checked}
        disabled={disabled}
        onClick={() => onCheckedChange(!checked)}
        className={cn(
          "mt-0.5 inline-flex h-6 w-6 shrink-0 items-center justify-center rounded-md border transition-colors",
          "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2",
          checked
            ? "border-emerald-600 bg-emerald-600 text-white shadow-sm"
            : "border-input bg-background text-muted-foreground/50 hover:border-emerald-500 hover:text-emerald-600",
        )}
      >
        <Check className="h-3.5 w-3.5" strokeWidth={3} />
      </button>
      <span>{children}</span>
    </label>
  );
};
