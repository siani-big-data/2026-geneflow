"use client";

import { useState, useEffect } from "react";
import { useTranslations } from "next-intl";
import { Link } from "@/lib/navigation";
import { useParams } from "next/navigation";
import {
  ArrowLeft,
  Globe,
  Lock,
  MoreVertical,
  Download,
  Share2,
  Settings as SettingsIcon,
  UserPlus,
  Beaker,
  FileText,
  BarChart3,
  Users,
  CheckCircle2,
  Clock,
  Calendar,
  Eye,
  Trash2,
  Edit,
  Upload,
  Mail,
  Copy,
  Check,
  Sparkles,
  X,
  Loader2,
  Star,
} from "lucide-react";
import { cn } from "@/lib/utils";
import { AIAssistant } from "@/components/shared";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  useStudy,
  useStudyMembers,
  useStudyPapers,
  useStudyStats,
  useDeleteStudy,
  useUpdateStudy,
  useUpdateStudySettings,
  useSendInvitation,
  useRemoveStudyMember,
  useChangeMemberRole,
  useRecordStudyView,
  useStarStudy,
  useUnstarStudy,
  useIsStudyStarred,
} from "@/hooks";
import type { Study, StudyMember, UpdateStudyInput, SendInvitationInput } from "@/types";

const tabIds = ["overview", "traces", "analysis", "team", "settings"] as const;
const tabIcons = {
  overview: Beaker,
  traces: FileText,
  analysis: BarChart3,
  team: Users,
  settings: SettingsIcon,
};

