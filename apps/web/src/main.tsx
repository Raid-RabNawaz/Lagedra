import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import "./index.css";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { GoogleOAuthProvider } from "@react-oauth/google";
import { AuthProvider } from "@/app/auth/AuthProvider";
import { ErrorBoundary } from "@/app/layout/ErrorBoundary";
import { App } from "@/app/App";
import { appConfig } from "@/app/config";

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 1,
      refetchOnWindowFocus: false,
    },
  },
});

function Root() {
  const inner = (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <App />
      </AuthProvider>
    </QueryClientProvider>
  );

  if (appConfig.googleClientId) {
    return (
      <GoogleOAuthProvider clientId={appConfig.googleClientId}>
        {inner}
      </GoogleOAuthProvider>
    );
  }

  return inner;
}

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <ErrorBoundary>
      <Root />
    </ErrorBoundary>
  </StrictMode>,
);
