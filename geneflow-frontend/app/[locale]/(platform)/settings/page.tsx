"use client";

import { useState, useRef, useEffect, useCallback } from "react";
import { useTranslations } from "next-intl";
import { Link, useRouter } from "@/lib/navigation";
import { PageHeader } from "@/components/layout";
import { Button } from "@/components/ui";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  User,
  Shield,
  CreditCard,
  Save,
  Camera,
  Mail,
  Lock,
  Globe,
  ArrowRight,
  Upload,
  AlertTriangle,
  Loader2,
} from "lucide-react";
import { cn } from "@/lib/utils";
import { TwoFactorSetup, ExternalLogins } from "@/components/auth";
import { profileService } from "@/services/profile.service";
import { subscriptionService, planService, paymentService, authService } from "@/services";
import { useAuthStore } from "@/stores/auth-store";
import { useTranslatedResearchFields } from "@/hooks";
import type { Profile, UpdateProfileRequest, UpdateResearchIdentifiersRequest, Subscription, Plan, PaymentMethod } from "@/types";

type SettingsSection = "account" | "security" | "billing";

const sections = [
  { id: "account" as const, nameKey: "account", icon: User },
  { id: "security" as const, nameKey: "security", icon: Shield },
  { id: "billing" as const, nameKey: "billing", icon: CreditCard },
];

