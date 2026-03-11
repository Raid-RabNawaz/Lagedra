import { type PropsWithChildren, useEffect } from "react";
import { authApi } from "@/features/auth/services/authApi";
import { useAuthStore } from "./authStore";

const refreshLeadTimeMs = 30_000;

export const AuthProvider = ({ children }: PropsWithChildren) => {
  const accessToken = useAuthStore((state) => state.accessToken);
  const refreshToken = useAuthStore((state) => state.refreshToken);
  const expiresAt = useAuthStore((state) => state.expiresAt);
  const setUser = useAuthStore((state) => state.setUser);
  const clearAuth = useAuthStore((state) => state.clearAuth);
  const setInitialized = useAuthStore((state) => state.setInitialized);

  useEffect(() => {
    let cancelled = false;

    const initialize = async () => {
      if (!accessToken || !refreshToken) {
        setInitialized(true);
        return;
      }

      try {
        const me = await authApi.getCurrentUser();
        if (!cancelled) {
          setUser(me);
        }
      } catch {
        clearAuth();
      } finally {
        if (!cancelled) {
          setInitialized(true);
        }
      }
    };

    void initialize();

    return () => {
      cancelled = true;
    };
  }, [accessToken, refreshToken, clearAuth, setInitialized, setUser]);

  useEffect(() => {
    if (!refreshToken || !expiresAt) {
      return;
    }

    const delay = Math.max(5000, expiresAt - Date.now() - refreshLeadTimeMs);
    const timer = window.setTimeout(async () => {
      try {
        await authApi.refreshSession(refreshToken);
      } catch {
        clearAuth();
      }
    }, delay);

    return () => window.clearTimeout(timer);
  }, [refreshToken, expiresAt, clearAuth]);

  return children;
};
