/**
 * Unified Validation Rules for GeneFlow
 *
 * These rules are synchronized with the backend validation.
 * Any changes here should be reflected in the backend validators.
 *
 * Backend files:
 * - Domain/Identity/ValueObjects/Email.cs
 * - Domain/Identity/ValueObjects/Username.cs
 * - Domain/Studies/ValueObjects/StudyTitle.cs
 * - Domain/Studies/ValueObjects/StudyDescription.cs
 * - Domain/Profiles/ValueObjects/PersonName.cs
 * - Domain/Profiles/ValueObjects/Bio.cs
 * - Domain/Profiles/ValueObjects/ResearchIdentifiers.cs
 * - API/Contracts/{Feature}/Requests/{Request}.cs
 */

// ============================================================================
// IDENTITY / AUTH VALIDATION
// ============================================================================

export const USERNAME_RULES = {
  MIN_LENGTH: 3,
  MAX_LENGTH: 50,
  PATTERN: /^[a-zA-Z0-9]+$/,
  PATTERN_DESCRIPTION: "letters and numbers only",
} as const;

export const EMAIL_RULES = {
  MAX_LENGTH: 256,
  // Same pattern as backend: Domain/Identity/Validators/EmailValidator.cs
  PATTERN: /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/,
} as const;

export const PASSWORD_RULES = {
  MIN_LENGTH: 8,
  MAX_LENGTH: 100,
  // Complexity requirements
  REQUIRE_UPPERCASE: true,
  REQUIRE_LOWERCASE: true,
  REQUIRE_NUMBER: true,
  REQUIRE_SPECIAL: false, // Not required currently
  // Patterns for individual checks
  PATTERNS: {
    UPPERCASE: /[A-Z]/,
    LOWERCASE: /[a-z]/,
    NUMBER: /\d/,
    SPECIAL: /[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]/,
  },
} as const;

export const TWO_FACTOR_CODE_RULES = {
  LENGTH: 6,
  PATTERN: /^\d{6}$/,
} as const;

// ============================================================================
// STUDY VALIDATION
// ============================================================================

export const STUDY_TITLE_RULES = {
  MIN_LENGTH: 5,
  MAX_LENGTH: 200,
} as const;

export const STUDY_DESCRIPTION_RULES = {
  MAX_LENGTH: 5000,
} as const;

export const STUDY_INSTITUTION_RULES = {
  MAX_LENGTH: 200,
} as const;

export const STUDY_PI_RULES = {
  MAX_LENGTH: 200,
} as const;

export const STUDY_TAGS_RULES = {
  MAX_COUNT: 10,
  MAX_TAG_LENGTH: 50,
} as const;

export const STUDY_INVITATION_MESSAGE_RULES = {
  MAX_LENGTH: 500,
} as const;

export const STUDY_PAPER_RULES = {
  TITLE_MAX_LENGTH: 500,
  AUTHORS_MAX_LENGTH: 2000,
  DOI_MAX_LENGTH: 100,
  ABSTRACT_MAX_LENGTH: 5000,
  JOURNAL_MAX_LENGTH: 200,
  YEAR_MIN: 1900,
  YEAR_MAX: 2100,
} as const;

// ============================================================================
// PROFILE VALIDATION
// ============================================================================

