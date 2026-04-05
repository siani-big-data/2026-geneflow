/**
 * Error message mapping for user-friendly error display.
 * Maps backend error codes to localized, user-friendly messages.
 */

export interface ErrorInfo {
  title: string;
  message: string;
  action?: string;
  actionLink?: string;
}

/**
 * Maps backend error codes to user-friendly messages.
 */
const errorMessages: Record<string, ErrorInfo> = {
  // =============================================================================
  // REGISTRATION ERRORS
  // =============================================================================
  "User.EmailAlreadyExists": {
    title: "Email already registered",
    message: "An account with this email already exists.",
    action: "Sign in instead",
    actionLink: "/login",
  },
  "User.UsernameAlreadyExists": {
    title: "Username taken",
    message: "This username is already in use. Please choose another one.",
  },
  "User.EmailInvalidFormat": {
    title: "Invalid email",
    message: "Please enter a valid email address.",
  },
  "User.UsernameInvalidFormat": {
    title: "Invalid username",
    message: "Username can only contain letters and numbers.",
  },
  "User.UsernameTooShort": {
    title: "Username too short",
    message: "Username must be at least 3 characters long.",
  },
  "User.UsernameTooLong": {
    title: "Username too long",
    message: "Username must not exceed 50 characters.",
  },
  "User.EmailTooLong": {
    title: "Email too long",
    message: "Email must not exceed 255 characters.",
  },

  // =============================================================================
  // LOGIN ERRORS
  // =============================================================================
  "User.InvalidCredentials": {
    title: "Invalid credentials",
    message: "The email/username or password you entered is incorrect.",
  },
  "User.NotFound": {
    title: "Account not found",
    message: "No account exists with this email or username.",
    action: "Create an account",
    actionLink: "/register",
  },
  "User.NotFoundByEmail": {
    title: "Account not found",
    message: "No account exists with this email address.",
    action: "Create an account",
    actionLink: "/register",
  },
  "User.EmailNotVerified": {
    title: "Email not verified",
    message: "Please verify your email address before signing in. Check your inbox for the verification link.",
    action: "Resend verification email",
  },
  "User.AccountLocked": {
    title: "Account locked",
    message: "Your account has been temporarily locked due to too many failed login attempts. Please try again later.",
  },
  "User.AccountLockedUntil": {
    title: "Account locked",
    message: "Your account is temporarily locked. Please try again later.",
  },
  "User.UserDeactivated": {
    title: "Account deactivated",
    message: "This account has been deactivated. Please contact support for assistance.",
  },

  // =============================================================================
  // TWO-FACTOR AUTHENTICATION ERRORS
  // =============================================================================
  "User.InvalidTwoFactorCode": {
    title: "Invalid code",
    message: "The verification code you entered is incorrect. Please try again.",
  },
  "User.TwoFactorCodeExpired": {
    title: "Code expired",
    message: "The verification code has expired. Please request a new one.",
  },
  "User.TwoFactorCodeAlreadyUsed": {
    title: "Code already used",
    message: "This verification code has already been used. Please request a new one.",
  },
  "User.TwoFactorRequired": {
    title: "Verification required",
    message: "Please enter the verification code sent to your email.",
  },

  // =============================================================================
  // EMAIL VERIFICATION ERRORS
  // =============================================================================
  "User.InvalidVerificationToken": {
    title: "Invalid or expired link",
    message: "This verification link is invalid or has expired.",
    action: "Resend verification email",
  },

  // =============================================================================
  // PASSWORD RESET ERRORS
  // =============================================================================
  "User.InvalidPasswordResetToken": {
    title: "Invalid or expired link",
    message: "This password reset link is invalid or has expired.",
    action: "Request new reset link",
    actionLink: "/forgot-password",
  },
  "User.PasswordResetTokenExpired": {
    title: "Link expired",
    message: "This password reset link has expired.",
    action: "Request new reset link",
    actionLink: "/forgot-password",
  },

  // =============================================================================
  // SESSION ERRORS
  // =============================================================================
  "User.RefreshTokenExpired": {
    title: "Session expired",
    message: "Your session has expired. Please sign in again.",
    action: "Sign in",
    actionLink: "/login",
  },
  "User.RefreshTokenRevoked": {
    title: "Session ended",
    message: "Your session has been ended. Please sign in again.",
    action: "Sign in",
    actionLink: "/login",
  },
  "SESSION_EXPIRED": {
    title: "Session expired",
    message: "Your session has expired. Please sign in again.",
    action: "Sign in",
    actionLink: "/login",
  },

  // =============================================================================
  // GENERIC ERRORS
  // =============================================================================
  "InternalServerError": {
    title: "Something went wrong",
    message: "An unexpected error occurred. Please try again later.",
  },
  "UNKNOWN_ERROR": {
    title: "Error",
    message: "An unexpected error occurred. Please try again.",
  },
};

/**
 * Gets user-friendly error information from a backend error code.
 */
export function getErrorInfo(code: string, fallbackMessage?: string): ErrorInfo {
  const info = errorMessages[code];

  if (info) {
    return info;
  }

  // Return a generic error with the fallback message if provided
  return {
    title: "Error",
    message: fallbackMessage || "An unexpected error occurred. Please try again.",
  };
}

/**
 * Extracts error code from various error formats.
 */
export function extractErrorCode(error: unknown): string {
  if (typeof error === "object" && error !== null) {
    const err = error as Record<string, unknown>;

    // Check for code property
    if (typeof err.code === "string") {
      return err.code;
    }

    // Check for nested error
    if (typeof err.error === "object" && err.error !== null) {
      const nested = err.error as Record<string, unknown>;
      if (typeof nested.code === "string") {
        return nested.code;
      }
    }
  }

  return "UNKNOWN_ERROR";
}

/**
 * Gets the error message from various error formats.
 */
export function extractErrorMessage(error: unknown): string {
  if (typeof error === "object" && error !== null) {
    const err = error as Record<string, unknown>;

    if (typeof err.message === "string") {
      return err.message;
    }
  }

  if (error instanceof Error) {
    return error.message;
  }

  return "An unexpected error occurred.";
}