export default function SettingsPage() {
  const t = useTranslations("settings");
  const tCommon = useTranslations("common");
  const router = useRouter();
  const { user, logout } = useAuthStore();
  const { fields: researchFields } = useTranslatedResearchFields();

  // Profile state
  const [profile, setProfile] = useState<Profile | null>(null);
  const [isLoadingProfile, setIsLoadingProfile] = useState(true);
  const [isSavingProfile, setIsSavingProfile] = useState(false);
  const [profileSaveSuccess, setProfileSaveSuccess] = useState(false);
  const [isSavingIdentifiers, setIsSavingIdentifiers] = useState(false);
  const [identifiersSaveSuccess, setIdentifiersSaveSuccess] = useState(false);

  // Subscription state
  const [subscription, setSubscription] = useState<Subscription | null>(null);
  const [currentPlan, setCurrentPlan] = useState<Plan | null>(null);
  const [defaultPaymentMethod, setDefaultPaymentMethod] = useState<PaymentMethod | null>(null);

  // Form data
  const [formData, setFormData] = useState<UpdateProfileRequest>({
    firstName: "",
    lastName: null,
    bio: null,
    location: null,
    professionalRole: null,
    institutionName: null,
    institutionDepartment: null,
    researchField: null,
  });

  const [identifiersData, setIdentifiersData] = useState<UpdateResearchIdentifiersRequest>({
    orcidId: null,
    website: null,
  });

  const [activeSection, setActiveSection] = useState<SettingsSection>("account");
  const [twoFactorEnabled, setTwoFactorEnabled] = useState(false);

  // Password change state
  const [passwordData, setPasswordData] = useState({
    currentPassword: "",
    newPassword: "",
    confirmPassword: "",
  });
  const [isChangingPassword, setIsChangingPassword] = useState(false);
  const [passwordError, setPasswordError] = useState<string | null>(null);
  const [passwordSuccess, setPasswordSuccess] = useState(false);

  // Modal states
  const [uploadPhotoOpen, setUploadPhotoOpen] = useState(false);
  const [deactivateOpen, setDeactivateOpen] = useState(false);
  const [deleteAccountOpen, setDeleteAccountOpen] = useState(false);

  const fileInputRef = useRef<HTMLInputElement>(null);
  const [selectedPhoto, setSelectedPhoto] = useState<File | null>(null);
  const [isUploadingPhoto, setIsUploadingPhoto] = useState(false);
  const [isRemovingPhoto, setIsRemovingPhoto] = useState(false);
  const [photoError, setPhotoError] = useState<string | null>(null);

  // Account management state
  const [isDeactivating, setIsDeactivating] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);
  const [deleteConfirmText, setDeleteConfirmText] = useState("");
  const [deleteError, setDeleteError] = useState<string | null>(null);

  // Fetch profile data
  const fetchProfile = useCallback(async () => {
    try {
      setIsLoadingProfile(true);
      const profileData = await profileService.getCurrentProfile();
      setProfile(profileData);

      // Initialize form data
      setFormData({
        firstName: profileData.firstName,
        lastName: profileData.lastName,
        bio: profileData.bio,
        location: profileData.location,
        professionalRole: profileData.professionalRole,
        institutionName: profileData.institutionName,
        institutionDepartment: profileData.institutionDepartment,
        researchField: profileData.researchField,
      });

      setIdentifiersData({
        orcidId: profileData.orcidId,
        website: profileData.website,
      });
    } catch (err) {
      console.error("Failed to load profile:", err);
    } finally {
      setIsLoadingProfile(false);
    }
  }, []);

  // Fetch billing data
  const fetchBillingData = useCallback(async () => {
    try {
      const [subscriptionData, plansData, paymentMethodData] = await Promise.all([
        subscriptionService.getCurrent(),
        planService.getAll(),
        paymentService.getDefault(),
      ]);

      setSubscription(subscriptionData);
      setDefaultPaymentMethod(paymentMethodData);

      if (subscriptionData) {
        const plan = plansData.find((p) => p.id === subscriptionData.planId);
        setCurrentPlan(plan || null);
      }
    } catch (err) {
      console.error("Failed to load billing data:", err);
    }
  }, []);

  useEffect(() => {
    fetchProfile();
    fetchBillingData();
  }, [fetchProfile, fetchBillingData]);

  const handleSaveChanges = async () => {
    try {
      setIsSavingProfile(true);
      const updatedProfile = await profileService.updateProfile(formData);
      setProfile(updatedProfile);
      setProfileSaveSuccess(true);
      setTimeout(() => setProfileSaveSuccess(false), 2000);
    } catch (err) {
      console.error("Failed to save profile:", err);
    } finally {
      setIsSavingProfile(false);
    }
  };

  const handleSaveIdentifiers = async () => {
    try {
      setIsSavingIdentifiers(true);
      const updatedProfile = await profileService.updateResearchIdentifiers(identifiersData);
      setProfile(updatedProfile);
      setIdentifiersSaveSuccess(true);
      setTimeout(() => setIdentifiersSaveSuccess(false), 2000);
    } catch (err) {
      console.error("Failed to save identifiers:", err);
    } finally {
      setIsSavingIdentifiers(false);
    }
  };

  const handlePhotoSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) {
      setSelectedPhoto(file);
    }
  };

  const handleUploadPhoto = async () => {
    if (!selectedPhoto) return;

    try {
      setIsUploadingPhoto(true);
      setPhotoError(null);
      const updatedProfile = await profileService.uploadProfilePhoto(selectedPhoto);
      setProfile(updatedProfile);
      setUploadPhotoOpen(false);
      setSelectedPhoto(null);
    } catch (err: unknown) {
      const error = err as { message?: string };
      setPhotoError(error.message || t("account.photoUploadError"));
    } finally {
      setIsUploadingPhoto(false);
    }
  };

  const handleRemovePhoto = async () => {
    try {
      setIsRemovingPhoto(true);
      await profileService.deleteProfilePhoto();
      setProfile(prev => prev ? { ...prev, photoUrl: null, photoThumbnailUrl: null } : null);
    } catch (err) {
      console.error("Failed to remove photo:", err);
    } finally {
      setIsRemovingPhoto(false);
    }
  };

  const handleChangePassword = async () => {
    setPasswordError(null);
    setPasswordSuccess(false);

    // Validate passwords match
    if (passwordData.newPassword !== passwordData.confirmPassword) {
      setPasswordError(t("security.passwordsDoNotMatch"));
      return;
    }

    // Validate password length
    if (passwordData.newPassword.length < 8) {
      setPasswordError(t("security.passwordTooShort"));
      return;
    }

    try {
      setIsChangingPassword(true);
      await authService.changePassword({
        currentPassword: passwordData.currentPassword,
        newPassword: passwordData.newPassword,
      });
      setPasswordSuccess(true);
      setPasswordData({ currentPassword: "", newPassword: "", confirmPassword: "" });
      setTimeout(() => setPasswordSuccess(false), 3000);
    } catch (err: unknown) {
      const error = err as { message?: string };
      setPasswordError(error.message || t("security.changePasswordError"));
    } finally {
      setIsChangingPassword(false);
    }
  };

  const handleCancelPasswordChange = () => {
    setPasswordData({ currentPassword: "", newPassword: "", confirmPassword: "" });
    setPasswordError(null);
  };

  const handleDeactivateAccount = async () => {
    try {
      setIsDeactivating(true);
      await authService.deactivateAccount();
      await logout();
      router.push("/login");
    } catch (err) {
      console.error("Failed to deactivate account:", err);
    } finally {
      setIsDeactivating(false);
      setDeactivateOpen(false);
    }
  };

  const handleDeleteAccount = async () => {
    if (deleteConfirmText !== "DELETE") {
      setDeleteError(t("dialogs.deleteAccount.confirmError"));
      return;
    }

    try {
      setIsDeleting(true);
      setDeleteError(null);
      await authService.deleteAccount(deleteConfirmText);
      await logout();
      router.push("/login");
    } catch (err: unknown) {
      const error = err as { message?: string };
      setDeleteError(error.message || t("dialogs.deleteAccount.error"));
    } finally {
      setIsDeleting(false);
    }
  };

  return (
    <div className="space-y-6">
      <PageHeader
        title={t("title")}
        description={t("description")}
      />

      <div className="flex gap-6">
        {/* Sidebar Navigation */}
        <aside className="w-56 flex-shrink-0">
          <nav className="space-y-1">
            {sections.map((section) => {
              const Icon = section.icon;
              const isActive = activeSection === section.id;
              return (
                <button
                  key={section.id}
                  onClick={() => setActiveSection(section.id)}
                  className={cn(
                    "flex w-full items-center gap-3 rounded-lg px-4 py-2.5 text-sm font-medium transition-all",
                    isActive
                      ? "bg-gradient-to-r from-teal/10 to-blue-deep/5 text-teal shadow-sm"
                      : "text-muted-foreground hover:bg-muted/50 hover:text-foreground"
                  )}
                >
                  <Icon className="h-4.5 w-4.5" />
                  {t(`sections.${section.nameKey}`)}
                </button>
              );
            })}
          </nav>
        </aside>

        {/* Main Content */}
        <div className="min-w-0 flex-1 space-y-6">
          {/* Account Section */}
          {activeSection === "account" && (
            <>
              {isLoadingProfile ? (
                <div className="flex min-h-[200px] items-center justify-center">
                  <Loader2 className="h-8 w-8 animate-spin text-teal" />
                </div>
              ) : (
                <>
                  <div className="rounded-xl border border-border bg-card p-6">
                    <h2 className="mb-5 text-lg font-semibold text-foreground">{t("account.profileInfo")}</h2>

                    <div className="mb-6 flex items-start gap-6 border-b border-border pb-6">
                      <div className="group relative">
                        {profile?.photoUrl ? (
                          <img
                            src={profileService.resolveStorageUrl(profile.photoThumbnailUrl || profile.photoUrl) || undefined}
                            alt={profile.fullName}
                            className="h-20 w-20 rounded-xl object-cover shadow-sm"
                          />
                        ) : (
                          <div className="flex h-20 w-20 items-center justify-center rounded-xl bg-gradient-to-br from-teal to-blue-deep text-2xl font-semibold text-white shadow-sm">
                            {profile?.initials || "??"}
                          </div>
                        )}
                        <button
                          className="absolute bottom-0 right-0 rounded-lg border border-border bg-background p-1.5 opacity-0 shadow-md transition-all hover:bg-muted group-hover:opacity-100"
                          aria-label="Change profile photo"
                          onClick={() => setUploadPhotoOpen(true)}
                        >
                          <Camera className="h-3.5 w-3.5 text-foreground" />
                        </button>
                      </div>
                      <div className="flex-1">
                        <h3 className="mb-1 text-base font-medium text-foreground">{t("account.profilePhoto")}</h3>
                        <p className="mb-3 text-sm text-muted-foreground">{t("account.profilePhotoDesc")}</p>
                        <div className="flex gap-2">
                          <button
                            onClick={() => setUploadPhotoOpen(true)}
                            className="rounded-lg border border-border px-3 py-1.5 text-xs font-medium transition-all hover:bg-muted/50"
                          >
                            {t("account.uploadNew")}
                          </button>
                          {profile?.photoUrl && (
                            <button
                              onClick={handleRemovePhoto}
                              disabled={isRemovingPhoto}
                              className="flex items-center gap-1 px-3 py-1.5 text-xs font-medium text-muted-foreground transition-all hover:text-foreground disabled:opacity-50"
                            >
                              {isRemovingPhoto && <Loader2 className="h-3 w-3 animate-spin" />}
                              {t("account.remove")}
                            </button>
                          )}
                        </div>
                      </div>
                    </div>

                    <div className="space-y-4">
                      <div className="grid grid-cols-2 gap-4">
                        <div className="space-y-2">
                          <label htmlFor="first-name" className="text-sm font-medium text-foreground">
                            {t("account.firstName")}
                          </label>
                          <input
                            id="first-name"
                            type="text"
                            value={formData.firstName}
                            onChange={(e) => setFormData({ ...formData, firstName: e.target.value })}
                            className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm text-foreground transition-all focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/20"
                          />
                        </div>
                        <div className="space-y-2">
                          <label htmlFor="last-name" className="text-sm font-medium text-foreground">
                            {t("account.lastName")}
                          </label>
                          <input
                            id="last-name"
                            type="text"
                            value={formData.lastName || ""}
                            onChange={(e) => setFormData({ ...formData, lastName: e.target.value || null })}
                            className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm text-foreground transition-all focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/20"
                          />
                        </div>
                      </div>

                      <div className="space-y-2">
                        <label htmlFor="email" className="text-sm font-medium text-foreground">
                          {t("account.emailAddress")}
                        </label>
                        <div className="relative">
                          <Mail className="absolute left-3.5 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                          <input
                            id="email"
                            type="email"
                            value={user?.email || ""}
                            disabled
                            className="w-full rounded-lg border border-border bg-muted/30 py-2.5 pl-10 pr-3.5 text-sm text-muted-foreground transition-all"
                          />
                        </div>
                        <p className="text-xs text-muted-foreground">{t("account.emailReadOnly")}</p>
                      </div>

                      <div className="grid grid-cols-2 gap-4">
                        <div className="space-y-2">
                          <label htmlFor="role" className="text-sm font-medium text-foreground">
                            {t("account.role")}
                          </label>
                          <input
                            id="role"
                            type="text"
                            value={formData.professionalRole || ""}
                            onChange={(e) => setFormData({ ...formData, professionalRole: e.target.value || null })}
                            className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm text-foreground transition-all focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/20"
                          />
                        </div>
                        <div className="space-y-2">
                          <label htmlFor="institution" className="text-sm font-medium text-foreground">
                            {t("account.institution")}
                          </label>
                          <input
                            id="institution"
                            type="text"
                            value={formData.institutionName || ""}
                            onChange={(e) => setFormData({ ...formData, institutionName: e.target.value || null })}
                            className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm text-foreground transition-all focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/20"
                          />
                        </div>
                      </div>

                      <div className="space-y-2">
                        <label htmlFor="research-field" className="text-sm font-medium text-foreground">
                          {t("account.researchField")}
                        </label>
                        <select
                          id="research-field"
                          value={formData.researchField || ""}
                          onChange={(e) => setFormData({ ...formData, researchField: e.target.value || null })}
                          className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm text-foreground transition-all focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/20"
                        >
                          <option value="">{t("account.selectField")}</option>
                          {researchFields.map((field) => (
                            <option key={field.id} value={field.name}>
                              {field.label}
                            </option>
                          ))}
                        </select>
                      </div>

                      <div className="space-y-2">
                        <label htmlFor="bio" className="text-sm font-medium text-foreground">
                          {t("account.bio")}
                        </label>
                        <textarea
                          id="bio"
                          rows={4}
                          value={formData.bio || ""}
                          onChange={(e) => setFormData({ ...formData, bio: e.target.value || null })}
                          className="w-full resize-none rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm text-foreground transition-all focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/20"
                        />
                      </div>
                    </div>

                    <div className="mt-6 flex items-center justify-end gap-3 border-t border-border pt-6">
                      <Button variant="ghost" onClick={fetchProfile} disabled={isSavingProfile}>
                        {tCommon("cancel")}
                      </Button>
                      <Button onClick={handleSaveChanges} disabled={isSavingProfile}>
                        {isSavingProfile ? (
                          <Loader2 className="h-4 w-4 animate-spin" />
                        ) : (
                          <Save className="h-4 w-4" />
                        )}
                        {profileSaveSuccess ? t("account.saved") : t("account.saveChanges")}
                      </Button>
                    </div>
                  </div>

                  <div className="rounded-xl border border-border bg-card p-6">
                    <h2 className="mb-4 text-lg font-semibold text-foreground">{t("account.researchIdentifiers")}</h2>
                    <div className="space-y-4">
                      <div className="space-y-2">
                        <label htmlFor="orcid" className="text-sm font-medium text-foreground">
                          {t("account.orcid")}
                        </label>
                        <input
                          id="orcid"
                          type="text"
                          placeholder="0000-0000-0000-0000"
                          value={identifiersData.orcidId || ""}
                          onChange={(e) => setIdentifiersData({ ...identifiersData, orcidId: e.target.value || null })}
                          className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 font-mono text-sm text-foreground transition-all focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/20"
                        />
                      </div>
                      <div className="space-y-2">
                        <label htmlFor="website" className="text-sm font-medium text-foreground">
                          {t("account.website")}
                        </label>
                        <div className="relative">
                          <Globe className="absolute left-3.5 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                          <input
                            id="website"
                            type="url"
                            placeholder="https://"
                            value={identifiersData.website || ""}
                            onChange={(e) => setIdentifiersData({ ...identifiersData, website: e.target.value || null })}
                            className="w-full rounded-lg border border-border bg-background py-2.5 pl-10 pr-3.5 text-sm text-foreground transition-all focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/20"
                          />
                        </div>
                      </div>
                    </div>
                    <div className="mt-6 flex items-center justify-end border-t border-border pt-6">
                      <Button onClick={handleSaveIdentifiers} disabled={isSavingIdentifiers}>
                        {isSavingIdentifiers ? (
                          <Loader2 className="h-4 w-4 animate-spin" />
                        ) : (
                          <Save className="h-4 w-4" />
                        )}
                        {identifiersSaveSuccess ? t("account.saved") : t("account.saveChanges")}
                      </Button>
                    </div>
                  </div>
                </>
              )}

              {/* Danger Zone */}
              <div className="rounded-xl border border-red-500/30 bg-card p-6">
                <h2 className="mb-4 text-lg font-semibold text-red-500">{t("account.dangerZone")}</h2>
                <div className="space-y-4">
                  <div className="flex items-start justify-between border-b border-border pb-4">
                    <div className="flex-1">
                      <h3 className="mb-1 text-sm font-medium text-foreground">{t("account.deactivateAccount")}</h3>
                      <p className="text-sm text-muted-foreground">
                        {t("account.deactivateDesc")}
                      </p>
                    </div>
                    <button
                      onClick={() => setDeactivateOpen(true)}
                      className="rounded-lg border border-red-500 px-4 py-2 text-sm font-medium text-red-500 transition-all hover:bg-red-500/10"
                    >
                      {t("account.deactivate")}
                    </button>
                  </div>
                  <div className="flex items-start justify-between">
                    <div className="flex-1">
                      <h3 className="mb-1 text-sm font-medium text-foreground">{t("account.deleteAccount")}</h3>
                      <p className="text-sm text-muted-foreground">
                        {t("account.deleteDesc")}
                      </p>
                    </div>
                    <button
                      onClick={() => setDeleteAccountOpen(true)}
                      className="rounded-lg bg-red-500 px-4 py-2 text-sm font-medium text-white transition-all hover:bg-red-500/90"
                    >
                      {t("account.deleteAccount")}
                    </button>
                  </div>
                </div>
              </div>
            </>
          )}

          {/* Security Section */}
          {activeSection === "security" && (
            <>
              {/* Password Change - Only show for users with password (not OAuth-only) */}
              {user?.hasPassword && (
                <div className="rounded-xl border border-border bg-card p-6">
                  <h2 className="mb-5 text-lg font-semibold text-foreground">{t("security.password")}</h2>
                  <div className="space-y-4">
                    {passwordError && (
                      <div className="rounded-lg border border-red-500/30 bg-red-500/10 p-3 text-sm text-red-500">
                        {passwordError}
                      </div>
                    )}
                    {passwordSuccess && (
                      <div className="rounded-lg border border-emerald-500/30 bg-emerald-500/10 p-3 text-sm text-emerald-500">
                        {t("security.passwordChanged")}
                      </div>
                    )}
                    <div className="space-y-2">
                      <label htmlFor="current-password" className="text-sm font-medium text-foreground">
                        {t("security.currentPassword")}
                      </label>
                      <div className="relative">
                        <Lock className="absolute left-3.5 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                        <input
                          id="current-password"
                          type="password"
                          value={passwordData.currentPassword}
                          onChange={(e) => setPasswordData({ ...passwordData, currentPassword: e.target.value })}
                          placeholder="••••••••"
                          className="w-full rounded-lg border border-border bg-background py-2.5 pl-10 pr-3.5 text-sm text-foreground placeholder:text-muted-foreground/50 transition-all focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/20"
                        />
                      </div>
                    </div>
                    <div className="space-y-2">
                      <label htmlFor="new-password" className="text-sm font-medium text-foreground">
                        {t("security.newPassword")}
                      </label>
                      <div className="relative">
                        <Lock className="absolute left-3.5 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                        <input
                          id="new-password"
                          type="password"
                          value={passwordData.newPassword}
                          onChange={(e) => setPasswordData({ ...passwordData, newPassword: e.target.value })}
                          placeholder="••••••••"
                          className="w-full rounded-lg border border-border bg-background py-2.5 pl-10 pr-3.5 text-sm text-foreground placeholder:text-muted-foreground/50 transition-all focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/20"
                        />
                      </div>
                    </div>
                    <div className="space-y-2">
                      <label htmlFor="confirm-password" className="text-sm font-medium text-foreground">
                        {t("security.confirmPassword")}
                      </label>
                      <div className="relative">
                        <Lock className="absolute left-3.5 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                        <input
                          id="confirm-password"
                          type="password"
                          value={passwordData.confirmPassword}
                          onChange={(e) => setPasswordData({ ...passwordData, confirmPassword: e.target.value })}
                          placeholder="••••••••"
                          className="w-full rounded-lg border border-border bg-background py-2.5 pl-10 pr-3.5 text-sm text-foreground placeholder:text-muted-foreground/50 transition-all focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/20"
                        />
                      </div>
                    </div>
                  </div>
                  <div className="mt-6 flex items-center justify-end gap-3 border-t border-border pt-6">
                    <Button variant="ghost" onClick={handleCancelPasswordChange} disabled={isChangingPassword}>
                      {tCommon("cancel")}
                    </Button>
                  <Button onClick={handleChangePassword} disabled={isChangingPassword}>
                    {isChangingPassword ? (
                      <Loader2 className="h-4 w-4 animate-spin" />
                    ) : null}
                    {passwordSuccess ? t("security.updated") : t("security.updatePassword")}
                  </Button>
                  </div>
                </div>
              )}

              <TwoFactorSetup
                isEnabled={twoFactorEnabled}
                onEnableChange={setTwoFactorEnabled}
              />

              <ExternalLogins />
            </>
          )}

          {/* Billing Section */}
          {activeSection === "billing" && (
            <>
              <div className="rounded-xl border border-border bg-gradient-to-br from-teal/5 to-blue-deep/5 p-8 text-center">
                <CreditCard className="mx-auto mb-4 h-12 w-12 text-teal" />
                <h2 className="mb-2 text-xl font-semibold text-foreground">{t("billing.title")}</h2>
                <p className="mx-auto mb-6 max-w-md text-sm text-muted-foreground">
                  {t("billing.description")}
                </p>
                <Link href="/settings/billing">
                  <Button>
                    {t("billing.goToBilling")}
                    <ArrowRight className="h-4 w-4" />
                  </Button>
                </Link>
              </div>

              <div className="grid grid-cols-1 gap-4 md:grid-cols-3">
                <div className="rounded-xl border border-border bg-card p-5">
                  <p className="mb-2 text-xs text-muted-foreground">{t("billing.currentPlan")}</p>
                  <p className="mb-1 text-lg font-semibold text-foreground">
                    {subscription?.planName || currentPlan?.name || "-"}
                  </p>
                  <p className={cn(
                    "text-xs",
                    subscription?.status === "Active" && "text-emerald-500",
                    subscription?.status === "Trial" && "text-blue-500",
                    subscription?.status === "Cancelled" && "text-yellow-500",
                    subscription?.status === "Expired" && "text-destructive",
                    !subscription && "text-muted-foreground"
                  )}>
                    {subscription?.status || t("billing.noSubscription")}
                  </p>
                </div>
                <div className="rounded-xl border border-border bg-card p-5">
                  <p className="mb-2 text-xs text-muted-foreground">{t("billing.nextBilling")}</p>
                  <p className="mb-1 text-lg font-semibold text-foreground">
                    {subscription?.currentPeriod.endDate
                      ? new Date(subscription.currentPeriod.endDate).toLocaleDateString()
                      : "-"}
                  </p>
                  <p className="text-xs text-muted-foreground">
                    {currentPlan && !currentPlan.isFree
                      ? `${subscription?.billingCycle === "Monthly"
                          ? currentPlan.pricing.monthlyPrice
                          : currentPlan.pricing.annualPrice}€/${subscription?.billingCycle === "Monthly" ? "mes" : "año"}`
                      : currentPlan?.isFree
                        ? t("billing.free")
                        : "-"}
                  </p>
                </div>
                <div className="rounded-xl border border-border bg-card p-5">
                  <p className="mb-2 text-xs text-muted-foreground">{t("billing.paymentMethod")}</p>
                  <p className="mb-1 text-lg font-semibold text-foreground">
                    {defaultPaymentMethod
                      ? `${defaultPaymentMethod.card.brand.charAt(0).toUpperCase() + defaultPaymentMethod.card.brand.slice(1)} **** ${defaultPaymentMethod.card.last4}`
                      : t("billing.noPaymentMethod")}
                  </p>
                  <p className="text-xs text-muted-foreground">
                    {defaultPaymentMethod
                      ? `Expires ${defaultPaymentMethod.card.formattedExpiration}`
                      : "-"}
                  </p>
                </div>
              </div>
            </>
          )}

        </div>
      </div>

      {/* Upload Photo Dialog */}
      <Dialog open={uploadPhotoOpen} onOpenChange={setUploadPhotoOpen}>
        <DialogContent className="sm:max-w-[420px]">
          <DialogHeader>
            <DialogTitle>{t("dialogs.uploadPhoto.title")}</DialogTitle>
            <DialogDescription>
              {t("dialogs.uploadPhoto.description")}
            </DialogDescription>
          </DialogHeader>
          <div className="py-4">
            <div
              className={cn(
                "group cursor-pointer rounded-lg border-2 border-dashed p-8 text-center transition-all",
                selectedPhoto
                  ? "border-teal bg-teal/5"
                  : "border-border hover:border-teal/50"
              )}
              onClick={() => fileInputRef.current?.click()}
            >
              <input
                ref={fileInputRef}
                type="file"
                accept="image/jpeg,image/png"
                onChange={handlePhotoSelect}
                className="hidden"
              />
              <Upload className="mx-auto mb-3 h-10 w-10 text-muted-foreground" />
              {selectedPhoto ? (
                <p className="text-sm font-medium text-foreground">{selectedPhoto.name}</p>
              ) : (
                <p className="text-sm text-muted-foreground">{t("dialogs.uploadPhoto.clickToSelect")}</p>
              )}
            </div>
          </div>
          {photoError && (
            <div className="rounded-lg border border-red-500/30 bg-red-500/10 p-3 text-sm text-red-500">
              {photoError}
            </div>
          )}
          <DialogFooter>
            <button
              onClick={() => {
                setSelectedPhoto(null);
                setPhotoError(null);
                setUploadPhotoOpen(false);
              }}
              disabled={isUploadingPhoto}
              className="rounded-lg px-4 py-2.5 text-sm font-medium hover:bg-muted disabled:opacity-50"
            >
              {tCommon("cancel")}
            </button>
            <button
              onClick={handleUploadPhoto}
              disabled={!selectedPhoto || isUploadingPhoto}
              className="flex items-center gap-2 rounded-lg bg-teal px-4 py-2.5 text-sm font-medium text-white hover:bg-teal/90 disabled:opacity-50"
            >
              {isUploadingPhoto && <Loader2 className="h-4 w-4 animate-spin" />}
              {t("dialogs.uploadPhoto.upload")}
            </button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Deactivate Account Dialog */}
      <Dialog open={deactivateOpen} onOpenChange={setDeactivateOpen}>
        <DialogContent className="sm:max-w-[420px]">
          <DialogHeader>
            <DialogTitle>{t("dialogs.deactivate.title")}</DialogTitle>
            <DialogDescription>
              {t("dialogs.deactivate.description")}
            </DialogDescription>
          </DialogHeader>
          <div className="py-4">
            <div className="flex items-start gap-3 rounded-lg border border-amber-500/30 bg-amber-500/10 p-4">
              <AlertTriangle className="h-5 w-5 flex-shrink-0 text-amber-500" />
              <p className="text-sm text-amber-700 dark:text-amber-400">
                {t("dialogs.deactivate.warning")}
              </p>
            </div>
          </div>
          <DialogFooter>
            <button
              onClick={() => setDeactivateOpen(false)}
              disabled={isDeactivating}
              className="rounded-lg px-4 py-2.5 text-sm font-medium hover:bg-muted disabled:opacity-50"
            >
              {tCommon("cancel")}
            </button>
            <button
              onClick={handleDeactivateAccount}
              disabled={isDeactivating}
              className="flex items-center gap-2 rounded-lg border border-red-500 px-4 py-2.5 text-sm font-medium text-red-500 hover:bg-red-500/10 disabled:opacity-50"
            >
              {isDeactivating && <Loader2 className="h-4 w-4 animate-spin" />}
              {t("dialogs.deactivate.confirm")}
            </button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Delete Account Dialog */}
      <Dialog open={deleteAccountOpen} onOpenChange={setDeleteAccountOpen}>
        <DialogContent className="sm:max-w-[420px]">
          <DialogHeader>
            <DialogTitle className="text-red-500">{t("dialogs.deleteAccount.title")}</DialogTitle>
            <DialogDescription>
              {t("dialogs.deleteAccount.description")}
            </DialogDescription>
          </DialogHeader>
          <div className="py-4">
            <div className="space-y-4">
              <div className="flex items-start gap-3 rounded-lg border border-red-500/30 bg-red-500/10 p-4">
                <AlertTriangle className="h-5 w-5 flex-shrink-0 text-red-500" />
                <div className="text-sm text-red-700 dark:text-red-400">
                  <p className="font-medium">{t("dialogs.deleteAccount.warningTitle")}</p>
                  <ul className="mt-1 list-disc pl-4 space-y-0.5">
                    <li>{t("dialogs.deleteAccount.warningProfile")}</li>
                    <li>{t("dialogs.deleteAccount.warningStudies")}</li>
                    <li>{t("dialogs.deleteAccount.warningTraces")}</li>
                  </ul>
                </div>
              </div>
              <div className="space-y-2">
                <label className="text-sm font-medium text-foreground">
                  {t("dialogs.deleteAccount.confirmLabel")}
                </label>
                <input
                  type="text"
                  value={deleteConfirmText}
                  onChange={(e) => setDeleteConfirmText(e.target.value)}
                  className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm"
                  placeholder={t("dialogs.deleteAccount.confirmPlaceholder")}
                />
              </div>
              {deleteError && (
                <div className="rounded-lg border border-red-500/30 bg-red-500/10 p-3 text-sm text-red-500">
                  {deleteError}
                </div>
              )}
            </div>
          </div>
          <DialogFooter>
            <button
              onClick={() => {
                setDeleteAccountOpen(false);
                setDeleteConfirmText("");
                setDeleteError(null);
              }}
              disabled={isDeleting}
              className="rounded-lg px-4 py-2.5 text-sm font-medium hover:bg-muted disabled:opacity-50"
            >
              {tCommon("cancel")}
            </button>
            <button
              onClick={handleDeleteAccount}
              disabled={isDeleting || deleteConfirmText !== "DELETE"}
              className="flex items-center gap-2 rounded-lg bg-red-500 px-4 py-2.5 text-sm font-medium text-white hover:bg-red-500/90 disabled:opacity-50"
            >
              {isDeleting && <Loader2 className="h-4 w-4 animate-spin" />}
              {t("dialogs.deleteAccount.confirm")}
            </button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

    </div>
  );
}
