import { isRouteErrorResponse, useNavigate, useRouteError } from "react-router-dom";
import { Home, RotateCcw } from "lucide-react";
import { ErrorState } from "@/components/shared/ErrorState";
import { Button } from "@/components/ui/button";
import { toFriendlyError } from "@/lib/errors";

/**
 * React Router `errorElement`. Renders inside the surrounding route layout
 * (e.g. AppShell) so a single page failing doesn't blow away the chrome.
 *
 * Handles both:
 *   - thrown render errors (anything thrown by a page component or its
 *     children, including TanStack Query queries running in `useSuspenseQuery`)
 *   - React Router responses (404 navigations, loader/action errors)
 */
export function RouteErrorBoundary() {
  const error = useRouteError();
  const navigate = useNavigate();

  let title: string | undefined;
  let message: string | undefined;

  if (isRouteErrorResponse(error)) {
    if (error.status === 404) {
      title = "Page not found";
      message = "The page you were looking for doesn't exist or has been moved.";
    } else {
      title = `Error ${error.status}`;
      message = error.statusText || "Something went wrong loading this page.";
    }
  } else {
    const friendly = toFriendlyError(error);
    title = friendly.title;
    message = friendly.message;
  }

  if (import.meta.env.DEV) {
    console.error("[RouteErrorBoundary] caught:", error);
  }

  return (
    <ErrorState
      variant="page"
      title={title}
      message={message}
      onRetry={() => navigate(0)}
    >
      <Button variant="ghost" size="sm" onClick={() => navigate(-1)}>
        <RotateCcw className="h-4 w-4" />
        Go back
      </Button>
      <Button size="sm" onClick={() => navigate("/")}>
        <Home className="h-4 w-4" />
        Home
      </Button>
    </ErrorState>
  );
}
