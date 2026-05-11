import { Component, type ErrorInfo, type ReactNode } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { Home, RotateCcw } from "lucide-react";
import { ErrorState } from "@/components/shared/ErrorState";
import { Button } from "@/components/ui/button";

type Props = {
  children: ReactNode;
  /** Reset boundary state whenever this key changes (e.g. route pathname). */
  resetKey: string;
  onReload: () => void;
  onGoHome: () => void;
  onGoBack: () => void;
};

type State = { error: Error | null };

class PageBoundaryInner extends Component<Props, State> {
  constructor(props: Props) {
    super(props);
    this.state = { error: null };
  }

  static getDerivedStateFromError(error: Error): State {
    return { error };
  }

  componentDidUpdate(prev: Props) {
    if (prev.resetKey !== this.props.resetKey && this.state.error) {
      this.setState({ error: null });
    }
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    if (import.meta.env.DEV) {
      console.error("[PageBoundary] caught:", error, info.componentStack);
    }
  }

  render() {
    if (!this.state.error) {
      return this.props.children;
    }

    return (
      <ErrorState
        variant="page"
        error={this.state.error}
        onRetry={this.props.onReload}
      >
        <Button variant="ghost" size="sm" onClick={this.props.onGoBack}>
          <RotateCcw className="h-4 w-4" />
          Go back
        </Button>
        <Button size="sm" onClick={this.props.onGoHome}>
          <Home className="h-4 w-4" />
          Home
        </Button>
      </ErrorState>
    );
  }
}

/**
 * Per-page error boundary that lives **inside** the surrounding layout
 * (e.g. `AppShell`'s `<Outlet />`). When a page render throws, the chrome
 * (sidebar, header) stays mounted and only the page area shows the error.
 *
 * Resets automatically on navigation so errors don't persist after the user
 * moves to another page.
 */
export function PageBoundary({ children }: { children: ReactNode }) {
  const location = useLocation();
  const navigate = useNavigate();

  return (
    <PageBoundaryInner
      resetKey={location.pathname + location.search}
      onReload={() => navigate(0)}
      onGoBack={() => navigate(-1)}
      onGoHome={() => navigate("/")}
    >
      {children}
    </PageBoundaryInner>
  );
}
