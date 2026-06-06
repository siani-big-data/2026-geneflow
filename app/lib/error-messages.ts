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
  // PASSWORD COMPLEXITY ERRORS
  // =============================================================================
  "User.PasswordTooShort": {
    title: "Password too short",
    message: "Password must be at least 8 characters long.",
  },
  "User.PasswordTooLong": {
    title: "Password too long",
    message: "Password must not exceed 100 characters.",
  },
  "User.PasswordRequiresUppercase": {
    title: "Password needs uppercase",
    message: "Password must contain at least one uppercase letter.",
  },
  "User.PasswordRequiresLowercase": {
    title: "Password needs lowercase",
    message: "Password must contain at least one lowercase letter.",
  },
  "User.PasswordRequiresDigit": {
    title: "Password needs number",
    message: "Password must contain at least one number.",
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
  "User.TwoFactorSetupExpired": {
    title: "Setup expired",
    message: "Two-factor setup has expired. Please start the setup process again.",
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
  // OAUTH ERRORS
  // =============================================================================
  "OAuth.InvalidToken": {
    title: "Authentication failed",
    message: "The authentication token is invalid or expired. Please try again.",
  },
  "OAuth.TokenValidationFailed": {
    title: "Authentication failed",
    message: "Could not verify your account with the provider. Please try again.",
  },
  "OAuth.EmailNotProvided": {
    title: "Email required",
    message: "Your account does not have an email address. Please use email/password login.",
  },
  "OAuth.ProviderNotSupported": {
    title: "Provider not supported",
    message: "This authentication provider is not supported.",
  },
  "OAuth.AccountAlreadyLinked": {
    title: "Already linked",
    message: "This external account is already linked to another user.",
  },
  "OAuth.ProviderAlreadyLinked": {
    title: "Provider already linked",
    message: "You already have an account linked with this provider.",
  },
  "OAuth.CannotUnlinkLastLogin": {
    title: "Cannot unlink",
    message: "You cannot unlink your only login method. Set a password first or link another provider.",
  },
  "OAuth.ProviderNotLinked": {
    title: "Not linked",
    message: "This provider is not linked to your account.",
  },

  // =============================================================================
  // PROFILE ERRORS
  // =============================================================================
  "Profile.NotFound": {
    title: "Profile not found",
    message: "The profile you're looking for doesn't exist.",
  },
  "Profile.AlreadyExists": {
    title: "Profile exists",
    message: "You already have a profile. You can update it instead.",
  },
  "Profile.FirstNameRequired": {
    title: "First name required",
    message: "Please enter your first name.",
  },
  "Profile.FirstNameTooShort": {
    title: "First name too short",
    message: "First name must be at least 1 character.",
  },
  "Profile.FirstNameTooLong": {
    title: "First name too long",
    message: "First name cannot exceed 100 characters.",
  },
  "Profile.FirstNameInvalidFormat": {
    title: "Invalid first name",
    message: "First name can only contain letters, spaces, hyphens, and apostrophes.",
  },
  "Profile.LastNameTooShort": {
    title: "Last name too short",
    message: "Last name must be at least 2 characters.",
  },
  "Profile.LastNameTooLong": {
    title: "Last name too long",
    message: "Last name cannot exceed 100 characters.",
  },
  "Profile.LastNameInvalidFormat": {
    title: "Invalid last name",
    message: "Last name can only contain letters, spaces, hyphens, and apostrophes.",
  },
  "Profile.BioTooShort": {
    title: "Bio too short",
    message: "Bio must be at least 10 characters if provided.",
  },
  "Profile.BioTooLong": {
    title: "Bio too long",
    message: "Bio cannot exceed 500 characters.",
  },
  "Profile.LocationTooShort": {
    title: "Location too short",
    message: "Location must be at least 2 characters if provided.",
  },
  "Profile.LocationTooLong": {
    title: "Location too long",
    message: "Location cannot exceed 200 characters.",
  },
  "Profile.ProfessionalRoleTooShort": {
    title: "Role too short",
    message: "Professional role must be at least 2 characters if provided.",
  },
  "Profile.ProfessionalRoleTooLong": {
    title: "Role too long",
    message: "Professional role cannot exceed 100 characters.",
  },
  "Profile.InstitutionNameTooShort": {
    title: "Institution name too short",
    message: "Institution name must be at least 2 characters if provided.",
  },
  "Profile.InstitutionNameTooLong": {
    title: "Institution name too long",
    message: "Institution name cannot exceed 200 characters.",
  },
  "Profile.InstitutionDepartmentTooShort": {
    title: "Department too short",
    message: "Department must be at least 2 characters if provided.",
  },
  "Profile.InstitutionDepartmentTooLong": {
    title: "Department too long",
    message: "Department name cannot exceed 200 characters.",
  },
  "Profile.OrcidIdInvalidFormat": {
    title: "Invalid ORCID",
    message: "ORCID must be in format 0000-0000-0000-0000.",
  },
  "Profile.WebsiteInvalidFormat": {
    title: "Invalid website",
    message: "Please enter a valid URL (e.g., https://example.com).",
  },
  "Profile.WebsiteTooLong": {
    title: "URL too long",
    message: "Website URL cannot exceed 500 characters.",
  },
  "Profile.PhotoUrlInvalidFormat": {
    title: "Invalid photo URL",
    message: "Photo URL must be a valid URL.",
  },
  "Profile.PhotoUrlTooLong": {
    title: "Photo URL too long",
    message: "Photo URL cannot exceed the maximum length.",
  },
  "Profile.PhotoSizeExceedsLimit": {
    title: "Photo too large",
    message: "Photo size cannot exceed 10 MB.",
  },
  "Profile.InvalidResearchField": {
    title: "Invalid research field",
    message: "Please select a valid research field.",
  },
  "Profile.InvalidPhotoFormat": {
    title: "Unsupported format",
    message: "Photo format not supported. Use JPEG, PNG, GIF, or WebP.",
  },
  "Profile.PhotoTooLarge": {
    title: "Photo too large",
    message: "Photo size cannot exceed 10 MB.",
  },
  "Profile.InvalidPhotoData": {
    title: "Invalid photo",
    message: "The photo data is invalid or corrupted. Please try another image.",
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
