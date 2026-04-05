"use client";

import { useState, useRef } from "react";
import { useTranslations } from "next-intl";
import { Link } from "@/lib/navigation";
import {
  Search,
  Filter,
  Download,
  Upload,
  MoreVertical,
  Eye,
  Trash2,
  CheckCircle2,
  Clock,
  XCircle,
  FileText,
  Calendar,
  User,
  ArrowUpDown,
  ChevronDown,
  Waves,
  RotateCcw,
  Copy,
  ArrowUp,
  ArrowDown,
} from "lucide-react";
import { cn } from "@/lib/utils";
import { EmptyState } from "@/components/shared";
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

const traces = [
  { id: "TR-2026-08945", sample: "SMD-T2D-1258", study: "GF-2026-089", studyName: "Type 2 Diabetes GWAS", uploaded: "2026-03-16 16:45", owner: "Dr. Sarah Martinez", status: "Processed", quality: 98.2, size: "2.4 MB" },
  { id: "TR-2026-08944", sample: "SMD-T2D-1257", study: "GF-2026-089", studyName: "Type 2 Diabetes GWAS", uploaded: "2026-03-16 16:42", owner: "Dr. Sarah Martinez", status: "Processed", quality: 97.8, size: "2.3 MB" },
  { id: "TR-2026-08943", sample: "SMD-T2D-1256", study: "GF-2026-089", studyName: "Type 2 Diabetes GWAS", uploaded: "2026-03-16 16:40", owner: "A. Rodriguez", status: "Processing", quality: null, size: "2.5 MB" },
  { id: "TR-2026-08942", sample: "SMD-RD-0445", study: "GF-2026-087", studyName: "Rare Disease Panel", uploaded: "2026-03-16 16:38", owner: "Dr. James Wong", status: "Validating", quality: null, size: "2.2 MB" },
  { id: "TR-2026-08941", sample: "SMD-T2D-1255", study: "GF-2026-089", studyName: "Type 2 Diabetes GWAS", uploaded: "2026-03-16 16:35", owner: "A. Rodriguez", status: "Failed", quality: 82.1, size: "2.4 MB" },
  { id: "TR-2026-08940", sample: "SMD-CB-0789", study: "GF-2026-085", studyName: "Cancer Biomarkers", uploaded: "2026-03-16 16:30", owner: "Dr. Emily Chen", status: "Processed", quality: 96.5, size: "2.6 MB" },
  { id: "TR-2026-08939", sample: "SMD-CB-0788", study: "GF-2026-085", studyName: "Cancer Biomarkers", uploaded: "2026-03-16 16:28", owner: "Dr. Emily Chen", status: "Processed", quality: 97.1, size: "2.5 MB" },
  { id: "TR-2026-08938", sample: "SMD-BRCA-0156", study: "GF-2026-082", studyName: "BRCA Variants", uploaded: "2026-03-16 16:25", owner: "Dr. Michael Park", status: "Uploaded", quality: null, size: "2.3 MB" },
  { id: "TR-2026-08937", sample: "SMD-RD-0444", study: "GF-2026-087", studyName: "Rare Disease Panel", uploaded: "2026-03-16 16:22", owner: "Dr. James Wong", status: "Processing", quality: null, size: "2.4 MB" },
  { id: "TR-2026-08936", sample: "SMD-T2D-1254", study: "GF-2026-089", studyName: "Type 2 Diabetes GWAS", uploaded: "2026-03-16 16:20", owner: "Dr. Sarah Martinez", status: "Processed", quality: 98.5, size: "2.5 MB" },
  { id: "TR-2026-08935", sample: "SMD-CB-0787", study: "GF-2026-085", studyName: "Cancer Biomarkers", uploaded: "2026-03-16 16:18", owner: "Dr. Emily Chen", status: "Processed", quality: 95.8, size: "2.4 MB" },
  { id: "TR-2026-08934", sample: "SMD-T2D-1253", study: "GF-2026-089", studyName: "Type 2 Diabetes GWAS", uploaded: "2026-03-16 16:15", owner: "A. Rodriguez", status: "Failed", quality: 79.3, size: "2.1 MB" },
];

