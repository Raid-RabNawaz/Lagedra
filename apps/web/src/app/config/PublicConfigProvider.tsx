import { type PropsWithChildren, useEffect } from "react";
import { configApi } from "./configApi";
import { usePublicConfigStore } from "./publicConfigStore";

/**
 * Loads platform-wide public config (currently just the pre-launch flag) once
 * at startup and mirrors it into a zustand store the router guards can read.
 *
 * We intentionally do NOT block rendering on this fetch — the marketing/browse
 * pages are safe to show either way, and route guards treat "not yet loaded"
 * as "no gating" so nothing flashes an incorrect redirect. A failed fetch
 * simply leaves the platform in normal (non-pre-launch) mode.
 */
export const PublicConfigProvider = ({ children }: PropsWithChildren) => {
  const setConfig = usePublicConfigStore((s) => s.setConfig);
  const setLoaded = usePublicConfigStore((s) => s.setLoaded);

  useEffect(() => {
    let cancelled = false;

    void (async () => {
      try {
        const config = await configApi.getPublicConfig();
        if (!cancelled) {
          setConfig({ preLaunchEnabled: config.preLaunchEnabled });
        }
      } catch {
        // Leave defaults (pre-launch off) on failure.
      } finally {
        if (!cancelled) {
          setLoaded(true);
        }
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [setConfig, setLoaded]);

  return children;
};
