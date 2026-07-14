import { create } from "zustand";

const CACHE_KEY = "lagedra.publicConfig";

type CachedConfig = { preLaunchEnabled: boolean };

const readCache = (): CachedConfig | null => {
  if (typeof window === "undefined") return null;
  try {
    const raw = window.localStorage.getItem(CACHE_KEY);
    if (!raw) return null;
    const parsed = JSON.parse(raw) as CachedConfig;
    return { preLaunchEnabled: Boolean(parsed.preLaunchEnabled) };
  } catch {
    return null;
  }
};

const writeCache = (config: CachedConfig): void => {
  if (typeof window === "undefined") return;
  try {
    window.localStorage.setItem(CACHE_KEY, JSON.stringify(config));
  } catch {
    // Best-effort cache; ignore quota/serialization errors.
  }
};

const cached = readCache();

export type PublicConfigState = {
  /** Whether the platform is running in pre-launch (founding-partner) mode. */
  preLaunchEnabled: boolean;
  /** True once the config value is known (from cache or a completed fetch). */
  isLoaded: boolean;
  setConfig: (config: { preLaunchEnabled: boolean }) => void;
  setLoaded: (value: boolean) => void;
};

export const usePublicConfigStore = create<PublicConfigState>((set) => ({
  // Seed the flag from the last-known value, but only treat the config as
  // "loaded" when the cache says pre-launch is ON. That lets a returning
  // pre-launch visitor be gated on the very first paint (no flash of the
  // product), while a cached "off" — or no cache — must be re-confirmed by the
  // network fetch before any gated route renders. Without this, a stale "off"
  // (e.g. cached while the flag was temporarily disabled) would briefly leak
  // the full site until the fresh config arrives.
  preLaunchEnabled: cached?.preLaunchEnabled ?? false,
  isLoaded: cached?.preLaunchEnabled === true,
  setConfig: ({ preLaunchEnabled }) => {
    writeCache({ preLaunchEnabled });
    set(() => ({ preLaunchEnabled }));
  },
  setLoaded: (value) => set(() => ({ isLoaded: value })),
}));

/** Non-hook accessor for use inside route guards / imperative code. */
export const publicConfig = {
  getState: usePublicConfigStore.getState,
};
