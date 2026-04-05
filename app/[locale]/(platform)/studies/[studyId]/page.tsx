"use client";

import { useState } from "react";
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
  AlertCircle,
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

const studyData = {
  id: "GF-2026-089",
  name: "Genome-Wide Association Study - Type 2 Diabetes Cohort",
  field: "Diabetes Research",
  status: "Active",
  visibility: "Private",
  description:
    "Comprehensive GWAS investigating genetic variants associated with Type 2 Diabetes in a diverse population cohort. This multi-year study aims to identify novel genetic markers and validate known associations.",
  created: "2026-03-10",
  updated: "2 hours ago",
  pi: "Dr. Sarah Martinez",
  institution: "Stanford Medical Center",
  samples: 1247,
  traces: 8934,
  quality: 97.2,
  progress: 73,
};

const members = [
  { name: "Dr. Sarah Martinez", role: "Principal Investigator", email: "s.martinez@stanford.edu", avatar: "SM", status: "Active" },
  { name: "Dr. James Wong", role: "Co-Investigator", email: "j.wong@stanford.edu", avatar: "JW", status: "Active" },
  { name: "Dr. Emily Chen", role: "Research Scientist", email: "e.chen@stanford.edu", avatar: "EC", status: "Active" },
  { name: "Alex Rodriguez", role: "Lab Technician", email: "a.rodriguez@stanford.edu", avatar: "AR", status: "Active" },
  { name: "Dr. Michael Park", role: "Bioinformatician", email: "m.park@stanford.edu", avatar: "MP", status: "Pending" },
];

const traces = [
  { id: "TR-2026-08934", sample: "SMD-T2D-1247", status: "Processed", quality: 98.2, length: "850 bp", uploaded: "2026-03-16 14:23", by: "Dr. Martinez" },
  { id: "TR-2026-08933", sample: "SMD-T2D-1246", status: "Processed", quality: 97.8, length: "845 bp", uploaded: "2026-03-16 14:20", by: "Dr. Martinez" },
  { id: "TR-2026-08932", sample: "SMD-T2D-1245", status: "Processing", quality: null, length: "852 bp", uploaded: "2026-03-16 14:18", by: "A. Rodriguez" },
  { id: "TR-2026-08931", sample: "SMD-T2D-1244", status: "Failed", quality: 82.1, length: "840 bp", uploaded: "2026-03-16 14:15", by: "A. Rodriguez" },
  { id: "TR-2026-08930", sample: "SMD-T2D-1243", status: "Processed", quality: 96.5, length: "848 bp", uploaded: "2026-03-16 14:12", by: "Dr. Chen" },
  { id: "TR-2026-08929", sample: "SMD-T2D-1242", status: "Processed", quality: 97.1, length: "851 bp", uploaded: "2026-03-16 14:10", by: "Dr. Chen" },
  { id: "TR-2026-08928", sample: "SMD-T2D-1241", status: "Review", quality: 95.8, length: "843 bp", uploaded: "2026-03-16 14:08", by: "Dr. Wong" },
  { id: "TR-2026-08927", sample: "SMD-T2D-1240", status: "Processed", quality: 98.5, length: "849 bp", uploaded: "2026-03-16 14:05", by: "Dr. Wong" },
];

