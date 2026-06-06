"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { useRouter } from "@/lib/navigation";
import { Loader2, User, Briefcase, BookOpen, Globe } from "lucide-react";
import { Button } from "@/components/ui";
import { cn } from "@/lib/utils";
import { profileService } from "@/services/profile.service";
import { useAuthStore } from "@/stores/auth-store";
import { useTranslatedResearchFields } from "@/hooks";
import type { CreateProfileRequest } from "@/types";
import {
  validatePersonName,
  validateBio,
  validateLocation,
  validateProfessionalRole,
  validateInstitution,
  validateOrcid,
  validateWebsite,
  PERSON_NAME_RULES,
  BIO_RULES,
  LOCATION_RULES,
  PROFESSIONAL_ROLE_RULES,
  INSTITUTION_RULES,
  WEBSITE_RULES,
} from "@/lib/validation";

export default function CompleteProfilePage() {
  const t = useTranslations("completeProfile");
  const tCommon = useTranslations("common");
  const router = useRouter();
  const { user, setProfile } = useAuthStore();
  const { fields: researchFields } = useTranslatedResearchFields();

  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [step, setStep] = useState(1);

  const [formData, setFormData] = useState<CreateProfileRequest>({
    firstName: "",
    lastName: null,
    bio: null,
    location: null,
    professionalRole: null,
    institutionName: null,
    institutionDepartment: null,
    researchField: null,
    orcidId: null,
    website: null,
  });

  // Validation errors by step
  const [step1Errors, setStep1Errors] = useState<{
    firstName?: string;
    lastName?: string;
    location?: string;
    bio?: string;
  }>({});

  const [step2Errors, setStep2Errors] = useState<{
    professionalRole?: string;
    institutionName?: string;
    institutionDepartment?: string;
  }>({});

  const [step3Errors, setStep3Errors] = useState<{
    orcidId?: string;
    website?: string;
  }>({});

  // Validate step 1 fields
  const validateStep1 = (): boolean => {
    const errors: typeof step1Errors = {};

    // Validate person name
    const nameResult = validatePersonName(formData.firstName, formData.lastName || undefined);
    if (!nameResult.valid) {
      errors.firstName = nameResult.errors.firstName;
      errors.lastName = nameResult.errors.lastName;
    }

    // Validate location
    if (formData.location) {
      const locationResult = validateLocation(formData.location);
      if (!locationResult.valid) {
        errors.location = locationResult.error;
      }
    }

    // Validate bio
    if (formData.bio) {
      const bioResult = validateBio(formData.bio);
      if (!bioResult.valid) {
        errors.bio = bioResult.error;
      }
    }

    setStep1Errors(errors);
    return Object.keys(errors).length === 0;
  };

  // Validate step 2 fields
  const validateStep2 = (): boolean => {
    const errors: typeof step2Errors = {};

    // Validate professional role
    if (formData.professionalRole) {
      const roleResult = validateProfessionalRole(formData.professionalRole);
      if (!roleResult.valid) {
        errors.professionalRole = roleResult.error;
      }
    }

    // Validate institution
    const institutionResult = validateInstitution(
      formData.institutionName || undefined,
      formData.institutionDepartment || undefined
    );
    if (!institutionResult.valid) {
      errors.institutionName = institutionResult.errors.name;
      errors.institutionDepartment = institutionResult.errors.department;
    }

    setStep2Errors(errors);
    return Object.keys(errors).length === 0;
  };

  // Validate step 3 fields
  const validateStep3 = (): boolean => {
    const errors: typeof step3Errors = {};

    // Validate ORCID
    if (formData.orcidId) {
      const orcidResult = validateOrcid(formData.orcidId);
      if (!orcidResult.valid) {
        errors.orcidId = orcidResult.error;
      }
    }

    // Validate website
    if (formData.website) {
      const websiteResult = validateWebsite(formData.website);
      if (!websiteResult.valid) {
        errors.website = websiteResult.error;
      }
    }

    setStep3Errors(errors);
    return Object.keys(errors).length === 0;
  };

  // Prevent any form submission via Enter key - only allow explicit button clicks
  const handleFormSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    // If not on the last step, advance to next step
    if (step < 3) {
      handleNext();
    }
    // On step 3, do nothing - require explicit button click
  };

  // Only called when user explicitly clicks the submit button on step 3
  const handleCreateProfile = async () => {
    if (step !== 3) return; // Safety check

    // Validate step 3 before submitting
    if (!validateStep3()) {
      return;
    }

    setError(null);
    setIsSubmitting(true);

    try {
      const profile = await profileService.createProfile(formData);
      // Update the auth store with the new profile so AuthGuard knows it exists
      setProfile(profile);
      router.push("/dashboard");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to create profile");
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleNext = () => {
    setError(null);

    if (step === 1) {
      if (!validateStep1()) {
        return;
      }
    } else if (step === 2) {
      if (!validateStep2()) {
        return;
      }
    }

    setStep(step + 1);
  };

  const handleBack = () => {
    setError(null);
    setStep(step - 1);
  };

  return (
    <div className="flex min-h-[calc(100vh-4rem)] items-center justify-center bg-background p-4">
      <div className="w-full max-w-xl">
        {/* Header */}
        <div className="mb-8 text-center">
          <h1 className="mb-2 text-2xl font-semibold text-foreground">
            {t("title")}
          </h1>
          <p className="text-muted-foreground">
            {t("subtitle")}
          </p>
        </div>

        {/* Progress */}
        <div className="mb-8 flex items-center justify-center gap-2">
          {[1, 2, 3].map((s) => (
            <div
              key={s}
              className={`h-2 w-16 rounded-full transition-colors ${
                s <= step ? "bg-teal" : "bg-muted"
              }`}
            />
          ))}
        </div>

        {/* Form */}
        <form onSubmit={handleFormSubmit} className="rounded-xl border border-border bg-card p-6">
          {error && (
            <div className="mb-6 rounded-lg bg-destructive/10 p-4 text-sm text-destructive">
              {error}
            </div>
          )}

          {/* Step 1: Basic Info */}
          {step === 1 && (
            <div className="space-y-5">
              <div className="mb-6 flex items-center gap-3">
                <div className="rounded-lg bg-teal/10 p-2">
                  <User className="h-5 w-5 text-teal" />
                </div>
                <div>
                  <h2 className="font-medium text-foreground">{t("steps.basicInfo.title")}</h2>
                  <p className="text-sm text-muted-foreground">{t("steps.basicInfo.description")}</p>
                </div>
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-2">
                  <label htmlFor="firstName" className="text-sm font-medium text-foreground">
                    {t("fields.firstName")} <span className="text-destructive">*</span>
                  </label>
                  <input
                    id="firstName"
                    type="text"
                    required
                    maxLength={PERSON_NAME_RULES.FIRST_NAME_MAX_LENGTH}
                    value={formData.firstName}
                    onChange={(e) => {
                      setFormData({ ...formData, firstName: e.target.value });
                      if (step1Errors.firstName) {
                        setStep1Errors({ ...step1Errors, firstName: undefined });
                      }
                    }}
                    className={cn(
                      "w-full rounded-lg border bg-background px-3.5 py-2.5 text-sm text-foreground transition-all focus:outline-none focus:ring-2",
                      step1Errors.firstName
                        ? "border-destructive focus:border-destructive focus:ring-destructive/20"
                        : "border-border focus:border-teal focus:ring-teal/20"
                    )}
                    placeholder={t("placeholders.firstName")}
                  />
                  {step1Errors.firstName && (
                    <p className="text-xs text-destructive">{step1Errors.firstName}</p>
                  )}
                </div>
                <div className="space-y-2">
                  <label htmlFor="lastName" className="text-sm font-medium text-foreground">
                    {t("fields.lastName")}
                  </label>
                  <input
                    id="lastName"
                    type="text"
                    maxLength={PERSON_NAME_RULES.LAST_NAME_MAX_LENGTH}
                    value={formData.lastName || ""}
                    onChange={(e) => {
                      setFormData({ ...formData, lastName: e.target.value || null });
                      if (step1Errors.lastName) {
                        setStep1Errors({ ...step1Errors, lastName: undefined });
                      }
                    }}
                    className={cn(
                      "w-full rounded-lg border bg-background px-3.5 py-2.5 text-sm text-foreground transition-all focus:outline-none focus:ring-2",
                      step1Errors.lastName
                        ? "border-destructive focus:border-destructive focus:ring-destructive/20"
                        : "border-border focus:border-teal focus:ring-teal/20"
                    )}
                    placeholder={t("placeholders.lastName")}
                  />
                  {step1Errors.lastName && (
                    <p className="text-xs text-destructive">{step1Errors.lastName}</p>
                  )}
                </div>
              </div>

              <div className="space-y-2">
                <label htmlFor="location" className="text-sm font-medium text-foreground">
                  {t("fields.location")}
                </label>
                <input
                  id="location"
                  type="text"
                  maxLength={LOCATION_RULES.MAX_LENGTH}
                  value={formData.location || ""}
                  onChange={(e) => {
                    setFormData({ ...formData, location: e.target.value || null });
                    if (step1Errors.location) {
                      setStep1Errors({ ...step1Errors, location: undefined });
                    }
                  }}
                  className={cn(
                    "w-full rounded-lg border bg-background px-3.5 py-2.5 text-sm text-foreground transition-all focus:outline-none focus:ring-2",
                    step1Errors.location
                      ? "border-destructive focus:border-destructive focus:ring-destructive/20"
                      : "border-border focus:border-teal focus:ring-teal/20"
                  )}
                  placeholder={t("placeholders.location")}
                />
                {step1Errors.location && (
                  <p className="text-xs text-destructive">{step1Errors.location}</p>
                )}
                <p className="text-xs text-muted-foreground">
                  {formData.location?.length || 0}/{LOCATION_RULES.MAX_LENGTH}
                </p>
              </div>

              <div className="space-y-2">
                <label htmlFor="bio" className="text-sm font-medium text-foreground">
                  {t("fields.bio")}
                </label>
                <textarea
                  id="bio"
                  rows={3}
                  maxLength={BIO_RULES.MAX_LENGTH}
                  value={formData.bio || ""}
                  onChange={(e) => {
                    setFormData({ ...formData, bio: e.target.value || null });
                    if (step1Errors.bio) {
                      setStep1Errors({ ...step1Errors, bio: undefined });
                    }
                  }}
                  className={cn(
                    "w-full resize-none rounded-lg border bg-background px-3.5 py-2.5 text-sm text-foreground transition-all focus:outline-none focus:ring-2",
                    step1Errors.bio
                      ? "border-destructive focus:border-destructive focus:ring-destructive/20"
                      : "border-border focus:border-teal focus:ring-teal/20"
                  )}
                  placeholder={t("placeholders.bio")}
                />
                {step1Errors.bio && (
                  <p className="text-xs text-destructive">{step1Errors.bio}</p>
                )}
                <p className="text-xs text-muted-foreground">
                  {formData.bio?.length || 0}/{BIO_RULES.MAX_LENGTH}
                </p>
              </div>
            </div>
          )}

          {/* Step 2: Professional Info */}
          {step === 2 && (
            <div className="space-y-5">
              <div className="mb-6 flex items-center gap-3">
                <div className="rounded-lg bg-blue-deep/10 p-2">
                  <Briefcase className="h-5 w-5 text-blue-deep" />
                </div>
                <div>
                  <h2 className="font-medium text-foreground">{t("steps.professionalInfo.title")}</h2>
                  <p className="text-sm text-muted-foreground">{t("steps.professionalInfo.description")}</p>
                </div>
              </div>

              <div className="space-y-2">
                <label htmlFor="professionalRole" className="text-sm font-medium text-foreground">
                  {t("fields.role")}
                </label>
                <input
                  id="professionalRole"
                  type="text"
                  maxLength={PROFESSIONAL_ROLE_RULES.MAX_LENGTH}
                  value={formData.professionalRole || ""}
                  onChange={(e) => {
                    setFormData({ ...formData, professionalRole: e.target.value || null });
                    if (step2Errors.professionalRole) {
                      setStep2Errors({ ...step2Errors, professionalRole: undefined });
                    }
                  }}
                  className={cn(
                    "w-full rounded-lg border bg-background px-3.5 py-2.5 text-sm text-foreground transition-all focus:outline-none focus:ring-2",
                    step2Errors.professionalRole
                      ? "border-destructive focus:border-destructive focus:ring-destructive/20"
                      : "border-border focus:border-teal focus:ring-teal/20"
                  )}
                  placeholder={t("placeholders.role")}
                />
                {step2Errors.professionalRole && (
                  <p className="text-xs text-destructive">{step2Errors.professionalRole}</p>
                )}
                <p className="text-xs text-muted-foreground">
                  {formData.professionalRole?.length || 0}/{PROFESSIONAL_ROLE_RULES.MAX_LENGTH}
                </p>
              </div>

              <div className="space-y-2">
                <label htmlFor="institutionName" className="text-sm font-medium text-foreground">
                  {t("fields.institution")}
                </label>
                <input
                  id="institutionName"
                  type="text"
                  maxLength={INSTITUTION_RULES.NAME_MAX_LENGTH}
                  value={formData.institutionName || ""}
                  onChange={(e) => {
                    setFormData({ ...formData, institutionName: e.target.value || null });
                    if (step2Errors.institutionName) {
                      setStep2Errors({ ...step2Errors, institutionName: undefined });
                    }
                  }}
                  className={cn(
                    "w-full rounded-lg border bg-background px-3.5 py-2.5 text-sm text-foreground transition-all focus:outline-none focus:ring-2",
                    step2Errors.institutionName
                      ? "border-destructive focus:border-destructive focus:ring-destructive/20"
                      : "border-border focus:border-teal focus:ring-teal/20"
                  )}
                  placeholder={t("placeholders.institution")}
                />
                {step2Errors.institutionName && (
                  <p className="text-xs text-destructive">{step2Errors.institutionName}</p>
                )}
              </div>

              <div className="space-y-2">
                <label htmlFor="institutionDepartment" className="text-sm font-medium text-foreground">
                  {t("fields.department")}
                </label>
                <input
                  id="institutionDepartment"
                  type="text"
                  maxLength={INSTITUTION_RULES.DEPARTMENT_MAX_LENGTH}
                  value={formData.institutionDepartment || ""}
                  onChange={(e) => {
                    setFormData({ ...formData, institutionDepartment: e.target.value || null });
                    if (step2Errors.institutionDepartment) {
                      setStep2Errors({ ...step2Errors, institutionDepartment: undefined });
                    }
                  }}
                  className={cn(
                    "w-full rounded-lg border bg-background px-3.5 py-2.5 text-sm text-foreground transition-all focus:outline-none focus:ring-2",
                    step2Errors.institutionDepartment
                      ? "border-destructive focus:border-destructive focus:ring-destructive/20"
                      : "border-border focus:border-teal focus:ring-teal/20"
                  )}
                  placeholder={t("placeholders.department")}
                />
                {step2Errors.institutionDepartment && (
                  <p className="text-xs text-destructive">{step2Errors.institutionDepartment}</p>
                )}
              </div>

              <div className="space-y-2">
                <label htmlFor="researchField" className="text-sm font-medium text-foreground">
                  {t("fields.researchField")}
                </label>
                <select
                  id="researchField"
                  value={formData.researchField || ""}
                  onChange={(e) => setFormData({ ...formData, researchField: e.target.value || null })}
                  className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm text-foreground transition-all focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/20"
                >
                  <option value="">{t("placeholders.researchField")}</option>
                  {researchFields.map((field) => (
                    <option key={field.id} value={field.name}>
                      {field.label}
                    </option>
                  ))}
                </select>
              </div>
            </div>
          )}

          {/* Step 3: Research Identifiers */}
          {step === 3 && (
            <div className="space-y-5">
              <div className="mb-6 flex items-center gap-3">
                <div className="rounded-lg bg-teal/10 p-2">
                  <Globe className="h-5 w-5 text-teal" />
                </div>
                <div>
                  <h2 className="font-medium text-foreground">{t("steps.researchIds.title")}</h2>
                  <p className="text-sm text-muted-foreground">{t("steps.researchIds.description")}</p>
                </div>
              </div>

              <div className="space-y-2">
                <label htmlFor="orcidId" className="text-sm font-medium text-foreground">
                  {t("fields.orcid")}
                </label>
                <input
                  id="orcidId"
                  type="text"
                  maxLength={19}
                  value={formData.orcidId || ""}
                  onChange={(e) => {
                    setFormData({ ...formData, orcidId: e.target.value || null });
                    if (step3Errors.orcidId) {
                      setStep3Errors({ ...step3Errors, orcidId: undefined });
                    }
                  }}
                  className={cn(
                    "w-full rounded-lg border bg-background px-3.5 py-2.5 font-mono text-sm text-foreground transition-all focus:outline-none focus:ring-2",
                    step3Errors.orcidId
                      ? "border-destructive focus:border-destructive focus:ring-destructive/20"
                      : "border-border focus:border-teal focus:ring-teal/20"
                  )}
                  placeholder="0000-0000-0000-0000"
                />
                {step3Errors.orcidId ? (
                  <p className="text-xs text-destructive">{step3Errors.orcidId}</p>
                ) : (
                  <p className="text-xs text-muted-foreground">{t("hints.orcid")}</p>
                )}
              </div>

              <div className="space-y-2">
                <label htmlFor="website" className="text-sm font-medium text-foreground">
                  {t("fields.website")}
                </label>
                <input
                  id="website"
                  type="url"
                  maxLength={WEBSITE_RULES.MAX_LENGTH}
                  value={formData.website || ""}
                  onChange={(e) => {
                    setFormData({ ...formData, website: e.target.value || null });
                    if (step3Errors.website) {
                      setStep3Errors({ ...step3Errors, website: undefined });
                    }
                  }}
                  className={cn(
                    "w-full rounded-lg border bg-background px-3.5 py-2.5 text-sm text-foreground transition-all focus:outline-none focus:ring-2",
                    step3Errors.website
                      ? "border-destructive focus:border-destructive focus:ring-destructive/20"
                      : "border-border focus:border-teal focus:ring-teal/20"
                  )}
                  placeholder="https://example.com"
                />
                {step3Errors.website && (
                  <p className="text-xs text-destructive">{step3Errors.website}</p>
                )}
              </div>
            </div>
          )}

          {/* Navigation */}
          <div className="mt-8 flex items-center justify-between">
            {step > 1 ? (
              <Button type="button" variant="ghost" onClick={handleBack} disabled={isSubmitting}>
                {tCommon("back")}
              </Button>
            ) : (
              <div />
            )}

            {step < 3 ? (
              <Button type="button" onClick={handleNext}>
                {tCommon("next")}
              </Button>
            ) : (
              <Button type="button" onClick={handleCreateProfile} disabled={isSubmitting}>
                {isSubmitting && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                {t("submit")}
              </Button>
            )}
          </div>
        </form>

        {/* Skip option */}
        <p className="mt-4 text-center text-sm text-muted-foreground">
          {t("skipHint")}
        </p>
      </div>
    </div>
  );
}
