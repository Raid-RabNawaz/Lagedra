import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import "./index.css";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { GoogleOAuthProvider } from "@react-oauth/google";
import { AuthProvider } from "@/app/auth/AuthProvider";
import { ErrorBoundary } from "@/app/layout/ErrorBoundary";
import { App } from "@/app/App";
import { appConfig } from "@/app/config";
import { getApiErrorStatus } from "@/api/errors";

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: (failureCount, error) => {
        const status = getApiErrorStatus(error);
        // Never retry client errors — they will not succeed on retry and we
        // want the UI to surface 401/403/404 immediately.
        if (status !== undefined && status >= 400 && status < 500) {
          return false;
        }
        return failureCount < 1;
      },
      refetchOnWindowFocus: false,
    },
    mutations: {
      retry: (failureCount, error) => {
        const status = getApiErrorStatus(error);
        if (status !== undefined && status >= 400 && status < 500) {
          return false;
        }
        return failureCount < 1;
      },
    },
  },
});

const appTree = (
  <QueryClientProvider client={queryClient}>
    <AuthProvider>
      <App />
    </AuthProvider>
  </QueryClientProvider>
);

const root = appConfig.googleClientId ? (
  <GoogleOAuthProvider clientId={appConfig.googleClientId}>{appTree}</GoogleOAuthProvider>
) : (
  appTree
);

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <ErrorBoundary>{root}</ErrorBoundary>
  </StrictMode>,
);
