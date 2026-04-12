"use client";

import { useState, useMemo } from "react";
import { useTranslations } from "next-intl";
import { Link } from "@/lib/navigation";
import {
  Search,
  Filter,
  Globe,
  Users,
  FileText,
  Award,
  Star,
  Eye,
  ChevronDown,
  Grid3X3,
  List,
  Loader2,
  AlertCircle,
} from "lucide-react";
import { cn } from "@/lib/utils";
import {
  usePublicStudies,
  useFeaturedStudies,
  useResearchFields,
} from "@/hooks";
import type { StudySummary, ResearchField } from "@/types";

export default function DiscoverPage() {
  const t = useTranslations("discover");
  const [viewMode, setViewMode] = useState<"grid" | "list">("grid");
  const [selectedFieldId, setSelectedFieldId] = useState<number | null>(null);
  const [searchQuery, setSearchQuery] = useState("");
  const [showFilters, setShowFilters] = useState(false);
  const [currentPage, setCurrentPage] = useState(1);
  const itemsPerPage = 6;

  // Fetch research fields for filter dropdown
  const { data: researchFieldsData } = useResearchFields();
  const researchFields: ResearchField[] = researchFieldsData ?? [];

  // Build filters for API
  const filters = useMemo(() => {
    const f: { researchFieldId?: number; search?: string } = {};
    if (selectedFieldId) f.researchFieldId = selectedFieldId;
    if (searchQuery.trim()) f.search = searchQuery.trim();
    return f;
  }, [selectedFieldId, searchQuery]);

  // Fetch public studies with filters and pagination
  const {
    data: publicStudiesData,
    isLoading: isLoadingPublic,
    error: publicError,
  } = usePublicStudies(filters, currentPage, itemsPerPage);

  // Fetch featured studies (only on first page with no filters)
  const showFeatured =
    currentPage === 1 && !selectedFieldId && !searchQuery.trim();
  const { data: featuredStudiesData, isLoading: isLoadingFeatured } =
    useFeaturedStudies(1, 3);

  const publicStudies: StudySummary[] = publicStudiesData?.items ?? [];
  const totalCount = publicStudiesData?.totalCount ?? 0;
  const totalPages = Math.ceil(totalCount / itemsPerPage);

  const featuredStudies: StudySummary[] = showFeatured
    ? (featuredStudiesData?.items ?? [])
    : [];

  const isLoading = isLoadingPublic || (showFeatured && isLoadingFeatured);

  // Format relative time
  const formatRelativeTime = (dateString: string): string => {
    const date = new Date(dateString);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffHours = Math.floor(diffMs / (1000 * 60 * 60));
    const diffDays = Math.floor(diffHours / 24);

    if (diffHours < 1) return t("time.justNow");
    if (diffHours < 24) return t("time.hoursAgo", { count: diffHours });
    if (diffDays < 7) return t("time.daysAgo", { count: diffDays });
    return date.toLocaleDateString();
  };

  return (
    <div className="space-y-8">
      {/* Hero Section */}
      <div className="-mx-16 -mt-10 border-b border-border bg-gradient-to-b from-card to-background px-16 py-12">
        <div className="mx-auto max-w-3xl text-center">
          <div className="mb-4 inline-flex items-center gap-2 rounded-full bg-teal/10 px-3 py-1 text-sm font-medium text-teal">
            <Globe className="h-4 w-4" />
            {t("badge")}
          </div>
          <h1 className="mb-4 text-3xl font-semibold text-foreground">
            {t("title")}
          </h1>
          <p className="mb-8 text-base leading-relaxed text-muted-foreground">
            {t("description")}
          </p>

          {/* Search Bar */}
          <div className="relative mx-auto max-w-2xl">
            <Search className="pointer-events-none absolute left-4 top-1/2 h-5 w-5 -translate-y-1/2 text-muted-foreground" />
            <input
              type="text"
              placeholder={t("searchPlaceholder")}
              value={searchQuery}
              onChange={(e) => {
                setSearchQuery(e.target.value);
                setCurrentPage(1);
              }}
              className="w-full rounded-xl border border-border bg-card py-4 pl-12 pr-4 text-base shadow-sm transition-all duration-200 placeholder:text-muted-foreground/60 hover:shadow-md focus:border-teal/30 focus:outline-none focus:ring-2 focus:ring-teal/20 dark:border-border/50 dark:bg-card/50 dark:hover:bg-card"
            />
          </div>
        </div>
      </div>

      {/* Featured Studies */}
      {showFeatured && featuredStudies.length > 0 && (
        <div>
          <div className="mb-4 flex items-center gap-2">
            <Award className="h-5 w-5 text-amber-500" />
            <h2 className="text-lg font-semibold text-foreground">
              {t("featuredStudies")}
            </h2>
          </div>
          <div className="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-3">
            {featuredStudies.map((study) => (
              <Link
                key={study.id}
                href={`/studies/${study.id}`}
                className="group rounded-xl border border-teal/20 bg-gradient-to-br from-teal/5 via-card to-blue-deep/5 p-6 transition-all duration-300 hover:-translate-y-1 hover:border-teal/40 hover:shadow-xl hover:shadow-teal/5"
              >
                <div className="mb-4 flex items-start justify-between">
                  <span className="text-sm font-semibold tracking-tight text-blue-deep">
                    {study.id}
                  </span>
                  <div className="rounded-lg border border-amber-500/20 bg-amber-500/10 p-2">
                    <Award className="h-4 w-4 text-amber-500 transition-transform duration-200 group-hover:rotate-12 group-hover:scale-110" />
                  </div>
                </div>
                <h3 className="mb-2 line-clamp-2 font-semibold text-foreground transition-colors duration-200 group-hover:text-teal">
                  {study.title}
                </h3>
                <p className="mb-4 text-sm font-medium text-muted-foreground">
                  {study.institution || t("card.noInstitution")}
                </p>
                <div className="flex items-center gap-5 text-xs text-muted-foreground">
                  <span className="flex items-center gap-1.5">
                    <FileText className="h-3.5 w-3.5" strokeWidth={2} />
                    <span className="font-medium">
                      {study.paperCount.toLocaleString()}
                    </span>
                  </span>
                  <span className="flex items-center gap-1.5">
                    <Eye className="h-3.5 w-3.5" strokeWidth={2} />
                    <span className="font-medium">
                      {study.viewsCount.toLocaleString()}
                    </span>
                  </span>
                  <span className="flex items-center gap-1.5">
                    <Star className="h-3.5 w-3.5" strokeWidth={2} />
                    <span className="font-medium">{study.starsCount}</span>
                  </span>
                </div>
              </Link>
            ))}
          </div>
        </div>
      )}

      {/* Filters and View Controls */}
      <div className="flex flex-col items-start justify-between gap-4 lg:flex-row lg:items-center">
        <div className="flex flex-wrap items-center gap-2">
          <button
            onClick={() => setShowFilters(!showFilters)}
            className={cn(
              "flex items-center gap-2 rounded-lg border px-4 py-2 text-sm font-medium transition-all",
              showFilters
                ? "border-teal/30 bg-teal/10 text-teal"
                : "border-border bg-card text-foreground hover:bg-muted/50"
            )}
          >
            <Filter className="h-4 w-4" />
            {t("filters.button")}
            <ChevronDown
              className={cn(
                "h-3.5 w-3.5 transition-transform",
                showFilters && "rotate-180"
              )}
            />
          </button>

          {/* Research Field Filter */}
          <div className="relative">
            <select
              value={selectedFieldId ?? ""}
              onChange={(e) => {
                const val = e.target.value;
                setSelectedFieldId(val ? parseInt(val, 10) : null);
                setCurrentPage(1);
              }}
              className="cursor-pointer appearance-none rounded-lg border border-border bg-card py-2 pl-4 pr-10 text-sm font-medium text-foreground transition-all hover:bg-muted/50 focus:outline-none focus:ring-2 focus:ring-teal/20"
            >
              <option value="">{t("fields.allFields")}</option>
              {researchFields.map((field) => (
                <option key={field.id} value={field.id}>
                  {field.name}
                </option>
              ))}
            </select>
            <ChevronDown className="pointer-events-none absolute right-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          </div>

          {selectedFieldId && (
            <button
              onClick={() => {
                setSelectedFieldId(null);
                setCurrentPage(1);
              }}
              className="text-sm text-muted-foreground transition-colors hover:text-foreground"
            >
              {t("filters.clearFilters")}
            </button>
          )}
        </div>

        <div className="flex items-center gap-2">
          <span className="text-sm text-muted-foreground">
            {totalCount} {totalCount === 1 ? t("count.study") : t("count.studies")}
          </span>
          <div className="h-4 w-px bg-border" />
          <div className="flex items-center gap-1 rounded-lg bg-muted/30 p-1">
            <button
              onClick={() => setViewMode("grid")}
              className={cn(
                "rounded p-1.5 transition-all",
                viewMode === "grid"
                  ? "bg-card text-teal shadow-sm"
                  : "text-muted-foreground hover:text-foreground"
              )}
            >
              <Grid3X3 className="h-4 w-4" />
            </button>
            <button
              onClick={() => setViewMode("list")}
              className={cn(
                "rounded p-1.5 transition-all",
                viewMode === "list"
                  ? "bg-card text-teal shadow-sm"
                  : "text-muted-foreground hover:text-foreground"
              )}
            >
              <List className="h-4 w-4" />
            </button>
          </div>
        </div>
      </div>

      {/* Advanced Filters Panel */}
      {showFilters && (
        <div className="rounded-xl border border-border bg-card p-6">
          <div className="grid grid-cols-1 gap-6 md:grid-cols-3">
            <div>
              <label className="mb-2 block text-sm font-medium text-foreground">
                {t("filters.visibility")}
              </label>
              <div className="space-y-2">
                <label className="flex items-center gap-2">
                  <input
                    type="checkbox"
                    defaultChecked
                    className="rounded border-border text-teal focus:ring-teal"
                  />
                  <span className="text-sm text-foreground">{t("filters.publicStudies")}</span>
                </label>
                <label className="flex items-center gap-2">
                  <input
                    type="checkbox"
                    className="rounded border-border text-teal focus:ring-teal"
                  />
                  <span className="text-sm text-foreground">
                    {t("filters.collaborativeProjects")}
                  </span>
                </label>
              </div>
            </div>
            <div>
              <label className="mb-2 block text-sm font-medium text-foreground">
                {t("filters.studyType")}
              </label>
              <div className="space-y-2">
                <label className="flex items-center gap-2">
                  <input
                    type="checkbox"
                    defaultChecked
                    className="rounded border-border text-teal focus:ring-teal"
                  />
                  <span className="text-sm text-foreground">{t("filters.wholeGenome")}</span>
                </label>
                <label className="flex items-center gap-2">
                  <input
                    type="checkbox"
                    defaultChecked
                    className="rounded border-border text-teal focus:ring-teal"
                  />
                  <span className="text-sm text-foreground">{t("filters.rnaSeq")}</span>
                </label>
                <label className="flex items-center gap-2">
                  <input
                    type="checkbox"
                    defaultChecked
                    className="rounded border-border text-teal focus:ring-teal"
                  />
                  <span className="text-sm text-foreground">{t("filters.targetedPanels")}</span>
                </label>
              </div>
            </div>
            <div>
              <label className="mb-2 block text-sm font-medium text-foreground">
                {t("filters.sampleSize")}
              </label>
              <div className="space-y-2">
                <label className="flex items-center gap-2">
                  <input
                    type="checkbox"
                    className="rounded border-border text-teal focus:ring-teal"
                  />
                  <span className="text-sm text-foreground">
                    {t("filters.lessThan100")}
                  </span>
                </label>
                <label className="flex items-center gap-2">
                  <input
                    type="checkbox"
                    className="rounded border-border text-teal focus:ring-teal"
                  />
                  <span className="text-sm text-foreground">
                    {t("filters.between100And1000")}
                  </span>
                </label>
                <label className="flex items-center gap-2">
                  <input
                    type="checkbox"
                    className="rounded border-border text-teal focus:ring-teal"
                  />
                  <span className="text-sm text-foreground">
                    {t("filters.moreThan1000")}
                  </span>
                </label>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Loading State */}
      {isLoading && (
        <div className="flex items-center justify-center py-16">
          <Loader2 className="h-8 w-8 animate-spin text-teal" />
        </div>
      )}

      {/* Error State */}
      {publicError && !isLoading && (
        <div className="rounded-xl border border-destructive/50 bg-destructive/10 p-6 text-center">
          <AlertCircle className="mx-auto mb-2 h-8 w-8 text-destructive" />
          <p className="text-sm text-destructive">
            {t("error.loadFailed")}
          </p>
        </div>
      )}

      {/* Studies Grid */}
      {!isLoading && !publicError && viewMode === "grid" && (
        <div className="grid grid-cols-1 gap-6 md:grid-cols-2 lg:grid-cols-3">
          {publicStudies.map((study) => (
            <Link
              key={study.id}
              href={`/studies/${study.id}`}
              className="group overflow-hidden rounded-xl border border-border bg-card transition-all duration-200 hover:-translate-y-0.5 hover:border-teal/40 hover:shadow-lg hover:shadow-black/5 active:scale-[0.99]"
            >
              <div className="p-6">
                <div className="mb-3 flex items-start justify-between">
                  <span className="text-sm font-medium text-blue-deep">
                    {study.id}
                  </span>
                  <div className="flex items-center gap-1.5 rounded-full bg-emerald-500/10 px-2.5 py-1">
                    <Globe className="h-3 w-3 text-emerald-500" />
                    <span className="text-xs font-medium text-emerald-500">
                      {t("card.public")}
                    </span>
                  </div>
                </div>

                <h3 className="mb-2 line-clamp-2 font-medium text-foreground transition-colors duration-200 group-hover:text-teal">
                  {study.title}
                </h3>

                <div className="mb-4 space-y-1">
                  <p className="text-sm text-muted-foreground">
                    {study.institution || t("card.noInstitution")}
                  </p>
                  {study.principalInvestigator && (
                    <p className="text-sm text-muted-foreground">
                      {study.principalInvestigator}
                    </p>
                  )}
                </div>

                {study.description && (
                  <p className="mb-4 line-clamp-2 text-sm text-muted-foreground">
                    {study.description}
                  </p>
                )}

                <div className="mb-4 flex flex-wrap items-center gap-1.5">
                  <span className="rounded-md bg-teal/10 px-2.5 py-1 text-xs font-medium text-teal">
                    {study.researchFieldName}
                  </span>
                  {study.tags.slice(0, 2).map((tag) => (
                    <span
                      key={tag}
                      className="rounded-md bg-muted px-2.5 py-1 text-xs text-muted-foreground"
                    >
                      {tag}
                    </span>
                  ))}
                </div>

                <div className="mb-4 grid grid-cols-2 gap-3">
                  <div className="rounded-lg bg-muted/30 p-3">
                    <p className="mb-1 text-xs text-muted-foreground">{t("card.papers")}</p>
                    <p className="text-sm font-semibold text-foreground">
                      {study.paperCount.toLocaleString()}
                    </p>
                  </div>
                  <div className="rounded-lg bg-muted/30 p-3">
                    <p className="mb-1 text-xs text-muted-foreground">{t("card.members")}</p>
                    <p className="text-sm font-semibold text-foreground">
                      {study.memberCount.toLocaleString()}
                    </p>
                  </div>
                </div>

                <div className="flex items-center justify-between border-t border-border pt-4">
                  <div className="flex items-center gap-1 text-xs text-muted-foreground">
                    <Users className="h-3.5 w-3.5" />
                    <span>{study.memberCount}</span>
                  </div>
                  <div className="flex items-center gap-3 text-xs text-muted-foreground">
                    <span className="flex items-center gap-1">
                      <Eye className="h-3.5 w-3.5" />
                      {study.viewsCount.toLocaleString()}
                    </span>
                    <span className="flex items-center gap-1">
                      <Star className="h-3.5 w-3.5" />
                      {study.starsCount}
                    </span>
                  </div>
                </div>
              </div>
            </Link>
          ))}
        </div>
      )}

      {/* Studies List */}
      {!isLoading && !publicError && viewMode === "list" && (
        <div className="space-y-4">
          {publicStudies.map((study) => (
            <Link
              key={study.id}
              href={`/studies/${study.id}`}
              className="group flex gap-6 rounded-xl border border-border bg-card p-6 transition-all duration-200 hover:border-teal/30 hover:shadow-md"
            >
              <div className="flex-1">
                <div className="mb-3 flex items-start justify-between">
                  <div className="flex items-center gap-3">
                    <span className="text-sm font-medium text-blue-deep">
                      {study.id}
                    </span>
                    <div className="flex items-center gap-1.5 rounded-full bg-emerald-500/10 px-2.5 py-1">
                      <Globe className="h-3 w-3 text-emerald-500" />
                      <span className="text-xs font-medium text-emerald-500">
                        {t("card.public")}
                      </span>
                    </div>
                    <span className="rounded-md bg-teal/10 px-2.5 py-1 text-xs font-medium text-teal">
                      {study.researchFieldName}
                    </span>
                  </div>
                </div>

                <h3 className="mb-2 text-lg font-medium text-foreground transition-colors duration-200 group-hover:text-teal">
                  {study.title}
                </h3>

                <div className="mb-3 flex items-center gap-4 text-sm text-muted-foreground">
                  <span>{study.institution || t("card.noInstitution")}</span>
                  {study.principalInvestigator && (
                    <>
                      <span>-</span>
                      <span>{study.principalInvestigator}</span>
                    </>
                  )}
                </div>

                {study.description && (
                  <p className="mb-4 line-clamp-2 text-sm text-muted-foreground">
                    {study.description}
                  </p>
                )}

                <div className="flex items-center gap-4 text-sm">
                  <span className="text-muted-foreground">
                    <span className="font-medium text-foreground">
                      {study.paperCount.toLocaleString()}
                    </span>{" "}
                    {t("card.papers").toLowerCase()}
                  </span>
                  <span className="text-muted-foreground">
                    <span className="font-medium text-foreground">
                      {study.memberCount.toLocaleString()}
                    </span>{" "}
                    {t("card.members").toLowerCase()}
                  </span>
                  <span className="flex items-center gap-1 text-muted-foreground">
                    <Users className="h-4 w-4" />
                    {study.memberCount}
                  </span>
                  <span className="flex items-center gap-1 text-muted-foreground">
                    <Eye className="h-4 w-4" />
                    {study.viewsCount.toLocaleString()}
                  </span>
                  <span className="flex items-center gap-1 text-muted-foreground">
                    <Star className="h-4 w-4" />
                    {study.starsCount}
                  </span>
                </div>
              </div>
            </Link>
          ))}
        </div>
      )}

      {/* Pagination */}
      {!isLoading && !publicError && totalCount > 0 && totalPages > 1 && (
        <div className="flex items-center justify-between rounded-xl border border-border bg-card px-6 py-4 shadow-sm">
          <p className="text-sm text-muted-foreground">
            {t("pagination.showing")}{" "}
            <span className="font-medium text-foreground">
              {(currentPage - 1) * itemsPerPage + 1}-
              {Math.min(currentPage * itemsPerPage, totalCount)}
            </span>{" "}
            {t("pagination.of")}{" "}
            <span className="font-medium text-foreground">{totalCount}</span>{" "}
            {t("pagination.studies")}
          </p>
          <div className="flex gap-2">
            <button
              onClick={() => setCurrentPage((p) => Math.max(1, p - 1))}
              disabled={currentPage === 1}
              className={cn(
                "rounded-lg border border-border px-3 py-2 text-sm font-medium transition-all duration-200 active:scale-95",
                currentPage === 1
                  ? "cursor-not-allowed opacity-50"
                  : "text-foreground hover:bg-muted/50"
              )}
            >
              {t("pagination.previous")}
            </button>
            {Array.from({ length: Math.min(totalPages, 5) }, (_, i) => {
              // Show pages around current page
              let page: number;
              if (totalPages <= 5) {
                page = i + 1;
              } else if (currentPage <= 3) {
                page = i + 1;
              } else if (currentPage >= totalPages - 2) {
                page = totalPages - 4 + i;
              } else {
                page = currentPage - 2 + i;
              }
              return (
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
              );
            })}
            <button
              onClick={() => setCurrentPage((p) => Math.min(totalPages, p + 1))}
              disabled={currentPage === totalPages}
              className={cn(
                "rounded-lg border border-border px-3 py-2 text-sm font-medium transition-all duration-200 active:scale-95",
                currentPage === totalPages
                  ? "cursor-not-allowed opacity-50"
                  : "text-foreground hover:bg-muted/50"
              )}
            >
              {t("pagination.next")}
            </button>
          </div>
        </div>
      )}

      {/* Empty State */}
      {!isLoading && !publicError && publicStudies.length === 0 && (
        <div className="py-16 text-center">
          <div className="mb-4 inline-flex items-center justify-center rounded-full bg-muted/50 p-4">
            <Search className="h-8 w-8 text-muted-foreground" />
          </div>
          <h3 className="mb-2 text-lg font-medium text-foreground">
            {t("empty.title")}
          </h3>
          <p className="mb-6 text-sm text-muted-foreground">
            {t("empty.description")}
          </p>
          <button
            onClick={() => {
              setSearchQuery("");
              setSelectedFieldId(null);
              setCurrentPage(1);
            }}
            className="rounded-lg bg-teal px-4 py-2 text-white transition-all hover:bg-teal/90"
          >
            {t("filters.clearAll")}
          </button>
        </div>
      )}
    </div>
  );
}
