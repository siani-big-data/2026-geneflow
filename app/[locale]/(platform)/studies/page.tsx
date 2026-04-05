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

const studyTypes = [
  "All Types",
  "GWAS",
  "WES",
  "WGS",
  "RNA-Seq",
  "Targeted",
  "Metagenomics",
  "PGx",
  "scRNA-Seq",
];

const statusOptions = [
  { value: "All Status", key: "allStatus" },
  { value: "active", key: "active" },
  { value: "completed", key: "completed" },
  { value: "on-hold", key: "onHold" },
  { value: "draft", key: "draft" },
];

const studies = [
  {
    id: "GF-2026-089",
    name: "Genome-Wide Association Study - Type 2 Diabetes",
    type: "GWAS",
    samples: 1247,
    traces: 8934,
    created: "2026-03-10",
    pi: "Dr. Sarah Martinez",
    status: "active" as const,
    tags: ["Diabetes", "GWAS", "Population Study"],
    progress: 78,
  },
  {
    id: "GF-2026-087",
    name: "Whole Exome Sequencing - Rare Disease Panel",
    type: "WES",
    samples: 342,
    traces: 2156,
    created: "2026-03-08",
    pi: "Dr. James Wong",
    status: "active" as const,
    tags: ["Rare Disease", "Clinical", "Exome"],
    progress: 45,
  },
  {
    id: "GF-2026-085",
    name: "RNA-Seq Analysis - Cancer Biomarkers",
    type: "RNA-Seq",
    samples: 856,
    traces: 6123,
    created: "2026-03-05",
    pi: "Dr. Emily Chen",
    status: "completed" as const,
    tags: ["Cancer", "Biomarkers", "Transcriptomics"],
    progress: 100,
  },
  {
    id: "GF-2026-082",
    name: "Targeted Sequencing - BRCA1/2 Variants",
    type: "Targeted",
    samples: 125,
    traces: 892,
    created: "2026-03-02",
    pi: "Dr. Michael Park",
    status: "draft" as const,
    tags: ["BRCA", "Hereditary Cancer", "Targeted"],
    progress: 12,
  },
  {
    id: "GF-2026-078",
    name: "Whole Genome Sequencing - Cardiovascular Risk",
    type: "WGS",
    samples: 2341,
    traces: 15234,
    created: "2026-02-28",
    pi: "Dr. Lisa Anderson",
    status: "active" as const,
    tags: ["Cardiovascular", "WGS", "Prevention"],
    progress: 62,
  },
  {
    id: "GF-2026-075",
    name: "Microbiome Analysis - IBD Cohort",
    type: "Metagenomics",
    samples: 567,
    traces: 4521,
    created: "2026-02-25",
    pi: "Dr. Robert Kim",
    status: "active" as const,
    tags: ["Microbiome", "IBD", "Metagenomics"],
    progress: 89,
  },
  {
    id: "GF-2026-071",
    name: "Pharmacogenomics - Drug Response Study",
    type: "PGx",
    samples: 893,
    traces: 5832,
    created: "2026-02-20",
    pi: "Dr. Maria Garcia",
    status: "completed" as const,
    tags: ["PGx", "Drug Response", "Precision Medicine"],
    progress: 100,
  },
  {
    id: "GF-2026-068",
    name: "Single Cell RNA-Seq - Tumor Heterogeneity",
    type: "scRNA-Seq",
    samples: 214,
    traces: 3421,
    created: "2026-02-15",
    pi: "Dr. David Lee",
    status: "active" as const,
    tags: ["Single Cell", "Cancer", "Heterogeneity"],
    progress: 34,
  },
];

