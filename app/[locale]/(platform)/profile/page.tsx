"use client";

import { useState, useRef, useEffect, useCallback } from "react";
import { useTranslations } from "next-intl";
import { Link } from "@/lib/navigation";
import {
  Camera,
  Mail,
  MapPin,
  Briefcase,
  BookOpen,
  Edit,
  Settings,
  Activity,
  FileText,
  Users,
  BarChart3,
  ChevronRight,
  Globe,
  Calendar,
  Upload,
  Loader2,
  AlertCircle,
} from "lucide-react";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Button,
} from "@/components/ui";
import { cn } from "@/lib/utils";
import { profileService } from "@/services/profile.service";
import { useAuthStore } from "@/stores/auth-store";
import type { Profile, ProfileStats, UpdateProfileRequest, UpdateResearchIdentifiersRequest } from "@/types";
import { RESEARCH_FIELDS, getResearchFieldLabel } from "@/types/profile";

export default function ProfilePage() {
  const t = useTranslations("profile");
  const tCommon = useTranslations("common");
  const { user, setProfile: setStoreProfile } = useAuthStore();

  // Profile data state (local for this page)
  const [profile, setProfile] = useState<Profile | null>(null);
  const [stats, setStats] = useState<ProfileStats | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Dialog states
  const [editProfileOpen, setEditProfileOpen] = useState(false);
  const [editBioOpen, setEditBioOpen] = useState(false);
  const [uploadPhotoOpen, setUploadPhotoOpen] = useState(false);
  const [viewActivityOpen, setViewActivityOpen] = useState(false);

  // Form states
  const [isSaving, setIsSaving] = useState(false);
  const [selectedPhoto, setSelectedPhoto] = useState<File | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  // Form data for edit profile
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

  // Form data for research identifiers
  const [identifiersData, setIdentifiersData] = useState<UpdateResearchIdentifiersRequest>({
    orcidId: null,
    website: null,
  });

  // Fetch profile data
  const fetchProfile = useCallback(async () => {
    try {
      setIsLoading(true);
      setError(null);

      // Fetch profile (required)
      const profileData = await profileService.getCurrentProfile();
      setProfile(profileData);

      // Fetch stats with fallback to defaults if endpoint fails
      let statsData: ProfileStats;
      try {
        statsData = await profileService.getCurrentProfileStats();
      } catch {
        // Fallback stats when endpoint is not available
        statsData = {
          totalStudies: 0,
          ownedStudies: 0,
          totalTraces: 0,
          totalAlignments: 0,
          completedAlignments: 0,
          lastActivityAt: null,
          memberSince: profileData.createdAt,
        };
      }
      setStats(statsData);

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
      setError(err instanceof Error ? err.message : "Failed to load profile");
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchProfile();
  }, [fetchProfile]);

  // Handle profile update
  const handleUpdateProfile = async () => {
    if (!profile) return;

    try {
      setIsSaving(true);
      const updatedProfile = await profileService.updateProfile(formData);
      setProfile(updatedProfile);
      setStoreProfile(updatedProfile); // Update store for sidebar/header
      setEditProfileOpen(false);
    } catch (err) {
      console.error("Failed to update profile:", err);
    } finally {
      setIsSaving(false);
    }
  };

  // Handle bio/identifiers update
  const handleUpdateBio = async () => {
    if (!profile) return;

    try {
      setIsSaving(true);

      // Update basic info (including bio)
      await profileService.updateProfile({
        ...formData,
        bio: identifiersData.orcidId !== profile.orcidId || identifiersData.website !== profile.website
          ? formData.bio
          : formData.bio,
      });

      // Update research identifiers
      const updatedProfile = await profileService.updateResearchIdentifiers(identifiersData);

      setProfile(updatedProfile);
      setStoreProfile(updatedProfile); // Update store for sidebar/header
      setEditBioOpen(false);
    } catch (err) {
      console.error("Failed to update bio:", err);
    } finally {
      setIsSaving(false);
    }
  };

  // Handle photo upload
  const handlePhotoSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) {
      // Validate file size (max 10MB)
      if (file.size > 10 * 1024 * 1024) {
        setError("File size must be less than 10MB");
        return;
      }
      setSelectedPhoto(file);
      setError(null);
    }
  };

  const handleUploadPhoto = async () => {
    if (!selectedPhoto) return;

    try {
      setIsSaving(true);
      setError(null);
      const updatedProfile = await profileService.uploadProfilePhoto(selectedPhoto);
      setProfile(updatedProfile);
      // Also update the auth store so sidebar/header update immediately
      setStoreProfile(updatedProfile);
      setUploadPhotoOpen(false);
      setSelectedPhoto(null);
    } catch (err) {
      console.error("Failed to upload photo:", err);
      setError(err instanceof Error ? err.message : "Failed to upload photo");
    } finally {
      setIsSaving(false);
    }
  };

  // Activity stats for display
  const activityStats = [
    { labelKey: "activeStudies", value: stats?.totalStudies.toString() ?? "0", icon: FileText, color: "text-teal" },
    { labelKey: "totalSamples", value: stats?.totalTraces.toLocaleString() ?? "0", icon: Activity, color: "text-blue-deep" },
    { labelKey: "collaborators", value: "-", icon: Users, color: "text-teal" },
    { labelKey: "analysesRun", value: stats?.completedAlignments.toString() ?? "0", icon: BarChart3, color: "text-blue-deep" },
  ] as const;

  // Loading state
  if (isLoading) {
    return (
      <div className="flex min-h-[400px] items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-teal" />
      </div>
    );
  }

  // Error state
  if (error || !profile) {
    return (
      <div className="flex min-h-[400px] flex-col items-center justify-center gap-4">
        <AlertCircle className="h-12 w-12 text-destructive" />
        <p className="text-lg font-medium text-foreground">{error || "Profile not found"}</p>
        <Button onClick={fetchProfile}>Try Again</Button>
      </div>
    );
  }

  return (
    <div className="-mx-16 -mt-10 min-h-full bg-background">
      {/* Header Section */}
      <div className="border-b border-border bg-card">
        <div className="mx-auto max-w-[1400px] px-8 py-8">
          <div className="flex items-start gap-8">
            {/* Avatar */}
            <div className="group relative">
              {profile.photoUrl ? (
                <img
                  src={profileService.resolveStorageUrl(profile.photoThumbnailUrl) || profileService.resolveStorageUrl(profile.photoUrl) || undefined}
                  alt={profile.fullName}
                  className="h-28 w-28 rounded-2xl object-cover shadow-lg"
                />
              ) : (
                <div className="flex h-28 w-28 items-center justify-center rounded-2xl bg-gradient-to-br from-teal to-blue-deep text-3xl font-semibold text-white shadow-lg">
                  {profile.initials}
                </div>
              )}
              <button
                onClick={() => setUploadPhotoOpen(true)}
                className="absolute bottom-0 right-0 rounded-lg border border-border bg-background p-2 opacity-0 shadow-md transition-all hover:bg-muted group-hover:opacity-100"
              >
                <Camera className="h-4 w-4 text-foreground" />
              </button>
            </div>

            {/* Profile Info */}
            <div className="flex-1">
              <div className="mb-3 flex items-start justify-between">
                <div>
                  <h1 className="mb-1 text-2xl font-semibold text-foreground">{profile.fullName}</h1>
                  <p className="text-base text-muted-foreground">{profile.professionalRole || t("noRole")}</p>
                </div>
                <div className="flex items-center gap-2">
                  <Button onClick={() => setEditProfileOpen(true)}>
                    <Edit className="h-4 w-4" />
                    {t("editProfile")}
                  </Button>
                  <Link href="/settings">
                    <Button variant="outline">
                      <Settings className="h-4 w-4" />
                      {t("settings")}
                    </Button>
                  </Link>
                </div>
              </div>

              <div className="grid grid-cols-1 gap-3 text-sm md:grid-cols-2">
                {profile.institutionName && (
                  <div className="flex items-center gap-2 text-muted-foreground">
                    <Briefcase className="h-4 w-4" />
                    <span>{profile.institutionDisplayName || profile.institutionName}</span>
                  </div>
                )}
                {user?.email && (
                  <div className="flex items-center gap-2 text-muted-foreground">
                    <Mail className="h-4 w-4" />
                    <span>{user.email}</span>
                  </div>
                )}
                {profile.researchField && (
                  <div className="flex items-center gap-2 text-muted-foreground">
                    <BookOpen className="h-4 w-4" />
                    <span>{getResearchFieldLabel(profile.researchField)}</span>
                  </div>
                )}
                {profile.location && (
                  <div className="flex items-center gap-2 text-muted-foreground">
                    <MapPin className="h-4 w-4" />
                    <span>{profile.location}</span>
                  </div>
                )}
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Content */}
      <div className="mx-auto max-w-[1400px] px-8 py-8">
        <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
          {/* Left Column - Main Content */}
          <div className="space-y-6 lg:col-span-2">
            {/* Activity Stats */}
            <div className="rounded-xl border border-border bg-card p-6">
              <h2 className="mb-5 text-lg font-semibold text-foreground">{t("activityOverview")}</h2>
              <div className="grid grid-cols-2 gap-4 md:grid-cols-4">
                {activityStats.map((stat, idx) => {
                  const Icon = stat.icon;
                  return (
                    <div key={idx} className="text-center">
                      <div
                        className={cn(
                          "mb-3 inline-flex rounded-xl bg-muted/50 p-3",
                          stat.color
                        )}
                      >
                        <Icon className="h-5 w-5" />
                      </div>
                      <p className="mb-1 text-2xl font-semibold text-foreground">{stat.value}</p>
                      <p className="text-xs text-muted-foreground">{t(`stats.${stat.labelKey}`)}</p>
                    </div>
                  );
                })}
              </div>
            </div>

            {/* Bio */}
            <div className="rounded-xl border border-border bg-card p-6">
              <div className="mb-4 flex items-start justify-between">
                <h2 className="text-lg font-semibold text-foreground">{t("about.title")}</h2>
                <button
                  onClick={() => setEditBioOpen(true)}
                  className="flex items-center gap-1 text-sm font-medium text-teal hover:text-teal/80"
                >
                  <Edit className="h-3.5 w-3.5" />
                  {t("about.edit")}
                </button>
              </div>
              <p className="mb-5 text-sm leading-relaxed text-muted-foreground">
                {profile.bio || t("about.noBio")}
              </p>
              <div className="flex flex-wrap gap-6 border-t border-border pt-4">
                {profile.orcidId && (
                  <div>
                    <p className="mb-1 text-xs text-muted-foreground">{t("about.orcid")}</p>
                    <a
                      href={profile.orcidUrl || `https://orcid.org/${profile.orcidId}`}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="font-mono text-sm text-blue-deep hover:underline"
                    >
                      {profile.orcidId}
                    </a>
                  </div>
                )}
                {profile.website && (
                  <div>
                    <p className="mb-1 text-xs text-muted-foreground">{t("about.website")}</p>
                    <a
                      href={profile.website}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="flex items-center gap-1 text-sm text-blue-deep hover:underline"
                    >
                      <Globe className="h-3.5 w-3.5" />
                      {t("about.labWebsite")}
                    </a>
                  </div>
                )}
                <div>
                  <p className="mb-1 text-xs text-muted-foreground">{t("about.memberSince")}</p>
                  <p className="flex items-center gap-1.5 text-sm text-foreground">
                    <Calendar className="h-3.5 w-3.5 text-muted-foreground" />
                    {new Date(stats?.memberSince || profile.createdAt).toLocaleDateString("en-US", {
                      month: "long",
                      year: "numeric",
                    })}
                  </p>
                </div>
              </div>
            </div>

            {/* Studies Placeholder */}
            <div className="overflow-hidden rounded-xl border border-border bg-card">
              <div className="border-b border-border p-6">
                <h2 className="text-lg font-semibold text-foreground">{t("studies.title")}</h2>
                <p className="mt-1 text-sm text-muted-foreground">
                  {t("studies.description")}
                </p>
              </div>
              <div className="p-6 text-center text-muted-foreground">
                <FileText className="mx-auto mb-3 h-12 w-12 opacity-30" />
                <p>{stats?.totalStudies === 0 ? t("studies.noStudies") : `${stats?.totalStudies} studies`}</p>
              </div>
            </div>
          </div>

          {/* Right Column - Sidebar */}
          <div className="space-y-6">
            {/* Recent Activity */}
            <div className="rounded-xl border border-border bg-card p-6">
              <h2 className="mb-4 text-base font-semibold text-foreground">{t("recentActivity.title")}</h2>
              <div className="text-center text-muted-foreground">
                <Activity className="mx-auto mb-3 h-8 w-8 opacity-30" />
                <p className="text-sm">{t("recentActivity.noActivity")}</p>
              </div>
            </div>

            {/* Quick Actions */}
            <div className="rounded-xl border border-border bg-card p-6">
              <h2 className="mb-4 text-base font-semibold text-foreground">{t("quickActions.title")}</h2>
              <div className="space-y-2">
                <Link
                  href="/studies"
                  className="group flex items-center gap-3 rounded-lg p-3 transition-all hover:bg-muted/50"
                >
                  <div className="rounded-lg bg-teal/10 p-2 transition-colors group-hover:bg-teal/20">
                    <FileText className="h-4 w-4 text-teal" />
                  </div>
                  <span className="text-sm font-medium text-foreground">{t("quickActions.createStudy")}</span>
                </Link>
                <Link
                  href="/pipelines"
                  className="group flex items-center gap-3 rounded-lg p-3 transition-all hover:bg-muted/50"
                >
                  <div className="rounded-lg bg-blue-deep/10 p-2 transition-colors group-hover:bg-blue-deep/20">
                    <BarChart3 className="h-4 w-4 text-blue-deep" />
                  </div>
                  <span className="text-sm font-medium text-foreground">{t("quickActions.runAnalysis")}</span>
                </Link>
                <Link
                  href="/settings/collaboration"
                  className="group flex items-center gap-3 rounded-lg p-3 transition-all hover:bg-muted/50"
                >
                  <div className="rounded-lg bg-teal/10 p-2 transition-colors group-hover:bg-teal/20">
                    <Users className="h-4 w-4 text-teal" />
                  </div>
                  <span className="text-sm font-medium text-foreground">{t("quickActions.manageTeam")}</span>
                </Link>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Edit Profile Modal */}
      <Dialog open={editProfileOpen} onOpenChange={setEditProfileOpen}>
        <DialogContent className="sm:max-w-[540px]">
          <DialogHeader>
            <DialogTitle>{t("dialogs.editProfile.title")}</DialogTitle>
            <DialogDescription>
              {t("dialogs.editProfile.description")}
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-5 py-4">
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <label htmlFor="first-name" className="text-sm font-medium text-foreground">
                  {t("dialogs.editProfile.firstName")}
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
                  {t("dialogs.editProfile.lastName")}
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
              <label htmlFor="role" className="text-sm font-medium text-foreground">
                {t("dialogs.editProfile.role")}
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
                {t("dialogs.editProfile.institution")}
              </label>
              <input
                id="institution"
                type="text"
                value={formData.institutionName || ""}
                onChange={(e) => setFormData({ ...formData, institutionName: e.target.value || null })}
                className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm text-foreground transition-all focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/20"
              />
            </div>

            <div className="space-y-2">
              <label htmlFor="research-field" className="text-sm font-medium text-foreground">
                {t("dialogs.editProfile.researchField")}
              </label>
              <select
                id="research-field"
                value={formData.researchField || ""}
                onChange={(e) => setFormData({ ...formData, researchField: e.target.value || null })}
                className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm text-foreground transition-all focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/20"
              >
                <option value="">Select a field...</option>
                {RESEARCH_FIELDS.map((field) => (
                  <option key={field.id} value={field.name}>
                    {field.label}
                  </option>
                ))}
              </select>
            </div>

            <div className="space-y-2">
              <label htmlFor="location" className="text-sm font-medium text-foreground">
                {t("dialogs.editProfile.location")}
              </label>
              <input
                id="location"
                type="text"
                value={formData.location || ""}
                onChange={(e) => setFormData({ ...formData, location: e.target.value || null })}
                className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm text-foreground transition-all focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/20"
              />
            </div>
          </div>
          <DialogFooter>
            <Button variant="ghost" onClick={() => setEditProfileOpen(false)} disabled={isSaving}>
              {tCommon("cancel")}
            </Button>
            <Button onClick={handleUpdateProfile} disabled={isSaving}>
              {isSaving && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              {t("dialogs.editProfile.saveChanges")}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Edit Bio Modal */}
      <Dialog open={editBioOpen} onOpenChange={setEditBioOpen}>
        <DialogContent className="sm:max-w-[540px]">
          <DialogHeader>
            <DialogTitle>{t("dialogs.editBio.title")}</DialogTitle>
            <DialogDescription>
              {t("dialogs.editBio.description")}
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-5 py-4">
            <div className="space-y-2">
              <label htmlFor="bio" className="text-sm font-medium text-foreground">
                {t("dialogs.editBio.biography")}
              </label>
              <textarea
                id="bio"
                rows={5}
                value={formData.bio || ""}
                onChange={(e) => setFormData({ ...formData, bio: e.target.value || null })}
                className="w-full resize-none rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm text-foreground transition-all focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/20"
              />
            </div>

            <div className="space-y-2">
              <label htmlFor="orcid" className="text-sm font-medium text-foreground">
                {t("dialogs.editBio.orcid")}
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
                {t("dialogs.editBio.website")}
              </label>
              <input
                id="website"
                type="url"
                placeholder="https://"
                value={identifiersData.website || ""}
                onChange={(e) => setIdentifiersData({ ...identifiersData, website: e.target.value || null })}
                className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm text-foreground transition-all focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/20"
              />
            </div>
          </div>
          <DialogFooter>
            <Button variant="ghost" onClick={() => setEditBioOpen(false)} disabled={isSaving}>
              {tCommon("cancel")}
            </Button>
            <Button onClick={handleUpdateBio} disabled={isSaving}>
              {isSaving && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              {t("dialogs.editBio.saveChanges")}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Upload Photo Dialog */}
      <Dialog open={uploadPhotoOpen} onOpenChange={(open) => {
        if (!isSaving) {
          setUploadPhotoOpen(open);
          if (!open) setSelectedPhoto(null);
        }
      }}>
        <DialogContent className="sm:max-w-[420px]">
          <DialogHeader>
            <DialogTitle>{t("dialogs.uploadPhoto.title")}</DialogTitle>
            <DialogDescription>
              {t("dialogs.uploadPhoto.description")}
            </DialogDescription>
          </DialogHeader>
          <div className="py-4">
            {error && (
              <div className="mb-4 rounded-lg bg-destructive/10 p-3 text-sm text-destructive">
                {error}
              </div>
            )}
            <div
              className={cn(
                "group cursor-pointer rounded-lg border-2 border-dashed p-8 text-center transition-all",
                selectedPhoto
                  ? "border-teal bg-teal/5"
                  : "border-border hover:border-teal/50",
                isSaving && "pointer-events-none opacity-50"
              )}
              onClick={() => !isSaving && fileInputRef.current?.click()}
            >
              <input
                ref={fileInputRef}
                type="file"
                accept="image/jpeg,image/png,image/gif,image/webp"
                onChange={handlePhotoSelect}
                className="hidden"
                disabled={isSaving}
              />
              <Upload className="mx-auto mb-3 h-10 w-10 text-muted-foreground" />
              {selectedPhoto ? (
                <div>
                  <p className="text-sm font-medium text-foreground">{selectedPhoto.name}</p>
                  <p className="mt-1 text-xs text-muted-foreground">
                    {(selectedPhoto.size / 1024 / 1024).toFixed(2)} MB
                  </p>
                </div>
              ) : (
                <div>
                  <p className="text-sm text-muted-foreground">{t("dialogs.uploadPhoto.clickToSelect")}</p>
                  <p className="mt-1 text-xs text-muted-foreground">
                    JPEG, PNG, GIF, WebP (max 10MB)
                  </p>
                </div>
              )}
            </div>
          </div>
          <DialogFooter>
            <Button
              variant="ghost"
              onClick={() => {
                setSelectedPhoto(null);
                setUploadPhotoOpen(false);
                setError(null);
              }}
              disabled={isSaving}
            >
              {tCommon("cancel")}
            </Button>
            <Button onClick={handleUploadPhoto} disabled={!selectedPhoto || isSaving}>
              {isSaving && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              {t("dialogs.uploadPhoto.upload")}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
