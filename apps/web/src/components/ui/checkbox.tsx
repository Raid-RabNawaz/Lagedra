import * as React from "react";
import { Check } from "lucide-react";
import { cn } from "@/lib/utils";

type CheckboxProps = Omit<React.InputHTMLAttributes<HTMLInputElement>, "type" | "onCheckedChange"> & {
  onCheckedChange?: (checked: boolean) => void;
};

/**
 * Controlled checkbox with a deterministic, self-drawn checkmark.
 *
 * We intentionally do NOT rely on the browser's native tick (via
 * `accent-color`): its rendering varies across browsers/OS themes and could
 * leave the box looking empty even while `checked` was true — so a host could
 * toggle "I agree" (enabling "Accept & seal") without any visual confirmation
 * before sealing an immutable record. Instead the native control is hidden
 * (`appearance-none`) and an overlaid check icon is shown via `peer-checked`,
 * which looks identical everywhere.
 */
const Checkbox = React.forwardRef<HTMLInputElement, CheckboxProps>(
  ({ className, onCheckedChange, onChange, ...props }, ref) => {
    const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
      onChange?.(e);
      onCheckedChange?.(e.target.checked);
    };

    return (
      <span className={cn("relative inline-flex h-4 w-4 shrink-0", className)}>
        <input
          type="checkbox"
          ref={ref}
          onChange={handleChange}
          className="peer h-4 w-4 shrink-0 cursor-pointer appearance-none rounded border border-input bg-background ring-offset-background checked:border-primary checked:bg-primary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
          {...props}
        />
        <Check
          aria-hidden
          strokeWidth={3.5}
          className="pointer-events-none absolute inset-0 h-4 w-4 scale-75 text-primary-foreground opacity-0 peer-checked:opacity-100 peer-disabled:opacity-50"
        />
      </span>
    );
  },
);
Checkbox.displayName = "Checkbox";

export { Checkbox };
