import { useNavigate } from "react-router-dom";

/**
 * Returns a handler that navigates to the previous history entry when the user
 * arrived here via in-app navigation. Falls back to `fallbackTo` when there is
 * no prior SPA history (direct link, new tab, refresh).
 *
 * React Router stores a monotonically increasing `idx` on `history.state` for
 * every push; idx === 0 means this is the first entry in the session stack.
 */
export function useSmartBack(fallbackTo: string) {
  const navigate = useNavigate();

  return () => {
    const idx = (window.history.state as { idx?: number } | null)?.idx;
    if (typeof idx === "number" && idx > 0) {
      navigate(-1);
      return;
    }
    navigate(fallbackTo);
  };
}
