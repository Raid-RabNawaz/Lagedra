import { create } from "zustand";

export type AppMode = "guest" | "host";

const STORAGE_KEY = "lagedra.mode";

function getPersistedMode(): AppMode {
  if (typeof window === "undefined") return "guest";
  const stored = window.localStorage.getItem(STORAGE_KEY);
  return stored === "host" ? "host" : "guest";
}

type ModeStoreState = {
  mode: AppMode;
  setMode: (mode: AppMode) => void;
  toggleMode: () => void;
};

export const useModeStore = create<ModeStoreState>((set) => ({
  mode: getPersistedMode(),
  setMode: (mode) => {
    window.localStorage.setItem(STORAGE_KEY, mode);
    set({ mode });
  },
  toggleMode: () =>
    set((state) => {
      const next: AppMode = state.mode === "guest" ? "host" : "guest";
      window.localStorage.setItem(STORAGE_KEY, next);
      return { mode: next };
    }),
}));