const statusConfig = {
  Uploaded: { color: "text-indigo-500", bg: "bg-indigo-500/10", icon: FileText },
  Validating: { color: "text-amber-500", bg: "bg-amber-500/10", icon: Clock },
  Processing: { color: "text-teal", bg: "bg-teal/10", icon: Clock },
  Processed: { color: "text-emerald-500", bg: "bg-emerald-500/10", icon: CheckCircle2 },
  Failed: { color: "text-red-500", bg: "bg-red-500/10", icon: XCircle },
};

const stats = [
  { labelKey: "totalTraces", value: "12,847", statusKey: "processed", color: "text-emerald-500" },
  { labelKey: "processing", value: "2,341", statusKey: "inProgress", color: "text-teal" },
  { labelKey: "pending", value: "456", statusKey: "queued", color: "text-amber-500" },
  { labelKey: "failed", value: "23", statusKey: "requiresReview", color: "text-red-500" },
];

const statusOptions = [
  { value: "All Status", key: "allStatus" },
  { value: "Uploaded", key: "uploaded" },
  { value: "Validating", key: "validating" },
  { value: "Processing", key: "processing" },
  { value: "Processed", key: "processed" },
  { value: "Failed", key: "failed" },
] as const;
const dateRangeOptions = ["All Time", "Today", "Last 7 Days", "Last 30 Days", "Last 90 Days"];
const initialOwners = ["All Owners", ...Array.from(new Set(traces.map((t) => t.owner)))];

type SortField = "id" | "sample" | "uploaded" | "quality";
type SortDirection = "asc" | "desc";

