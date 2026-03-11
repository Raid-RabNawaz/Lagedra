import type { UserRole } from "@/app/auth/roles";

export type ErrorResponse = {
  error?: string;
  detail?: string;
  message?: string;
};

export type AuthResultDto = {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
  role: UserRole | number;
};

export type UserProfileDto = {
  userId: string;
  email: string;
  role: UserRole | number;
  isActive: boolean;
  firstName?: string | null;
  lastName?: string | null;
  displayName?: string | null;
  phoneNumber?: string | null;
  bio?: string | null;
  profilePhotoUrl?: string | null;
  city?: string | null;
  state?: string | null;
  country?: string | null;
  languages?: string | null;
  occupation?: string | null;
  dateOfBirth?: string | null;
  emergencyContactName?: string | null;
  emergencyContactPhone?: string | null;
  isGovernmentIdVerified?: boolean;
  isPhoneVerified?: boolean;
  responseRatePercent?: number | null;
  responseTimeMinutes?: number | null;
  memberSince: string;
  lastLoginAt?: string | null;
};

export type LoginRequest = {
  email: string;
  password: string;
};

export type RegisterRequest = {
  email: string;
  password: string;
  role: UserRole;
};

export type RegisterResponse = {
  userId: string;
  message: string;
  dev_verificationToken?: string;
  dev_verificationUrl?: string;
};

export type RefreshTokenRequest = {
  refreshToken: string;
};

export type ForgotPasswordRequest = {
  email: string;
};

export type ResetPasswordRequest = {
  userId: string;
  token: string;
  newPassword: string;
};

export type UpdateProfileRequest = {
  firstName: string | null;
  lastName: string | null;
  displayName: string | null;
  phoneNumber: string | null;
  bio: string | null;
  profilePhotoUrl: string | null;
  city: string | null;
  state: string | null;
  country: string | null;
  languages: string | null;
  occupation: string | null;
  dateOfBirth: string | null;
  emergencyContactName: string | null;
  emergencyContactPhone: string | null;
};
