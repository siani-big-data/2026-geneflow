"use client";

import { useState, useRef } from "react";
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

const userData = {
  name: "Dr. Sarah Martinez",
  role: "Principal Investigator",
  institution: "Stanford Medical Center",
  email: "s.martinez@stanford.edu",
  researchField: "Diabetes Research & Genomics",
  location: "Stanford, CA",
  joinDate: "2024-01-15",
  bio: "Molecular geneticist specializing in Type 2 Diabetes research with a focus on genome-wide association studies and precision medicine approaches. Leading multi-institutional collaborative research projects.",
  orcid: "0000-0002-1234-5678",
  website: "https://martinez-lab.stanford.edu",
};

const activityStats = [
  { labelKey: "activeStudies", value: "12", icon: FileText, color: "text-teal" },
  { labelKey: "totalSamples", value: "8,934", icon: Activity, color: "text-blue-deep" },
  { labelKey: "collaborators", value: "47", icon: Users, color: "text-teal" },
  { labelKey: "analysesRun", value: "156", icon: BarChart3, color: "text-blue-deep" },
] as const;

const participatedStudies = [
  {
    id: "GF-2026-089",
    name: "Genome-Wide Association Study - Type 2 Diabetes",
    role: "Principal Investigator",
    status: "Active",
    samples: 1247,
  },
  {
    id: "GF-2026-087",
    name: "Whole Exome Sequencing - Rare Disease Panel",
    role: "Co-Investigator",
    status: "Active",
    samples: 342,
  },
  {
    id: "GF-2026-085",
    name: "RNA-Seq Analysis - Cancer Biomarkers",
    role: "Collaborator",
    status: "Completed",
    samples: 856,
  },
  {
    id: "GF-2026-082",
    name: "Targeted Sequencing - BRCA1/2 Variants",
    role: "Collaborator",
    status: "Active",
    samples: 125,
  },
];

const recentActivity = [
  { action: "Uploaded 24 new traces", study: "GF-2026-089", timestamp: "2 hours ago" },
  { action: "Completed Variant Calling Analysis", study: "GF-2026-089", timestamp: "5 hours ago" },
  { action: "Invited team member", study: "GF-2026-087", timestamp: "1 day ago" },
  { action: "Created new study", study: "GF-2026-091", timestamp: "2 days ago" },
];

const allActivity = [
  { action: "Uploaded 24 new traces", study: "GF-2026-089", timestamp: "2 hours ago" },
  { action: "Completed Variant Calling Analysis", study: "GF-2026-089", timestamp: "5 hours ago" },
  { action: "Invited team member", study: "GF-2026-087", timestamp: "1 day ago" },
  { action: "Created new study", study: "GF-2026-091", timestamp: "2 days ago" },
  { action: "Exported analysis results", study: "GF-2026-089", timestamp: "3 days ago" },
  { action: "Updated study settings", study: "GF-2026-085", timestamp: "4 days ago" },
  { action: "Ran Quality Control pipeline", study: "GF-2026-089", timestamp: "5 days ago" },
  { action: "Added collaborator Dr. Wong", study: "GF-2026-087", timestamp: "1 week ago" },
];