export default function TracesPage() {
  const t = useTranslations("traces");
  const tCommon = useTranslations("common");
  const [tracesData, setTracesData] = useState(traces);
  const [selectedTraces, setSelectedTraces] = useState<string[]>([]);
  const [isDragging, setIsDragging] = useState(false);
  const [searchQuery, setSearchQuery] = useState("");
  const [selectedStatus, setSelectedStatus] = useState("All Status");
  const [selectedDateRange, setSelectedDateRange] = useState("All Time");
  const [selectedOwner, setSelectedOwner] = useState("All Owners");
  const [currentPage, setCurrentPage] = useState(1);
  const [notification, setNotification] = useState<{ message: string; type: "success" | "error" | "info" } | null>(null);
  const [uploadOpen, setUploadOpen] = useState(false);
  const [uploadedFiles, setUploadedFiles] = useState<File[]>([]);
  const [sortField, setSortField] = useState<SortField | null>(null);
  const [sortDirection, setSortDirection] = useState<SortDirection>("asc");
  const fileInputRef = useRef<HTMLInputElement>(null);
  const itemsPerPage = 10;

  const handleSort = (field: SortField) => {
    if (sortField === field) {
      setSortDirection(sortDirection === "asc" ? "desc" : "asc");
    } else {
      setSortField(field);
      setSortDirection("asc");
    }
  };

  const handleFileSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = Array.from(e.target.files || []);
    setUploadedFiles(files);
  };

  const handleUpload = () => {
    if (uploadedFiles.length > 0) {
      showNotification(`${uploadedFiles.length} file(s) uploaded successfully`, "success");
      setUploadedFiles([]);
      setUploadOpen(false);
    }
  };

  const handleReprocessSingle = (id: string) => {
    setTracesData((prev) =>
      prev.map((t) =>
        t.id === id ? { ...t, status: "Processing", quality: null } : t
      )
    );
    showNotification(`Trace ${id} queued for reprocessing`);
  };

  const handleCopyId = (id: string) => {
    navigator.clipboard.writeText(id);
    showNotification(`Copied ${id} to clipboard`, "info");
  };

  const showNotification = (message: string, type: "success" | "error" | "info" = "success") => {
    setNotification({ message, type });
    setTimeout(() => setNotification(null), 3000);
  };

  const handleDeleteSelected = () => {
    setTracesData((prev) => prev.filter((t) => !selectedTraces.includes(t.id)));
    showNotification(`${selectedTraces.length} trace(s) deleted successfully`);
    setSelectedTraces([]);
  };

  const handleDeleteSingle = (id: string) => {
    setTracesData((prev) => prev.filter((t) => t.id !== id));
    setSelectedTraces((prev) => prev.filter((t) => t !== id));
    showNotification("Trace deleted successfully");
  };

  const handleDownloadSelected = () => {
    showNotification(`Downloading ${selectedTraces.length} trace(s)...`, "info");
  };

  const handleDownloadSingle = (id: string) => {
    showNotification(`Downloading trace ${id}...`, "info");
  };

  const handleReprocess = () => {
    // Update status to "Processing" for selected traces
    setTracesData((prev) =>
      prev.map((t) =>
        selectedTraces.includes(t.id) ? { ...t, status: "Processing", quality: null } : t
      )
    );
    showNotification(`${selectedTraces.length} trace(s) queued for reprocessing`);
    setSelectedTraces([]);
  };

  const handleExport = () => {
    showNotification("Exporting traces to CSV...", "info");
  };

  const filteredTraces = tracesData
    .filter((trace) => {
      const matchesSearch =
        searchQuery === "" ||
        trace.id.toLowerCase().includes(searchQuery.toLowerCase()) ||
        trace.sample.toLowerCase().includes(searchQuery.toLowerCase()) ||
        trace.study.toLowerCase().includes(searchQuery.toLowerCase()) ||
        trace.owner.toLowerCase().includes(searchQuery.toLowerCase());
      const matchesStatus =
        selectedStatus === "All Status" || trace.status === selectedStatus;
      const matchesOwner =
        selectedOwner === "All Owners" || trace.owner === selectedOwner;

      // Date range filter
      let matchesDateRange = true;
      if (selectedDateRange !== "All Time") {
        const uploadDate = new Date(trace.uploaded.replace(" ", "T"));
        const now = new Date();
        const diffDays = Math.floor((now.getTime() - uploadDate.getTime()) / (1000 * 60 * 60 * 24));

        switch (selectedDateRange) {
          case "Today":
            matchesDateRange = diffDays === 0;
            break;
          case "Last 7 Days":
            matchesDateRange = diffDays <= 7;
            break;
          case "Last 30 Days":
            matchesDateRange = diffDays <= 30;
            break;
          case "Last 90 Days":
            matchesDateRange = diffDays <= 90;
            break;
        }
      }

      return matchesSearch && matchesStatus && matchesOwner && matchesDateRange;
    })
    .sort((a, b) => {
      if (!sortField) return 0;
      let comparison = 0;
      switch (sortField) {
        case "id":
          comparison = a.id.localeCompare(b.id);
          break;
        case "sample":
          comparison = a.sample.localeCompare(b.sample);
          break;
        case "uploaded":
          comparison = new Date(a.uploaded).getTime() - new Date(b.uploaded).getTime();
          break;
        case "quality":
          comparison = (a.quality || 0) - (b.quality || 0);
          break;
      }
      return sortDirection === "asc" ? comparison : -comparison;
    });

  const totalPages = Math.ceil(filteredTraces.length / itemsPerPage);
  const paginatedTraces = filteredTraces.slice(
    (currentPage - 1) * itemsPerPage,
    currentPage * itemsPerPage
  );

  const toggleSelectTrace = (id: string) => {
    if (selectedTraces.includes(id)) {
      setSelectedTraces(selectedTraces.filter((t) => t !== id));
    } else {
      setSelectedTraces([...selectedTraces, id]);
    }
  };

  const toggleSelectAll = () => {
    if (selectedTraces.length === paginatedTraces.length) {
      setSelectedTraces([]);
    } else {
      setSelectedTraces(paginatedTraces.map((t) => t.id));
    }
  };

  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragging(true);
  };

  const handleDragLeave = () => {
    setIsDragging(false);
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragging(false);
  };

  return (
    <div className="space-y-6">
      {/* Notification Toast */}
      {notification && (
        <div
          className={cn(
            "fixed right-6 top-20 z-50 flex items-center gap-3 rounded-lg border px-4 py-3 shadow-lg transition-all animate-in slide-in-from-top-2",
            notification.type === "success" && "border-emerald-500/30 bg-emerald-500/10 text-emerald-600 dark:text-emerald-400",
            notification.type === "error" && "border-red-500/30 bg-red-500/10 text-red-600 dark:text-red-400",
            notification.type === "info" && "border-blue-500/30 bg-blue-500/10 text-blue-600 dark:text-blue-400"
          )}
        >
          {notification.type === "success" && <CheckCircle2 className="h-5 w-5" />}
          {notification.type === "error" && <XCircle className="h-5 w-5" />}
          {notification.type === "info" && <Clock className="h-5 w-5" />}
          <span className="text-sm font-medium">{notification.message}</span>
        </div>
      )}

      {/* Page Header */}
      <div className="flex items-start justify-between">
        <div className="space-y-1">
          <h1 className="text-2xl font-semibold text-foreground">{t("title")}</h1>
          <p className="text-sm text-muted-foreground">
            {t("description")}
          </p>
        </div>
      </div>

      {/* Stats Cards */}
      <div className="grid grid-cols-1 gap-4 md:grid-cols-4">
        {stats.map((stat) => (
          <div key={stat.labelKey} className="rounded-lg border border-border bg-card p-5">
            <p className="mb-2 text-sm text-muted-foreground">{t(`stats.${stat.labelKey}`)}</p>
            <p className="mb-1 text-2xl font-semibold text-foreground">{stat.value}</p>
            <p className={cn("text-xs", stat.color)}>{t(`stats.${stat.statusKey}`)}</p>
          </div>
        ))}
      </div>

      {/* Batch Upload Area */}
      <div
        className={cn(
          "rounded-xl border-2 border-dashed bg-card p-8 transition-all",
          isDragging
            ? "border-teal bg-teal/5"
            : "border-border hover:border-teal/50 hover:bg-muted/20"
        )}
        onDragOver={handleDragOver}
        onDragLeave={handleDragLeave}
        onDrop={handleDrop}
      >
        <div className="flex flex-col items-center justify-center text-center">
          <div className="mb-4 flex h-14 w-14 items-center justify-center rounded-full bg-teal/10">
            <Upload className="h-6 w-6 text-teal" />
          </div>
          <h2 className="mb-2 text-base font-medium text-foreground">
            {t("upload.title")}
          </h2>
          <p className="mb-4 max-w-md text-sm text-muted-foreground">
            {t("upload.description")}
          </p>
          <button
            onClick={() => setUploadOpen(true)}
            className="rounded-lg bg-teal px-4 py-2 text-sm font-medium text-white transition-all hover:bg-teal/90"
          >
            {t("upload.chooseFiles")}
          </button>
          <p className="mt-4 text-xs text-muted-foreground">
            {t("upload.supportedFormats")}
          </p>
        </div>
      </div>

      {/* Filters and Search */}
      <div className="rounded-lg border border-border bg-card p-4">
        <div className="flex flex-col gap-4 lg:flex-row">
          <div className="relative flex-1">
            <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            <input
              type="text"
              placeholder={t("search.placeholder")}
              value={searchQuery}
              onChange={(e) => {
                setSearchQuery(e.target.value);
                setCurrentPage(1);
              }}
              className="w-full rounded-lg border border-border bg-background py-2 pl-10 pr-4 text-sm transition-all focus:border-teal/30 focus:outline-none focus:ring-2 focus:ring-teal/20"
            />
          </div>
          <div className="flex gap-2">
            <div className="relative">
              <label htmlFor="status-filter" className="sr-only">Filter by status</label>
              <select
                id="status-filter"
                value={selectedStatus}
                onChange={(e) => {
                  setSelectedStatus(e.target.value);
                  setCurrentPage(1);
                }}
                className="cursor-pointer appearance-none rounded-lg border border-border bg-card py-2 pl-4 pr-10 text-sm font-medium text-foreground transition-all hover:bg-muted/50 focus:outline-none focus:ring-2 focus:ring-teal/20"
              >
                {statusOptions.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.key === "allStatus" ? t(`filters.${option.key}`) : t(`status.${option.key}`)}
                  </option>
                ))}
              </select>
              <ChevronDown className="pointer-events-none absolute right-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            </div>
            <div className="relative">
              <label htmlFor="date-filter" className="sr-only">Filter by date range</label>
              <Calendar className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              <select
                id="date-filter"
                value={selectedDateRange}
                onChange={(e) => {
                  setSelectedDateRange(e.target.value);
                  setCurrentPage(1);
                }}
                className="cursor-pointer appearance-none rounded-lg border border-border bg-card py-2 pl-9 pr-10 text-sm font-medium text-foreground transition-all hover:bg-muted/50 focus:outline-none focus:ring-2 focus:ring-teal/20"
              >
                {dateRangeOptions.map((range) => (
                  <option key={range} value={range}>
                    {range}
                  </option>
                ))}
              </select>
              <ChevronDown className="pointer-events-none absolute right-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            </div>
            <div className="relative">
              <label htmlFor="owner-filter" className="sr-only">Filter by owner</label>
              <User className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              <select
                id="owner-filter"
                value={selectedOwner}
                onChange={(e) => {
                  setSelectedOwner(e.target.value);
                  setCurrentPage(1);
                }}
                className="cursor-pointer appearance-none rounded-lg border border-border bg-card py-2 pl-9 pr-10 text-sm font-medium text-foreground transition-all hover:bg-muted/50 focus:outline-none focus:ring-2 focus:ring-teal/20"
              >
                {initialOwners.map((owner) => (
                  <option key={owner} value={owner}>
                    {owner}
                  </option>
                ))}
              </select>
              <ChevronDown className="pointer-events-none absolute right-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            </div>
            <button
              onClick={handleExport}
              className="flex items-center gap-2 rounded-lg border border-border px-4 py-2 text-sm font-medium text-foreground transition-all hover:bg-muted/50 active:scale-[0.98]"
            >
              <Download className="h-4 w-4" />
              {tCommon("export")}
            </button>
          </div>
        </div>
      </div>

      {/* Batch Actions */}
      {selectedTraces.length > 0 && (
        <div className="rounded-lg border border-teal/20 bg-teal/5 p-4">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3">
              <CheckCircle2 className="h-5 w-5 text-teal" />
              <span className="text-sm font-medium text-foreground">
                {t("batch.selected", { count: selectedTraces.length })}
              </span>
            </div>
            <div className="flex items-center gap-2">
              <button
                onClick={handleDownloadSelected}
                className="rounded-lg border border-border bg-card px-4 py-2 text-sm font-medium text-foreground transition-all hover:bg-muted/50 active:scale-[0.98]"
              >
                {t("batch.downloadSelected")}
              </button>
              <button
                onClick={handleReprocess}
                className="rounded-lg border border-border bg-card px-4 py-2 text-sm font-medium text-foreground transition-all hover:bg-muted/50 active:scale-[0.98]"
              >
                {t("batch.reprocess")}
              </button>
              <button
                onClick={handleDeleteSelected}
                className="rounded-lg border border-red-500/30 bg-card px-4 py-2 text-sm font-medium text-red-500 transition-all hover:bg-red-500/5 active:scale-[0.98]"
              >
                {t("batch.deleteSelected")}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Traces Table */}
      {filteredTraces.length === 0 ? (
        <EmptyState
          icon={Waves}
          title={t("empty.title")}
          description={
            searchQuery || selectedStatus !== "All Status"
              ? t("empty.description")
              : t("empty.uploadFirst")
          }
        />
      ) : (
        <div className="overflow-hidden rounded-lg border border-border bg-card">
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead>
                <tr className="border-b border-border bg-muted/30">
                  <th className="px-6 py-3 text-left">
                    <input
                      type="checkbox"
                      checked={selectedTraces.length === paginatedTraces.length && paginatedTraces.length > 0}
                      onChange={toggleSelectAll}
                      className="h-4 w-4 rounded border-border text-teal focus:ring-teal focus:ring-offset-0"
                      aria-label="Select all traces"
                    />
                  </th>
                  <th className="px-6 py-3 text-left">
                    <button
                      onClick={() => handleSort("id")}
                      className={cn(
                        "flex items-center gap-1.5 text-xs font-medium uppercase transition-colors hover:text-foreground",
                        sortField === "id" ? "text-teal" : "text-muted-foreground"
                      )}
                    >
                      {t("table.traceId")}
                      {sortField === "id" ? (
                        sortDirection === "asc" ? <ArrowUp className="h-3.5 w-3.5" /> : <ArrowDown className="h-3.5 w-3.5" />
                      ) : (
                        <ArrowUpDown className="h-3.5 w-3.5" />
                      )}
                    </button>
                  </th>
                  <th className="px-6 py-3 text-left">
                    <button
                      onClick={() => handleSort("sample")}
                      className={cn(
                        "flex items-center gap-1.5 text-xs font-medium uppercase transition-colors hover:text-foreground",
                        sortField === "sample" ? "text-teal" : "text-muted-foreground"
                      )}
                    >
                      {t("table.sampleName")}
                      {sortField === "sample" ? (
                        sortDirection === "asc" ? <ArrowUp className="h-3.5 w-3.5" /> : <ArrowDown className="h-3.5 w-3.5" />
                      ) : (
                        <ArrowUpDown className="h-3.5 w-3.5" />
                      )}
                    </button>
                  </th>
                  <th className="px-6 py-3 text-left">
                    <span className="text-xs font-medium uppercase text-muted-foreground">{t("table.study")}</span>
                  </th>
                  <th className="px-6 py-3 text-left">
                    <button
                      onClick={() => handleSort("uploaded")}
                      className={cn(
                        "flex items-center gap-1.5 text-xs font-medium uppercase transition-colors hover:text-foreground",
                        sortField === "uploaded" ? "text-teal" : "text-muted-foreground"
                      )}
                    >
                      {t("table.uploadDate")}
                      {sortField === "uploaded" ? (
                        sortDirection === "asc" ? <ArrowUp className="h-3.5 w-3.5" /> : <ArrowDown className="h-3.5 w-3.5" />
                      ) : (
                        <ArrowUpDown className="h-3.5 w-3.5" />
                      )}
                    </button>
                  </th>
                  <th className="px-6 py-3 text-left">
                    <span className="text-xs font-medium uppercase text-muted-foreground">{t("table.owner")}</span>
                  </th>
                  <th className="px-6 py-3 text-left">
                    <span className="text-xs font-medium uppercase text-muted-foreground">{t("table.status")}</span>
                  </th>
                  <th className="px-6 py-3 text-left">
                    <button
                      onClick={() => handleSort("quality")}
                      className={cn(
                        "flex items-center gap-1.5 text-xs font-medium uppercase transition-colors hover:text-foreground",
                        sortField === "quality" ? "text-teal" : "text-muted-foreground"
                      )}
                    >
                      {t("table.quality")}
                      {sortField === "quality" ? (
                        sortDirection === "asc" ? <ArrowUp className="h-3.5 w-3.5" /> : <ArrowDown className="h-3.5 w-3.5" />
                      ) : (
                        <ArrowUpDown className="h-3.5 w-3.5" />
                      )}
                    </button>
                  </th>
                  <th className="px-6 py-3 text-left">
                    <span className="text-xs font-medium uppercase text-muted-foreground">{t("table.size")}</span>
                  </th>
                  <th className="px-6 py-3 text-left">
                    <span className="text-xs font-medium uppercase text-muted-foreground">{t("table.actions")}</span>
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border">
                {paginatedTraces.map((trace) => {
                  const config = statusConfig[trace.status as keyof typeof statusConfig];
                  const StatusIcon = config.icon;
                  return (
                    <tr
                      key={trace.id}
                      className={cn(
                        "transition-colors",
                        selectedTraces.includes(trace.id) ? "bg-teal/5" : "hover:bg-muted/20"
                      )}
                    >
                      <td className="px-6 py-4">
                        <input
                          type="checkbox"
                          checked={selectedTraces.includes(trace.id)}
                          onChange={() => toggleSelectTrace(trace.id)}
                          className="h-4 w-4 rounded border-border text-teal focus:ring-teal focus:ring-offset-0"
                          aria-label={`Select trace ${trace.id}`}
                        />
                      </td>
                      <td className="px-6 py-4">
                        <span className="text-sm font-medium text-blue-deep">{trace.id}</span>
                      </td>
                      <td className="px-6 py-4">
                        <span className="font-mono text-sm text-foreground">{trace.sample}</span>
                      </td>
                      <td className="px-6 py-4">
                        <Link
                          href={`/studies/${trace.study}`}
                          className="text-sm text-muted-foreground transition-colors hover:text-teal"
                        >
                          {trace.study}
                        </Link>
                      </td>
                      <td className="px-6 py-4">
                        <span className="text-sm text-muted-foreground">{trace.uploaded}</span>
                      </td>
                      <td className="px-6 py-4">
                        <span className="text-sm text-foreground">{trace.owner}</span>
                      </td>
                      <td className="px-6 py-4">
                        <span
                          className={cn(
                            "inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-medium",
                            config.bg,
                            config.color
                          )}
                        >
                          <StatusIcon className="h-3 w-3" />
                          {t(`status.${trace.status.toLowerCase()}`)}
                        </span>
                      </td>
                      <td className="px-6 py-4">
                        {trace.quality !== null ? (
                          <span
                            className={cn(
                              "text-sm font-medium",
                              trace.quality >= 95
                                ? "text-emerald-500"
                                : trace.quality >= 85
                                ? "text-amber-500"
                                : "text-red-500"
                            )}
                          >
                            {trace.quality}%
                          </span>
                        ) : (
                          <span className="text-sm text-muted-foreground">—</span>
                        )}
                      </td>
                      <td className="px-6 py-4">
                        <span className="text-sm text-muted-foreground">{trace.size}</span>
                      </td>
                      <td className="px-6 py-4">
                        <div className="flex items-center gap-1">
                          <Link
                            href={`/traces/${trace.id}`}
                            className="rounded-md p-1.5 text-muted-foreground transition-all hover:bg-teal/10 hover:text-teal"
                            aria-label={`View trace ${trace.id}`}
                          >
                            <Eye className="h-4 w-4" />
                          </Link>
                          <button
                            onClick={() => handleDownloadSingle(trace.id)}
                            className="rounded-md p-1.5 text-muted-foreground transition-all hover:bg-muted hover:text-foreground"
                            aria-label={`Download trace ${trace.id}`}
                          >
                            <Download className="h-4 w-4" />
                          </button>
                          <button
                            onClick={() => handleDeleteSingle(trace.id)}
                            className="rounded-md p-1.5 text-muted-foreground transition-all hover:bg-red-500/10 hover:text-red-500"
                            aria-label={`Delete trace ${trace.id}`}
                          >
                            <Trash2 className="h-4 w-4" />
                          </button>
                          <DropdownMenu>
                            <DropdownMenuTrigger asChild>
                              <button
                                className="rounded-md p-1.5 text-muted-foreground transition-all hover:bg-muted hover:text-foreground"
                                aria-label={`More actions for trace ${trace.id}`}
                              >
                                <MoreVertical className="h-4 w-4" />
                              </button>
                            </DropdownMenuTrigger>
                            <DropdownMenuContent align="end" className="w-48">
                              <DropdownMenuItem onClick={() => handleCopyId(trace.id)}>
                                <Copy className="h-4 w-4" />
                                {t("actions.copyId")}
                              </DropdownMenuItem>
                              <DropdownMenuItem onClick={() => handleReprocessSingle(trace.id)}>
                                <RotateCcw className="h-4 w-4" />
                                {t("actions.reprocess")}
                              </DropdownMenuItem>
                              <DropdownMenuSeparator />
                              <DropdownMenuItem
                                className="text-red-500 focus:text-red-500"
                                onClick={() => handleDeleteSingle(trace.id)}
                              >
                                <Trash2 className="h-4 w-4" />
                                {tCommon("delete")}
                              </DropdownMenuItem>
                            </DropdownMenuContent>
                          </DropdownMenu>
                        </div>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>

          {/* Table Footer with Pagination */}
          <div className="border-t border-border px-6 py-4">
            <div className="flex items-center justify-between">
              <p className="text-sm text-muted-foreground">
                {t("pagination.showing")}{" "}
                <span className="font-medium text-foreground">
                  {(currentPage - 1) * itemsPerPage + 1}-
                  {Math.min(currentPage * itemsPerPage, filteredTraces.length)}
                </span>{" "}
                {t("pagination.of")} <span className="font-medium text-foreground">{filteredTraces.length}</span> {t("pagination.traces")}
              </p>
              <div className="flex items-center gap-2">
                <button
                  onClick={() => setCurrentPage((p) => Math.max(1, p - 1))}
                  disabled={currentPage === 1}
                  className={cn(
                    "rounded-md border border-border px-3 py-1.5 text-sm font-medium transition-all",
                    currentPage === 1
                      ? "cursor-not-allowed opacity-50"
                      : "text-muted-foreground hover:bg-muted/50 hover:text-foreground"
                  )}
                >
                  {tCommon("previous")}
                </button>
                {Array.from({ length: Math.min(totalPages, 5) }, (_, i) => i + 1).map((page) => (
                  <button
                    key={page}
                    onClick={() => setCurrentPage(page)}
                    className={cn(
                      "rounded-md px-3 py-1.5 text-sm font-medium transition-all",
                      currentPage === page
                        ? "bg-teal text-white"
                        : "border border-border text-foreground hover:bg-muted/50"
                    )}
                  >
                    {page}
                  </button>
                ))}
                {totalPages > 5 && (
                  <>
                    <span className="px-2 text-sm text-muted-foreground">...</span>
                    <button
                      onClick={() => setCurrentPage(totalPages)}
                      className={cn(
                        "rounded-md px-3 py-1.5 text-sm font-medium transition-all",
                        currentPage === totalPages
                          ? "bg-teal text-white"
                          : "border border-border text-foreground hover:bg-muted/50"
                      )}
                    >
                      {totalPages}
                    </button>
                  </>
                )}
                <button
                  onClick={() => setCurrentPage((p) => Math.min(totalPages, p + 1))}
                  disabled={currentPage === totalPages}
                  className={cn(
                    "rounded-md border border-border px-3 py-1.5 text-sm font-medium transition-all",
                    currentPage === totalPages
                      ? "cursor-not-allowed opacity-50"
                      : "text-foreground hover:bg-muted/50"
                  )}
                >
                  {tCommon("next")}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Upload Traces Dialog */}
      <Dialog open={uploadOpen} onOpenChange={setUploadOpen}>
        <DialogContent className="sm:max-w-[500px]">
          <DialogHeader>
            <DialogTitle>{t("upload.dialogTitle")}</DialogTitle>
            <DialogDescription>
              {t("upload.dialogDescription")}
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-5 py-4">
            <div
              className={cn(
                "group cursor-pointer rounded-lg border-2 border-dashed p-8 text-center transition-all",
                uploadedFiles.length > 0
                  ? "border-teal bg-teal/5"
                  : "border-border hover:border-teal/50 hover:bg-muted/20"
              )}
              onClick={() => fileInputRef.current?.click()}
            >
              <input
                ref={fileInputRef}
                type="file"
                multiple
                accept=".ab1,.scf,.abi"
                onChange={handleFileSelect}
                className="hidden"
                aria-label="Choose trace files to upload"
              />
              <Upload className="mx-auto mb-3 h-10 w-10 text-muted-foreground group-hover:text-teal" />
              {uploadedFiles.length > 0 ? (
                <>
                  <p className="mb-1 text-sm font-medium text-foreground">
                    {t("upload.filesSelected", { count: uploadedFiles.length })}
                  </p>
                  <p className="text-xs text-muted-foreground">
                    {uploadedFiles.map((f) => f.name).join(", ")}
                  </p>
                </>
              ) : (
                <>
                  <p className="mb-1 text-sm font-medium text-foreground">
                    {t("upload.clickToUpload")}
                  </p>
                  <p className="text-xs text-muted-foreground">
                    {t("upload.fileTypes")}
                  </p>
                </>
              )}
            </div>
          </div>
          <DialogFooter>
            <button
              onClick={() => {
                setUploadedFiles([]);
                setUploadOpen(false);
              }}
              className="rounded-lg px-4 py-2.5 text-sm font-medium text-foreground hover:bg-muted"
            >
              {tCommon("cancel")}
            </button>
            <button
              onClick={handleUpload}
              disabled={uploadedFiles.length === 0}
              className="rounded-lg bg-teal px-4 py-2.5 text-sm font-medium text-white hover:bg-teal/90 disabled:opacity-50"
            >
              {t("upload.uploadFiles")}
            </button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