export const PERSON_NAME_RULES = {
  FIRST_NAME_MIN_LENGTH: 2,
  FIRST_NAME_MAX_LENGTH: 100,
  LAST_NAME_MIN_LENGTH: 2,
  LAST_NAME_MAX_LENGTH: 100,
  // Allows letters (any language), spaces, hyphens, apostrophes
  PATTERN: /^[\p{L}\s\-']+$/u,
  PATTERN_DESCRIPTION: "letters, spaces, hyphens, and apostrophes only",
} as const;

export const BIO_RULES = {
  MIN_LENGTH: 10,
  MAX_LENGTH: 500,
} as const;

export const LOCATION_RULES = {
  MIN_LENGTH: 2,
  MAX_LENGTH: 200,
} as const;

export const PROFESSIONAL_ROLE_RULES = {
  MIN_LENGTH: 2,
  MAX_LENGTH: 100,
} as const;

export const INSTITUTION_RULES = {
  NAME_MIN_LENGTH: 2,
  NAME_MAX_LENGTH: 200,
  DEPARTMENT_MIN_LENGTH: 2,
  DEPARTMENT_MAX_LENGTH: 200,
} as const;

export const RESEARCH_FIELD_RULES = {
  MAX_LENGTH: 50,
} as const;

export const ORCID_RULES = {
  // Format: 0000-0000-0000-0000 or 0000-0000-0000-000X
  PATTERN: /^\d{4}-\d{4}-\d{4}-\d{3}[\dX]$/,
  FORMAT_HINT: "0000-0000-0000-0000",
} as const;

export const WEBSITE_RULES = {
  MAX_LENGTH: 500,
  // Full URL pattern matching backend
  PATTERN: /^https?:\/\/[\w\-]+(\.[\w\-]+)+(\/[\w\-._~:/?#\[\]@!$&'()*+,;=%]*)?$/,
} as const;

export const PROFILE_PHOTO_RULES = {
  MAX_SIZE_BYTES: 10 * 1024 * 1024, // 10 MB
  MAX_SIZE_MB: 10,
  ALLOWED_TYPES: ["image/jpeg", "image/png", "image/gif", "image/webp"],
  ALLOWED_EXTENSIONS: [".jpg", ".jpeg", ".png", ".gif", ".webp"],
} as const;

// ============================================================================
// TRACE VALIDATION
// ============================================================================

export const TRACE_NAME_RULES = {
  MIN_LENGTH: 3,
  MAX_LENGTH: 100,
} as const;

export const TRACE_DESCRIPTION_RULES = {
  MAX_LENGTH: 500,
} as const;

export const TRACE_FILE_RULES = {
  MAX_SIZE_BYTES: 50 * 1024 * 1024, // 50 MB
  MAX_SIZE_MB: 50,
  ALLOWED_EXTENSIONS: [".ab1", ".scf", ".abi", ".fasta", ".fa", ".fastq", ".fq"],
  ALLOWED_TYPES: [
    "application/octet-stream",
    "text/plain",
    "application/x-fasta",
    "application/x-fastq",
  ],
} as const;

export const TRACE_ANNOTATION_RULES = {
  LABEL_MAX_LENGTH: 100,
  DESCRIPTION_MAX_LENGTH: 500,
  // Hex color pattern: #RGB or #RRGGBB
  COLOR_PATTERN: /^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$/,
} as const;

// ============================================================================
// VALIDATION HELPER FUNCTIONS
// ============================================================================

/**
 * Validates a username against the rules
 */
export function validateUsername(username: string): { valid: boolean; error?: string } {
  const trimmed = username.trim();

  if (trimmed.length < USERNAME_RULES.MIN_LENGTH) {
    return { valid: false, error: `Username must be at least ${USERNAME_RULES.MIN_LENGTH} characters` };
  }

  if (trimmed.length > USERNAME_RULES.MAX_LENGTH) {
    return { valid: false, error: `Username must not exceed ${USERNAME_RULES.MAX_LENGTH} characters` };
  }

  if (!USERNAME_RULES.PATTERN.test(trimmed)) {
    return { valid: false, error: `Username can only contain ${USERNAME_RULES.PATTERN_DESCRIPTION}` };
  }

  return { valid: true };
}

/**
 * Validates an email against the rules
 */
export function validateEmail(email: string): { valid: boolean; error?: string } {
  const trimmed = email.trim();

  if (trimmed.length === 0) {
    return { valid: false, error: "Email is required" };
  }

  if (trimmed.length > EMAIL_RULES.MAX_LENGTH) {
    return { valid: false, error: `Email must not exceed ${EMAIL_RULES.MAX_LENGTH} characters` };
  }

  if (!EMAIL_RULES.PATTERN.test(trimmed)) {
    return { valid: false, error: "Please enter a valid email address" };
  }

  return { valid: true };
}

/**
 * Validates password complexity
 */
export function validatePassword(password: string): {
  valid: boolean;
  requirements: {
    minLength: boolean;
    maxLength: boolean;
    hasUppercase: boolean;
    hasLowercase: boolean;
    hasNumber: boolean;
    hasSpecial?: boolean;
  };
  error?: string;
} {
  const requirements = {
    minLength: password.length >= PASSWORD_RULES.MIN_LENGTH,
    maxLength: password.length <= PASSWORD_RULES.MAX_LENGTH,
    hasUppercase: PASSWORD_RULES.PATTERNS.UPPERCASE.test(password),
    hasLowercase: PASSWORD_RULES.PATTERNS.LOWERCASE.test(password),
    hasNumber: PASSWORD_RULES.PATTERNS.NUMBER.test(password),
    hasSpecial: PASSWORD_RULES.REQUIRE_SPECIAL ? PASSWORD_RULES.PATTERNS.SPECIAL.test(password) : undefined,
  };

  const valid =
    requirements.minLength &&
    requirements.maxLength &&
    (!PASSWORD_RULES.REQUIRE_UPPERCASE || requirements.hasUppercase) &&
    (!PASSWORD_RULES.REQUIRE_LOWERCASE || requirements.hasLowercase) &&
    (!PASSWORD_RULES.REQUIRE_NUMBER || requirements.hasNumber) &&
    (!PASSWORD_RULES.REQUIRE_SPECIAL || requirements.hasSpecial);

  return { valid, requirements };
}

/**
 * Validates a study title against the rules
 */
export function validateStudyTitle(title: string): { valid: boolean; error?: string } {
  const trimmed = title.trim();

  if (trimmed.length < STUDY_TITLE_RULES.MIN_LENGTH) {
    return { valid: false, error: `Study title must be at least ${STUDY_TITLE_RULES.MIN_LENGTH} characters` };
  }

  if (trimmed.length > STUDY_TITLE_RULES.MAX_LENGTH) {
    return { valid: false, error: `Study title must not exceed ${STUDY_TITLE_RULES.MAX_LENGTH} characters` };
  }

  return { valid: true };
}

/**
 * Validates a study description against the rules
 */
export function validateStudyDescription(description: string): { valid: boolean; error?: string } {
  if (description.length > STUDY_DESCRIPTION_RULES.MAX_LENGTH) {
    return { valid: false, error: `Description must not exceed ${STUDY_DESCRIPTION_RULES.MAX_LENGTH} characters` };
  }

  return { valid: true };
}

/**
 * Validates a person's name against the rules
 */
export function validatePersonName(
  firstName: string,
  lastName?: string
): { valid: boolean; errors: { firstName?: string; lastName?: string } } {
  const errors: { firstName?: string; lastName?: string } = {};

  const trimmedFirst = firstName.trim();
  if (trimmedFirst.length === 0) {
    errors.firstName = "First name is required";
  } else if (trimmedFirst.length < PERSON_NAME_RULES.FIRST_NAME_MIN_LENGTH) {
    errors.firstName = `First name must be at least ${PERSON_NAME_RULES.FIRST_NAME_MIN_LENGTH} characters`;
  } else if (trimmedFirst.length > PERSON_NAME_RULES.FIRST_NAME_MAX_LENGTH) {
    errors.firstName = `First name must not exceed ${PERSON_NAME_RULES.FIRST_NAME_MAX_LENGTH} characters`;
  } else if (!PERSON_NAME_RULES.PATTERN.test(trimmedFirst)) {
    errors.firstName = `First name can only contain ${PERSON_NAME_RULES.PATTERN_DESCRIPTION}`;
  }

  if (lastName) {
    const trimmedLast = lastName.trim();
    if (trimmedLast.length > 0 && trimmedLast.length < PERSON_NAME_RULES.LAST_NAME_MIN_LENGTH) {
      errors.lastName = `Last name must be at least ${PERSON_NAME_RULES.LAST_NAME_MIN_LENGTH} characters`;
    } else if (trimmedLast.length > PERSON_NAME_RULES.LAST_NAME_MAX_LENGTH) {
      errors.lastName = `Last name must not exceed ${PERSON_NAME_RULES.LAST_NAME_MAX_LENGTH} characters`;
    } else if (trimmedLast.length > 0 && !PERSON_NAME_RULES.PATTERN.test(trimmedLast)) {
      errors.lastName = `Last name can only contain ${PERSON_NAME_RULES.PATTERN_DESCRIPTION}`;
    }
  }

  return { valid: Object.keys(errors).length === 0, errors };
}

/**
 * Validates a bio against the rules
 */
export function validateBio(bio: string): { valid: boolean; error?: string } {
  if (!bio || bio.trim().length === 0) {
    return { valid: true }; // Optional field
  }

  const trimmed = bio.trim();

  if (trimmed.length < BIO_RULES.MIN_LENGTH) {
    return { valid: false, error: `Bio must be at least ${BIO_RULES.MIN_LENGTH} characters if provided` };
  }

  if (trimmed.length > BIO_RULES.MAX_LENGTH) {
    return { valid: false, error: `Bio must not exceed ${BIO_RULES.MAX_LENGTH} characters` };
  }

  return { valid: true };
}

/**
 * Validates a location against the rules
 */
export function validateLocation(location: string): { valid: boolean; error?: string } {
  if (!location || location.trim().length === 0) {
    return { valid: true }; // Optional field
  }

  const trimmed = location.trim();

  if (trimmed.length < LOCATION_RULES.MIN_LENGTH) {
    return { valid: false, error: `Location must be at least ${LOCATION_RULES.MIN_LENGTH} characters if provided` };
  }

  if (trimmed.length > LOCATION_RULES.MAX_LENGTH) {
    return { valid: false, error: `Location must not exceed ${LOCATION_RULES.MAX_LENGTH} characters` };
  }

  return { valid: true };
}

/**
 * Validates a professional role against the rules
 */
export function validateProfessionalRole(role: string): { valid: boolean; error?: string } {
  if (!role || role.trim().length === 0) {
    return { valid: true }; // Optional field
  }

  const trimmed = role.trim();

  if (trimmed.length < PROFESSIONAL_ROLE_RULES.MIN_LENGTH) {
    return { valid: false, error: `Professional role must be at least ${PROFESSIONAL_ROLE_RULES.MIN_LENGTH} characters if provided` };
  }

  if (trimmed.length > PROFESSIONAL_ROLE_RULES.MAX_LENGTH) {
    return { valid: false, error: `Professional role must not exceed ${PROFESSIONAL_ROLE_RULES.MAX_LENGTH} characters` };
  }

  return { valid: true };
}

/**
 * Validates institution information against the rules
 */
export function validateInstitution(
  name?: string,
  department?: string
): { valid: boolean; errors: { name?: string; department?: string } } {
  const errors: { name?: string; department?: string } = {};

  if (name && name.trim().length > 0) {
    const trimmedName = name.trim();
    if (trimmedName.length < INSTITUTION_RULES.NAME_MIN_LENGTH) {
      errors.name = `Institution name must be at least ${INSTITUTION_RULES.NAME_MIN_LENGTH} characters`;
    } else if (trimmedName.length > INSTITUTION_RULES.NAME_MAX_LENGTH) {
      errors.name = `Institution name must not exceed ${INSTITUTION_RULES.NAME_MAX_LENGTH} characters`;
    }
  }

  if (department && department.trim().length > 0) {
    const trimmedDept = department.trim();
    if (trimmedDept.length < INSTITUTION_RULES.DEPARTMENT_MIN_LENGTH) {
      errors.department = `Department must be at least ${INSTITUTION_RULES.DEPARTMENT_MIN_LENGTH} characters`;
    } else if (trimmedDept.length > INSTITUTION_RULES.DEPARTMENT_MAX_LENGTH) {
      errors.department = `Department must not exceed ${INSTITUTION_RULES.DEPARTMENT_MAX_LENGTH} characters`;
    }
  }

  return { valid: Object.keys(errors).length === 0, errors };
}

/**
 * Validates a profile photo file
 */
export function validateProfilePhoto(file: File): { valid: boolean; error?: string } {
  // Check file size
  if (file.size > PROFILE_PHOTO_RULES.MAX_SIZE_BYTES) {
    return { valid: false, error: `Photo size must not exceed ${PROFILE_PHOTO_RULES.MAX_SIZE_MB}MB` };
  }

  // Check file type
  if (!(PROFILE_PHOTO_RULES.ALLOWED_TYPES as readonly string[]).includes(file.type)) {
    return { valid: false, error: `Photo format not supported. Allowed: ${PROFILE_PHOTO_RULES.ALLOWED_EXTENSIONS.join(", ")}` };
  }

  return { valid: true };
}

/**
 * Validates an ORCID ID
 */
export function validateOrcid(orcid: string): { valid: boolean; error?: string } {
  if (!orcid || orcid.trim().length === 0) {
    return { valid: true }; // Optional field
  }

  if (!ORCID_RULES.PATTERN.test(orcid.trim())) {
    return { valid: false, error: `ORCID must be in format ${ORCID_RULES.FORMAT_HINT}` };
  }

  return { valid: true };
}

/**
 * Validates a website URL
 */
export function validateWebsite(url: string): { valid: boolean; error?: string } {
  if (!url || url.trim().length === 0) {
    return { valid: true }; // Optional field
  }

  const trimmed = url.trim();

  if (trimmed.length > WEBSITE_RULES.MAX_LENGTH) {
    return { valid: false, error: `URL must not exceed ${WEBSITE_RULES.MAX_LENGTH} characters` };
  }

  if (!WEBSITE_RULES.PATTERN.test(trimmed)) {
    return { valid: false, error: "Please enter a valid URL (https://example.com)" };
  }

  return { valid: true };
}

/**
 * Validates a two-factor authentication code
 */
export function validateTwoFactorCode(code: string): { valid: boolean; error?: string } {
  if (!TWO_FACTOR_CODE_RULES.PATTERN.test(code)) {
    return { valid: false, error: `Code must be exactly ${TWO_FACTOR_CODE_RULES.LENGTH} digits` };
  }

  return { valid: true };
}

/**
 * Validates a trace name
 */
export function validateTraceName(name: string): { valid: boolean; error?: string } {
  const trimmed = name.trim();

  if (trimmed.length < TRACE_NAME_RULES.MIN_LENGTH) {
    return { valid: false, error: `Trace name must be at least ${TRACE_NAME_RULES.MIN_LENGTH} characters` };
  }

  if (trimmed.length > TRACE_NAME_RULES.MAX_LENGTH) {
    return { valid: false, error: `Trace name must not exceed ${TRACE_NAME_RULES.MAX_LENGTH} characters` };
  }

  return { valid: true };
}

/**
 * Validates a hex color code
 */
export function validateHexColor(color: string): { valid: boolean; error?: string } {
  if (!TRACE_ANNOTATION_RULES.COLOR_PATTERN.test(color)) {
    return { valid: false, error: "Please enter a valid hex color (e.g., #FF5733)" };
  }

  return { valid: true };
}

/**
 * Validates file size
 */
export function validateFileSize(
  sizeBytes: number,
  maxSizeBytes: number
): { valid: boolean; error?: string } {
  if (sizeBytes > maxSizeBytes) {
    const maxMB = Math.round(maxSizeBytes / (1024 * 1024));
    return { valid: false, error: `File size must be less than ${maxMB}MB` };
  }

  return { valid: true };
}

/**
 * Validates file extension
 */
export function validateFileExtension(
  filename: string,
  allowedExtensions: readonly string[]
): { valid: boolean; error?: string } {
  const ext = filename.toLowerCase().substring(filename.lastIndexOf("."));

  if (!allowedExtensions.includes(ext)) {
    return {
      valid: false,
      error: `File type not supported. Allowed: ${allowedExtensions.join(", ")}`,
    };
  }

  return { valid: true };
}
