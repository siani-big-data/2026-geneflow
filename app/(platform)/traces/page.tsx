"use client";

import { useState } from "react";
import Link from "next/link";
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
} from "lucide-react";
import { cn } from "@/lib/utils";
import { EmptyState } from "@/components/shared";

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
  { label: "Total Traces", value: "12,847", status: "Processed", color: "text-emerald-500" },
  { label: "Processing", value: "2,341", status: "In Progress", color: "text-teal" },
  { label: "Pending", value: "456", status: "Queued", color: "text-amber-500" },
  { label: "Failed", value: "23", status: "Requires Review", color: "text-red-500" },
];

const statusOptions = ["All Status", "Uploaded", "Validating", "Processing", "Processed", "Failed"];
const dateRangeOptions = ["All Time", "Today", "Last 7 Days", "Last 30 Days", "Last 90 Days"];
const ownerOptions = ["All Owners", ...Array.from(new Set(traces.map((t) => t.owner)))];

export default function TracesPage() {
  const [selectedTraces, setSelectedTraces] = useState<string[]>([]);
  const [isDragging, setIsDragging] = useState(false);
  const [searchQuery, setSearchQuery] = useState("");
  const [selectedStatus, setSelectedStatus] = useState("All Status");
  const [selectedDateRange, setSelectedDateRange] = useState("All Time");
  const [selectedOwner, setSelectedOwner] = useState("All Owners");
  const [currentPage, setCurrentPage] = useState(1);
  const itemsPerPage = 10;

  const filteredTraces = traces.filter((trace) => {
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
      {/* Page Header */}
      <div className="flex items-start justify-between">
        <div className="space-y-1">
          <h1 className="text-2xl font-semibold text-foreground">Chromatogram Traces</h1>
          <p className="text-sm text-muted-foreground">
            Upload, manage, and monitor sequencing trace files
          </p>
        </div>
      </div>

      {/* Stats Cards */}
      <div className="grid grid-cols-1 gap-4 md:grid-cols-4">
        {stats.map((stat) => (
          <div key={stat.label} className="rounded-lg border border-border bg-card p-5">
            <p className="mb-2 text-sm text-muted-foreground">{stat.label}</p>
            <p className="mb-1 text-2xl font-semibold text-foreground">{stat.value}</p>
            <p className={cn("text-xs", stat.color)}>{stat.status}</p>
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
          <h3 className="mb-2 text-base font-medium text-foreground">
            Upload Chromatogram Traces
          </h3>
          <p className="mb-4 max-w-md text-sm text-muted-foreground">
            Drag and drop your .ab1, .scf, or .abi files here, or click to browse
          </p>
          <button className="rounded-lg bg-teal px-4 py-2 text-sm font-medium text-white transition-all hover:bg-teal/90">
            Choose Files
          </button>
          <p className="mt-4 text-xs text-muted-foreground">
            Supported formats: AB1, SCF, ABI • Max file size: 50 MB per file
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
              placeholder="Search by trace ID, sample name, or study..."
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
              <select
                value={selectedStatus}
                onChange={(e) => {
                  setSelectedStatus(e.target.value);
                  setCurrentPage(1);
                }}
                className="cursor-pointer appearance-none rounded-lg border border-border bg-card py-2 pl-4 pr-10 text-sm font-medium text-foreground transition-all hover:bg-muted/50 focus:outline-none focus:ring-2 focus:ring-teal/20"
              >
                {statusOptions.map((status) => (
                  <option key={status} value={status}>
                    {status}
                  </option>
                ))}
              </select>
              <ChevronDown className="pointer-events-none absolute right-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            </div>
            <div className="relative">
              <Calendar className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              <select
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
              <User className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              <select
                value={selectedOwner}
                onChange={(e) => {
                  setSelectedOwner(e.target.value);
                  setCurrentPage(1);
                }}
                className="cursor-pointer appearance-none rounded-lg border border-border bg-card py-2 pl-9 pr-10 text-sm font-medium text-foreground transition-all hover:bg-muted/50 focus:outline-none focus:ring-2 focus:ring-teal/20"
              >
                {ownerOptions.map((owner) => (
                  <option key={owner} value={owner}>
                    {owner}
                  </option>
                ))}
              </select>
              <ChevronDown className="pointer-events-none absolute right-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            </div>
            <button className="flex items-center gap-2 rounded-lg border border-border px-4 py-2 text-sm font-medium text-foreground transition-all hover:bg-muted/50">
              <Download className="h-4 w-4" />
              Export
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
                {selectedTraces.length} trace{selectedTraces.length > 1 ? "s" : ""} selected
              </span>
            </div>
            <div className="flex items-center gap-2">
              <button className="rounded-lg border border-border bg-card px-4 py-2 text-sm font-medium text-foreground transition-all hover:bg-muted/50">
                Download Selected
              </button>
              <button className="rounded-lg border border-border bg-card px-4 py-2 text-sm font-medium text-foreground transition-all hover:bg-muted/50">
                Reprocess
              </button>
              <button className="rounded-lg border border-red-500/30 bg-card px-4 py-2 text-sm font-medium text-red-500 transition-all hover:bg-red-500/5">
                Delete Selected
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Traces Table */}
      {filteredTraces.length === 0 ? (
        <EmptyState
          icon={Waves}
          title="No traces found"
          description={
            searchQuery || selectedStatus !== "All Status"
              ? "Try adjusting your search or filters"
              : "Upload your first trace to get started"
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
                    />
                  </th>
                  <th className="px-6 py-3 text-left">
                    <button className="flex items-center gap-1.5 text-xs font-medium uppercase text-muted-foreground transition-colors hover:text-foreground">
                      Trace ID
                      <ArrowUpDown className="h-3.5 w-3.5" />
                    </button>
                  </th>
                  <th className="px-6 py-3 text-left">
                    <button className="flex items-center gap-1.5 text-xs font-medium uppercase text-muted-foreground transition-colors hover:text-foreground">
                      Sample Name
                      <ArrowUpDown className="h-3.5 w-3.5" />
                    </button>
                  </th>
                  <th className="px-6 py-3 text-left">
                    <span className="text-xs font-medium uppercase text-muted-foreground">Study</span>
                  </th>
                  <th className="px-6 py-3 text-left">
                    <button className="flex items-center gap-1.5 text-xs font-medium uppercase text-muted-foreground transition-colors hover:text-foreground">
                      Upload Date
                      <ArrowUpDown className="h-3.5 w-3.5" />
                    </button>
                  </th>
                  <th className="px-6 py-3 text-left">
                    <span className="text-xs font-medium uppercase text-muted-foreground">Owner</span>
                  </th>
                  <th className="px-6 py-3 text-left">
                    <span className="text-xs font-medium uppercase text-muted-foreground">Status</span>
                  </th>
                  <th className="px-6 py-3 text-left">
                    <button className="flex items-center gap-1.5 text-xs font-medium uppercase text-muted-foreground transition-colors hover:text-foreground">
                      Quality
                      <ArrowUpDown className="h-3.5 w-3.5" />
                    </button>
                  </th>
                  <th className="px-6 py-3 text-left">
                    <span className="text-xs font-medium uppercase text-muted-foreground">Size</span>
                  </th>
                  <th className="px-6 py-3 text-left">
                    <span className="text-xs font-medium uppercase text-muted-foreground">Actions</span>
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
                          {trace.status}
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
                            title="View trace"
                          >
                            <Eye className="h-4 w-4" />
                          </Link>
                          <button
                            className="rounded-md p-1.5 text-muted-foreground transition-all hover:bg-muted hover:text-foreground"
                            title="Download"
                          >
                            <Download className="h-4 w-4" />
                          </button>
                          <button
                            className="rounded-md p-1.5 text-muted-foreground transition-all hover:bg-red-500/10 hover:text-red-500"
                            title="Delete"
                          >
                            <Trash2 className="h-4 w-4" />
                          </button>
                          <button
                            className="rounded-md p-1.5 text-muted-foreground transition-all hover:bg-muted hover:text-foreground"
                            title="More actions"
                          >
                            <MoreVertical className="h-4 w-4" />
                          </button>
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
                Showing{" "}
                <span className="font-medium text-foreground">
                  {(currentPage - 1) * itemsPerPage + 1}-
                  {Math.min(currentPage * itemsPerPage, filteredTraces.length)}
                </span>{" "}
                of <span className="font-medium text-foreground">{filteredTraces.length}</span> traces
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
                  Previous
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
                  Next
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