export default function ProfilePage() {
  const t = useTranslations("profile");
  const tCommon = useTranslations("common");
  const [editProfileOpen, setEditProfileOpen] = useState(false);
  const [editBioOpen, setEditBioOpen] = useState(false);
  const [uploadPhotoOpen, setUploadPhotoOpen] = useState(false);
  const [viewActivityOpen, setViewActivityOpen] = useState(false);
  const [selectedPhoto, setSelectedPhoto] = useState<File | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const handlePhotoSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) {
      setSelectedPhoto(file);
    }
  };

  const handleUploadPhoto = () => {
    if (selectedPhoto) {
      console.log("Uploading photo:", selectedPhoto.name);
      setUploadPhotoOpen(false);
      setSelectedPhoto(null);
    }
  };

  return (
    <div className="-mx-16 -mt-10 min-h-full bg-background">
      {/* Header Section */}
      <div className="border-b border-border bg-card">
        <div className="mx-auto max-w-[1400px] px-8 py-8">
          <div className="flex items-start gap-8">
            {/* Avatar */}
            <div className="group relative">
              <div className="flex h-28 w-28 items-center justify-center rounded-2xl bg-gradient-to-br from-teal to-blue-deep text-3xl font-semibold text-white shadow-lg">
                SM
              </div>
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
                  <h1 className="mb-1 text-2xl font-semibold text-foreground">{userData.name}</h1>
                  <p className="text-base text-muted-foreground">{userData.role}</p>
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
                <div className="flex items-center gap-2 text-muted-foreground">
                  <Briefcase className="h-4 w-4" />
                  <span>{userData.institution}</span>
                </div>
                <div className="flex items-center gap-2 text-muted-foreground">
                  <Mail className="h-4 w-4" />
                  <span>{userData.email}</span>
                </div>
                <div className="flex items-center gap-2 text-muted-foreground">
                  <BookOpen className="h-4 w-4" />
                  <span>{userData.researchField}</span>
                </div>
                <div className="flex items-center gap-2 text-muted-foreground">
                  <MapPin className="h-4 w-4" />
                  <span>{userData.location}</span>
                </div>
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
              <p className="mb-5 text-sm leading-relaxed text-muted-foreground">{userData.bio}</p>
              <div className="flex flex-wrap gap-6 border-t border-border pt-4">
                <div>
                  <p className="mb-1 text-xs text-muted-foreground">{t("about.orcid")}</p>
                  <a
                    href={`https://orcid.org/${userData.orcid}`}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="font-mono text-sm text-blue-deep hover:underline"
                  >
                    {userData.orcid}
                  </a>
                </div>
                <div>
                  <p className="mb-1 text-xs text-muted-foreground">{t("about.website")}</p>
                  <a
                    href={userData.website}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="flex items-center gap-1 text-sm text-blue-deep hover:underline"
                  >
                    <Globe className="h-3.5 w-3.5" />
                    {t("about.labWebsite")}
                  </a>
                </div>
                <div>
                  <p className="mb-1 text-xs text-muted-foreground">{t("about.memberSince")}</p>
                  <p className="flex items-center gap-1.5 text-sm text-foreground">
                    <Calendar className="h-3.5 w-3.5 text-muted-foreground" />
                    {new Date(userData.joinDate).toLocaleDateString("en-US", {
                      month: "long",
                      year: "numeric",
                    })}
                  </p>
                </div>
              </div>
            </div>

            {/* Participated Studies */}
            <div className="overflow-hidden rounded-xl border border-border bg-card">
              <div className="border-b border-border p-6">
                <h2 className="text-lg font-semibold text-foreground">{t("studies.title")}</h2>
                <p className="mt-1 text-sm text-muted-foreground">
                  {t("studies.description")}
                </p>
              </div>
              <div className="divide-y divide-border">
                {participatedStudies.map((study) => {
                  const roleKey = study.role === "Principal Investigator"
                    ? "principalInvestigator"
                    : study.role === "Co-Investigator"
                      ? "coInvestigator"
                      : "collaborator";
                  return (
                    <Link
                      key={study.id}
                      href={`/studies/${study.id}`}
                      className="group flex items-center justify-between p-6 transition-colors hover:bg-muted/20"
                    >
                      <div className="flex-1">
                        <div className="mb-2 flex items-center gap-3">
                          <span className="text-sm font-medium text-blue-deep">{study.id}</span>
                          <span
                            className={cn(
                              "rounded-full px-2.5 py-0.5 text-xs font-medium",
                              study.status === "Active"
                                ? "bg-teal/10 text-teal"
                                : "bg-muted text-muted-foreground"
                            )}
                          >
                            {tCommon(`status.${study.status.toLowerCase()}`)}
                          </span>
                        </div>
                        <h3 className="mb-1 font-medium text-foreground transition-colors group-hover:text-teal">
                          {study.name}
                        </h3>
                        <div className="flex items-center gap-4 text-sm text-muted-foreground">
                          <span>{t(`studies.roles.${roleKey}`)}</span>
                          <span>•</span>
                          <span>{study.samples.toLocaleString()} {t("studies.samples")}</span>
                        </div>
                      </div>
                      <ChevronRight className="h-5 w-5 text-muted-foreground transition-colors group-hover:text-teal" />
                    </Link>
                  );
                })}
              </div>
            </div>
          </div>

          {/* Right Column - Sidebar */}
          <div className="space-y-6">
            {/* Recent Activity */}
            <div className="rounded-xl border border-border bg-card p-6">
              <h2 className="mb-4 text-base font-semibold text-foreground">{t("recentActivity.title")}</h2>
              <div className="space-y-4">
                {recentActivity.map((activity, idx) => (
                  <div key={idx} className="flex gap-3">
                    <div className="mt-2 h-2 w-2 flex-shrink-0 rounded-full bg-teal" />
                    <div className="flex-1">
                      <p className="mb-0.5 text-sm font-medium text-foreground">{activity.action}</p>
                      <p className="text-xs text-muted-foreground">{activity.study}</p>
                      <p className="mt-1 text-xs text-muted-foreground">{activity.timestamp}</p>
                    </div>
                  </div>
                ))}
              </div>
              <button
                onClick={() => setViewActivityOpen(true)}
                className="mt-4 w-full border-t border-border pt-4 text-sm font-medium text-teal hover:text-teal/80"
              >
                {t("recentActivity.viewAll")}
              </button>
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
                  defaultValue="Sarah"
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
                  defaultValue="Martinez"
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
                defaultValue={userData.role}
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
                defaultValue={userData.institution}
                className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm text-foreground transition-all focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/20"
              />
            </div>

            <div className="space-y-2">
              <label htmlFor="research-field" className="text-sm font-medium text-foreground">
                {t("dialogs.editProfile.researchField")}
              </label>
              <input
                id="research-field"
                type="text"
                defaultValue={userData.researchField}
                className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm text-foreground transition-all focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/20"
              />
            </div>

            <div className="space-y-2">
              <label htmlFor="location" className="text-sm font-medium text-foreground">
                {t("dialogs.editProfile.location")}
              </label>
              <input
                id="location"
                type="text"
                defaultValue={userData.location}
                className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm text-foreground transition-all focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/20"
              />
            </div>
          </div>
          <DialogFooter>
            <Button variant="ghost" onClick={() => setEditProfileOpen(false)}>
              {tCommon("cancel")}
            </Button>
            <Button onClick={() => setEditProfileOpen(false)}>{t("dialogs.editProfile.saveChanges")}</Button>
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
                defaultValue={userData.bio}
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
                defaultValue={userData.orcid}
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
                defaultValue={userData.website}
                className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm text-foreground transition-all focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/20"
              />
            </div>
          </div>
          <DialogFooter>
            <Button variant="ghost" onClick={() => setEditBioOpen(false)}>
              {tCommon("cancel")}
            </Button>
            <Button onClick={() => setEditBioOpen(false)}>{t("dialogs.editBio.saveChanges")}</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

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
          <DialogFooter>
            <Button
              variant="ghost"
              onClick={() => {
                setSelectedPhoto(null);
                setUploadPhotoOpen(false);
              }}
            >
              {tCommon("cancel")}
            </Button>
            <Button onClick={handleUploadPhoto} disabled={!selectedPhoto}>
              {t("dialogs.uploadPhoto.upload")}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* View All Activity Dialog */}
      <Dialog open={viewActivityOpen} onOpenChange={setViewActivityOpen}>
        <DialogContent className="sm:max-w-[540px]">
          <DialogHeader>
            <DialogTitle>{t("dialogs.activityHistory.title")}</DialogTitle>
            <DialogDescription>
              {t("dialogs.activityHistory.description")}
            </DialogDescription>
          </DialogHeader>
          <div className="max-h-[400px] overflow-y-auto py-4">
            <div className="space-y-4">
              {allActivity.map((activity, idx) => (
                <div key={idx} className="flex gap-3">
                  <div className="mt-2 h-2 w-2 flex-shrink-0 rounded-full bg-teal" />
                  <div className="flex-1 border-b border-border pb-4">
                    <p className="mb-0.5 text-sm font-medium text-foreground">{activity.action}</p>
                    <div className="flex items-center gap-2 text-xs text-muted-foreground">
                      <span>{activity.study}</span>
                      <span>•</span>
                      <span>{activity.timestamp}</span>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </div>
          <DialogFooter>
            <Button variant="ghost" onClick={() => setViewActivityOpen(false)}>
              {tCommon("close")}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
