"use client";

import { useState, useEffect } from "react";
import { useTranslations } from "next-intl";
import { Link } from "@/lib/navigation";
import { useParams, useSearchParams } from "next/navigation";
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
  Workflow,
  BookOpen,
  History,
  MessageSquare,
} from "lucide-react";
import { cn } from "@/lib/utils";
import { AIAssistant } from "@/components/shared";
import { PapersTab } from "@/components/studies/PapersTab";
import { StudyTimelineTab } from "@/components/studies/study-timeline-tab";
import { DiscussionsTab } from "@/components/discussions/discussions-tab";
import { PipelinesTab } from "@/components/pipelines";
import { TraceProcessingSubscriber } from "@/components/traces/trace-processing-subscriber";
import { StarButton } from "@/components/social/star-button";
import { PinStudyButton } from "@/components/social/pin-study-button";
import { FollowButton } from "@/components/social/follow-button";
import { ReadmeSection } from "@/components/studies/readme-section";
import { useAuthStore } from "@/stores/auth-store";
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
  useStudyTraces,
  useTraceCounts,
  useUploadTrace,
  useDeleteTrace,
  useRetryTraceProcessing,
  useStudyPermissions,
  useExportStudy,
} from "@/hooks";
import { profileService } from "@/services/profile.service";
import type {
  Study,
  StudyMember,
  UpdateStudyInput,
  SendInvitationInput,
  TraceSummary,
  TraceFilters,
} from "@/types";

const tabIds = ["overview", "traces", "pipelines", "papers", "discussions", "timeline", "team", "settings"] as const;
const tabIcons = {
  overview: Beaker,
  traces: FileText,
  pipelines: Workflow,
  papers: BookOpen,
  discussions: MessageSquare,
  timeline: History,
  team: Users,
  settings: SettingsIcon,
};

