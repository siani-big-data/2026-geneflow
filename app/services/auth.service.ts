/**
 * Authentication Service for GeneFlow.
 *
 * Handles all authentication-related API calls:
 * - Login/Logout
 * - Registration
 * - Email verification
 * - Password reset
 * - Two-factor authentication
 * - Token refresh
 */

import { api, tokenStorage } from "@/lib/api-client";
import type {
  LoginRequest,
  LoginResponse,
  RegisterRequest,
  RegisterResponse,
  RefreshTokenResponse,
  VerifyEmailRequest,
  RequestPasswordResetRequest,
  ResetPasswordRequest,
  TwoFactorVerifyRequest,
  TwoFactorSetupResponse,
  EnableTwoFactorRequest,
  AuthUser,
} from "@/types";

export const authService = {
  // ===========================================================================
  // AUTHENTICATION
  // ===========================================================================

  /**
   * Login with email and password.
   * May return requiresTwoFactor if 2FA is enabled.
   */
  async login(data: LoginRequest): Promise<LoginResponse> {
    const response = await api.post<LoginResponse>("/api/v1/auth/login", data, {
      skipAuth: true,
    });

    // Store tokens if login successful and no 2FA required
    if (response.tokens && !response.requiresTwoFactor) {
      tokenStorage.setTokens(
        response.tokens.accessToken,
        response.tokens.refreshToken,
        response.tokens.expiresAt
      );
    }

    return response;
  },

  /**
   * Verify 2FA code after login.
   */
  async verifyTwoFactor(
    email: string,
    data: TwoFactorVerifyRequest
  ): Promise<LoginResponse> {
    const response = await api.post<LoginResponse>(
      "/api/v1/auth/2fa/verify",
      { email, ...data },
      { skipAuth: true }
    );

    if (response.tokens) {
      tokenStorage.setTokens(
        response.tokens.accessToken,
        response.tokens.refreshToken,
        response.tokens.expiresAt
      );
    }

    return response;
  },

  /**
   * Logout the current user.
   */
  async logout(): Promise<void> {
    const refreshToken = tokenStorage.getRefreshToken();

    try {
      if (refreshToken) {
        await api.post("/api/v1/auth/logout", { refreshToken });
      }
    } finally {
      tokenStorage.clearTokens();
    }
  },

  /**
   * Refresh the access token.
   */
  async refreshToken(): Promise<RefreshTokenResponse> {
    const refreshToken = tokenStorage.getRefreshToken();

    if (!refreshToken) {
      throw new Error("No refresh token available");
    }

    const response = await api.post<RefreshTokenResponse>(
      "/api/v1/auth/refresh",
      { refreshToken },
      { skipAuth: true }
    );

    tokenStorage.setTokens(
      response.accessToken,
      response.refreshToken,
      response.expiresAt
    );

    return response;
  },

  // ===========================================================================
  // REGISTRATION
  // ===========================================================================

  /**
   * Register a new user.
   */
  async register(data: RegisterRequest): Promise<RegisterResponse> {
    return api.post<RegisterResponse>("/api/v1/auth/register", data, {
      skipAuth: true,
    });
  },

  /**
   * Verify email with token.
   */
  async verifyEmail(data: VerifyEmailRequest): Promise<void> {
    return api.post("/api/v1/users/verify-email", data, { skipAuth: true });
  },

  /**
   * Resend email verification.
   */
  async resendVerificationEmail(email: string): Promise<void> {
    return api.post(
      "/api/v1/users/resend-verification",
      { email },
      { skipAuth: true }
    );
  },

  // ===========================================================================
  // PASSWORD RESET
  // ===========================================================================

  /**
   * Request a password reset email.
   */
  async requestPasswordReset(data: RequestPasswordResetRequest): Promise<void> {
    return api.post("/api/v1/auth/request-password-reset", data, {
      skipAuth: true,
    });
  },

  /**
   * Reset password with token.
   */
  async resetPassword(data: ResetPasswordRequest): Promise<void> {
    return api.post("/api/v1/auth/reset-password", data, { skipAuth: true });
  },

  // ===========================================================================
  // TWO-FACTOR AUTHENTICATION
  // ===========================================================================

  /**
   * Request a 2FA code to be sent via email.
   */
  async requestTwoFactorCode(email: string): Promise<void> {
    return api.post("/api/v1/auth/2fa/request-code", { email }, { skipAuth: true });
  },

  /**
   * Get 2FA setup information (secret and QR code).
   */
  async getTwoFactorSetup(): Promise<TwoFactorSetupResponse> {
    return api.get<TwoFactorSetupResponse>("/api/v1/users/2fa/setup");
  },

  /**
   * Enable 2FA with verification code.
   */
  async enableTwoFactor(data: EnableTwoFactorRequest): Promise<void> {
    return api.post("/api/v1/users/2fa/enable", data);
  },

  /**
   * Disable 2FA.
   */
  async disableTwoFactor(code: string): Promise<void> {
    return api.post("/api/v1/users/2fa/disable", { code });
  },

  // ===========================================================================
  // USER INFO
  // ===========================================================================

  /**
   * Get the current authenticated user.
   */
  async getCurrentUser(): Promise<AuthUser> {
    return api.get<AuthUser>("/api/v1/users/me");
  },

  /**
   * Check if user is authenticated (has valid token).
   */
  isAuthenticated(): boolean {
    return !!tokenStorage.getAccessToken() && !tokenStorage.isTokenExpired();
  },

  /**
   * Get stored access token.
   */
  getAccessToken(): string | null {
    return tokenStorage.getAccessToken();
  },
};
