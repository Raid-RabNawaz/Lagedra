import { endpoints } from "@/api/endpoints";
import { http } from "@/api/http";
import type {
  AuthResultDto,
  ChangePasswordRequest,
  ExternalLoginRequest,
  ForgotPasswordRequest,
  LoginRequest,
  RefreshTokenRequest,
  RegisterRequest,
  RegisterResponse,
  ResetPasswordRequest,
  UpdateProfileRequest,
  UpdateRoleRequest,
  UserProfileDto,
} from "@/api/types";
import { useAuthStore } from "@/app/auth/authStore";

const applyAuthResult = (result: AuthResultDto): void => {
  useAuthStore.getState().setTokens({
    accessToken: result.accessToken,
    refreshToken: result.refreshToken,
    expiresIn: result.expiresIn,
  });
};

export const authApi = {
  async login(payload: LoginRequest): Promise<AuthResultDto> {
    const response = await http.post<AuthResultDto>(endpoints.auth.login, payload);
    applyAuthResult(response.data);
    return response.data;
  },

  async externalLogin(payload: ExternalLoginRequest): Promise<AuthResultDto> {
    const response = await http.post<AuthResultDto>(endpoints.auth.externalLogin, payload);
    applyAuthResult(response.data);
    return response.data;
  },

  async register(payload: RegisterRequest): Promise<RegisterResponse> {
    const response = await http.post<RegisterResponse>(endpoints.auth.register, payload);
    return response.data;
  },

  async verifyEmail(userId: string, token: string): Promise<{ message: string }> {
    const response = await http.get<{ message: string }>(endpoints.auth.verifyEmail, {
      params: { userId, token },
    });
    return response.data;
  },

  async forgotPassword(payload: ForgotPasswordRequest): Promise<{ message: string }> {
    const response = await http.post<{ message: string }>(endpoints.auth.forgotPassword, payload);
    return response.data;
  },

  async resetPassword(payload: ResetPasswordRequest): Promise<{ message: string }> {
    const response = await http.post<{ message: string }>(endpoints.auth.resetPassword, payload);
    return response.data;
  },

  async changePassword(payload: ChangePasswordRequest): Promise<{ message: string }> {
    const response = await http.post<{ message: string }>(endpoints.auth.changePassword, payload);
    return response.data;
  },

  async refreshSession(refreshToken: string): Promise<AuthResultDto> {
    const body: RefreshTokenRequest = { refreshToken };
    const response = await http.post<AuthResultDto>(endpoints.auth.refresh, body);
    applyAuthResult(response.data);
    return response.data;
  },

  async logout(): Promise<void> {
    const refreshToken = useAuthStore.getState().refreshToken;
    if (refreshToken) {
      await http.post(endpoints.auth.logout, { refreshToken });
    }
    useAuthStore.getState().clearAuth();
  },

  async getCurrentUser(): Promise<UserProfileDto> {
    const response = await http.get<UserProfileDto>(endpoints.auth.me);
    return response.data;
  },

  async updateProfile(payload: UpdateProfileRequest): Promise<UserProfileDto> {
    const response = await http.put<UserProfileDto>(endpoints.auth.me, payload);
    useAuthStore.getState().setUser(response.data);
    return response.data;
  },

  async listUsers(page = 1, pageSize = 50): Promise<UserProfileDto[]> {
    const response = await http.get<UserProfileDto[]>(endpoints.auth.users, {
      params: { page, pageSize },
    });
    return response.data;
  },

  async updateUserRole(userId: string, payload: UpdateRoleRequest): Promise<{ message: string }> {
    const response = await http.put<{ message: string }>(endpoints.auth.userRole(userId), payload);
    return response.data;
  },
};