export default function StudiesPage() {
  const t = useTranslations("studies");
  const tCommon = useTranslations("common");
  const [searchQuery, setSearchQuery] = useState("");
  const [selectedType, setSelectedType] = useState("All Types");
  const [selectedStatus, setSelectedStatus] = useState("all");
  const [newStudyOpen, setNewStudyOpen] = useState(false);
  const [showFilters, setShowFilters] = useState(false);
  const [currentPage, setCurrentPage] = useState(1);
  const [exportOpen, setExportOpen] = useState(false);
  const [deleteStudyOpen, setDeleteStudyOpen] = useState(false);
  const [selectedStudy, setSelectedStudy] = useState<typeof studies[0] | null>(null);

  const handleExport = (format: string) => {
    // Simulate export
    console.log(`Exporting studies as ${format}`);
    setExportOpen(false);
  };

  const handleDeleteStudy = () => {
    if (selectedStudy) {
      console.log(`Deleting study ${selectedStudy.id}`);
      setDeleteStudyOpen(false);
      setSelectedStudy(null);
    }
  };

  const filteredStudies = studies.filter((study) => {
    const matchesSearch =
      searchQuery === "" ||
      study.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
      study.id.toLowerCase().includes(searchQuery.toLowerCase()) ||
      study.pi.toLowerCase().includes(searchQuery.toLowerCase());
    const matchesType =
      selectedType === "All Types" || study.type === selectedType;
    const matchesStatus =
      selectedStatus === "all" ||
      study.status === selectedStatus;
    return matchesSearch && matchesType && matchesStatus;
  });

  const totalPages = Math.ceil(filteredStudies.length / 6);
  const paginatedStudies = filteredStudies.slice(
    (currentPage - 1) * 6,
    currentPage * 6
  );

  return (
    <div className="space-y-6">
      {/* Page Header */}
      <div className="flex items-start justify-between">
        <div className="space-y-1">
          <h1 className="text-2xl font-semibold text-foreground">{t("title")}</h1>
          <p className="text-sm text-muted-foreground">
            {t("description")}
          </p>
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
              <span className="text-sm text-muted-foreground">{t("filters.type")}:</span>
              <div className="relative">
                <select
                  value={selectedType}
                  onChange={(e) => setSelectedType(e.target.value)}
                  className="cursor-pointer appearance-none rounded-lg border border-border bg-background py-1.5 pl-3 pr-8 text-sm focus:outline-none focus:ring-2 focus:ring-teal/20"
                >
                  {studyTypes.map((type) => (
                    <option key={type} value={type}>
                      {type === "All Types" ? t("filters.allTypes") : type}
                    </option>
                  ))}
                </select>
                <ChevronDown className="pointer-events-none absolute right-2 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              </div>
            </div>
            <div className="flex items-center gap-2">
              <span className="text-sm text-muted-foreground">{t("filters.status")}:</span>
              <div className="relative">
                <select
                  value={selectedStatus}
                  onChange={(e) => setSelectedStatus(e.target.value)}
                  className="cursor-pointer appearance-none rounded-lg border border-border bg-background py-1.5 pl-3 pr-8 text-sm focus:outline-none focus:ring-2 focus:ring-teal/20"
                >
                  <option value="all">{t("filters.allStatus")}</option>
                  {statusOptions.slice(1).map((status) => (
                    <option key={status.value} value={status.value}>
                      {t(`status.${status.key}`)}
                    </option>
                  ))}
                </select>
                <ChevronDown className="pointer-events-none absolute right-2 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              </div>
            </div>
            {(selectedType !== "All Types" ||
              selectedStatus !== "all") && (
              <button
                onClick={() => {
                  setSelectedType("All Types");
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
            searchQuery || selectedType !== "All Types" || selectedStatus !== "all"
              ? t("noStudiesDescription")
              : t("createFirstStudy")
          }
        >
          {!searchQuery && selectedType === "All Types" && selectedStatus === "all" && (
            <Button onClick={() => setNewStudyOpen(true)}>
              <Plus className="h-4 w-4" />
              {t("newStudy")}
            </Button>
          )}
        </EmptyState>
      ) : (
        <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
          {paginatedStudies.map((study) => (
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
                    <StatusBadge status={study.status} />
                  </div>
                  <h2 className="line-clamp-2 text-base font-medium text-foreground transition-colors duration-200 group-hover:text-teal">
                    {study.name}
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
                      aria-label={`More options for ${study.name}`}
                    >
                      <MoreVertical className="h-4 w-4" />
                    </button>
                  </DropdownMenuTrigger>
                  <DropdownMenuContent align="end" className="w-48" onClick={(e) => e.stopPropagation()}>
                    <DropdownMenuItem asChild>
                      <Link href={`/studies/${study.id}`} className="flex items-center gap-2">
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

              {/* Progress Bar */}
              <div className="mb-4">
                <div className="mb-1 flex items-center justify-between text-xs">
                  <span className="text-muted-foreground">{t("card.progress")}</span>
                  <span className="font-medium text-foreground">
                    {study.progress}%
                  </span>
                </div>
                <div className="h-1.5 overflow-hidden rounded-full bg-muted">
                  <div
                    className={cn(
                      "h-full rounded-full transition-all",
                      study.progress === 100
                        ? "bg-emerald-500"
                        : "bg-gradient-to-r from-teal to-blue-deep"
                    )}
                    style={{ width: `${study.progress}%` }}
                  />
                </div>
              </div>

              <div className="mb-4 grid grid-cols-3 gap-4">
                <div className="space-y-1">
                  <div className="flex items-center gap-1.5 text-muted-foreground">
                    <FileText className="h-3.5 w-3.5" />
                    <span className="text-xs">{t("card.type")}</span>
                  </div>
                  <p className="text-sm font-medium text-foreground">
                    {study.type}
                  </p>
                </div>
                <div className="space-y-1">
                  <div className="flex items-center gap-1.5 text-muted-foreground">
                    <Users className="h-3.5 w-3.5" />
                    <span className="text-xs">{t("card.samples")}</span>
                  </div>
                  <p className="text-sm font-medium text-foreground">
                    {study.samples.toLocaleString()}
                  </p>
                </div>
                <div className="space-y-1">
                  <div className="flex items-center gap-1.5 text-muted-foreground">
                    <Calendar className="h-3.5 w-3.5" />
                    <span className="text-xs">{t("card.created")}</span>
                  </div>
                  <p className="text-sm font-medium text-foreground">
                    {new Date(study.created).toLocaleDateString(undefined, {
                      month: "short",
                      day: "numeric",
                    })}
                  </p>
                </div>
              </div>

              <div className="mb-4">
                <p className="mb-1 text-xs text-muted-foreground">
                  {t("card.pi")}
                </p>
                <p className="text-sm font-medium text-foreground">
                  {study.pi}
                </p>
              </div>

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
            </Link>
          ))}
        </div>
      )}

      {/* Pagination */}
      {filteredStudies.length > 0 && (
        <div className="flex items-center justify-between rounded-xl border border-border bg-card px-6 py-4 shadow-sm">
          <p className="text-sm text-muted-foreground">
            {t("pagination.showing")}{" "}
            <span className="font-medium text-foreground">
              {(currentPage - 1) * 6 + 1}-
              {Math.min(currentPage * 6, filteredStudies.length)}
            </span>{" "}
            {t("pagination.of")}{" "}
            <span className="font-medium text-foreground">
              {filteredStudies.length}
            </span>{" "}
            {t("pagination.studies")}
          </p>
          <div className="flex gap-2">
            <button
              onClick={() => setCurrentPage((p) => Math.max(1, p - 1))}
              disabled={currentPage === 1}
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
              disabled={currentPage === totalPages}
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
            <DialogDescription>
              {t("export.description")}
            </DialogDescription>
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
              {t("delete.description", { name: selectedStudy?.name ?? "" })}
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
              className="rounded-lg bg-red-500 px-4 py-2.5 text-sm font-medium text-white hover:bg-red-500/90"
            >
              {t("delete.confirm")}
            </button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* New Study Dialog */}
      <Dialog open={newStudyOpen} onOpenChange={setNewStudyOpen}>
        <DialogContent className="sm:max-w-[540px]">
          <DialogHeader>
            <DialogTitle>{t("create.title")}</DialogTitle>
            <DialogDescription>
              {t("create.description")}
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-5 py-4">
            <div className="space-y-2">
              <label
                htmlFor="study-name"
                className="text-sm font-medium text-foreground"
              >
                {t("create.studyName")}
              </label>
              <input
                id="study-name"
                type="text"
                className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm text-foreground transition-all placeholder:text-muted-foreground focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/10"
                placeholder={t("create.studyNamePlaceholder")}
              />
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <label
                  htmlFor="study-type"
                  className="text-sm font-medium text-foreground"
                >
                  {t("create.studyType")}
                </label>
                <select
                  id="study-type"
                  className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm text-foreground transition-all focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/10"
                >
                  <option value="">{t("create.selectType")}</option>
                  <option value="GWAS">GWAS</option>
                  <option value="WES">Whole Exome Sequencing</option>
                  <option value="WGS">Whole Genome Sequencing</option>
                  <option value="RNA-Seq">RNA-Seq</option>
                  <option value="Targeted">Targeted Sequencing</option>
                  <option value="Metagenomics">Metagenomics</option>
                  <option value="PGx">Pharmacogenomics</option>
                  <option value="scRNA-Seq">Single Cell RNA-Seq</option>
                </select>
              </div>

              <div className="space-y-2">
                <label
                  htmlFor="sample-count"
                  className="text-sm font-medium text-foreground"
                >
                  {t("create.expectedSamples")}
                </label>
                <input
                  id="sample-count"
                  type="number"
                  className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm text-foreground transition-all placeholder:text-muted-foreground focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/10"
                  placeholder={t("create.expectedSamplesPlaceholder")}
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
                className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm text-foreground transition-all placeholder:text-muted-foreground focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/10"
                placeholder={t("create.piPlaceholder")}
              />
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <label
                  htmlFor="status"
                  className="text-sm font-medium text-foreground"
                >
                  {t("create.initialStatus")}
                </label>
                <select
                  id="status"
                  className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm text-foreground transition-all focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/10"
                >
                  <option value="draft">{t("status.draft")}</option>
                  <option value="active">{t("status.active")}</option>
                </select>
              </div>

              <div className="space-y-2">
                <label
                  htmlFor="start-date"
                  className="text-sm font-medium text-foreground"
                >
                  {t("create.startDate")}
                </label>
                <input
                  id="start-date"
                  type="date"
                  className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm text-foreground transition-all focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/10"
                />
              </div>
            </div>

            <div className="space-y-2">
              <label
                htmlFor="tags"
                className="text-sm font-medium text-foreground"
              >
                {t("create.tags")}
              </label>
              <input
                id="tags"
                type="text"
                className="w-full rounded-lg border border-border bg-background px-3.5 py-2.5 text-sm text-foreground transition-all placeholder:text-muted-foreground focus:border-teal focus:outline-none focus:ring-2 focus:ring-teal/10"
                placeholder={t("create.tagsPlaceholder")}
              />
              <p className="mt-1.5 text-xs text-muted-foreground">
                {t("create.tagsHelp")}
              </p>
            </div>

            <div className="space-y-2">
              <label
                htmlFor="description"
                className="text-sm font-medium text-foreground"
              >
                {t("create.studyDescription")}
              </label>
              <textarea
                id="description"
                rows={3}
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
              className="rounded-lg bg-teal px-4 py-2.5 text-sm font-medium text-white shadow-sm transition-all hover:bg-teal/90 hover:shadow-md"
              onClick={() => setNewStudyOpen(false)}
            >
              {t("create.submit")}
            </button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
