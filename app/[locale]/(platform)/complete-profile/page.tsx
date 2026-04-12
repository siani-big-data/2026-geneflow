"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { useRouter } from "@/lib/navigation";
import { Loader2, User, Briefcase, BookOpen, Globe } from "lucide-react";
import { Button } from "@/components/ui";
import { profileService } from "@/services/profile.service";
import { useAuthStore } from "@/stores/auth-store";
import { RESEARCH_FIELDS } from "@/types/profile";
import type { CreateProfileRequest } from "@/types";

export default function CompleteProfilePage() {
  const t = useTranslations("completeProfile");
  const tCommon = useTranslations("common");
  const router = useRouter();
  const { user, setProfile } = useAuthStore();

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
    if (step === 1 && !formData.firstName.trim()) {
      setError("First name is required");
      return;
    }
    setError(null);
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
                    {t("fields.firstName")} *
                  </label>
                  <input
                    id="firstName"
                    type="text"
                    required
                    value={formData.firstName}
                    onChange={(e) => setFormData({ ...formData, firstName: e.target.value })}
                    className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm text-foreground transition-all focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/20"
                    placeholder={t("placeholders.firstName")}
                  />
                </div>
                <div className="space-y-2">
                  <label htmlFor="lastName" className="text-sm font-medium text-foreground">
                    {t("fields.lastName")}
                  </label>
                  <input
                    id="lastName"
                    type="text"
                    value={formData.lastName || ""}
                    onChange={(e) => setFormData({ ...formData, lastName: e.target.value || null })}
                    className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm text-foreground transition-all focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/20"
                    placeholder={t("placeholders.lastName")}
                  />
                </div>
              </div>

              <div className="space-y-2">
                <label htmlFor="location" className="text-sm font-medium text-foreground">
                  {t("fields.location")}
                </label>
                <input
                  id="location"
                  type="text"
                  value={formData.location || ""}
                  onChange={(e) => setFormData({ ...formData, location: e.target.value || null })}
                  className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm text-foreground transition-all focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/20"
                  placeholder={t("placeholders.location")}
                />
              </div>

              <div className="space-y-2">
                <label htmlFor="bio" className="text-sm font-medium text-foreground">
                  {t("fields.bio")}
                </label>
                <textarea
                  id="bio"
                  rows={3}
                  value={formData.bio || ""}
                  onChange={(e) => setFormData({ ...formData, bio: e.target.value || null })}
                  className="w-full resize-none rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm text-foreground transition-all focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/20"
                  placeholder={t("placeholders.bio")}
                />
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
                  value={formData.professionalRole || ""}
                  onChange={(e) => setFormData({ ...formData, professionalRole: e.target.value || null })}
                  className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm text-foreground transition-all focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/20"
                  placeholder={t("placeholders.role")}
                />
              </div>

              <div className="space-y-2">
                <label htmlFor="institutionName" className="text-sm font-medium text-foreground">
                  {t("fields.institution")}
                </label>
                <input
                  id="institutionName"
                  type="text"
                  value={formData.institutionName || ""}
                  onChange={(e) => setFormData({ ...formData, institutionName: e.target.value || null })}
                  className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm text-foreground transition-all focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/20"
                  placeholder={t("placeholders.institution")}
                />
              </div>

              <div className="space-y-2">
                <label htmlFor="institutionDepartment" className="text-sm font-medium text-foreground">
                  {t("fields.department")}
                </label>
                <input
                  id="institutionDepartment"
                  type="text"
                  value={formData.institutionDepartment || ""}
                  onChange={(e) => setFormData({ ...formData, institutionDepartment: e.target.value || null })}
                  className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm text-foreground transition-all focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/20"
                  placeholder={t("placeholders.department")}
                />
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
                  {RESEARCH_FIELDS.map((field) => (
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
                  value={formData.orcidId || ""}
                  onChange={(e) => setFormData({ ...formData, orcidId: e.target.value || null })}
                  className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 font-mono text-sm text-foreground transition-all focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/20"
                  placeholder="0000-0000-0000-0000"
                />
                <p className="text-xs text-muted-foreground">{t("hints.orcid")}</p>
              </div>

              <div className="space-y-2">
                <label htmlFor="website" className="text-sm font-medium text-foreground">
                  {t("fields.website")}
                </label>
                <input
                  id="website"
                  type="url"
                  value={formData.website || ""}
                  onChange={(e) => setFormData({ ...formData, website: e.target.value || null })}
                  className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm text-foreground transition-all focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/20"
                  placeholder="https://"
                />
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