const analysisResults = [
  { name: "Variant Calling Analysis", type: "GATK Pipeline", status: "Completed", date: "2026-03-15", variants: "10,847", quality: "High" },
  { name: "Quality Control Report", type: "FastQC", status: "Completed", date: "2026-03-14", variants: "—", quality: "Pass" },
  { name: "Population Structure Analysis", type: "PCA", status: "In Progress", date: "2026-03-16", variants: "—", quality: "—" },
  { name: "Association Testing", type: "PLINK", status: "Queued", date: "—", variants: "—", quality: "—" },
];

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
  const [activeTab, setActiveTab] = useState("overview");
  const [shareOpen, setShareOpen] = useState(false);
  const [exportOpen, setExportOpen] = useState(false);
  const [uploadTracesOpen, setUploadTracesOpen] = useState(false);
  const [inviteMemberOpen, setInviteMemberOpen] = useState(false);
  const [newAnalysisOpen, setNewAnalysisOpen] = useState(false);
  const [aiAssistantOpen, setAiAssistantOpen] = useState(false);
  const [linkCopied, setLinkCopied] = useState(false);
  const [deleteStudyOpen, setDeleteStudyOpen] = useState(false);
  const [viewAnalysisOpen, setViewAnalysisOpen] = useState(false);
  const [selectedAnalysis, setSelectedAnalysis] = useState<typeof analysisResults[0] | null>(null);
  const [editMemberOpen, setEditMemberOpen] = useState(false);
  const [deleteMemberOpen, setDeleteMemberOpen] = useState(false);
  const [selectedMember, setSelectedMember] = useState<typeof members[0] | null>(null);
  const [saveSuccess, setSaveSuccess] = useState(false);

  const handleCopyLink = () => {
    navigator.clipboard.writeText(window.location.href);
    setLinkCopied(true);
    setTimeout(() => setLinkCopied(false), 2000);
  };

  const handleDownloadTrace = (traceId: string) => {
    console.log("Downloading trace:", traceId);
  };

  const handleViewAnalysis = (analysis: typeof analysisResults[0]) => {
    setSelectedAnalysis(analysis);
    setViewAnalysisOpen(true);
  };

  const handleDownloadAnalysis = (analysis: typeof analysisResults[0]) => {
    console.log("Downloading analysis:", analysis.name);
  };

  const handleEditMember = (member: typeof members[0]) => {
    setSelectedMember(member);
    setEditMemberOpen(true);
  };

  const handleDeleteMember = (member: typeof members[0]) => {
    setSelectedMember(member);
    setDeleteMemberOpen(true);
  };

  const handleSaveSettings = () => {
    setSaveSuccess(true);
    setTimeout(() => setSaveSuccess(false), 2000);
  };

  const handleDeleteStudy = () => {
    console.log("Deleting study");
    setDeleteStudyOpen(false);
  };

  const handleExportFormat = (format: string) => {
    console.log("Exporting as:", format);
    setExportOpen(false);
  };

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
                <span className="text-sm font-medium text-blue-deep">{studyData.id}</span>
                <span
                  className={cn(
                    "rounded-full px-2.5 py-1 text-xs font-medium",
                    studyData.status === "Active"
                      ? "bg-teal/10 text-teal"
                      : "bg-muted text-muted-foreground"
                  )}
                >
                  {tCommon(`status.${studyData.status.toLowerCase()}`)}
                </span>
                <div className="flex items-center gap-1.5 rounded-full bg-muted px-2.5 py-1 text-xs font-medium text-muted-foreground">
                  {studyData.visibility === "Private" ? (
                    <Lock className="h-3 w-3" />
                  ) : (
                    <Globe className="h-3 w-3" />
                  )}
                  {studyData.visibility === "Private" ? t("visibility.private") : t("visibility.public")}
                </div>
              </div>
              <h1 className="mb-2 text-2xl font-semibold text-foreground">{studyData.name}</h1>
              <div className="flex items-center gap-4 text-sm text-muted-foreground">
                <span>{studyData.field}</span>
                <span>•</span>
                <span>{studyData.pi}</span>
                <span>•</span>
                <span>{studyData.institution}</span>
              </div>
            </div>

            <div className="flex items-center gap-2">
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
                <p className="mb-2 text-sm text-muted-foreground">{t("stats.totalSamples")}</p>
                <p className="mb-1 text-3xl font-semibold text-foreground">
                  {studyData.samples.toLocaleString()}
                </p>
                <p className="text-xs text-emerald-500">{t("stats.allSamplesCollected")}</p>
              </div>
              <div className="rounded-xl border border-border bg-card p-5">
                <p className="mb-2 text-sm text-muted-foreground">{t("stats.chromatogramTraces")}</p>
                <p className="mb-1 text-3xl font-semibold text-foreground">
                  {studyData.traces.toLocaleString()}
                </p>
                <p className="text-xs text-teal">{t("stats.processed", { percent: studyData.progress })}</p>
              </div>
              <div className="rounded-xl border border-border bg-card p-5">
                <p className="mb-2 text-sm text-muted-foreground">{t("stats.qualityScore")}</p>
                <p className="mb-1 text-3xl font-semibold text-foreground">{studyData.quality}%</p>
                <p className="text-xs text-emerald-500">{t("stats.excellentQuality")}</p>
              </div>
              <div className="rounded-xl border border-border bg-card p-5">
                <p className="mb-2 text-sm text-muted-foreground">{t("stats.teamMembers")}</p>
                <p className="mb-1 text-3xl font-semibold text-foreground">{members.length}</p>
                <p className="text-xs text-muted-foreground">{t("stats.activeCollaborators")}</p>
              </div>
            </div>

            {/* Study Description & Progress */}
            <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
              <div className="rounded-xl border border-border bg-card p-6 lg:col-span-2">
                <h3 className="mb-4 text-base font-semibold text-foreground">{t("overview.studyDescription")}</h3>
                <p className="mb-6 text-sm leading-relaxed text-muted-foreground">
                  {studyData.description}
                </p>
                <div className="space-y-3">
                  <div className="flex items-center justify-between border-b border-border py-2">
                    <span className="text-sm text-muted-foreground">{t("overview.researchField")}</span>
                    <span className="text-sm font-medium text-foreground">{studyData.field}</span>
                  </div>
                  <div className="flex items-center justify-between border-b border-border py-2">
                    <span className="text-sm text-muted-foreground">{t("overview.principalInvestigator")}</span>
                    <span className="text-sm font-medium text-foreground">{studyData.pi}</span>
                  </div>
                  <div className="flex items-center justify-between border-b border-border py-2">
                    <span className="text-sm text-muted-foreground">{t("overview.institution")}</span>
                    <span className="text-sm font-medium text-foreground">{studyData.institution}</span>
                  </div>
                  <div className="flex items-center justify-between border-b border-border py-2">
                    <span className="text-sm text-muted-foreground">{t("overview.createdDate")}</span>
                    <span className="text-sm font-medium text-foreground">
                      {new Date(studyData.created).toLocaleDateString(undefined, {
                        year: "numeric",
                        month: "long",
                        day: "numeric",
                      })}
                    </span>
                  </div>
                </div>
              </div>

              <div className="rounded-xl border border-border bg-card p-6">
                <div className="mb-4 flex items-center justify-between">
                  <h3 className="text-base font-semibold text-foreground">{t("overview.processingProgress")}</h3>
                  <span className="text-sm font-medium text-teal">{studyData.progress}%</span>
                </div>
                <div className="mb-6 h-2 overflow-hidden rounded-full bg-muted">
                  <div
                    className="h-full rounded-full bg-gradient-to-r from-teal to-teal/80 transition-all"
                    style={{ width: `${studyData.progress}%` }}
                  />
                </div>
                <div className="space-y-4">
                  <div className="flex items-center gap-3">
                    <div className="rounded-lg bg-emerald-500/10 p-2">
                      <CheckCircle2 className="h-4 w-4 text-emerald-500" />
                    </div>
                    <div className="flex-1">
                      <p className="text-sm font-medium text-foreground">{t("overview.tracesProcessed")}</p>
                      <p className="text-xs text-muted-foreground">{t("overview.tracesOf", { processed: "6,522", total: "8,934" })}</p>
                    </div>
                  </div>
                  <div className="flex items-center gap-3">
                    <div className="rounded-lg bg-teal/10 p-2">
                      <Clock className="h-4 w-4 text-teal" />
                    </div>
                    <div className="flex-1">
                      <p className="text-sm font-medium text-foreground">{t("overview.inProgress")}</p>
                      <p className="text-xs text-muted-foreground">{t("overview.tracesProcessing", { count: "2,178" })}</p>
                    </div>
                  </div>
                  <div className="flex items-center gap-3">
                    <div className="rounded-lg bg-muted p-2">
                      <Calendar className="h-4 w-4 text-muted-foreground" />
                    </div>
                    <div className="flex-1">
                      <p className="text-sm font-medium text-foreground">{t("overview.pending")}</p>
                      <p className="text-xs text-muted-foreground">{t("overview.tracesQueued", { count: "234" })}</p>
                    </div>
                  </div>
                </div>
              </div>
            </div>

            {/* Recent Analysis */}
            <div className="overflow-hidden rounded-xl border border-border bg-card">
              <div className="border-b border-border p-6">
                <h3 className="text-base font-semibold text-foreground">{t("overview.recentAnalysis")}</h3>
                <p className="mt-1 text-sm text-muted-foreground">{t("overview.latestAnalysisRuns")}</p>
              </div>
              <div className="overflow-x-auto">
                <table className="w-full">
                  <thead>
                    <tr className="border-b border-border bg-muted/30">
                      <th className="px-6 py-3 text-left text-xs font-medium uppercase text-muted-foreground">{t("table.analysisName")}</th>
                      <th className="px-6 py-3 text-left text-xs font-medium uppercase text-muted-foreground">{t("table.type")}</th>
                      <th className="px-6 py-3 text-left text-xs font-medium uppercase text-muted-foreground">{t("table.status")}</th>
                      <th className="px-6 py-3 text-left text-xs font-medium uppercase text-muted-foreground">{t("table.date")}</th>
                      <th className="px-6 py-3 text-left text-xs font-medium uppercase text-muted-foreground">{t("table.results")}</th>
                      <th className="px-6 py-3 text-left text-xs font-medium uppercase text-muted-foreground">{t("table.actions")}</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-border">
                    {analysisResults.map((analysis, idx) => (
                      <tr key={idx} className="transition-colors hover:bg-muted/20">
                        <td className="px-6 py-4">
                          <span className="text-sm font-medium text-foreground">{analysis.name}</span>
                        </td>
                        <td className="px-6 py-4">
                          <span className="text-sm text-muted-foreground">{analysis.type}</span>
                        </td>
                        <td className="px-6 py-4">
                          <span
                            className={cn(
                              "inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-medium",
                              analysis.status === "Completed"
                                ? "bg-emerald-500/10 text-emerald-500"
                                : analysis.status === "In Progress"
                                ? "bg-teal/10 text-teal"
                                : "bg-muted text-muted-foreground"
                            )}
                          >
                            {tCommon(`status.${analysis.status === "In Progress" ? "inProgress" : analysis.status.toLowerCase()}`)}
                          </span>
                        </td>
                        <td className="px-6 py-4">
                          <span className="text-sm text-muted-foreground">{analysis.date}</span>
                        </td>
                        <td className="px-6 py-4">
                          <span className="text-sm text-foreground">{analysis.variants}</span>
                        </td>
                        <td className="px-6 py-4">
                          <button
                            onClick={() => handleViewAnalysis(analysis)}
                            className="text-sm font-medium text-teal hover:text-teal/80"
                          >
                            {t("table.view")}
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          </div>
        )}

        {activeTab === "traces" && (
          <div className="space-y-6">
            <div className="flex items-center justify-between">
              <div>
                <h2 className="text-xl font-semibold text-foreground">{t("traces.title")}</h2>
                <p className="mt-1 text-sm text-muted-foreground">{t("traces.description")}</p>
              </div>
              <button
                onClick={() => setUploadTracesOpen(true)}
                className="flex items-center gap-2 rounded-lg bg-teal px-4 py-2 text-white transition-all hover:bg-teal/90"
              >
                <Upload className="h-4 w-4" />
                {t("traces.uploadTraces")}
              </button>
            </div>

            <div className="overflow-hidden rounded-xl border border-border bg-card">
              <div className="overflow-x-auto">
                <table className="w-full">
                  <thead>
                    <tr className="border-b border-border bg-muted/30">
                      <th className="px-6 py-3 text-left text-xs font-medium uppercase text-muted-foreground">{t("traces.traceId")}</th>
                      <th className="px-6 py-3 text-left text-xs font-medium uppercase text-muted-foreground">{t("traces.sampleId")}</th>
                      <th className="px-6 py-3 text-left text-xs font-medium uppercase text-muted-foreground">{t("table.status")}</th>
                      <th className="px-6 py-3 text-left text-xs font-medium uppercase text-muted-foreground">{t("traces.quality")}</th>
                      <th className="px-6 py-3 text-left text-xs font-medium uppercase text-muted-foreground">{t("traces.length")}</th>
                      <th className="px-6 py-3 text-left text-xs font-medium uppercase text-muted-foreground">{t("traces.uploaded")}</th>
                      <th className="px-6 py-3 text-left text-xs font-medium uppercase text-muted-foreground">{t("traces.by")}</th>
                      <th className="px-6 py-3 text-left text-xs font-medium uppercase text-muted-foreground">{t("table.actions")}</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-border">
                    {traces.map((trace) => (
                      <tr key={trace.id} className="transition-colors hover:bg-muted/20">
                        <td className="px-6 py-4">
                          <Link href={`/traces/${trace.id}`} className="text-sm font-medium text-blue-deep hover:underline">
                            {trace.id}
                          </Link>
                        </td>
                        <td className="px-6 py-4">
                          <span className="font-mono text-sm text-foreground">{trace.sample}</span>
                        </td>
                        <td className="px-6 py-4">
                          <span
                            className={cn(
                              "inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-medium",
                              trace.status === "Processed" ? "bg-emerald-500/10 text-emerald-500" :
                              trace.status === "Processing" ? "bg-teal/10 text-teal" :
                              trace.status === "Review" ? "bg-amber-500/10 text-amber-500" :
                              "bg-red-500/10 text-red-500"
                            )}
                          >
                            {trace.status === "Processed" && <CheckCircle2 className="h-3 w-3" />}
                            {trace.status === "Processing" && <Clock className="h-3 w-3" />}
                            {trace.status === "Failed" && <AlertCircle className="h-3 w-3" />}
                            {tCommon(`status.${trace.status.toLowerCase()}`)}
                          </span>
                        </td>
                        <td className="px-6 py-4">
                          {trace.quality ? (
                            <span className={cn("text-sm font-medium", trace.quality >= 95 ? "text-emerald-500" : trace.quality >= 85 ? "text-amber-500" : "text-red-500")}>
                              {trace.quality}%
                            </span>
                          ) : (
                            <span className="text-sm text-muted-foreground">—</span>
                          )}
                        </td>
                        <td className="px-6 py-4"><span className="text-sm text-foreground">{trace.length}</span></td>
                        <td className="px-6 py-4"><span className="text-sm text-muted-foreground">{trace.uploaded}</span></td>
                        <td className="px-6 py-4"><span className="text-sm text-foreground">{trace.by}</span></td>
                        <td className="px-6 py-4">
                          <div className="flex items-center gap-2">
                            <Link href={`/traces/${trace.id}`} className="rounded-md p-1.5 text-muted-foreground transition-all hover:bg-teal/10 hover:text-teal">
                              <Eye className="h-4 w-4" />
                            </Link>
                            <button
                              onClick={() => handleDownloadTrace(trace.id)}
                              className="rounded-md p-1.5 text-muted-foreground transition-all hover:bg-muted hover:text-foreground"
                              title="Download trace"
                            >
                              <Download className="h-4 w-4" />
                            </button>
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          </div>
        )}

        {activeTab === "analysis" && (
          <div className="space-y-6">
            <div className="flex items-center justify-between">
              <div>
                <h2 className="text-xl font-semibold text-foreground">{t("analysis.title")}</h2>
                <p className="mt-1 text-sm text-muted-foreground">{t("analysis.description")}</p>
              </div>
              <button onClick={() => setNewAnalysisOpen(true)} className="flex items-center gap-2 rounded-lg bg-teal px-4 py-2 text-white transition-all hover:bg-teal/90">
                <BarChart3 className="h-4 w-4" />
                {t("analysis.newAnalysis")}
              </button>
            </div>

            <div className="grid grid-cols-1 gap-6 md:grid-cols-2">
              {analysisResults.map((analysis, idx) => (
                <div key={idx} className="rounded-xl border border-border bg-card p-6 transition-all hover:shadow-md">
                  <div className="mb-4 flex items-start justify-between">
                    <div className="flex-1">
                      <h3 className="mb-1 font-medium text-foreground">{analysis.name}</h3>
                      <p className="text-sm text-muted-foreground">{analysis.type}</p>
                    </div>
                    <span className={cn("rounded-full px-2.5 py-1 text-xs font-medium", analysis.status === "Completed" ? "bg-emerald-500/10 text-emerald-500" : analysis.status === "In Progress" ? "bg-teal/10 text-teal" : "bg-muted text-muted-foreground")}>
                      {tCommon(`status.${analysis.status === "In Progress" ? "inProgress" : analysis.status.toLowerCase()}`)}
                    </span>
                  </div>
                  <div className="mb-4 space-y-2">
                    <div className="flex items-center justify-between text-sm">
                      <span className="text-muted-foreground">{t("table.date")}</span>
                      <span className="text-foreground">{analysis.date}</span>
                    </div>
                    {analysis.variants !== "—" && (
                      <div className="flex items-center justify-between text-sm">
                        <span className="text-muted-foreground">{t("analysis.variants")}</span>
                        <span className="font-medium text-foreground">{analysis.variants}</span>
                      </div>
                    )}
                  </div>
                  <div className="flex items-center gap-2">
                    <button
                      onClick={() => handleViewAnalysis(analysis)}
                      className="flex-1 rounded-lg border border-border px-4 py-2 text-sm font-medium text-foreground transition-all hover:bg-muted/50"
                    >
                      {t("analysis.viewDetails")}
                    </button>
                    {analysis.status === "Completed" && (
                      <button
                        onClick={() => handleDownloadAnalysis(analysis)}
                        className="rounded-lg border border-border px-4 py-2 text-sm font-medium text-foreground transition-all hover:bg-muted/50"
                      >
                        <Download className="h-4 w-4" />
                      </button>
                    )}
                  </div>
                </div>
              ))}
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
              <button onClick={() => setInviteMemberOpen(true)} className="flex items-center gap-2 rounded-lg bg-teal px-4 py-2 text-white transition-all hover:bg-teal/90">
                <UserPlus className="h-4 w-4" />
                {t("team.inviteMember")}
              </button>
            </div>

            <div className="overflow-hidden rounded-xl border border-border bg-card">
              <table className="w-full">
                <thead>
                  <tr className="border-b border-border bg-muted/30">
                    <th className="px-6 py-3 text-left text-xs font-medium uppercase text-muted-foreground">{t("team.member")}</th>
                    <th className="px-6 py-3 text-left text-xs font-medium uppercase text-muted-foreground">{t("team.role")}</th>
                    <th className="px-6 py-3 text-left text-xs font-medium uppercase text-muted-foreground">{t("team.email")}</th>
                    <th className="px-6 py-3 text-left text-xs font-medium uppercase text-muted-foreground">{t("table.status")}</th>
                    <th className="px-6 py-3 text-left text-xs font-medium uppercase text-muted-foreground">{t("table.actions")}</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-border">
                  {members.map((member, idx) => (
                    <tr key={idx} className="transition-colors hover:bg-muted/20">
                      <td className="px-6 py-4">
                        <div className="flex items-center gap-3">
                          <div className="flex h-10 w-10 flex-shrink-0 items-center justify-center rounded-full bg-gradient-to-br from-teal to-blue-deep text-sm font-medium text-white">{member.avatar}</div>
                          <span className="text-sm font-medium text-foreground">{member.name}</span>
                        </div>
                      </td>
                      <td className="px-6 py-4"><span className="text-sm text-foreground">{member.role}</span></td>
                      <td className="px-6 py-4"><span className="text-sm text-muted-foreground">{member.email}</span></td>
                      <td className="px-6 py-4">
                        <span className={cn("inline-flex items-center rounded-full px-2.5 py-1 text-xs font-medium", member.status === "Active" ? "bg-emerald-500/10 text-emerald-500" : "bg-amber-500/10 text-amber-500")}>{tCommon(`status.${member.status.toLowerCase()}`)}</span>
                      </td>
                      <td className="px-6 py-4">
                        <div className="flex items-center gap-2">
                          <button
                            onClick={() => handleEditMember(member)}
                            className="rounded-md p-1.5 text-muted-foreground transition-all hover:bg-muted hover:text-foreground"
                          >
                            <Edit className="h-4 w-4" />
                          </button>
                          <button
                            onClick={() => handleDeleteMember(member)}
                            className="rounded-md p-1.5 text-muted-foreground transition-all hover:bg-red-500/10 hover:text-red-500"
                          >
                            <Trash2 className="h-4 w-4" />
                          </button>
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
          <div className="max-w-3xl space-y-6">
            <div>
              <h2 className="text-xl font-semibold text-foreground">{t("settings.title")}</h2>
              <p className="mt-1 text-sm text-muted-foreground">{t("settings.description")}</p>
            </div>

            <div className="space-y-6 rounded-xl border border-border bg-card p-6">
              <div>
                <label className="mb-2 block text-sm font-medium text-foreground">{t("settings.studyName")}</label>
                <input type="text" defaultValue={studyData.name} className="w-full rounded-lg border border-border bg-background px-4 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-teal/20" />
              </div>
              <div>
                <label className="mb-2 block text-sm font-medium text-foreground">{t("settings.studyDescription")}</label>
                <textarea rows={4} defaultValue={studyData.description} className="w-full resize-none rounded-lg border border-border bg-background px-4 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-teal/20" />
              </div>
              <div>
                <label className="mb-3 block text-sm font-medium text-foreground">{t("settings.visibility")}</label>
                <div className="space-y-3">
                  <label className="flex cursor-pointer items-start gap-3 rounded-lg border border-border p-4 transition-colors hover:bg-muted/20">
                    <input type="radio" name="visibility" value="private" defaultChecked className="mt-0.5" />
                    <div className="flex-1">
                      <div className="mb-1 flex items-center gap-2">
                        <Lock className="h-4 w-4 text-muted-foreground" />
                        <span className="text-sm font-medium text-foreground">{t("visibility.private")}</span>
                      </div>
                      <p className="text-xs text-muted-foreground">{t("settings.privateDesc")}</p>
                    </div>
                  </label>
                  <label className="flex cursor-pointer items-start gap-3 rounded-lg border border-border p-4 transition-colors hover:bg-muted/20">
                    <input type="radio" name="visibility" value="public" className="mt-0.5" />
                    <div className="flex-1">
                      <div className="mb-1 flex items-center gap-2">
                        <Globe className="h-4 w-4 text-muted-foreground" />
                        <span className="text-sm font-medium text-foreground">{t("visibility.public")}</span>
                      </div>
                      <p className="text-xs text-muted-foreground">{t("settings.publicDesc")}</p>
                    </div>
                  </label>
                </div>
              </div>
              <div className="border-t border-border pt-4">
                <button
                  onClick={handleSaveSettings}
                  className="rounded-lg bg-teal px-4 py-2 text-white transition-all hover:bg-teal/90"
                >
                  {saveSuccess ? t("settings.saved") : t("settings.saveChanges")}
                </button>
              </div>
            </div>

            <div className="rounded-xl border border-red-500/30 bg-card p-6">
              <h3 className="mb-2 text-base font-semibold text-red-500">{t("settings.dangerZone")}</h3>
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
              <input type="text" value={typeof window !== "undefined" ? window.location.href : ""} className="flex-1 rounded-lg border border-border bg-muted/50 px-3.5 py-2.5 font-mono text-sm" readOnly />
              <button onClick={handleCopyLink} className="flex items-center gap-2 rounded-lg bg-teal px-4 py-2.5 text-sm font-medium text-white">
                {linkCopied ? <Check className="h-4 w-4" /> : <Copy className="h-4 w-4" />}
                {linkCopied ? t("dialogs.share.copied") : t("dialogs.share.copy")}
              </button>
            </div>
          </div>
          <DialogFooter>
            <button onClick={() => setShareOpen(false)} className="rounded-lg px-4 py-2.5 text-sm font-medium hover:bg-muted">{t("dialogs.share.done")}</button>
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
                  <div className="rounded-lg bg-muted p-2 group-hover:bg-teal/10"><FileText className="h-4 w-4 text-muted-foreground group-hover:text-teal" /></div>
                  <span className="font-medium">{t(`dialogs.export.${format.key}`)}</span>
                </div>
                <Download className="h-4 w-4 text-muted-foreground" />
              </button>
            ))}
          </div>
        </DialogContent>
      </Dialog>

      <Dialog open={uploadTracesOpen} onOpenChange={setUploadTracesOpen}>
        <DialogContent className="sm:max-w-[500px]">
          <DialogHeader>
            <DialogTitle>{t("dialogs.uploadTraces.title")}</DialogTitle>
            <DialogDescription>{t("dialogs.uploadTraces.description")}</DialogDescription>
          </DialogHeader>
          <div className="space-y-5 py-4">
            <div className="group cursor-pointer rounded-lg border-2 border-dashed border-border p-8 text-center hover:border-teal/50 hover:bg-muted/20">
              <Upload className="mx-auto mb-3 h-10 w-10 text-muted-foreground group-hover:text-teal" />
              <p className="mb-1 text-sm font-medium">{t("dialogs.uploadTraces.dropzone")}</p>
              <p className="text-xs text-muted-foreground">{t("dialogs.uploadTraces.fileTypes")}</p>
            </div>
          </div>
          <DialogFooter>
            <button onClick={() => setUploadTracesOpen(false)} className="rounded-lg px-4 py-2.5 text-sm font-medium hover:bg-muted">{t("dialogs.uploadTraces.cancel")}</button>
            <button onClick={() => setUploadTracesOpen(false)} className="rounded-lg bg-teal px-4 py-2.5 text-sm font-medium text-white">{t("dialogs.uploadTraces.upload")}</button>
          </DialogFooter>
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
                <input type="email" className="w-full rounded-lg border border-border py-2.5 pl-10 pr-3.5 text-sm" placeholder={t("dialogs.inviteMember.emailPlaceholder")} />
              </div>
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">{t("dialogs.inviteMember.role")}</label>
              <select className="w-full rounded-lg border border-border px-3.5 py-2.5 text-sm">
                <option>{t("dialogs.inviteMember.selectRole")}</option>
                <option>{t("team.roles.coInvestigator")}</option>
                <option>{t("team.roles.researchScientist")}</option>
                <option>{t("team.roles.labTechnician")}</option>
                <option>{t("team.roles.viewer")}</option>
              </select>
            </div>
          </div>
          <DialogFooter>
            <button onClick={() => setInviteMemberOpen(false)} className="rounded-lg px-4 py-2.5 text-sm font-medium hover:bg-muted">{t("dialogs.inviteMember.cancel")}</button>
            <button onClick={() => setInviteMemberOpen(false)} className="rounded-lg bg-teal px-4 py-2.5 text-sm font-medium text-white">{t("dialogs.inviteMember.sendInvitation")}</button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog open={newAnalysisOpen} onOpenChange={setNewAnalysisOpen}>
        <DialogContent className="sm:max-w-[540px]">
          <DialogHeader>
            <DialogTitle>{t("dialogs.newAnalysis.title")}</DialogTitle>
            <DialogDescription>{t("dialogs.newAnalysis.description")}</DialogDescription>
          </DialogHeader>
          <div className="space-y-5 py-4">
            <div className="space-y-2">
              <label className="text-sm font-medium">{t("dialogs.newAnalysis.analysisName")}</label>
              <input type="text" className="w-full rounded-lg border border-border px-3.5 py-2.5 text-sm" placeholder={t("dialogs.newAnalysis.analysisNamePlaceholder")} />
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">{t("dialogs.newAnalysis.pipelineType")}</label>
              <select className="w-full rounded-lg border border-border px-3.5 py-2.5 text-sm">
                <option>{t("dialogs.newAnalysis.selectPipeline")}</option>
                <option>GATK Variant Calling</option>
                <option>FastQC Quality Control</option>
                <option>Population Structure (PCA)</option>
                <option>Association Testing (PLINK)</option>
              </select>
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <label className="text-sm font-medium">{t("dialogs.newAnalysis.priority")}</label>
                <select className="w-full rounded-lg border border-border px-3.5 py-2.5 text-sm">
                  <option>Normal</option>
                  <option>High</option>
                  <option>Urgent</option>
                </select>
              </div>
              <div className="space-y-2">
                <label className="text-sm font-medium">{t("dialogs.newAnalysis.notifications")}</label>
                <select className="w-full rounded-lg border border-border px-3.5 py-2.5 text-sm">
                  <option>On Completion</option>
                  <option>On Error Only</option>
                  <option>None</option>
                </select>
              </div>
            </div>
          </div>
          <DialogFooter>
            <button onClick={() => setNewAnalysisOpen(false)} className="rounded-lg px-4 py-2.5 text-sm font-medium hover:bg-muted">{t("dialogs.newAnalysis.cancel")}</button>
            <button onClick={() => setNewAnalysisOpen(false)} className="rounded-lg bg-teal px-4 py-2.5 text-sm font-medium text-white">{t("dialogs.newAnalysis.createAnalysis")}</button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* View Analysis Dialog */}
      <Dialog open={viewAnalysisOpen} onOpenChange={setViewAnalysisOpen}>
        <DialogContent className="sm:max-w-[500px]">
          <DialogHeader>
            <DialogTitle>{selectedAnalysis?.name}</DialogTitle>
            <DialogDescription>{t("dialogs.viewAnalysis.description")}</DialogDescription>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div className="grid grid-cols-2 gap-4">
              <div>
                <p className="text-xs text-muted-foreground mb-1">{t("table.type")}</p>
                <p className="text-sm font-medium">{selectedAnalysis?.type}</p>
              </div>
              <div>
                <p className="text-xs text-muted-foreground mb-1">{t("table.status")}</p>
                <p className="text-sm font-medium">{selectedAnalysis?.status}</p>
              </div>
              <div>
                <p className="text-xs text-muted-foreground mb-1">{t("table.date")}</p>
                <p className="text-sm font-medium">{selectedAnalysis?.date}</p>
              </div>
              <div>
                <p className="text-xs text-muted-foreground mb-1">{t("analysis.variants")}</p>
                <p className="text-sm font-medium">{selectedAnalysis?.variants}</p>
              </div>
            </div>
            {selectedAnalysis?.status === "Completed" && (
              <div className="rounded-lg border border-border bg-muted/30 p-4">
                <p className="text-sm text-muted-foreground">
                  {t("dialogs.viewAnalysis.analysisCompleted")}
                </p>
              </div>
            )}
          </div>
          <DialogFooter>
            <button onClick={() => setViewAnalysisOpen(false)} className="rounded-lg px-4 py-2.5 text-sm font-medium hover:bg-muted">{t("dialogs.viewAnalysis.close")}</button>
            {selectedAnalysis?.status === "Completed" && (
              <button
                onClick={() => {
                  handleDownloadAnalysis(selectedAnalysis);
                  setViewAnalysisOpen(false);
                }}
                className="rounded-lg bg-teal px-4 py-2.5 text-sm font-medium text-white"
              >
                {t("dialogs.viewAnalysis.downloadResults")}
              </button>
            )}
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Edit Member Dialog */}
      <Dialog open={editMemberOpen} onOpenChange={setEditMemberOpen}>
        <DialogContent className="sm:max-w-[420px]">
          <DialogHeader>
            <DialogTitle>{t("dialogs.editMember.title")}</DialogTitle>
            <DialogDescription>{t("dialogs.editMember.description", { name: selectedMember?.name ?? "" })}</DialogDescription>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <label className="text-sm font-medium">{t("team.role")}</label>
              <select
                defaultValue={selectedMember?.role}
                className="w-full rounded-lg border border-border px-3.5 py-2.5 text-sm"
              >
                <option>{t("team.roles.principalInvestigator")}</option>
                <option>{t("team.roles.coInvestigator")}</option>
                <option>{t("team.roles.researchScientist")}</option>
                <option>{t("team.roles.labTechnician")}</option>
                <option>{t("team.roles.bioinformatician")}</option>
                <option>{t("team.roles.viewer")}</option>
              </select>
            </div>
          </div>
          <DialogFooter>
            <button onClick={() => setEditMemberOpen(false)} className="rounded-lg px-4 py-2.5 text-sm font-medium hover:bg-muted">{t("dialogs.editMember.cancel")}</button>
            <button
              onClick={() => {
                console.log("Updating member:", selectedMember?.email);
                setEditMemberOpen(false);
              }}
              className="rounded-lg bg-teal px-4 py-2.5 text-sm font-medium text-white"
            >
              {t("dialogs.editMember.saveChanges")}
            </button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Delete Member Dialog */}
      <Dialog open={deleteMemberOpen} onOpenChange={setDeleteMemberOpen}>
        <DialogContent className="sm:max-w-[420px]">
          <DialogHeader>
            <DialogTitle>{t("dialogs.deleteMember.title")}</DialogTitle>
            <DialogDescription>
              {t("dialogs.deleteMember.description", { name: selectedMember?.name ?? "" })}
            </DialogDescription>
          </DialogHeader>
          <DialogFooter className="mt-4">
            <button onClick={() => setDeleteMemberOpen(false)} className="rounded-lg px-4 py-2.5 text-sm font-medium hover:bg-muted">{t("dialogs.deleteMember.cancel")}</button>
            <button
              onClick={() => {
                console.log("Removing member:", selectedMember?.email);
                setDeleteMemberOpen(false);
              }}
              className="rounded-lg bg-red-500 px-4 py-2.5 text-sm font-medium text-white hover:bg-red-500/90"
            >
              {t("dialogs.deleteMember.removeMember")}
            </button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Delete Study Dialog */}
      <Dialog open={deleteStudyOpen} onOpenChange={setDeleteStudyOpen}>
        <DialogContent className="sm:max-w-[420px]">
          <DialogHeader>
            <DialogTitle className="text-red-500">{t("dialogs.deleteStudy.title")}</DialogTitle>
            <DialogDescription>
              {t("dialogs.deleteStudy.description")}
            </DialogDescription>
          </DialogHeader>
          <div className="py-4">
            <div className="space-y-2">
              <label className="text-sm font-medium">
                {t("dialogs.deleteStudy.confirmLabel")} <span className="font-mono text-teal">{studyData.id}</span>
              </label>
              <input
                type="text"
                className="w-full rounded-lg border border-border px-3.5 py-2.5 text-sm"
                placeholder={studyData.id}
              />
            </div>
          </div>
          <DialogFooter>
            <button onClick={() => setDeleteStudyOpen(false)} className="rounded-lg px-4 py-2.5 text-sm font-medium hover:bg-muted">{t("dialogs.deleteStudy.cancel")}</button>
            <button
              onClick={handleDeleteStudy}
              className="rounded-lg bg-red-500 px-4 py-2.5 text-sm font-medium text-white hover:bg-red-500/90"
            >
              {t("dialogs.deleteStudy.delete")}
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
        {aiAssistantOpen ? (
          <X className="h-6 w-6" />
        ) : (
          <Sparkles className="h-6 w-6" />
        )}
      </button>

      {/* AI Assistant */}
      <AIAssistant
        isOpen={aiAssistantOpen}
        onClose={() => setAiAssistantOpen(false)}
        contextType="study"
        contextTitle={studyData.name}
        contextId={studyData.id}
      />
    </div>
  );
}
