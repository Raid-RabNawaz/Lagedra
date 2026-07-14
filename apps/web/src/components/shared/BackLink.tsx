import { ArrowLeft } from "lucide-react";
import { Button } from "@/components/ui/button";
import { useSmartBack } from "@/hooks/useSmartBack";
import { cn } from "@/lib/utils";

type BackLinkProps = {
  /** Destination when there is no prior in-app history (direct link / new tab). */
  fallbackTo: string;
  /** Visible label. Defaults to "Back". */
  label?: string;
  className?: string;
  /**
   * `link` — muted text control used in page headers.
   * `button` — outline button for empty / error states.
   */
  variant?: "link" | "button";
};

/**
 * Page-level back control. Prefer this over a hardcoded `<Link to="…">` so
 * users return to wherever they came from instead of a fixed parent route.
 */
export function BackLink({
  fallbackTo,
  label = "Back",
  className,
  variant = "link",
}: BackLinkProps) {
  const goBack = useSmartBack(fallbackTo);

  if (variant === "button") {
    return (
      <Button
        type="button"
        variant="outline"
        size="sm"
        onClick={goBack}
        className={className}
      >
        <ArrowLeft className="mr-2 h-4 w-4" />
        {label}
      </Button>
    );
  }

  return (
    <button
      type="button"
      onClick={goBack}
      className={cn(
        "inline-flex items-center gap-1.5 text-sm text-muted-foreground hover:text-foreground transition-colors",
        className,
      )}
    >
      <ArrowLeft className="h-4 w-4" />
      {label}
    </button>
  );
}
