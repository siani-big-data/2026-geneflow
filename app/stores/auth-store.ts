/**
 * Authentication Store using Zustand.
 *
 * Manages global authentication state including:
 * - User information
 * - Token storage
 * - Loading states
 * - 2FA flow state
 */

import { create } from "zustand";
import { persist, createJSONStorage } from "zustand/middleware";
import { authService } from "@/services";
import { tokenStorage } from "@/lib/api-client";
import type {
  AuthUser,
  AuthState,
  AuthStatus,
  LoginRequest,
  RegisterRequest,
  TwoFactorVerifyRequest,
} from "@/types";

// =============================================================================
// STORE INTERFACE
// =============================================================================

interface AuthStore extends AuthState {
  // Computed
  status: AuthStatus;

  // Actions
  login: (data: LoginRequest) => Promise<void>;
  verifyTwoFactor: (data: TwoFactorVerifyRequest) => Promise<void>;
  register: (data: RegisterRequest) => Promise<{ success: boolean; message: string }>;
  logout: () => Promise<void>;
  refreshUser: () => Promise<void>;
  initialize: () => Promise<void>;
  clearError: () => void;
  setError: (error: string) => void;
}

// =============================================================================
// INITIAL STATE
// =============================================================================

const initialState: AuthState = {
  user: null,
  accessToken: null,
  refreshToken: null,
  expiresAt: null,
  isAuthenticated: false,
  isLoading: true, // Start loading until initialized
  error: null,
  requiresTwoFactor: false,
  pendingTwoFactorEmail: null,
};

// =============================================================================
// STORE IMPLEMENTATION
// =============================================================================

export const useAuthStore = create<AuthStore>()(
  persist(
    (set, get) => ({
      ...initialState,

      // Computed status
      get status(): AuthStatus {
        const state = get();
        if (state.isLoading) return "loading";
        if (state.requiresTwoFactor) return "requires-2fa";
        if (state.isAuthenticated) return "authenticated";
        return "unauthenticated";
      },

      // ===========================================================================
      // LOGIN
      // ===========================================================================

      login: async (data: LoginRequest) => {
        set({ isLoading: true, error: null });

        try {
          const response = await authService.login(data);

          if (response.requiresTwoFactor) {
            // 2FA required - store email and wait for code
            set({
              isLoading: false,
              requiresTwoFactor: true,
              pendingTwoFactorEmail: data.email,
            });
            return;
          }

          // Login successful
          set({
            user: response.user,
            accessToken: response.tokens.accessToken,
            refreshToken: response.tokens.refreshToken,
            expiresAt: response.tokens.expiresAt,
            isAuthenticated: true,
            isLoading: false,
            requiresTwoFactor: false,
            pendingTwoFactorEmail: null,
          });
        } catch (error) {
          const message =
            error instanceof Error ? error.message : "Login failed";
          set({
            isLoading: false,
            error: message,
          });
          throw error;
        }
      },

      // ===========================================================================
      // 2FA VERIFICATION
      // ===========================================================================

      verifyTwoFactor: async (data: TwoFactorVerifyRequest) => {
        const { pendingTwoFactorEmail } = get();

        if (!pendingTwoFactorEmail) {
          throw new Error("No pending 2FA verification");
        }

        set({ isLoading: true, error: null });

        try {
          const response = await authService.verifyTwoFactor(
            pendingTwoFactorEmail,
            data
          );

          set({
            user: response.user,
            accessToken: response.tokens.accessToken,
            refreshToken: response.tokens.refreshToken,
            expiresAt: response.tokens.expiresAt,
            isAuthenticated: true,
            isLoading: false,
            requiresTwoFactor: false,
            pendingTwoFactorEmail: null,
          });
        } catch (error) {
          const message =
            error instanceof Error ? error.message : "2FA verification failed";
          set({
            isLoading: false,
            error: message,
          });
          throw error;
        }
      },

      // ===========================================================================
      // REGISTRATION
      // ===========================================================================

      register: async (data: RegisterRequest) => {
        set({ isLoading: true, error: null });

        try {
          const response = await authService.register(data);

          set({ isLoading: false });

          return {
            success: true,
            message:
              response.message ||
              "Registration successful. Please check your email to verify your account.",
          };
        } catch (error) {
          const message =
            error instanceof Error ? error.message : "Registration failed";
          set({
            isLoading: false,
            error: message,
          });
          throw error;
        }
      },

      // ===========================================================================
      // LOGOUT
      // ===========================================================================

      logout: async () => {
        set({ isLoading: true });

        try {
          await authService.logout();
        } finally {
          tokenStorage.clearTokens();
          set({
            ...initialState,
            isLoading: false,
          });
        }
      },

      // ===========================================================================
      // REFRESH USER
      // ===========================================================================

      refreshUser: async () => {
        if (!get().isAuthenticated) return;

        try {
          const user = await authService.getCurrentUser();
          set({ user });
        } catch (error) {
          // If refresh fails with 401, logout
          if (error instanceof Error && error.message.includes("401")) {
            get().logout();
          }
        }
      },

      // ===========================================================================
      // INITIALIZE
      // ===========================================================================

      initialize: async () => {
        // Check if we have stored tokens
        const accessToken = tokenStorage.getAccessToken();
        const refreshToken = tokenStorage.getRefreshToken();
        const expiresAt = tokenStorage.getTokenExpiry();

        if (!accessToken || !refreshToken) {
          set({ ...initialState, isLoading: false });
          return;
        }

        // Check if token is expired
        if (tokenStorage.isTokenExpired()) {
          try {
            // Try to refresh
            const response = await authService.refreshToken();
            const user = await authService.getCurrentUser();

            set({
              user,
              accessToken: response.accessToken,
              refreshToken: response.refreshToken,
              expiresAt: response.expiresAt,
              isAuthenticated: true,
              isLoading: false,
            });
          } catch {
            // Refresh failed, clear tokens
            tokenStorage.clearTokens();
            set({ ...initialState, isLoading: false });
          }
          return;
        }

        // Token is valid, fetch user
        try {
          const user = await authService.getCurrentUser();

          set({
            user,
            accessToken,
            refreshToken,
            expiresAt,
            isAuthenticated: true,
            isLoading: false,
          });
        } catch {
          // Fetch failed, clear tokens
          tokenStorage.clearTokens();
          set({ ...initialState, isLoading: false });
        }
      },

      // ===========================================================================
      // UTILITIES
      // ===========================================================================

      clearError: () => set({ error: null }),

      setError: (error: string) => set({ error }),
    }),
    {
      name: "geneflow-auth",
      storage: createJSONStorage(() => localStorage),
      partialize: (state) => ({
        // Only persist user info, not tokens (they're in tokenStorage)
        user: state.user,
        isAuthenticated: state.isAuthenticated,
      }),
    }
  )
);

// =============================================================================
// SELECTORS
// =============================================================================

export const selectUser = (state: AuthStore) => state.user;
export const selectIsAuthenticated = (state: AuthStore) => state.isAuthenticated;
export const selectIsLoading = (state: AuthStore) => state.isLoading;
export const selectAuthStatus = (state: AuthStore) => state.status;
export const selectAuthError = (state: AuthStore) => state.error;

// =============================================================================
// LOGOUT EVENT LISTENER
// =============================================================================

if (typeof window !== "undefined") {
  window.addEventListener("auth:logout", () => {
    useAuthStore.getState().logout();
  });
}