export default function StudyDetailPage() {
  const t = useTranslations("studies.detail");
  const tCommon = useTranslations("common");
  const params = useParams();
  const searchParams = useSearchParams();
  const studyId = params.studyId as string;
  const currentUserId = useAuthStore((s) => s.user?.id);

  // UI State - initialise activeTab from ?tab= query param when present
  // The "analysis" tab was removed in favour of the trace-level analysis
  // panel. Old bookmarks with `?tab=analysis` are silently redirected to the
  // overview tab so the page does not render an empty content area.
  const requestedTab = searchParams.get("tab");
  const initialTab =
    requestedTab && (tabIds as readonly string[]).includes(requestedTab)
      ? requestedTab
      : "overview";
  const [activeTab, setActiveTab] = useState(initialTab);
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

  // Settings form state
  const [settingsForm, setSettingsForm] = useState({
    title: "",
    description: "",
  });
  const [editMemberRole, setEditMemberRole] = useState<number>(4);

  // Traces state
  const [tracesPage, setTracesPage] = useState(1);
  const [tracesFilters, setTracesFilters] = useState<TraceFilters>({});
  const [uploadFiles, setUploadFiles] = useState<File[]>([]);
  const [isUploading, setIsUploading] = useState(false);

  // Queries
  const { data: study, isLoading, error } = useStudy(studyId);
  const {
    isMember,
    isPublicView,
    canManageMembers,
    canEditStudy,
    canEditContent,
    canChangeStatus,
    canDeleteStudy,
    visibleTabs,
  } = useStudyPermissions(study);
  const { data: members } = useStudyMembers(studyId);
  const { data: papers } = useStudyPapers(studyId);
  const { data: stats } = useStudyStats(studyId);
  const { data: isStarredData } = useIsStudyStarred(studyId);
  const { data: tracesData, isLoading: tracesLoading } = useStudyTraces(studyId, tracesPage, 10, tracesFilters);
  const { data: traceCounts } = useTraceCounts(studyId);

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
  const exportStudyMutation = useExportStudy();
  const uploadTraceMutation = useUploadTrace(studyId);
  const deleteTraceMutation = useDeleteTrace(studyId);
  const retryTraceMutation = useRetryTraceProcessing(studyId);

  // Initialize settings form when study loads
  useEffect(() => {
    if (study) {
      setSettingsForm({
        title: study.title,
        description: study.description ?? "",
      });
    }
  }, [study]);

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
    if (!study) return;
    try {
      await updateStudyMutation.mutateAsync({
        title: settingsForm.title,
        description: settingsForm.description || undefined,
        researchFieldId: study.researchFieldId,
      });
      setSaveSuccess(true);
      setTimeout(() => setSaveSuccess(false), 2000);
    } catch (err) {
      console.error("Failed to save settings:", err);
    }
  };

  const handleChangeMemberRole = async () => {
    if (selectedMember) {
      try {
        await changeMemberRoleMutation.mutateAsync({
          userId: selectedMember.userId,
          input: { newRoleId: editMemberRole as 2 | 3 | 4 },
        });
        setEditMemberOpen(false);
        setSelectedMember(null);
      } catch (err) {
        console.error("Failed to change member role:", err);
      }
    }
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

  const handleUploadTraces = async () => {
    if (uploadFiles.length === 0) return;
    setIsUploading(true);
    try {
      for (const file of uploadFiles) {
        await uploadTraceMutation.mutateAsync({
          studyId,
          name: file.name.replace(/\.[^/.]+$/, ""),
          file,
        });
      }
      setUploadTracesOpen(false);
      setUploadFiles([]);
    } catch (err) {
      console.error("Failed to upload traces:", err);
    } finally {
      setIsUploading(false);
    }
  };

  const handleDeleteTrace = async (traceId: string) => {
    try {
      await deleteTraceMutation.mutateAsync(traceId);
    } catch (err) {
      console.error("Failed to delete trace:", err);
    }
  };

  const handleRetryTrace = async (traceId: string) => {
    try {
      await retryTraceMutation.mutateAsync(traceId);
    } catch (err) {
      console.error("Failed to retry trace:", err);
    }
  };

  const formatFileSize = (bytes: number) => {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
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
                  {tCommon(`status.${study.statusName.toLowerCase()}`)}
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
                    {study.ownerId ? (
                      <Link
                        href={`/users/${study.ownerId}` as never}
                        className="hover:text-foreground hover:underline"
                      >
                        {study.principalInvestigator}
                      </Link>
                    ) : (
                      <span>{study.principalInvestigator}</span>
                    )}
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
              <StarButton studyId={studyId} showCount variant="outline" />
              <PinStudyButton studyId={studyId} variant="outline" />
              {/* TODO: Re-enable when AI Assistant feature is ready */}
              <button
                // onClick={() => setAiAssistantOpen(true)}
                disabled
                className="flex items-center gap-2 rounded-lg bg-gradient-to-r from-teal to-blue-deep px-4 py-2 text-sm font-medium text-white transition-all opacity-50 cursor-not-allowed"
                title="Coming soon"
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
              {/* Export - only for members. Single-click ZIP download. */}
              {isMember && (
                <button
                  onClick={() => exportStudyMutation.mutate(studyId)}
                  disabled={exportStudyMutation.isPending}
                  className="flex items-center gap-2 rounded-lg border border-border px-4 py-2 text-sm font-medium text-foreground transition-all hover:bg-muted/50 disabled:cursor-not-allowed disabled:opacity-60"
                >
                  {exportStudyMutation.isPending ? (
                    <Loader2 className="h-4 w-4 animate-spin" />
                  ) : (
                    <Download className="h-4 w-4" />
                  )}
                  {exportStudyMutation.isPending
                    ? t("dialogs.export.exporting")
                    : t("export")}
                </button>
              )}
            </div>
          </div>

          {/* Tabs - filtered based on user permissions */}
          <div className="flex items-center gap-1">
            {visibleTabs.map((tabId) => {
              const Icon = tabIcons[tabId as keyof typeof tabIcons];
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
                <p className="mb-2 text-sm text-muted-foreground">{t("stats.papersList")}</p>
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

            {/* README */}
            <ReadmeSection
              studyId={studyId}
              readmeMarkdown={study.readmeMarkdown}
              canEdit={study.currentUserPermissions.canEditStudy}
            />
          </div>
        )}

        {activeTab === "team" && (
          <div className="space-y-6">
            <div className="flex items-center justify-between">
              <div>
                <h2 className="text-xl font-semibold text-foreground">{t("team.title")}</h2>
                <p className="mt-1 text-sm text-muted-foreground">{t("team.description")}</p>
              </div>
              {/* Invite button - only for users who can manage members */}
              {canManageMembers && (
                <button
                  onClick={() => setInviteMemberOpen(true)}
                  className="flex items-center gap-2 rounded-lg bg-teal px-4 py-2 text-white transition-all hover:bg-teal/90"
                >
                  <UserPlus className="h-4 w-4" />
                  {t("team.inviteMember")}
                </button>
              )}
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
                              src={profileService.resolveStorageUrl(member.userAvatarUrl) || undefined}
                              alt={member.userName ?? "User"}
                              className="h-10 w-10 rounded-full object-cover"
                            />
                          ) : (
                            <div className="flex h-10 w-10 flex-shrink-0 items-center justify-center rounded-full bg-gradient-to-br from-teal to-blue-deep text-sm font-medium text-white">
                              {(member.userName ?? member.userEmail ?? "U").charAt(0).toUpperCase()}
                            </div>
                          )}
                          {member.userId ? (
                            <Link
                              href={`/users/${member.userId}` as never}
                              className="text-sm font-medium text-foreground hover:text-teal hover:underline"
                            >
                              {member.userName ?? member.userEmail ?? "Unknown"}
                            </Link>
                          ) : (
                            <span className="text-sm font-medium text-foreground">
                              {member.userName ?? member.userEmail ?? "Unknown"}
                            </span>
                          )}
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
                          {member.roleName ? member.roleName.charAt(0).toUpperCase() + member.roleName.slice(1) : "Member"}
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
                        <div className="flex items-center gap-2">
                          {/* Follow button — hidden by component for self */}
                          {member.userId && (
                            <FollowButton
                              userId={member.userId}
                              currentUserId={currentUserId}
                              size="sm"
                            />
                          )}
                          {/* Member actions - only for users who can manage members, not for owner */}
                          {canManageMembers && member.roleName !== "owner" && (
                            <>
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
                            </>
                          )}
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        )}

        {activeTab === "settings" && (
          <div className="mx-auto max-w-3xl space-y-6">
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
                  value={settingsForm.title}
                  onChange={(e) => setSettingsForm((f) => ({ ...f, title: e.target.value }))}
                  className="w-full rounded-lg border border-border bg-background px-4 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-teal/20"
                />
              </div>
              <div>
                <label className="mb-2 block text-sm font-medium text-foreground">
                  {t("settings.studyDescription")}
                </label>
                <textarea
                  rows={4}
                  value={settingsForm.description}
                  onChange={(e) => setSettingsForm((f) => ({ ...f, description: e.target.value }))}
                  className="w-full resize-none rounded-lg border border-border bg-background px-4 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-teal/20"
                />
              </div>
              <div className="border-t border-border pt-4">
                <button
                  onClick={handleSaveSettings}
                  disabled={updateStudyMutation.isPending || !settingsForm.title.trim()}
                  className="rounded-lg bg-teal px-4 py-2 text-white transition-all hover:bg-teal/90 disabled:opacity-50"
                >
                  {updateStudyMutation.isPending ? (
                    <Loader2 className="h-4 w-4 animate-spin" />
                  ) : saveSuccess ? (
                    t("settings.saved")
                  ) : (
                    t("settings.saveChanges")
                  )}
                </button>
              </div>
            </div>

            {/* Danger Zone - only visible to owners */}
            {canDeleteStudy && (
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
            )}
          </div>
        )}

        {/* Traces Tab */}
        {activeTab === "traces" && (
          <div className="space-y-6">
            {/* Header with stats and upload button */}
            <div className="flex items-center justify-between">
              <div>
                <h2 className="text-xl font-semibold text-foreground">{t("traces.title")}</h2>
                <p className="mt-1 text-sm text-muted-foreground">{t("traces.description")}</p>
              </div>
              {/* Upload button - only for users who can edit content */}
              {canEditContent && (
                <button
                  onClick={() => setUploadTracesOpen(true)}
                  className="flex items-center gap-2 rounded-lg bg-teal px-4 py-2 text-white transition-all hover:bg-teal/90"
                >
                  <Upload className="h-4 w-4" />
                  {t("traces.uploadTraces")}
                </button>
              )}
            </div>

            {/* Stats cards */}
            {traceCounts && (
              <div className="grid grid-cols-2 gap-4 md:grid-cols-4 lg:grid-cols-6">
                <div className="rounded-lg border border-border bg-card p-4">
                  <p className="text-2xl font-semibold text-foreground">{traceCounts.total}</p>
                  <p className="text-xs text-muted-foreground">{t("traces.stats.total")}</p>
                </div>
                <div className="rounded-lg border border-border bg-card p-4">
                  <p className="text-2xl font-semibold text-emerald-500">{traceCounts.processed}</p>
                  <p className="text-xs text-muted-foreground">{t("traces.stats.processed")}</p>
                </div>
                <div className="rounded-lg border border-border bg-card p-4">
                  <p className="text-2xl font-semibold text-blue-500">{traceCounts.processing}</p>
                  <p className="text-xs text-muted-foreground">{t("traces.stats.processing")}</p>
                </div>
                <div className="rounded-lg border border-border bg-card p-4">
                  <p className="text-2xl font-semibold text-amber-500">{traceCounts.uploaded}</p>
                  <p className="text-xs text-muted-foreground">{t("traces.stats.pending")}</p>
                </div>
                <div className="rounded-lg border border-border bg-card p-4">
                  <p className="text-2xl font-semibold text-red-500">{traceCounts.failed}</p>
                  <p className="text-xs text-muted-foreground">{t("traces.stats.failed")}</p>
                </div>
                <div className="rounded-lg border border-border bg-card p-4">
                  <p className="text-2xl font-semibold text-muted-foreground">{traceCounts.archived}</p>
                  <p className="text-xs text-muted-foreground">{t("traces.stats.archived")}</p>
                </div>
              </div>
            )}

            {/* Search & Filters */}
            <div className="flex items-center gap-3">
              <div className="relative flex-1">
                <input
                  type="text"
                  placeholder={t("traces.searchPlaceholder")}
                  value={tracesFilters.searchTerm || ""}
                  onChange={(e) => setTracesFilters((f) => ({ ...f, searchTerm: e.target.value }))}
                  className="w-full rounded-lg border border-border bg-background px-4 py-2 pl-10 text-sm focus:outline-none focus:ring-2 focus:ring-teal/20"
                />
                <Eye className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              </div>
              <select
                value={tracesFilters.statusId || ""}
                onChange={(e) =>
                  setTracesFilters((f) => ({
                    ...f,
                    statusId: e.target.value ? Number(e.target.value) : undefined,
                  }))
                }
                className="rounded-lg border border-border bg-background px-4 py-2 text-sm"
              >
                <option value="">{t("traces.filters.allStatuses")}</option>
                <option value="1">{t("traces.filters.uploaded")}</option>
                <option value="3">{t("traces.filters.processing")}</option>
                <option value="4">{t("traces.filters.processed")}</option>
                <option value="5">{t("traces.filters.failed")}</option>
              </select>
            </div>

            {/* Traces table */}
            {tracesLoading ? (
              <div className="flex h-64 items-center justify-center">
                <Loader2 className="h-8 w-8 animate-spin text-teal" />
              </div>
            ) : !tracesData?.items?.length ? (
              <div className="flex h-64 flex-col items-center justify-center gap-4 rounded-xl border border-dashed border-border">
                <FileText className="h-12 w-12 text-muted-foreground" />
                <div className="text-center">
                  <p className="font-medium text-foreground">{t("traces.noTraces")}</p>
                  <p className="text-sm text-muted-foreground">{t("traces.noTracesDesc")}</p>
                </div>
                {canEditContent && (
                  <button
                    onClick={() => setUploadTracesOpen(true)}
                    className="flex items-center gap-2 rounded-lg bg-teal px-4 py-2 text-white"
                  >
                    <Upload className="h-4 w-4" />
                    {t("traces.uploadFirst")}
                  </button>
                )}
              </div>
            ) : (
              <div className="overflow-hidden rounded-xl border border-border bg-card">
                {/* Open one SSE subscription per visible trace that is still
                    in a non-terminal state. The headless subscriber invalidates
                    ["traces", studyId] on every frame and on connect, so this
                    list refetches itself live. As soon as a trace reaches
                    Processed/Failed/Archived it falls out of the filter and
                    the connection is disposed automatically. */}
                {tracesData.items
                  .filter((t: TraceSummary) => {
                    const s = (t.status ?? "").toLowerCase();
                    return s === "uploaded" || s === "validating" || s === "processing";
                  })
                  .map((t: TraceSummary) => (
                    <TraceProcessingSubscriber
                      key={`sse-${t.id}`}
                      traceId={t.id}
                      studyId={studyId}
                    />
                  ))}
                <table className="w-full">
                  <thead>
                    <tr className="border-b border-border bg-muted/30">
                      <th className="px-6 py-3 text-left text-xs font-medium uppercase text-muted-foreground">
                        {t("traces.table.name")}
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium uppercase text-muted-foreground">
                        {t("traces.table.format")}
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium uppercase text-muted-foreground">
                        {t("traces.table.size")}
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium uppercase text-muted-foreground">
                        {t("traces.table.status")}
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium uppercase text-muted-foreground">
                        {t("traces.table.quality")}
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium uppercase text-muted-foreground">
                        {t("traces.table.uploaded")}
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium uppercase text-muted-foreground">
                        {t("table.actions")}
                      </th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-border">
                    {tracesData.items.map((trace: TraceSummary) => (
                      <tr key={trace.id} className="transition-colors hover:bg-muted/20">
                        <td className="px-6 py-4">
                          <Link
                            href={`/traces/${trace.id}?studyId=${studyId}`}
                            className="font-medium text-foreground hover:text-teal"
                          >
                            {trace.name}
                          </Link>
                          {trace.description && (
                            <p className="mt-0.5 text-xs text-muted-foreground">{trace.description}</p>
                          )}
                        </td>
                        <td className="px-6 py-4">
                          <span className="rounded bg-muted px-2 py-1 text-xs font-medium uppercase">
                            {trace.format}
                          </span>
                        </td>
                        <td className="px-6 py-4 text-sm text-muted-foreground">
                          {formatFileSize(trace.sizeBytes)}
                        </td>
                        <td className="px-6 py-4">
                          {(() => {
                            // Backend serialises trace.Status.Name as PascalCase
                            // ("Processed", "Processing", ...). Lowercase here so
                            // the badge styling matches and the displayed label is
                            // case-consistent with the rest of the UI.
                            const statusLower = (trace.status ?? "").toLowerCase();
                            return (
                              <span
                                className={cn(
                                  "rounded-full px-2.5 py-1 text-xs font-medium",
                                  statusLower === "processed" && "bg-emerald-500/10 text-emerald-500",
                                  statusLower === "processing" && "bg-blue-500/10 text-blue-500",
                                  statusLower === "validating" && "bg-blue-500/10 text-blue-500",
                                  statusLower === "uploaded" && "bg-amber-500/10 text-amber-500",
                                  statusLower === "failed" && "bg-red-500/10 text-red-500",
                                  statusLower === "archived" && "bg-muted text-muted-foreground"
                                )}
                              >
                                {trace.status}
                              </span>
                            );
                          })()}
                        </td>
                        <td className="px-6 py-4 text-sm">
                          {trace.averageQualityScore ? (
                            <span
                              className={cn(
                                "font-medium",
                                trace.averageQualityScore >= 30
                                  ? "text-emerald-500"
                                  : trace.averageQualityScore >= 20
                                  ? "text-amber-500"
                                  : "text-red-500"
                              )}
                            >
                              Q{trace.averageQualityScore.toFixed(0)}
                            </span>
                          ) : (
                            <span className="text-muted-foreground">-</span>
                          )}
                        </td>
                        <td className="px-6 py-4 text-sm text-muted-foreground">
                          {new Date(trace.createdAt).toLocaleDateString()}
                        </td>
                        <td className="px-6 py-4">
                          <div className="flex items-center gap-2">
                            <Link
                              href={`/traces/${trace.id}?studyId=${studyId}`}
                              className="rounded-md p-1.5 text-muted-foreground transition-all hover:bg-muted hover:text-foreground"
                              title={t("traces.actions.view")}
                            >
                              <Eye className="h-4 w-4" />
                            </Link>
                            {/* Retry - only for editors+ on failed traces */}
                            {canEditContent && (trace.status ?? "").toLowerCase() === "failed" && (
                              <button
                                onClick={() => handleRetryTrace(trace.id)}
                                className="rounded-md p-1.5 text-muted-foreground transition-all hover:bg-blue-500/10 hover:text-blue-500"
                                title={t("traces.actions.retry")}
                              >
                                <Clock className="h-4 w-4" />
                              </button>
                            )}
                            {/* Delete - only for editors+ */}
                            {canEditContent && (
                              <button
                                onClick={() => handleDeleteTrace(trace.id)}
                                className="rounded-md p-1.5 text-muted-foreground transition-all hover:bg-red-500/10 hover:text-red-500"
                                title={t("traces.actions.delete")}
                              >
                                <Trash2 className="h-4 w-4" />
                              </button>
                            )}
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>

                {/* Pagination */}
                {tracesData.totalPages > 1 && (
                  <div className="flex items-center justify-between border-t border-border px-6 py-3">
                    <p className="text-sm text-muted-foreground">
                      {t("traces.showing", {
                        from: (tracesPage - 1) * 10 + 1,
                        to: Math.min(tracesPage * 10, tracesData.totalCount),
                        total: tracesData.totalCount,
                      })}
                    </p>
                    <div className="flex items-center gap-2">
                      <button
                        onClick={() => setTracesPage((p) => Math.max(1, p - 1))}
                        disabled={!tracesData.hasPreviousPage}
                        className="rounded-lg border border-border px-3 py-1.5 text-sm disabled:opacity-50"
                      >
                        {t("traces.previous")}
                      </button>
                      <button
                        onClick={() => setTracesPage((p) => p + 1)}
                        disabled={!tracesData.hasNextPage}
                        className="rounded-lg border border-border px-3 py-1.5 text-sm disabled:opacity-50"
                      >
                        {t("traces.next")}
                      </button>
                    </div>
                  </div>
                )}
              </div>
            )}
          </div>
        )}

        {/* Pipelines Tab */}
        {activeTab === "pipelines" && (
          <PipelinesTab
            studyId={studyId}
            traces={(tracesData?.items ?? []).map((t: TraceSummary) => ({
              id: t.id,
              name: t.name,
              status: t.status,
            }))}
          />
        )}

        {/* Papers Tab */}
        {activeTab === "papers" && (
          <PapersTab
            studyId={studyId}
            canEdit={canEditContent}
          />
        )}

        {/* Discussions Tab — threaded discussions + comments + reactions */}
        {activeTab === "discussions" && <DiscussionsTab studyId={studyId} />}

        {/* Timeline Tab — chronological feed of activity scoped to this study */}
        {activeTab === "timeline" && <StudyTimelineTab studyId={studyId} />}

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
                  className="w-full rounded-lg border border-border bg-background py-2.5 pl-10 pr-3.5 text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring"
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
                className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
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

      {/* Edit Member Role Dialog */}
      <Dialog
        open={editMemberOpen}
        onOpenChange={(open) => {
          setEditMemberOpen(open);
          if (open && selectedMember) {
            // Set initial role based on member's current role
            const roleMap: Record<string, number> = { owner: 1, admin: 2, editor: 3, viewer: 4 };
            setEditMemberRole(roleMap[selectedMember.roleName] || 4);
          }
        }}
      >
        <DialogContent className="sm:max-w-[420px]">
          <DialogHeader>
            <DialogTitle>{t("dialogs.editMember.title")}</DialogTitle>
            <DialogDescription>
              {t("dialogs.editMember.description", { name: selectedMember?.userName ?? "" })}
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <label className="text-sm font-medium">{t("dialogs.inviteMember.role")}</label>
              <select
                value={editMemberRole}
                onChange={(e) => setEditMemberRole(Number(e.target.value))}
                className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm"
              >
                <option value={2}>Admin</option>
                <option value={3}>Editor</option>
                <option value={4}>Viewer</option>
              </select>
            </div>
          </div>
          <DialogFooter>
            <button
              onClick={() => setEditMemberOpen(false)}
              className="rounded-lg px-4 py-2.5 text-sm font-medium hover:bg-muted"
            >
              {t("dialogs.editMember.cancel")}
            </button>
            <button
              onClick={handleChangeMemberRole}
              disabled={changeMemberRoleMutation.isPending}
              className="rounded-lg bg-teal px-4 py-2.5 text-sm font-medium text-white disabled:opacity-50"
            >
              {changeMemberRoleMutation.isPending ? (
                <Loader2 className="h-4 w-4 animate-spin" />
              ) : (
                t("dialogs.editMember.saveChanges")
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

      {/* Upload Traces Dialog */}
      <Dialog open={uploadTracesOpen} onOpenChange={setUploadTracesOpen}>
        <DialogContent className="sm:max-w-[480px]">
          <DialogHeader>
            <DialogTitle>{t("dialogs.uploadTraces.title")}</DialogTitle>
            <DialogDescription>{t("dialogs.uploadTraces.description")}</DialogDescription>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div
              className={cn(
                "flex flex-col items-center justify-center rounded-lg border-2 border-dashed p-8 transition-colors",
                uploadFiles.length > 0
                  ? "border-teal bg-teal/5"
                  : "border-border hover:border-muted-foreground"
              )}
              onDragOver={(e) => {
                e.preventDefault();
                e.stopPropagation();
              }}
              onDrop={(e) => {
                e.preventDefault();
                e.stopPropagation();
                const files = Array.from(e.dataTransfer.files);
                setUploadFiles((prev) => [...prev, ...files]);
              }}
            >
              <Upload className="mb-3 h-8 w-8 text-muted-foreground" />
              <p className="mb-1 text-sm font-medium">{t("dialogs.uploadTraces.dropzone")}</p>
              <p className="mb-3 text-xs text-muted-foreground">{t("dialogs.uploadTraces.formats")}</p>
              <label className="cursor-pointer rounded-lg bg-muted px-4 py-2 text-sm font-medium hover:bg-muted/80">
                {t("dialogs.uploadTraces.browse")}
                <input
                  type="file"
                  multiple
                  accept=".ab1,.abi,.fasta,.fa,.fastq,.fq,.seq,.scf"
                  className="hidden"
                  onChange={(e) => {
                    const files = Array.from(e.target.files || []);
                    setUploadFiles((prev) => [...prev, ...files]);
                  }}
                />
              </label>
            </div>

            {uploadFiles.length > 0 && (
              <div className="max-h-40 space-y-2 overflow-y-auto">
                {uploadFiles.map((file, index) => (
                  <div key={index} className="flex items-center justify-between rounded-lg bg-muted/50 px-3 py-2">
                    <div className="flex items-center gap-2">
                      <FileText className="h-4 w-4 text-muted-foreground" />
                      <span className="text-sm">{file.name}</span>
                      <span className="text-xs text-muted-foreground">({formatFileSize(file.size)})</span>
                    </div>
                    <button
                      onClick={() => setUploadFiles((prev) => prev.filter((_, i) => i !== index))}
                      className="text-muted-foreground hover:text-foreground"
                    >
                      <X className="h-4 w-4" />
                    </button>
                  </div>
                ))}
              </div>
            )}
          </div>
          <DialogFooter>
            <button
              onClick={() => {
                setUploadTracesOpen(false);
                setUploadFiles([]);
              }}
              className="rounded-lg px-4 py-2.5 text-sm font-medium hover:bg-muted"
            >
              {t("dialogs.uploadTraces.cancel")}
            </button>
            <button
              onClick={handleUploadTraces}
              disabled={uploadFiles.length === 0 || isUploading}
              className="rounded-lg bg-teal px-4 py-2.5 text-sm font-medium text-white disabled:opacity-50"
            >
              {isUploading ? (
                <Loader2 className="h-4 w-4 animate-spin" />
              ) : (
                t("dialogs.uploadTraces.upload", { count: uploadFiles.length })
              )}
            </button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* TODO: Re-enable when AI Assistant feature is ready */}
      {/* AI Assistant Chat Bubble */}
      {/* <button
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
      </button> */}

      {/* AI Assistant */}
      {/* <AIAssistant
        isOpen={aiAssistantOpen}
        onClose={() => setAiAssistantOpen(false)}
        contextType="study"
        contextTitle={study.title}
        contextId={study.id}
      /> */}
    </div>
  );
}
