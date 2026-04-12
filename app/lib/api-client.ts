/**
 * API Client for GeneFlow Backend.
 *
 * Features:
 * - Automatic token injection
 * - Token refresh on 401
 * - Request/response interceptors
 * - Type-safe error handling
 */

import type { ApiError, RefreshTokenResponse } from "@/types";

// =============================================================================
// CONFIGURATION
// =============================================================================

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5286";
console.log("[API Client] Using API URL:", API_BASE_URL);

// Token storage keys
const ACCESS_TOKEN_KEY = "geneflow_access_token";
const REFRESH_TOKEN_KEY = "geneflow_refresh_token";
const TOKEN_EXPIRY_KEY = "geneflow_token_expiry";

// =============================================================================
// TOKEN MANAGEMENT
// =============================================================================

export const tokenStorage = {
  getAccessToken(): string | null {
    if (typeof window === "undefined") return null;
    return localStorage.getItem(ACCESS_TOKEN_KEY);
  },

  getRefreshToken(): string | null {
    if (typeof window === "undefined") return null;
    return localStorage.getItem(REFRESH_TOKEN_KEY);
  },

  getTokenExpiry(): string | null {
    if (typeof window === "undefined") return null;
    return localStorage.getItem(TOKEN_EXPIRY_KEY);
  },

  setTokens(accessToken: string, refreshToken: string, expiresAt: string): void {
    if (typeof window === "undefined") return;
    localStorage.setItem(ACCESS_TOKEN_KEY, accessToken);
    localStorage.setItem(REFRESH_TOKEN_KEY, refreshToken);
    localStorage.setItem(TOKEN_EXPIRY_KEY, expiresAt);
  },

  clearTokens(): void {
    if (typeof window === "undefined") return;
    localStorage.removeItem(ACCESS_TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);
    localStorage.removeItem(TOKEN_EXPIRY_KEY);
  },

  isTokenExpired(): boolean {
    const expiry = this.getTokenExpiry();
    if (!expiry) return true;
    return new Date(expiry) <= new Date();
  },

  isTokenExpiringSoon(bufferSeconds: number = 60): boolean {
    const expiry = this.getTokenExpiry();
    if (!expiry) return true;
    const expiryDate = new Date(expiry);
    const bufferDate = new Date(Date.now() + bufferSeconds * 1000);
    return expiryDate <= bufferDate;
  },
};

// =============================================================================
// API ERROR CLASS
// =============================================================================

export class ApiClientError extends Error {
  public readonly status: number;
  public readonly code: string;
  public readonly details?: Record<string, string[]>;

  constructor(error: ApiError) {
    super(error.message);
    this.name = "ApiClientError";
    this.status = error.status;
    this.code = error.code;
    this.details = error.details;
  }

  static fromResponse(status: number, body: unknown): ApiClientError {
    if (typeof body === "object" && body !== null) {
      const error = body as Partial<ApiError>;
      return new ApiClientError({
        message: error.message || "An error occurred",
        code: error.code || "UNKNOWN_ERROR",
        status: error.status || status,
        details: error.details,
      });
    }
    return new ApiClientError({
      message: "An unexpected error occurred",
      code: "UNKNOWN_ERROR",
      status,
    });
  }
}

// =============================================================================
// REFRESH TOKEN LOGIC
// =============================================================================

let isRefreshing = false;
let refreshPromise: Promise<boolean> | null = null;

async function refreshAccessToken(): Promise<boolean> {
  const refreshToken = tokenStorage.getRefreshToken();
  if (!refreshToken) {
    tokenStorage.clearTokens();
    return false;
  }

  try {
    const response = await fetch(`${API_BASE_URL}/api/auth/refresh`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({ refreshToken }),
    });

    if (!response.ok) {
      tokenStorage.clearTokens();
      return false;
    }

    const data: RefreshTokenResponse = await response.json();
    tokenStorage.setTokens(data.accessToken, data.refreshToken, data.expiresAt);
    return true;
  } catch {
    tokenStorage.clearTokens();
    return false;
  }
}

async function ensureValidToken(): Promise<boolean> {
  // If token is not expiring soon, we're good
  if (!tokenStorage.isTokenExpiringSoon()) {
    return true;
  }

  // If already refreshing, wait for the existing refresh
  if (isRefreshing && refreshPromise) {
    return refreshPromise;
  }

  // Start refresh
  isRefreshing = true;
  refreshPromise = refreshAccessToken().finally(() => {
    isRefreshing = false;
    refreshPromise = null;
  });

  return refreshPromise;
}

// =============================================================================
// API CLIENT
// =============================================================================

export interface RequestOptions extends Omit<RequestInit, "body"> {
  body?: unknown;
  skipAuth?: boolean;
}

