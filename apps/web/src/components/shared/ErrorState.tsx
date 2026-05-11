import { AlertTriangle, RotateCcw } from "lucide-react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import { toFriendlyError } from "@/lib/errors";

type ErrorStateProps = {
  /** Optional explicit title; otherwise derived from the error. */
  title?: string;
  /** Optional explicit message; otherwise derived from the error. */
  message?: string;
  /** The original error object (axios/Error/unknown). */
  error?: unknown;
  /** Called when the user clicks "Try again". */
  onRetry?: () => void;
  /** Visual variant. Defaults to "section" for in-page use. */
  variant?: "section" | "page";
  className?: string;
  children?: React.ReactNode;
};

export function ErrorState({
  title,
  message,
  error,
  onRetry,
  variant = "section",
  className,
  children,
}: ErrorStateProps) {
  const friendly = toFriendlyError(error);
  const resolvedTitle = title ?? friendly.title;
  const resolvedMessage = message ?? friendly.message;

  const isPage = variant === "page";

  return (
    <div
      role="alert"
      aria-live="polite"
      className={cn(
        "flex flex-col items-center justify-center text-center",
        isPage ? "min-h-[60vh] py-16 px-4" : "py-12 px-4",
        className,
      )}
    >
      <div className="mx-auto mb-4 flex h-14 w-14 items-center justify-center rounded-full bg-destructive/10">
        <AlertTriangle className="h-7 w-7 text-destructive" />
      </div>
      <h3 className={cn("font-semibold", isPage ? "text-2xl" : "text-lg")}>
        {resolvedTitle}
      </h3>
      <p className="mt-1.5 max-w-md text-sm text-muted-foreground">
        {resolvedMessage}
      </p>

      {(onRetry || children) && (
        <div className="mt-5 flex flex-wrap items-center justify-center gap-2">
          {onRetry && (
            <Button variant="outline" size="sm" onClick={onRetry}>
              <RotateCcw className="h-4 w-4" />
              Try again
            </Button>
          )}
          {children}
        </div>
      )}
    </div>
  );
}
