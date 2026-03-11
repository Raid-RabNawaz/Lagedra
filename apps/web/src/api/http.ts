import axios, { type AxiosError, type InternalAxiosRequestConfig } from "axios";
import { appConfig } from "@/app/config";
import { authStore } from "@/app/auth/authStore";
import { endpoints } from "./endpoints";
import type { AuthResultDto, ErrorResponse, RefreshTokenRequest } from "./types";

type RetriableRequest = InternalAxiosRequestConfig & { _retry?: boolean };

export const http = axios.create({
  baseURL: appConfig.apiBaseUrl,
  timeout: 10000,
});

const refreshClient = axios.create({
  baseURL: appConfig.apiBaseUrl,
  timeout: 10000,
});

const toCorrelationId = (): string => {
  if (typeof crypto !== "undefined" && "randomUUID" in crypto) {
    return crypto.randomUUID();
  }

  return `${Date.now()}-${Math.random().toString(36).slice(2)}`;
};

http.interceptors.request.use((request) => {
  const { accessToken } = authStore.getState();
  const headers = (request.headers ?? {}) as Record<string, string>;
  headers["X-Correlation-Id"] = toCorrelationId();
  if (accessToken) {
    headers.Authorization = `Bearer ${accessToken}`;
  }
  request.headers = headers;

  return request;
});

let activeRefreshPromise: Promise<AuthResultDto> | null = null;

const requestRefresh = async (): Promise<AuthResultDto> => {
  const { refreshToken } = authStore.getState();
  if (!refreshToken) {
    throw new Error("No refresh token available");
  }

  if (!activeRefreshPromise) {
    activeRefreshPromise = refreshClient
      .post<AuthResultDto, { data: AuthResultDto }, RefreshTokenRequest>(endpoints.auth.refresh, {
        refreshToken,
      })
      .then((response) => {
        authStore.getState().setTokens({
          accessToken: response.data.accessToken,
          refreshToken: response.data.refreshToken,
          expiresIn: response.data.expiresIn,
        });
        return response.data;
      })
      .finally(() => {
        activeRefreshPromise = null;
      });
  }

  return activeRefreshPromise;
};

const isAuthRefreshPath = (url?: string): boolean => url?.includes(endpoints.auth.refresh) ?? false;

http.interceptors.response.use(
  (response) => response,
  async (error: AxiosError<ErrorResponse>) => {
    const originalRequest = error.config as RetriableRequest | undefined;
    const status = error.response?.status;

    if (!originalRequest || status !== 401 || originalRequest._retry || isAuthRefreshPath(originalRequest.url)) {
      return Promise.reject(error);
    }

    try {
      originalRequest._retry = true;
      await requestRefresh();
      const { accessToken } = authStore.getState();
      if (accessToken) {
        const headers = (originalRequest.headers ?? {}) as Record<string, string>;
        headers.Authorization = `Bearer ${accessToken}`;
        originalRequest.headers = headers;
      }
      return http(originalRequest);
    } catch {
      authStore.getState().clearAuth();
      return Promise.reject(error);
    }
  },
);
