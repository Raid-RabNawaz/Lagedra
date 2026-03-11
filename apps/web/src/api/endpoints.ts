export const endpoints = {
  auth: {
    register: "/v1/auth/register",
    verifyEmail: "/v1/auth/verify-email",
    resendVerification: "/v1/auth/resend-verification",
    login: "/v1/auth/login",
    refresh: "/v1/auth/refresh",
    logout: "/v1/auth/logout",
    forgotPassword: "/v1/auth/forgot-password",
    resetPassword: "/v1/auth/reset-password",
    me: "/v1/auth/me",
  },
} as const;

