import { create } from "zustand";
import type { UserProfileDto } from "@/api/types";

const STORAGE_KEY = "lagedra.auth";

export type AuthStoreState = {
  accessToken: string | null;
  refreshToken: string | null;
  expiresAt: number | null;
  user: UserProfileDto | null;
  isInitialized: boolean;
  setTokens: (payload: { accessToken: string; refreshToken: string; expiresIn: number }) => void;
  setUser: (user: UserProfileDto | null) => void;
  setInitialized: (value: boolean) => void;
  clearAuth: () => void;
};

type PersistedAuthState = Pick<AuthStoreState, "accessToken" | "refreshToken" | "expiresAt">;

const getInitialState = (): PersistedAuthState => {
  const emptyState: PersistedAuthState = {
    accessToken: null,
    refreshToken: null,
    expiresAt: null,
  };

  if (typeof window === "undefined") {
    return emptyState;
  }

  const savedState = window.localStorage.getItem(STORAGE_KEY);
  if (!savedState) {
    return emptyState;
  }

  try {
    const parsed = JSON.parse(savedState) as PersistedAuthState;
    return {
      accessToken: parsed.accessToken ?? null,
      refreshToken: parsed.refreshToken ?? null,
      expiresAt: parsed.expiresAt ?? null,
    };
  } catch {
    return emptyState;
  }
};

const savePersistedState = (state: PersistedAuthState): void => {
  if (typeof window !== "undefined") {
    window.localStorage.setItem(STORAGE_KEY, JSON.stringify(state));
  }
};

const persistedState = getInitialState();

export const useAuthStore = create<AuthStoreState>((set) => ({
  accessToken: persistedState.accessToken,
  refreshToken: persistedState.refreshToken,
  expiresAt: persistedState.expiresAt,
  user: null,
  isInitialized: false,
  setTokens: ({ accessToken, refreshToken, expiresIn }) =>
    set(() => {
      const expiresAt = Date.now() + expiresIn * 1000;
      savePersistedState({ accessToken, refreshToken, expiresAt });
      return { accessToken, refreshToken, expiresAt };
    }),
  setUser: (user) => set(() => ({ user })),
  setInitialized: (value) => set(() => ({ isInitialized: value })),
  clearAuth: () =>
    set(() => {
      if (typeof window !== "undefined") {
        window.localStorage.removeItem(STORAGE_KEY);
      }
      return {
        accessToken: null,
        refreshToken: null,
        expiresAt: null,
        user: null,
      };
    }),
}));

export const authStore = {
  getState: useAuthStore.getState,
  setState: useAuthStore.setState,
};
