/**
 * Authentication-related types for GeneFlow.
 */

// =============================================================================
// REQUEST TYPES
// =============================================================================

export interface LoginRequest {
  identifier: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  username: string;
  password: string;
}

export interface RefreshTokenRequest {
  refreshToken: string;
}

export interface VerifyEmailRequest {
  token: string;
}

export interface RequestPasswordResetRequest {
  email: string;
}

export interface ResetPasswordRequest {
  token: string;
  newPassword: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

export interface TwoFactorVerifyRequest {
  code: string;
}

export interface EnableTwoFactorRequest {
  code: string;
}

// =============================================================================
// RESPONSE TYPES
// =============================================================================

export interface AuthTokens {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
}

export interface AuthUser {
  id: string;
  email: string;
  username: string;
  emailVerified: boolean;
  twoFactorEnabled: boolean;
  hasPassword: boolean;
  roles: string[];
  createdAt: string;
}

export interface LoginResponse {
  user: AuthUser;
  tokens: AuthTokens;
  requiresTwoFactor?: boolean;
}

export interface RegisterResponse {
  user: AuthUser;
  message: string;
}

export interface RefreshTokenResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
}

export interface TwoFactorSetupResponse {
  secret: string;
  qrCodeUri: string;
}

export interface ConfirmTwoFactorSetupRequest {
  code: string;
  // Note: secret is stored server-side and not sent by client for security
}

// =============================================================================
// OAUTH TYPES
// =============================================================================

export interface OAuthLoginRequest {
  token: string;
}

export interface LinkExternalLoginRequest {
  provider: string;
  token: string;
}

export interface ExternalLogin {
  provider: string;
  displayName: string | null;
  linkedAt: string;
}

// =============================================================================
// STATE TYPES
// =============================================================================

export interface AuthState {
  user: AuthUser | null;
  accessToken: string | null;
  refreshToken: string | null;
  expiresAt: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  error: string | null;
  requiresTwoFactor: boolean;
  pendingTwoFactorEmail: string | null;
}

export type AuthStatus =
  | "idle"
  | "loading"
  | "authenticated"
  | "unauthenticated"
  | "requires-2fa";