export async function apiClient<T>(
  endpoint: string,
  options: RequestOptions = {}
): Promise<T> {
  const { body, skipAuth = false, ...fetchOptions } = options;

  // Ensure valid token if authentication is needed
  if (!skipAuth && tokenStorage.getAccessToken()) {
    const tokenValid = await ensureValidToken();
    if (!tokenValid && !skipAuth) {
      // Token refresh failed, trigger logout
      if (typeof window !== "undefined") {
        window.dispatchEvent(new CustomEvent("auth:logout"));
      }
      throw new ApiClientError({
        message: "Session expired. Please log in again.",
        code: "SESSION_EXPIRED",
        status: 401,
      });
    }
  }

  const headers: HeadersInit = {
    "Content-Type": "application/json",
    ...fetchOptions.headers,
  };

  // Add authorization header if we have a token and not skipping auth
  const accessToken = tokenStorage.getAccessToken();
  if (accessToken && !skipAuth) {
    (headers as Record<string, string>)["Authorization"] = `Bearer ${accessToken}`;
  }

  const config: RequestInit = {
    ...fetchOptions,
    headers,
    body: body ? JSON.stringify(body) : undefined,
  };

  console.log("[API Client] Fetching:", `${API_BASE_URL}${endpoint}`);
  const response = await fetch(`${API_BASE_URL}${endpoint}`, config);

  // Handle 401 Unauthorized (token expired during request)
  if (response.status === 401 && !skipAuth) {
    const refreshed = await refreshAccessToken();
    if (refreshed) {
      // Retry with new token
      const newToken = tokenStorage.getAccessToken();
      (headers as Record<string, string>)["Authorization"] = `Bearer ${newToken}`;
      const retryResponse = await fetch(`${API_BASE_URL}${endpoint}`, {
        ...config,
        headers,
      });

      if (!retryResponse.ok) {
        const errorBody = await retryResponse.json().catch(() => null);
        throw ApiClientError.fromResponse(retryResponse.status, errorBody);
      }

      if (retryResponse.status === 204) {
        return undefined as T;
      }

      return retryResponse.json();
    }

    // Refresh failed, logout
    if (typeof window !== "undefined") {
      window.dispatchEvent(new CustomEvent("auth:logout"));
    }
    throw new ApiClientError({
      message: "Session expired. Please log in again.",
      code: "SESSION_EXPIRED",
      status: 401,
    });
  }

  // Handle other error responses
  if (!response.ok) {
    const errorBody = await response.json().catch(() => null);
    throw ApiClientError.fromResponse(response.status, errorBody);
  }

  // Handle 204 No Content
  if (response.status === 204) {
    return undefined as T;
  }

  return response.json();
}

// Convenience methods
export const api = {
  get: <T>(endpoint: string, options?: RequestOptions) =>
    apiClient<T>(endpoint, { ...options, method: "GET" }),

  post: <T>(endpoint: string, body?: unknown, options?: RequestOptions) =>
    apiClient<T>(endpoint, { ...options, method: "POST", body }),

  put: <T>(endpoint: string, body?: unknown, options?: RequestOptions) =>
    apiClient<T>(endpoint, { ...options, method: "PUT", body }),

  patch: <T>(endpoint: string, body?: unknown, options?: RequestOptions) =>
    apiClient<T>(endpoint, { ...options, method: "PATCH", body }),

  delete: <T>(endpoint: string, options?: RequestOptions) =>
    apiClient<T>(endpoint, { ...options, method: "DELETE" }),

  /**
   * Upload a file using FormData (multipart/form-data).
   * @param endpoint - API endpoint
   * @param file - File to upload
   * @param fieldName - Form field name (default: "file")
   */
  uploadFile: async <T>(
    endpoint: string,
    file: File,
    fieldName: string = "file"
  ): Promise<T> => {
    // Ensure valid token
    const tokenValid = await ensureValidToken();
    if (!tokenValid) {
      if (typeof window !== "undefined") {
        window.dispatchEvent(new CustomEvent("auth:logout"));
      }
      throw new ApiClientError({
        message: "Session expired. Please log in again.",
        code: "SESSION_EXPIRED",
        status: 401,
      });
    }

    const formData = new FormData();
    formData.append(fieldName, file);

    const headers: HeadersInit = {};
    const accessToken = tokenStorage.getAccessToken();
    if (accessToken) {
      headers["Authorization"] = `Bearer ${accessToken}`;
    }
    // Note: Don't set Content-Type for FormData - browser sets it with boundary

    const response = await fetch(`${API_BASE_URL}${endpoint}`, {
      method: "POST",
      headers,
      body: formData,
    });

    if (!response.ok) {
      const errorBody = await response.json().catch(() => null);
      throw ApiClientError.fromResponse(response.status, errorBody);
    }

    if (response.status === 204) {
      return undefined as T;
    }

    return response.json();
  },
};
