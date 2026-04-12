"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { Link } from "@/lib/navigation";
import {
  FileText,
  Users,
  Calendar,
  Plus,
  Search,
  Filter,
  Download,
  MoreVertical,
  Beaker,
  ChevronDown,
  Eye,
  Edit,
  Trash2,
  Copy,
  Archive,
  Share2,
  Loader2,
  Star,
} from "lucide-react";
import { cn } from "@/lib/utils";
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
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { StatusBadge, EmptyState } from "@/components/shared";
import {
  useMyStudies,
  useCreateStudy,
  useDeleteStudy,
  useResearchFields,
} from "@/hooks";
import type { StudySummary, CreateStudyInput } from "@/types";

const statusOptions = [
  { value: "all", key: "allStatus" },
  { value: "draft", key: "draft" },
  { value: "active", key: "active" },
  { value: "completed", key: "completed" },
  { value: "published", key: "published" },
  { value: "archived", key: "archived" },
];

export default function StudiesPage() {
  const t = useTranslations("studies");
  const tCommon = useTranslations("common");

  // State
  const [searchQuery, setSearchQuery] = useState("");
  const [selectedResearchField, setSelectedResearchField] = useState<number | null>(null);
  const [selectedStatus, setSelectedStatus] = useState("all");
  const [newStudyOpen, setNewStudyOpen] = useState(false);
  const [showFilters, setShowFilters] = useState(false);
  const [currentPage, setCurrentPage] = useState(1);
  const [exportOpen, setExportOpen] = useState(false);
  const [deleteStudyOpen, setDeleteStudyOpen] = useState(false);
  const [selectedStudy, setSelectedStudy] = useState<StudySummary | null>(null);

  // Form state for new study
  const [newStudyForm, setNewStudyForm] = useState<CreateStudyInput>({
    title: "",
    description: "",
    researchFieldId: 1,
    institution: "",
    principalInvestigator: "",
    tags: [],
  });
  const [tagsInput, setTagsInput] = useState("");

  // Queries
  const { data: studiesData, isLoading, error } = useMyStudies(currentPage, 6);
  const { data: researchFields } = useResearchFields();

  // Mutations
  const createStudyMutation = useCreateStudy();
  const deleteStudyMutation = useDeleteStudy();

  // Handlers
  const handleExport = (format: string) => {
    console.log(`Exporting studies as ${format}`);
    setExportOpen(false);
  };

  const handleDeleteStudy = async () => {
    if (selectedStudy) {
      try {
        await deleteStudyMutation.mutateAsync(selectedStudy.id);
        setDeleteStudyOpen(false);
        setSelectedStudy(null);
      } catch (err) {
        console.error("Failed to delete study:", err);
      }
    }
  };

  const handleCreateStudy = async () => {
    try {
      const input: CreateStudyInput = {
        ...newStudyForm,
        tags: tagsInput.split(",").map((t) => t.trim()).filter(Boolean),
      };
      await createStudyMutation.mutateAsync(input);
      setNewStudyOpen(false);
      setNewStudyForm({
        title: "",
        description: "",
        researchFieldId: 1,
        institution: "",
        principalInvestigator: "",
        tags: [],
      });
      setTagsInput("");
    } catch (err) {
      console.error("Failed to create study:", err);
    }
  };

  // Filter studies client-side (for search)
  const studies = studiesData?.items ?? [];
  const filteredStudies = studies.filter((study) => {
    const matchesSearch =
      searchQuery === "" ||
      study.title.toLowerCase().includes(searchQuery.toLowerCase()) ||
      study.id.toLowerCase().includes(searchQuery.toLowerCase()) ||
      (study.principalInvestigator?.toLowerCase().includes(searchQuery.toLowerCase()) ?? false);
    const matchesField =
      selectedResearchField === null || study.researchFieldId === selectedResearchField;
    const matchesStatus =
      selectedStatus === "all" || study.statusName === selectedStatus;
    return matchesSearch && matchesField && matchesStatus;
  });

  const totalPages = studiesData?.totalPages ?? 1;

  // Loading state
  if (isLoading) {
    return (
      <div className="flex h-96 items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-teal" />
      </div>
    );
  }

  // Error state
  if (error) {
    return (
      <EmptyState
        icon={Beaker}
        title="Error loading studies"
        description="Failed to load your studies. Please try again."
      >
        <Button onClick={() => window.location.reload()}>
          {tCommon("retry")}
        </Button>
      </EmptyState>
    );
  }

  return (
    <div className="space-y-6">
      {/* Page Header */}
      <div className="flex items-start justify-between">
        <div className="space-y-1">
          <h1 className="text-2xl font-semibold text-foreground">{t("title")}</h1>
          <p className="text-sm text-muted-foreground">{t("description")}</p>
        </div>
        <Button
          onClick={() => setNewStudyOpen(true)}
          className="bg-gradient-to-r from-teal to-teal/90 hover:from-teal/90 hover:to-teal/80"
        >
          <Plus className="h-4 w-4" />
          {t("newStudy")}
        </Button>
      </div>

      {/* Search and Filters */}
      <div className="rounded-xl border border-border bg-card p-4 shadow-sm">
        <div className="flex flex-col gap-4 md:flex-row">
          <div className="relative flex-1">
            <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            <input
              type="text"
              placeholder={t("searchPlaceholder")}
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="w-full rounded-lg border border-border bg-background py-2 pl-10 pr-4 text-sm transition-all duration-200 focus:border-teal/30 focus:outline-none focus:ring-2 focus:ring-teal/20"
            />
          </div>
          <div className="flex gap-2">
            <button
              onClick={() => setShowFilters(!showFilters)}
              className={cn(
                "flex items-center gap-2 rounded-lg border px-4 py-2 text-sm font-medium transition-all duration-200 active:scale-[0.98]",
                showFilters
                  ? "border-teal/30 bg-teal/10 text-teal"
                  : "border-border text-foreground hover:bg-muted/50"
              )}
            >
              <Filter className="h-4 w-4" />
              {tCommon("filter")}
              <ChevronDown
                className={cn(
                  "h-3.5 w-3.5 transition-transform",
                  showFilters && "rotate-180"
                )}
              />
            </button>
            <button
              onClick={() => setExportOpen(true)}
              className="flex items-center gap-2 rounded-lg border border-border px-4 py-2 text-sm font-medium text-foreground transition-all duration-200 hover:bg-muted/50 active:scale-[0.98]"
            >
              <Download className="h-4 w-4" />
              {tCommon("export")}
            </button>
          </div>
        </div>

        {/* Expanded Filters */}
        {showFilters && (
          <div className="mt-4 flex flex-wrap items-center gap-4 border-t border-border pt-4">
            <div className="flex items-center gap-2">
              <span className="text-sm text-muted-foreground">
                {t("detail.overview.researchField")}:
              </span>
              <div className="relative">
                <select
                  value={selectedResearchField ?? ""}
                  onChange={(e) =>
                    setSelectedResearchField(
                      e.target.value ? Number(e.target.value) : null
                    )
                  }
                  className="cursor-pointer appearance-none rounded-lg border border-border bg-background py-1.5 pl-3 pr-8 text-sm focus:outline-none focus:ring-2 focus:ring-teal/20"
                >
                  <option value="">{t("filters.allTypes")}</option>
                  {researchFields?.map((field) => (
                    <option key={field.id} value={field.id}>
                      {field.name}
                    </option>
                  ))}
                </select>
                <ChevronDown className="pointer-events-none absolute right-2 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              </div>
            </div>
            <div className="flex items-center gap-2">
              <span className="text-sm text-muted-foreground">
                {t("filters.status")}:
              </span>
              <div className="relative">
                <select
                  value={selectedStatus}
                  onChange={(e) => setSelectedStatus(e.target.value)}
                  className="cursor-pointer appearance-none rounded-lg border border-border bg-background py-1.5 pl-3 pr-8 text-sm focus:outline-none focus:ring-2 focus:ring-teal/20"
                >
                  {statusOptions.map((status) => (
                    <option key={status.value} value={status.value}>
                      {status.value === "all"
                        ? t("filters.allStatus")
                        : t(`status.${status.key}`)}
                    </option>
                  ))}
                </select>
                <ChevronDown className="pointer-events-none absolute right-2 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              </div>
            </div>
            {(selectedResearchField !== null || selectedStatus !== "all") && (
              <button
                onClick={() => {
                  setSelectedResearchField(null);
                  setSelectedStatus("all");
                }}
                className="text-sm text-muted-foreground transition-colors hover:text-foreground"
              >
                {t("filters.clearFilters")}
              </button>
            )}
          </div>
        )}
      </div>

      {/* Studies Grid */}
      {filteredStudies.length === 0 ? (
        <EmptyState
          icon={Beaker}
          title={t("noStudiesFound")}
          description={
            searchQuery || selectedResearchField !== null || selectedStatus !== "all"
              ? t("noStudiesDescription")
              : t("createFirstStudy")
          }
        >
          {!searchQuery && selectedResearchField === null && selectedStatus === "all" && (
            <Button onClick={() => setNewStudyOpen(true)}>
              <Plus className="h-4 w-4" />
              {t("newStudy")}
            </Button>
          )}
        </EmptyState>
      ) : (
        <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
          {filteredStudies.map((study) => (
            <Link
              key={study.id}
              href={`/studies/${study.id}`}
              className="group rounded-xl border border-border bg-card p-6 transition-all duration-200 hover:-translate-y-0.5 hover:border-teal/40 hover:shadow-lg hover:shadow-black/5 active:scale-[0.99]"
            >
              <div className="mb-4 flex items-start justify-between">
                <div className="flex-1">
                  <div className="mb-1 flex items-center gap-2">
                    <span className="text-sm font-medium text-blue-deep">
                      {study.id}
                    </span>
                    <StatusBadge status={study.statusName} />
                    {study.isFeatured && (
                      <span className="flex items-center gap-1 rounded-full bg-amber-100 px-2 py-0.5 text-xs font-medium text-amber-700 dark:bg-amber-900/30 dark:text-amber-400">
                        <Star className="h-3 w-3" />
                        {t("featured.badge")}
                      </span>
                    )}
                  </div>
                  <h2 className="line-clamp-2 text-base font-medium text-foreground transition-colors duration-200 group-hover:text-teal">
                    {study.title}
                  </h2>
                </div>
                <DropdownMenu>
                  <DropdownMenuTrigger asChild>
                    <button
                      className="rounded-lg p-1.5 text-muted-foreground transition-all duration-200 hover:bg-muted/70 hover:text-foreground active:scale-95"
                      onClick={(e) => {
                        e.preventDefault();
                        e.stopPropagation();
                      }}
                      aria-label={`More options for ${study.title}`}
                    >
                      <MoreVertical className="h-4 w-4" />
                    </button>
                  </DropdownMenuTrigger>
                  <DropdownMenuContent
                    align="end"
                    className="w-48"
                    onClick={(e) => e.stopPropagation()}
                  >
                    <DropdownMenuItem asChild>
                      <Link
                        href={`/studies/${study.id}`}
                        className="flex items-center gap-2"
                      >
                        <Eye className="h-4 w-4" />
                        {t("actions.viewDetails")}
                      </Link>
                    </DropdownMenuItem>
                    <DropdownMenuItem onClick={() => console.log(`Edit ${study.id}`)}>
                      <Edit className="h-4 w-4" />
                      {t("actions.editStudy")}
                    </DropdownMenuItem>
                    <DropdownMenuItem onClick={() => console.log(`Duplicate ${study.id}`)}>
                      <Copy className="h-4 w-4" />
                      {t("actions.duplicate")}
                    </DropdownMenuItem>
                    <DropdownMenuItem onClick={() => console.log(`Share ${study.id}`)}>
                      <Share2 className="h-4 w-4" />
                      {t("actions.share")}
                    </DropdownMenuItem>
                    <DropdownMenuSeparator />
                    <DropdownMenuItem onClick={() => console.log(`Archive ${study.id}`)}>
                      <Archive className="h-4 w-4" />
                      {t("actions.archive")}
                    </DropdownMenuItem>
                    <DropdownMenuItem
                      className="text-red-500 focus:text-red-500"
                      onClick={() => {
                        setSelectedStudy(study);
                        setDeleteStudyOpen(true);
                      }}
                    >
                      <Trash2 className="h-4 w-4" />
                      {t("actions.delete")}
                    </DropdownMenuItem>
                  </DropdownMenuContent>
                </DropdownMenu>
              </div>

              {/* Stats Row */}
              <div className="mb-4 grid grid-cols-4 gap-4">
                <div className="space-y-1">
                  <div className="flex items-center gap-1.5 text-muted-foreground">
                    <FileText className="h-3.5 w-3.5" />
                    <span className="text-xs">{t("detail.overview.researchField")}</span>
                  </div>
                  <p className="truncate text-sm font-medium text-foreground">
                    {study.researchFieldName}
                  </p>
                </div>
                <div className="space-y-1">
                  <div className="flex items-center gap-1.5 text-muted-foreground">
                    <Users className="h-3.5 w-3.5" />
                    <span className="text-xs">{t("detail.stats.teamMembers")}</span>
                  </div>
                  <p className="text-sm font-medium text-foreground">
                    {study.memberCount}
                  </p>
                </div>
                <div className="space-y-1">
                  <div className="flex items-center gap-1.5 text-muted-foreground">
                    <Eye className="h-3.5 w-3.5" />
                    <span className="text-xs">Views</span>
                  </div>
                  <p className="text-sm font-medium text-foreground">
                    {study.viewsCount}
                  </p>
                </div>
                <div className="space-y-1">
                  <div className="flex items-center gap-1.5 text-muted-foreground">
                    <Calendar className="h-3.5 w-3.5" />
                    <span className="text-xs">{t("card.created")}</span>
                  </div>
                  <p className="text-sm font-medium text-foreground">
                    {new Date(study.createdAt).toLocaleDateString(undefined, {
                      month: "short",
                      day: "numeric",
                    })}
                  </p>
                </div>
              </div>

              {/* PI */}
              {study.principalInvestigator && (
                <div className="mb-4">
                  <p className="mb-1 text-xs text-muted-foreground">{t("card.pi")}</p>
                  <p className="text-sm font-medium text-foreground">
                    {study.principalInvestigator}
                  </p>
                </div>
              )}

              {/* Tags */}
              {study.tags.length > 0 && (
                <div className="flex flex-wrap gap-1.5">
                  {study.tags.map((tag) => (
                    <span
                      key={tag}
                      className="rounded-md bg-muted px-2.5 py-1 text-xs text-foreground/70"
                    >
                      {tag}
                    </span>
                  ))}
                </div>
              )}
            </Link>
          ))}
        </div>
      )}

      {/* Pagination */}
      {studiesData && studiesData.totalCount > 0 && (
        <div className="flex items-center justify-between rounded-xl border border-border bg-card px-6 py-4 shadow-sm">
          <p className="text-sm text-muted-foreground">
            {t("pagination.showing")}{" "}
            <span className="font-medium text-foreground">
              {(currentPage - 1) * 6 + 1}-
              {Math.min(currentPage * 6, studiesData.totalCount)}
            </span>{" "}
            {t("pagination.of")}{" "}
            <span className="font-medium text-foreground">
              {studiesData.totalCount}
            </span>{" "}
            {t("pagination.studies")}
          </p>
          <div className="flex gap-2">
            <button
              onClick={() => setCurrentPage((p) => Math.max(1, p - 1))}
              disabled={!studiesData.hasPreviousPage}
              className="rounded-lg border border-border px-3 py-2 text-sm font-medium text-muted-foreground transition-all duration-200 hover:bg-muted/50 hover:text-foreground active:scale-95 disabled:cursor-not-allowed disabled:opacity-50"
            >
              {tCommon("previous")}
            </button>
            {Array.from({ length: totalPages }, (_, i) => i + 1).map((page) => (
              <button
                key={page}
                onClick={() => setCurrentPage(page)}
                className={cn(
                  "rounded-lg px-3 py-2 text-sm font-medium transition-all duration-200 active:scale-95",
                  currentPage === page
                    ? "bg-teal text-white shadow-sm"
                    : "border border-border text-foreground hover:bg-muted/50"
                )}
              >
                {page}
              </button>
            ))}
            <button
              onClick={() => setCurrentPage((p) => Math.min(totalPages, p + 1))}
              disabled={!studiesData.hasNextPage}
              className="rounded-lg border border-border px-3 py-2 text-sm font-medium text-foreground transition-all duration-200 hover:bg-muted/50 active:scale-95 disabled:cursor-not-allowed disabled:opacity-50"
            >
              {tCommon("next")}
            </button>
          </div>
        </div>
      )}

      {/* Export Dialog */}
      <Dialog open={exportOpen} onOpenChange={setExportOpen}>
        <DialogContent className="sm:max-w-[420px]">
          <DialogHeader>
            <DialogTitle>{t("export.title")}</DialogTitle>
            <DialogDescription>{t("export.description")}</DialogDescription>
          </DialogHeader>
          <div className="space-y-3 py-4">
            {[
              { format: "CSV", key: "csv", description: t("export.csvDescription") },
              { format: "JSON", key: "json", description: t("export.jsonDescription") },
              { format: "PDF", key: "pdf", description: t("export.pdfDescription") },
            ].map((option) => (
              <button
                key={option.format}
                onClick={() => handleExport(option.format)}
                className="group flex w-full items-center justify-between rounded-lg border border-border px-4 py-3.5 transition-all hover:border-teal/30 hover:bg-muted/50"
              >
                <div className="flex items-center gap-3">
                  <div className="rounded-lg bg-muted p-2 group-hover:bg-teal/10">
                    <FileText className="h-4 w-4 text-muted-foreground group-hover:text-teal" />
                  </div>
                  <div className="text-left">
                    <span className="font-medium text-foreground">{option.format}</span>
                    <p className="text-xs text-muted-foreground">{option.description}</p>
                  </div>
                </div>
                <Download className="h-4 w-4 text-muted-foreground" />
              </button>
            ))}
          </div>
        </DialogContent>
      </Dialog>

      {/* Delete Study Confirmation Dialog */}
      <Dialog open={deleteStudyOpen} onOpenChange={setDeleteStudyOpen}>
        <DialogContent className="sm:max-w-[420px]">
          <DialogHeader>
            <DialogTitle>{t("delete.title")}</DialogTitle>
            <DialogDescription>
              {t("delete.description", { name: selectedStudy?.title ?? "" })}
            </DialogDescription>
          </DialogHeader>
          <DialogFooter className="mt-4">
            <button
              onClick={() => setDeleteStudyOpen(false)}
              className="rounded-lg px-4 py-2.5 text-sm font-medium text-foreground hover:bg-muted"
            >
              {tCommon("cancel")}
            </button>
            <button
              onClick={handleDeleteStudy}
              disabled={deleteStudyMutation.isPending}
              className="rounded-lg bg-red-500 px-4 py-2.5 text-sm font-medium text-white hover:bg-red-500/90 disabled:opacity-50"
            >
              {deleteStudyMutation.isPending ? (
                <Loader2 className="h-4 w-4 animate-spin" />
              ) : (
                t("delete.confirm")
              )}
            </button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* New Study Dialog */}
      <Dialog open={newStudyOpen} onOpenChange={setNewStudyOpen}>
        <DialogContent className="sm:max-w-[540px]">
          <DialogHeader>
            <DialogTitle>{t("create.title")}</DialogTitle>
            <DialogDescription>{t("create.description")}</DialogDescription>
          </DialogHeader>
          <div className="space-y-5 py-4">
            <div className="space-y-2">
              <label htmlFor="study-name" className="text-sm font-medium text-foreground">
                {t("create.studyName")}
              </label>
              <input
                id="study-name"
                type="text"
                value={newStudyForm.title}
                onChange={(e) =>
                  setNewStudyForm((f) => ({ ...f, title: e.target.value }))
                }
                className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm text-foreground transition-all placeholder:text-muted-foreground focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/10"
                placeholder={t("create.studyNamePlaceholder")}
              />
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <label htmlFor="research-field" className="text-sm font-medium text-foreground">
                  {t("detail.overview.researchField")}
                </label>
                <select
                  id="research-field"
                  value={newStudyForm.researchFieldId}
                  onChange={(e) =>
                    setNewStudyForm((f) => ({
                      ...f,
                      researchFieldId: Number(e.target.value),
                    }))
                  }
                  className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm text-foreground transition-all focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/10"
                >
                  {researchFields?.map((field) => (
                    <option key={field.id} value={field.id}>
                      {field.name}
                    </option>
                  ))}
                </select>
              </div>

              <div className="space-y-2">
                <label htmlFor="institution" className="text-sm font-medium text-foreground">
                  {t("detail.overview.institution")}
                </label>
                <input
                  id="institution"
                  type="text"
                  value={newStudyForm.institution ?? ""}
                  onChange={(e) =>
                    setNewStudyForm((f) => ({ ...f, institution: e.target.value }))
                  }
                  className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm text-foreground transition-all placeholder:text-muted-foreground focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/10"
                  placeholder="e.g., MIT, Stanford"
                />
              </div>
            </div>

            <div className="space-y-2">
              <label
                htmlFor="principal-investigator"
                className="text-sm font-medium text-foreground"
              >
                {t("create.principalInvestigator")}
              </label>
              <input
                id="principal-investigator"
                type="text"
                value={newStudyForm.principalInvestigator ?? ""}
                onChange={(e) =>
                  setNewStudyForm((f) => ({
                    ...f,
                    principalInvestigator: e.target.value,
                  }))
                }
                className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm text-foreground transition-all placeholder:text-muted-foreground focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/10"
                placeholder={t("create.piPlaceholder")}
              />
            </div>

            <div className="space-y-2">
              <label htmlFor="tags" className="text-sm font-medium text-foreground">
                {t("create.tags")}
              </label>
              <input
                id="tags"
                type="text"
                value={tagsInput}
                onChange={(e) => setTagsInput(e.target.value)}
                className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm text-foreground transition-all placeholder:text-muted-foreground focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/10"
                placeholder={t("create.tagsPlaceholder")}
              />
              <p className="mt-1.5 text-xs text-muted-foreground">
                {t("create.tagsHelp")}
              </p>
            </div>

            <div className="space-y-2">
              <label htmlFor="description" className="text-sm font-medium text-foreground">
                {t("create.studyDescription")}
              </label>
              <textarea
                id="description"
                rows={3}
                value={newStudyForm.description ?? ""}
                onChange={(e) =>
                  setNewStudyForm((f) => ({ ...f, description: e.target.value }))
                }
                className="w-full resize-none rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm text-foreground transition-all placeholder:text-muted-foreground focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/10"
                placeholder={t("create.descriptionPlaceholder")}
              />
            </div>
          </div>
          <DialogFooter>
            <button
              type="button"
              className="rounded-lg px-4 py-2.5 text-sm font-medium text-foreground transition-all hover:bg-muted"
              onClick={() => setNewStudyOpen(false)}
            >
              {tCommon("cancel")}
            </button>
            <button
              type="submit"
              disabled={!newStudyForm.title || createStudyMutation.isPending}
              className="rounded-lg bg-teal px-4 py-2.5 text-sm font-medium text-white shadow-sm transition-all hover:bg-teal/90 hover:shadow-md disabled:opacity-50"
              onClick={handleCreateStudy}
            >
              {createStudyMutation.isPending ? (
                <Loader2 className="h-4 w-4 animate-spin" />
              ) : (
                t("create.submit")
              )}
            </button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