export default function StudyDetailPage() {
  const t = useTranslations("studies.detail");
  const tCommon = useTranslations("common");
  const params = useParams();
  const studyId = params.studyId as string;

  // UI State
  const [activeTab, setActiveTab] = useState("overview");
  const [shareOpen, setShareOpen] = useState(false);
  const [exportOpen, setExportOpen] = useState(false);
  const [uploadTracesOpen, setUploadTracesOpen] = useState(false);
  const [inviteMemberOpen, setInviteMemberOpen] = useState(false);
  const [aiAssistantOpen, setAiAssistantOpen] = useState(false);
  const [linkCopied, setLinkCopied] = useState(false);
  const [deleteStudyOpen, setDeleteStudyOpen] = useState(false);
  const [editMemberOpen, setEditMemberOpen] = useState(false);
  const [deleteMemberOpen, setDeleteMemberOpen] = useState(false);
  const [selectedMember, setSelectedMember] = useState<StudyMember | null>(null);
  const [saveSuccess, setSaveSuccess] = useState(false);

  // Form state
  const [inviteForm, setInviteForm] = useState<SendInvitationInput>({
    email: "",
    roleId: 4, // Viewer by default
    message: "",
  });

  // Queries
  const { data: study, isLoading, error } = useStudy(studyId);
  const { data: members } = useStudyMembers(studyId);
  const { data: papers } = useStudyPapers(studyId);
  const { data: stats } = useStudyStats(studyId);
  const { data: isStarredData } = useIsStudyStarred(studyId);

  // Mutations
  const deleteStudyMutation = useDeleteStudy();
  const updateStudyMutation = useUpdateStudy(studyId);
  const updateSettingsMutation = useUpdateStudySettings(studyId);
  const sendInvitationMutation = useSendInvitation(studyId);
  const removeStudyMemberMutation = useRemoveStudyMember(studyId);
  const changeMemberRoleMutation = useChangeMemberRole(studyId);
  const recordViewMutation = useRecordStudyView();
  const starStudyMutation = useStarStudy();
  const unstarStudyMutation = useUnstarStudy();

  // Record view on mount
  useEffect(() => {
    if (studyId) {
      recordViewMutation.mutate(studyId);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [studyId]);

  // Handlers
  const handleCopyLink = () => {
    navigator.clipboard.writeText(window.location.href);
    setLinkCopied(true);
    setTimeout(() => setLinkCopied(false), 2000);
  };

  const handleDeleteStudy = async () => {
    try {
      await deleteStudyMutation.mutateAsync(studyId);
      setDeleteStudyOpen(false);
      // Redirect to studies list
      window.location.href = "/studies";
    } catch (err) {
      console.error("Failed to delete study:", err);
    }
  };

  const handleSaveSettings = async () => {
    // This would save settings - simplified for now
    setSaveSuccess(true);
    setTimeout(() => setSaveSuccess(false), 2000);
  };

  const handleSendInvitation = async () => {
    try {
      await sendInvitationMutation.mutateAsync(inviteForm);
      setInviteMemberOpen(false);
      setInviteForm({ email: "", roleId: 4, message: "" });
    } catch (err) {
      console.error("Failed to send invitation:", err);
    }
  };

  const handleRemoveMember = async () => {
    if (selectedMember) {
      try {
        await removeStudyMemberMutation.mutateAsync(selectedMember.userId);
        setDeleteMemberOpen(false);
        setSelectedMember(null);
      } catch (err) {
        console.error("Failed to remove member:", err);
      }
    }
  };

  const handleToggleStar = async () => {
    if (isStarredData?.isStarred) {
      await unstarStudyMutation.mutateAsync(studyId);
    } else {
      await starStudyMutation.mutateAsync(studyId);
    }
  };

  const handleExportFormat = (format: string) => {
    console.log("Exporting as:", format);
    setExportOpen(false);
  };

  // Loading state
  if (isLoading) {
    return (
      <div className="flex h-96 items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-teal" />
      </div>
    );
  }

  // Error state
  if (error || !study) {
    return (
      <div className="flex h-96 flex-col items-center justify-center gap-4">
        <Beaker className="h-12 w-12 text-muted-foreground" />
        <p className="text-lg font-medium">Study not found</p>
        <Link href="/studies" className="text-teal hover:underline">
          {t("backToStudies")}
        </Link>
      </div>
    );
  }

  const isPublic = study.statusName === "published";
  const membersList = members ?? study.members ?? [];
  const papersList = papers ?? study.papers ?? [];

  return (
    <div className="-mx-16 -mt-10">
      {/* Header Section */}
      <div className="border-b border-border bg-card">
        <div className="px-16 py-6">
          {/* Back Button */}
          <Link
            href="/studies"
            className="mb-4 inline-flex items-center gap-2 text-sm text-muted-foreground transition-colors hover:text-foreground"
          >
            <ArrowLeft className="h-4 w-4" />
            {t("backToStudies")}
          </Link>

          {/* Study Header */}
          <div className="mb-6 flex items-start justify-between">
            <div className="flex-1">
              <div className="mb-2 flex items-center gap-3">
                <span className="text-sm font-medium text-blue-deep">{study.id}</span>
                <span
                  className={cn(
                    "rounded-full px-2.5 py-1 text-xs font-medium",
                    study.statusName === "active"
                      ? "bg-teal/10 text-teal"
                      : study.statusName === "completed"
                      ? "bg-emerald-500/10 text-emerald-500"
                      : study.statusName === "published"
                      ? "bg-blue-500/10 text-blue-500"
                      : "bg-muted text-muted-foreground"
                  )}
                >
                  {tCommon(`status.${study.statusName}`)}
                </span>
                <div className="flex items-center gap-1.5 rounded-full bg-muted px-2.5 py-1 text-xs font-medium text-muted-foreground">
                  {isPublic ? <Globe className="h-3 w-3" /> : <Lock className="h-3 w-3" />}
                  {isPublic ? t("visibility.public") : t("visibility.private")}
                </div>
                {study.isFeatured && (
                  <span className="flex items-center gap-1 rounded-full bg-amber-100 px-2 py-0.5 text-xs font-medium text-amber-700 dark:bg-amber-900/30 dark:text-amber-400">
                    <Star className="h-3 w-3" />
                    Featured
                  </span>
                )}
              </div>
              <h1 className="mb-2 text-2xl font-semibold text-foreground">{study.title}</h1>
              <div className="flex items-center gap-4 text-sm text-muted-foreground">
                <span>{study.researchFieldName}</span>
                {study.principalInvestigator && (
                  <>
                    <span>•</span>
                    <span>{study.principalInvestigator}</span>
                  </>
                )}
                {study.institution && (
                  <>
                    <span>•</span>
                    <span>{study.institution}</span>
                  </>
                )}
              </div>
            </div>

            <div className="flex items-center gap-2">
              <button
                onClick={handleToggleStar}
                className={cn(
                  "flex items-center gap-2 rounded-lg border px-4 py-2 text-sm font-medium transition-all",
                  isStarredData?.isStarred
                    ? "border-amber-500 bg-amber-500/10 text-amber-600"
                    : "border-border text-foreground hover:bg-muted/50"
                )}
              >
                <Star className={cn("h-4 w-4", isStarredData?.isStarred && "fill-current")} />
                {stats?.starsCount ?? study.metrics.starsCount}
              </button>
              <button
                onClick={() => setAiAssistantOpen(true)}
                className="flex items-center gap-2 rounded-lg bg-gradient-to-r from-teal to-blue-deep px-4 py-2 text-sm font-medium text-white transition-all hover:opacity-90"
              >
                <Sparkles className="h-4 w-4" />
                {t("aiAssistant")}
              </button>
              <button
                onClick={() => setShareOpen(true)}
                className="flex items-center gap-2 rounded-lg border border-border px-4 py-2 text-sm font-medium text-foreground transition-all hover:bg-muted/50"
              >
                <Share2 className="h-4 w-4" />
                {t("share")}
              </button>
              <button
                onClick={() => setExportOpen(true)}
                className="flex items-center gap-2 rounded-lg border border-border px-4 py-2 text-sm font-medium text-foreground transition-all hover:bg-muted/50"
              >
                <Download className="h-4 w-4" />
                {t("export")}
              </button>
              <button className="rounded-lg border border-border p-2 text-foreground transition-all hover:bg-muted/50">
                <MoreVertical className="h-4 w-4" />
              </button>
            </div>
          </div>

          {/* Tabs */}
          <div className="flex items-center gap-1">
            {tabIds.map((tabId) => {
              const Icon = tabIcons[tabId];
              return (
                <button
                  key={tabId}
                  onClick={() => setActiveTab(tabId)}
                  className={cn(
                    "flex items-center gap-2 px-4 py-3 text-sm font-medium transition-all",
                    activeTab === tabId
                      ? "border-b-2 border-teal text-teal"
                      : "text-muted-foreground hover:text-foreground"
                  )}
                >
                  <Icon className="h-4 w-4" />
                  {t(`tabs.${tabId}`)}
                </button>
              );
            })}
          </div>
        </div>
      </div>

      {/* Content */}
      <div className="px-16 py-8">
        {activeTab === "overview" && (
          <div className="space-y-6">
            {/* Stats Cards */}
            <div className="grid grid-cols-1 gap-4 md:grid-cols-4">
              <div className="rounded-xl border border-border bg-card p-5">
                <p className="mb-2 text-sm text-muted-foreground">{t("stats.teamMembers")}</p>
                <p className="mb-1 text-3xl font-semibold text-foreground">
                  {membersList.length}
                </p>
                <p className="text-xs text-muted-foreground">{t("stats.activeCollaborators")}</p>
              </div>
              <div className="rounded-xl border border-border bg-card p-5">
                <p className="mb-2 text-sm text-muted-foreground">Papers</p>
                <p className="mb-1 text-3xl font-semibold text-foreground">
                  {papersList.length}
                </p>
                <p className="text-xs text-muted-foreground">Associated papers</p>
              </div>
              <div className="rounded-xl border border-border bg-card p-5">
                <p className="mb-2 text-sm text-muted-foreground">Views</p>
                <p className="mb-1 text-3xl font-semibold text-foreground">
                  {stats?.viewsCount ?? study.metrics.viewsCount}
                </p>
                <p className="text-xs text-muted-foreground">Total views</p>
              </div>
              <div className="rounded-xl border border-border bg-card p-5">
                <p className="mb-2 text-sm text-muted-foreground">Stars</p>
                <p className="mb-1 text-3xl font-semibold text-foreground">
                  {stats?.starsCount ?? study.metrics.starsCount}
                </p>
                <p className="text-xs text-muted-foreground">Favorites</p>
              </div>
            </div>

            {/* Study Description */}
            <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
              <div className="rounded-xl border border-border bg-card p-6 lg:col-span-2">
                <h3 className="mb-4 text-base font-semibold text-foreground">
                  {t("overview.studyDescription")}
                </h3>
                <p className="mb-6 text-sm leading-relaxed text-muted-foreground">
                  {study.description || "No description provided."}
                </p>
                <div className="space-y-3">
                  <div className="flex items-center justify-between border-b border-border py-2">
                    <span className="text-sm text-muted-foreground">
                      {t("overview.researchField")}
                    </span>
                    <span className="text-sm font-medium text-foreground">
                      {study.researchFieldName}
                    </span>
                  </div>
                  {study.principalInvestigator && (
                    <div className="flex items-center justify-between border-b border-border py-2">
                      <span className="text-sm text-muted-foreground">
                        {t("overview.principalInvestigator")}
                      </span>
                      <span className="text-sm font-medium text-foreground">
                        {study.principalInvestigator}
                      </span>
                    </div>
                  )}
                  {study.institution && (
                    <div className="flex items-center justify-between border-b border-border py-2">
                      <span className="text-sm text-muted-foreground">
                        {t("overview.institution")}
                      </span>
                      <span className="text-sm font-medium text-foreground">
                        {study.institution}
                      </span>
                    </div>
                  )}
                  <div className="flex items-center justify-between border-b border-border py-2">
                    <span className="text-sm text-muted-foreground">
                      {t("overview.createdDate")}
                    </span>
                    <span className="text-sm font-medium text-foreground">
                      {new Date(study.createdAt).toLocaleDateString(undefined, {
                        year: "numeric",
                        month: "long",
                        day: "numeric",
                      })}
                    </span>
                  </div>
                </div>
              </div>

              {/* Tags */}
              <div className="rounded-xl border border-border bg-card p-6">
                <h3 className="mb-4 text-base font-semibold text-foreground">Tags</h3>
                {study.tags.length > 0 ? (
                  <div className="flex flex-wrap gap-2">
                    {study.tags.map((tag) => (
                      <span
                        key={tag}
                        className="rounded-md bg-muted px-3 py-1.5 text-sm text-foreground/70"
                      >
                        {tag}
                      </span>
                    ))}
                  </div>
                ) : (
                  <p className="text-sm text-muted-foreground">No tags added</p>
                )}
              </div>
            </div>
          </div>
        )}

        {activeTab === "team" && (
          <div className="space-y-6">
            <div className="flex items-center justify-between">
              <div>
                <h2 className="text-xl font-semibold text-foreground">{t("team.title")}</h2>
                <p className="mt-1 text-sm text-muted-foreground">{t("team.description")}</p>
              </div>
              <button
                onClick={() => setInviteMemberOpen(true)}
                className="flex items-center gap-2 rounded-lg bg-teal px-4 py-2 text-white transition-all hover:bg-teal/90"
              >
                <UserPlus className="h-4 w-4" />
                {t("team.inviteMember")}
              </button>
            </div>

            <div className="overflow-hidden rounded-xl border border-border bg-card">
              <table className="w-full">
                <thead>
                  <tr className="border-b border-border bg-muted/30">
                    <th className="px-6 py-3 text-left text-xs font-medium uppercase text-muted-foreground">
                      {t("team.member")}
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium uppercase text-muted-foreground">
                      {t("team.role")}
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium uppercase text-muted-foreground">
                      {t("team.email")}
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium uppercase text-muted-foreground">
                      Joined
                    </th>
                    <th className="px-6 py-3 text-left text-xs font-medium uppercase text-muted-foreground">
                      {t("table.actions")}
                    </th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-border">
                  {membersList.map((member) => (
                    <tr key={member.userId} className="transition-colors hover:bg-muted/20">
                      <td className="px-6 py-4">
                        <div className="flex items-center gap-3">
                          {member.userAvatarUrl ? (
                            <img
                              src={member.userAvatarUrl}
                              alt={member.userName}
                              className="h-10 w-10 rounded-full object-cover"
                            />
                          ) : (
                            <div className="flex h-10 w-10 flex-shrink-0 items-center justify-center rounded-full bg-gradient-to-br from-teal to-blue-deep text-sm font-medium text-white">
                              {member.userName.charAt(0).toUpperCase()}
                            </div>
                          )}
                          <span className="text-sm font-medium text-foreground">
                            {member.userName}
                          </span>
                        </div>
                      </td>
                      <td className="px-6 py-4">
                        <span
                          className={cn(
                            "rounded-full px-2.5 py-1 text-xs font-medium",
                            member.roleName === "owner"
                              ? "bg-amber-500/10 text-amber-600"
                              : member.roleName === "admin"
                              ? "bg-purple-500/10 text-purple-600"
                              : "bg-muted text-foreground"
                          )}
                        >
                          {member.roleName.charAt(0).toUpperCase() + member.roleName.slice(1)}
                        </span>
                      </td>
                      <td className="px-6 py-4">
                        <span className="text-sm text-muted-foreground">{member.userEmail}</span>
                      </td>
                      <td className="px-6 py-4">
                        <span className="text-sm text-muted-foreground">
                          {new Date(member.joinedAt).toLocaleDateString()}
                        </span>
                      </td>
                      <td className="px-6 py-4">
                        {member.roleName !== "owner" && (
                          <div className="flex items-center gap-2">
                            <button
                              onClick={() => {
                                setSelectedMember(member);
                                setEditMemberOpen(true);
                              }}
                              className="rounded-md p-1.5 text-muted-foreground transition-all hover:bg-muted hover:text-foreground"
                            >
                              <Edit className="h-4 w-4" />
                            </button>
                            <button
                              onClick={() => {
                                setSelectedMember(member);
                                setDeleteMemberOpen(true);
                              }}
                              className="rounded-md p-1.5 text-muted-foreground transition-all hover:bg-red-500/10 hover:text-red-500"
                            >
                              <Trash2 className="h-4 w-4" />
                            </button>
                          </div>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        )}

        {activeTab === "settings" && (
          <div className="max-w-3xl space-y-6">
            <div>
              <h2 className="text-xl font-semibold text-foreground">{t("settings.title")}</h2>
              <p className="mt-1 text-sm text-muted-foreground">{t("settings.description")}</p>
            </div>

            <div className="space-y-6 rounded-xl border border-border bg-card p-6">
              <div>
                <label className="mb-2 block text-sm font-medium text-foreground">
                  {t("settings.studyName")}
                </label>
                <input
                  type="text"
                  defaultValue={study.title}
                  className="w-full rounded-lg border border-border bg-background px-4 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-teal/20"
                />
              </div>
              <div>
                <label className="mb-2 block text-sm font-medium text-foreground">
                  {t("settings.studyDescription")}
                </label>
                <textarea
                  rows={4}
                  defaultValue={study.description ?? ""}
                  className="w-full resize-none rounded-lg border border-border bg-background px-4 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-teal/20"
                />
              </div>
              <div className="border-t border-border pt-4">
                <button
                  onClick={handleSaveSettings}
                  disabled={updateStudyMutation.isPending}
                  className="rounded-lg bg-teal px-4 py-2 text-white transition-all hover:bg-teal/90"
                >
                  {saveSuccess ? t("settings.saved") : t("settings.saveChanges")}
                </button>
              </div>
            </div>

            <div className="rounded-xl border border-red-500/30 bg-card p-6">
              <h3 className="mb-2 text-base font-semibold text-red-500">
                {t("settings.dangerZone")}
              </h3>
              <p className="mb-4 text-sm text-muted-foreground">{t("settings.dangerZoneDesc")}</p>
              <button
                onClick={() => setDeleteStudyOpen(true)}
                className="rounded-lg border border-red-500 px-4 py-2 text-red-500 transition-all hover:bg-red-500/10"
              >
                {t("settings.deleteStudy")}
              </button>
            </div>
          </div>
        )}

        {/* Placeholder for other tabs */}
        {activeTab === "traces" && (
          <div className="flex h-64 items-center justify-center text-muted-foreground">
            Traces tab - Coming soon
          </div>
        )}
        {activeTab === "analysis" && (
          <div className="flex h-64 items-center justify-center text-muted-foreground">
            Analysis tab - Coming soon
          </div>
        )}
      </div>

      {/* Dialogs */}
      <Dialog open={shareOpen} onOpenChange={setShareOpen}>
        <DialogContent className="sm:max-w-[480px]">
          <DialogHeader>
            <DialogTitle>{t("dialogs.share.title")}</DialogTitle>
            <DialogDescription>{t("dialogs.share.description")}</DialogDescription>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div className="flex items-center gap-2">
              <input
                type="text"
                value={typeof window !== "undefined" ? window.location.href : ""}
                className="flex-1 rounded-lg border border-border bg-muted/50 px-3.5 py-2.5 font-mono text-sm"
                readOnly
              />
              <button
                onClick={handleCopyLink}
                className="flex items-center gap-2 rounded-lg bg-teal px-4 py-2.5 text-sm font-medium text-white"
              >
                {linkCopied ? <Check className="h-4 w-4" /> : <Copy className="h-4 w-4" />}
                {linkCopied ? t("dialogs.share.copied") : t("dialogs.share.copy")}
              </button>
            </div>
          </div>
          <DialogFooter>
            <button
              onClick={() => setShareOpen(false)}
              className="rounded-lg px-4 py-2.5 text-sm font-medium hover:bg-muted"
            >
              {t("dialogs.share.done")}
            </button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog open={exportOpen} onOpenChange={setExportOpen}>
        <DialogContent className="sm:max-w-[420px]">
          <DialogHeader>
            <DialogTitle>{t("dialogs.export.title")}</DialogTitle>
            <DialogDescription>{t("dialogs.export.description")}</DialogDescription>
          </DialogHeader>
          <div className="space-y-3 py-4">
            {[
              { key: "pdfReport", value: "PDF Report" },
              { key: "csvData", value: "CSV Data" },
              { key: "jsonFormat", value: "JSON Format" },
            ].map((format) => (
              <button
                key={format.key}
                onClick={() => handleExportFormat(format.value)}
                className="group flex w-full items-center justify-between rounded-lg border border-border px-4 py-3.5 transition-all hover:border-teal/30 hover:bg-muted/50"
              >
                <div className="flex items-center gap-3">
                  <div className="rounded-lg bg-muted p-2 group-hover:bg-teal/10">
                    <FileText className="h-4 w-4 text-muted-foreground group-hover:text-teal" />
                  </div>
                  <span className="font-medium">{t(`dialogs.export.${format.key}`)}</span>
                </div>
                <Download className="h-4 w-4 text-muted-foreground" />
              </button>
            ))}
          </div>
        </DialogContent>
      </Dialog>

      <Dialog open={inviteMemberOpen} onOpenChange={setInviteMemberOpen}>
        <DialogContent className="sm:max-w-[480px]">
          <DialogHeader>
            <DialogTitle>{t("dialogs.inviteMember.title")}</DialogTitle>
            <DialogDescription>{t("dialogs.inviteMember.description")}</DialogDescription>
          </DialogHeader>
          <div className="space-y-5 py-4">
            <div className="space-y-2">
              <label className="text-sm font-medium">{t("dialogs.inviteMember.emailAddress")}</label>
              <div className="relative">
                <Mail className="absolute left-3.5 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                <input
                  type="email"
                  value={inviteForm.email}
                  onChange={(e) => setInviteForm((f) => ({ ...f, email: e.target.value }))}
                  className="w-full rounded-lg border border-border py-2.5 pl-10 pr-3.5 text-sm"
                  placeholder={t("dialogs.inviteMember.emailPlaceholder")}
                />
              </div>
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">{t("dialogs.inviteMember.role")}</label>
              <select
                value={inviteForm.roleId}
                onChange={(e) =>
                  setInviteForm((f) => ({ ...f, roleId: Number(e.target.value) as 2 | 3 | 4 }))
                }
                className="w-full rounded-lg border border-border px-3.5 py-2.5 text-sm"
              >
                <option value={2}>Admin</option>
                <option value={3}>Editor</option>
                <option value={4}>Viewer</option>
              </select>
            </div>
          </div>
          <DialogFooter>
            <button
              onClick={() => setInviteMemberOpen(false)}
              className="rounded-lg px-4 py-2.5 text-sm font-medium hover:bg-muted"
            >
              {t("dialogs.inviteMember.cancel")}
            </button>
            <button
              onClick={handleSendInvitation}
              disabled={!inviteForm.email || sendInvitationMutation.isPending}
              className="rounded-lg bg-teal px-4 py-2.5 text-sm font-medium text-white disabled:opacity-50"
            >
              {sendInvitationMutation.isPending ? (
                <Loader2 className="h-4 w-4 animate-spin" />
              ) : (
                t("dialogs.inviteMember.sendInvitation")
              )}
            </button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog open={deleteMemberOpen} onOpenChange={setDeleteMemberOpen}>
        <DialogContent className="sm:max-w-[420px]">
          <DialogHeader>
            <DialogTitle>{t("dialogs.deleteMember.title")}</DialogTitle>
            <DialogDescription>
              {t("dialogs.deleteMember.description", { name: selectedMember?.userName ?? "" })}
            </DialogDescription>
          </DialogHeader>
          <DialogFooter className="mt-4">
            <button
              onClick={() => setDeleteMemberOpen(false)}
              className="rounded-lg px-4 py-2.5 text-sm font-medium hover:bg-muted"
            >
              {t("dialogs.deleteMember.cancel")}
            </button>
            <button
              onClick={handleRemoveMember}
              disabled={removeStudyMemberMutation.isPending}
              className="rounded-lg bg-red-500 px-4 py-2.5 text-sm font-medium text-white hover:bg-red-500/90 disabled:opacity-50"
            >
              {removeStudyMemberMutation.isPending ? (
                <Loader2 className="h-4 w-4 animate-spin" />
              ) : (
                t("dialogs.deleteMember.removeMember")
              )}
            </button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog open={deleteStudyOpen} onOpenChange={setDeleteStudyOpen}>
        <DialogContent className="sm:max-w-[420px]">
          <DialogHeader>
            <DialogTitle className="text-red-500">{t("dialogs.deleteStudy.title")}</DialogTitle>
            <DialogDescription>{t("dialogs.deleteStudy.description")}</DialogDescription>
          </DialogHeader>
          <DialogFooter className="mt-4">
            <button
              onClick={() => setDeleteStudyOpen(false)}
              className="rounded-lg px-4 py-2.5 text-sm font-medium hover:bg-muted"
            >
              {t("dialogs.deleteStudy.cancel")}
            </button>
            <button
              onClick={handleDeleteStudy}
              disabled={deleteStudyMutation.isPending}
              className="rounded-lg bg-red-500 px-4 py-2.5 text-sm font-medium text-white hover:bg-red-500/90 disabled:opacity-50"
            >
              {deleteStudyMutation.isPending ? (
                <Loader2 className="h-4 w-4 animate-spin" />
              ) : (
                t("dialogs.deleteStudy.delete")
              )}
            </button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* AI Assistant Chat Bubble */}
      <button
        onClick={() => setAiAssistantOpen(!aiAssistantOpen)}
        className={cn(
          "fixed bottom-6 z-40 flex h-14 w-14 items-center justify-center rounded-full shadow-lg transition-all duration-300 hover:scale-105 hover:shadow-xl",
          aiAssistantOpen
            ? "right-[416px] bg-muted/90 text-foreground backdrop-blur-sm hover:bg-muted"
            : "right-6 bg-gradient-to-r from-teal to-blue-deep text-white"
        )}
        aria-label={aiAssistantOpen ? "Close AI Assistant" : "Open AI Assistant"}
      >
        {aiAssistantOpen ? <X className="h-6 w-6" /> : <Sparkles className="h-6 w-6" />}
      </button>

      {/* AI Assistant */}
      <AIAssistant
        isOpen={aiAssistantOpen}
        onClose={() => setAiAssistantOpen(false)}
        contextType="study"
        contextTitle={study.title}
        contextId={study.id}
      />
    </div>
  );
}
