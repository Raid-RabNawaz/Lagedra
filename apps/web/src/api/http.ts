import axios, { AxiosHeaders, type AxiosError, type InternalAxiosRequestConfig } from "axios";
import { appConfig } from "@/app/config";
import { authStore } from "@/app/auth/authStore";
import { endpoints } from "./endpoints";
import type { AuthResultDto, ErrorResponse, RefreshTokenRequest } from "./types";

type RetriableRequest = InternalAxiosRequestConfig & {
  _retry?: boolean;
  _consentRetry?: boolean;
};
type ConsentTypeDto = "KYCConsent" | "DataProcessing";
type ConsentRecordDto = {
  consentType: ConsentTypeDto;
  withdrawnAt: string | null;
};
type RecordConsentRequest = {
  userId: string;
  consentType: ConsentTypeDto;
  ipAddress: string;
  userAgent: string;
};

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
  request.headers = request.headers ?? new AxiosHeaders();
  request.headers.set("X-Correlation-Id", toCorrelationId());
  if (accessToken) {
    request.headers.set("Authorization", `Bearer ${accessToken}`);
  }

  return request;
});

let activeRefreshPromise: Promise<AuthResultDto> | null = null;
let activeConsentPromise: Promise<void> | null = null;

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
const isPrivacyConsentPath = (url?: string): boolean => url?.includes("/v1/privacy/consent") ?? false;

const requestRequiredConsents = async (): Promise<void> => {
  const { user, accessToken } = authStore.getState();
  if (!user?.userId || !accessToken) {
    throw new Error("No authenticated user available for consent retry");
  }

  if (!activeConsentPromise) {
    const authHeaders = new AxiosHeaders();
    authHeaders.set("Authorization", `Bearer ${accessToken}`);
    authHeaders.set("X-Correlation-Id", toCorrelationId());

    activeConsentPromise = (async () => {
      const consentsResponse = await refreshClient.get<ConsentRecordDto[]>(
        endpoints.privacy.userConsents(user.userId),
        { headers: authHeaders },
      );

      const active = new Set(
        consentsResponse.data
          .filter((record) => record.withdrawnAt == null)
          .map((record) => record.consentType),
      );

      const required: ConsentTypeDto[] = ["KYCConsent", "DataProcessing"];
      for (const consentType of required) {
        if (active.has(consentType)) {
          continue;
        }

        const body: RecordConsentRequest = {
          userId: user.userId,
          consentType,
          ipAddress: "0.0.0.0",
          userAgent: typeof navigator !== "undefined" && navigator.userAgent
            ? navigator.userAgent
            : "LagedraWeb",
        };

        await refreshClient.post(endpoints.privacy.recordConsent, body, {
          headers: authHeaders,
        });
      }
    })().finally(() => {
      activeConsentPromise = null;
    });
  }

  return activeConsentPromise;
};

http.interceptors.response.use(
  (response) => response,
  async (error: AxiosError<ErrorResponse>) => {
    const originalRequest = error.config as RetriableRequest | undefined;
    const status = error.response?.status;

    if (originalRequest && status === 451 && !originalRequest._consentRetry && !isPrivacyConsentPath(originalRequest.url)) {
      try {
        originalRequest._consentRetry = true;
        await requestRequiredConsents();
        return http(originalRequest);
      } catch {
        return Promise.reject(error);
      }
    }

    if (!originalRequest || status !== 401 || originalRequest._retry || isAuthRefreshPath(originalRequest.url)) {
      return Promise.reject(error);
    }

    try {
      originalRequest._retry = true;
      await requestRefresh();
      const { accessToken } = authStore.getState();
      if (accessToken) {
        originalRequest.headers = originalRequest.headers ?? new AxiosHeaders();
        originalRequest.headers.set("Authorization", `Bearer ${accessToken}`);
      }
      return http(originalRequest);
    } catch {
      authStore.getState().clearAuth();
      return Promise.reject(error);
    }
  },
);
