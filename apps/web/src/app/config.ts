const env = import.meta.env;

export const appConfig = {
  apiBaseUrl: (env.VITE_API_BASE_URL as string | undefined) ?? "http://localhost:5000",
  googleClientId: (env.VITE_GOOGLE_CLIENT_ID as string | undefined) ?? "",
};
