const env = import.meta.env;

export const appConfig = {
  apiBaseUrl: env.VITE_API_BASE_URL ?? "http://localhost:5000",
};

