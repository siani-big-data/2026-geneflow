"use client";

import { useState, useCallback } from "react";
import { useTranslations } from "next-intl";
import { Link } from "@/lib/navigation";
import {
  ArrowLeft,
  ChevronDown,
  Download,
  Share2,
  Printer,
  CheckCircle2,
  Info,
  AlertCircle,
  Sparkles,
  X,
  ZoomIn,
  ZoomOut,
  Maximize2,
  Activity,
  List,
  BarChart3,
  Copy,
  Scissors,
  MessageSquare,
} from "lucide-react";
import { cn } from "@/lib/utils";
import { AIAssistant, Chromatogram } from "@/components/shared";
import type { ChromatogramControls, ChromatogramStats } from "@/components/shared";

const availableTraces = [
  { id: "TR-2026-08945", sample: "SMD-T2D-1258", quality: 98.2 },
  { id: "TR-2026-08944", sample: "SMD-T2D-1257", quality: 97.8 },
  { id: "TR-2026-08940", sample: "SMD-CB-0789", quality: 96.5 },
  { id: "TR-2026-08939", sample: "SMD-CB-0788", quality: 97.1 },
  { id: "TR-2026-08936", sample: "SMD-T2D-1254", quality: 98.5 },
];

export default function TraceViewerPage() {
  const t = useTranslations("traces.detail");
  const [selectedTrace, setSelectedTrace] = useState(availableTraces[0]);
  const [showDropdown, setShowDropdown] = useState(false);
  const [aiAssistantOpen, setAiAssistantOpen] = useState(false);
  const [notification, setNotification] = useState<{
    message: string;
    type: "success" | "error" | "info";
  } | null>(null);

  // Chromatogram controls and stats
  const [chromControls, setChromControls] = useState<ChromatogramControls | null>(null);
  const [chromStats, setChromStats] = useState<ChromatogramStats | null>(null);

  const handleControlsReady = useCallback((controls: ChromatogramControls) => {
    setChromControls(controls);
  }, []);

  const handleStatsReady = useCallback((stats: ChromatogramStats) => {
    setChromStats(stats);
  }, []);

  const showNotification = (message: string, type: "success" | "error" | "info" = "success") => {
    setNotification({ message, type });
    setTimeout(() => setNotification(null), 3000);
  };

  const handleExport = () => {
    showNotification(t("notifications.preparingExport"), "info");
  };

  return (
    <div className="-mx-16 -mt-10 flex h-[calc(100vh-0px)] flex-col bg-background">
      {/* Toast Notification */}
      {notification && (
        <div
          className={cn(
            "fixed right-8 top-24 z-50 flex items-center gap-2 rounded-lg border px-4 py-3 shadow-lg transition-all",
            notification.type === "success" &&
              "border-emerald-500/30 bg-emerald-500/10 text-emerald-500",
            notification.type === "error" && "border-red-500/30 bg-red-500/10 text-red-500",
            notification.type === "info" && "border-blue-deep/30 bg-blue-deep/10 text-blue-deep"
          )}
        >
          {notification.type === "success" && <CheckCircle2 className="h-4 w-4" />}
          {notification.type === "error" && <AlertCircle className="h-4 w-4" />}
          {notification.type === "info" && <Info className="h-4 w-4" />}
          <span className="text-sm font-medium">{notification.message}</span>
        </div>
      )}

      {/* Top Control Bar */}
      <div className="flex-shrink-0 border-b border-border bg-card">
        <div className="px-16 py-3">
          {/* Back Button */}
          <Link
            href="/traces"
            className="mb-3 inline-flex items-center gap-2 text-sm text-muted-foreground transition-colors hover:text-foreground"
          >
            <ArrowLeft className="h-4 w-4" />
            {t("backToTraces")}
          </Link>

          {/* Main Header Row */}
          <div className="flex items-center justify-between gap-4">
            {/* Left: Trace Selector + Stats */}
            <div className="flex items-center gap-6">
              {/* Trace Selector */}
              <div className="relative">
                <button
                  onClick={() => setShowDropdown(!showDropdown)}
                  className="flex min-w-[280px] items-center gap-3 rounded-lg border border-border bg-background px-3 py-2 transition-all hover:bg-muted/30"
                >
                  <div className="flex-1 text-left">
                    <div className="flex items-center gap-2">
                      <span className="text-sm font-medium text-blue-deep">{selectedTrace.id}</span>
                      <span className="text-xs text-muted-foreground">-</span>
                      <span className="font-mono text-xs text-foreground">
                        {selectedTrace.sample}
                      </span>
                    </div>
                  </div>
                  <ChevronDown className="h-4 w-4 text-muted-foreground" />
                </button>

                {showDropdown && (
                  <div className="absolute left-0 top-full z-10 mt-2 w-full overflow-hidden rounded-lg border border-border bg-card shadow-lg">
                    <div className="border-b border-border bg-muted/20 p-2">
                      <input
                        type="text"
                        placeholder={t("searchTraces")}
                        className="w-full rounded border border-border bg-background px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-teal/20"
                      />
                    </div>
                    <div className="max-h-64 overflow-y-auto">
                      {availableTraces.map((trace) => (
                        <button
                          key={trace.id}
                          onClick={() => {
                            setSelectedTrace(trace);
                            setShowDropdown(false);
                          }}
                          className={cn(
                            "w-full border-l-2 px-4 py-2.5 text-left transition-colors hover:bg-muted/50",
                            selectedTrace.id === trace.id
                              ? "border-teal bg-teal/5"
                              : "border-transparent"
                          )}
                        >
                          <div className="flex items-center gap-2">
                            <span className="text-sm font-medium text-blue-deep">{trace.id}</span>
                            <span className="font-mono text-xs text-foreground">{trace.sample}</span>
                            <span className="ml-auto text-xs font-medium text-emerald-500">{trace.quality}%</span>
                          </div>
                        </button>
                      ))}
                    </div>
                  </div>
                )}
              </div>

              <div className="h-8 w-px bg-border" />

              {/* Stats from Chromatogram */}
              {chromStats && (
                <div className="flex items-center gap-5">
                  <div className="flex flex-col">
                    <span className="text-[10px] uppercase tracking-wide text-muted-foreground">{t("stats.length")}</span>
                    <span className="text-sm font-semibold text-foreground">{chromStats.sequenceLength} bp</span>
                  </div>
                  <div className="flex flex-col">
                    <span className="text-[10px] uppercase tracking-wide text-muted-foreground">{t("stats.quality")}</span>
                    <span className={cn(
                      "text-sm font-semibold",
                      chromStats.averageQuality >= 30 ? "text-emerald-500" : chromStats.averageQuality >= 20 ? "text-amber-500" : "text-red-500"
                    )}>
                      Q{chromStats.averageQuality}
                    </span>
                  </div>
                  <div className="flex flex-col">
                    <span className="text-[10px] uppercase tracking-wide text-muted-foreground">{t("stats.gc")}</span>
                    <span className="text-sm font-semibold text-foreground">{chromStats.gcContent}%</span>
                  </div>
                </div>
              )}
            </div>

            {/* Center: Chromatogram Controls */}
            {chromControls && (
              <div className="flex items-center gap-2">
                {/* View Mode Toggle */}
                <div className="flex rounded-lg border border-border bg-muted/30 p-0.5">
                  <button
                    onClick={() => chromControls.setViewMode("trace")}
                    className={cn(
                      "flex h-7 w-8 items-center justify-center rounded-md transition-all",
                      chromControls.viewMode === "trace"
                        ? "bg-teal text-white"
                        : "text-muted-foreground hover:text-foreground"
                    )}
                    title={t("viewModes.trace")}
                  >
                    <Activity className="h-4 w-4" />
                  </button>
                  <button
                    onClick={() => chromControls.setViewMode("sequence")}
                    className={cn(
                      "flex h-7 w-8 items-center justify-center rounded-md transition-all",
                      chromControls.viewMode === "sequence"
                        ? "bg-teal text-white"
                        : "text-muted-foreground hover:text-foreground"
                    )}
                    title={t("viewModes.sequence")}
                  >
                    <List className="h-4 w-4" />
                  </button>
                </div>

                <div className="h-6 w-px bg-border" />

                {/* Zoom Controls */}
                <div className="flex items-center gap-1 rounded-lg border border-border bg-muted/30 p-0.5">
                  <button
                    onClick={chromControls.zoomOut}
                    disabled={chromControls.zoomLevel <= chromControls.minZoom}
                    className="flex h-7 w-7 items-center justify-center rounded-md text-muted-foreground transition-all hover:bg-background hover:text-foreground disabled:opacity-40"
                    title={t("controls.zoomOut")}
                  >
                    <ZoomOut className="h-4 w-4" />
                  </button>
                  <span className="min-w-[42px] text-center text-xs font-medium text-foreground">
                    {Math.round(chromControls.zoomLevel * 100)}%
                  </span>
                  <button
                    onClick={chromControls.zoomIn}
                    disabled={chromControls.zoomLevel >= chromControls.maxZoom}
                    className="flex h-7 w-7 items-center justify-center rounded-md text-muted-foreground transition-all hover:bg-background hover:text-foreground disabled:opacity-40"
                    title={t("controls.zoomIn")}
                  >
                    <ZoomIn className="h-4 w-4" />
                  </button>
                  <button
                    onClick={chromControls.resetZoom}
                    className="flex h-7 w-7 items-center justify-center rounded-md text-muted-foreground transition-all hover:bg-background hover:text-foreground"
                    title={t("controls.resetZoom")}
                  >
                    <Maximize2 className="h-4 w-4" />
                  </button>
                </div>

                <div className="h-6 w-px bg-border" />

                {/* Quality Toggle */}
                <button
                  onClick={() => chromControls.setShowQuality(!chromControls.showQuality)}
                  className={cn(
                    "flex h-8 items-center gap-1.5 rounded-lg border px-2.5 text-xs font-medium transition-all",
                    chromControls.showQuality
                      ? "border-teal bg-teal text-white"
                      : "border-border text-muted-foreground hover:border-teal hover:text-foreground"
                  )}
                  title={t("controls.quality")}
                >
                  <BarChart3 className="h-3.5 w-3.5" />
                  {t("controls.quality")}
                </button>

                <div className="h-6 w-px bg-border" />

                {/* Tools - Placeholder for future */}
                <button
                  className="flex h-8 items-center gap-1.5 rounded-lg border border-border px-2.5 text-xs font-medium text-muted-foreground transition-all hover:border-teal hover:text-foreground"
                  title={t("controls.trim")}
                >
                  <Scissors className="h-3.5 w-3.5" />
                  {t("controls.trim")}
                </button>

                <button
                  className="flex h-8 items-center gap-1.5 rounded-lg border border-border px-2.5 text-xs font-medium text-muted-foreground transition-all hover:border-teal hover:text-foreground"
                  title={t("controls.annotate")}
                >
                  <MessageSquare className="h-3.5 w-3.5" />
                  {t("controls.annotate")}
                </button>
              </div>
            )}

            {/* Right: Action Buttons */}
            <div className="flex items-center gap-2">
              {chromControls && (
                <>
                  <button
                    onClick={chromControls.copySequence}
                    className="flex h-8 items-center gap-1.5 rounded-lg border border-border px-2.5 text-xs font-medium text-muted-foreground transition-all hover:border-teal hover:text-foreground"
                    title={t("controls.copySequence")}
                  >
                    <Copy className="h-3.5 w-3.5" />
                    {chromControls.copied ? t("controls.copied") : t("controls.copy")}
                  </button>

                  <button
                    onClick={chromControls.exportFasta}
                    className="flex h-8 items-center gap-1.5 rounded-lg border border-border px-2.5 text-xs font-medium text-muted-foreground transition-all hover:border-teal hover:text-foreground"
                    title={t("controls.exportFasta")}
                  >
                    <Download className="h-3.5 w-3.5" />
                    FASTA
                  </button>

                  <div className="h-6 w-px bg-border" />
                </>
              )}

              <button
                onClick={() => setAiAssistantOpen(true)}
                className="flex h-8 items-center gap-1.5 rounded-lg bg-gradient-to-r from-teal to-blue-deep px-3 text-xs font-medium text-white transition-all hover:opacity-90"
              >
                <Sparkles className="h-3.5 w-3.5" />
                {t("aiAssistant")}
              </button>

              <button className="flex h-8 w-8 items-center justify-center rounded-lg border border-border text-muted-foreground transition-all hover:bg-muted/50 hover:text-foreground">
                <Share2 className="h-4 w-4" />
              </button>
              <button className="flex h-8 w-8 items-center justify-center rounded-lg border border-border text-muted-foreground transition-all hover:bg-muted/50 hover:text-foreground">
                <Printer className="h-4 w-4" />
              </button>
            </div>
          </div>
        </div>
      </div>

      {/* Main Content Area */}
      <div className="flex-1 overflow-auto p-4">
        <Chromatogram
          className="h-[1000px] w-full"
          hideHeader
          onControlsReady={handleControlsReady}
          onStatsReady={handleStatsReady}
        />
      </div>

      {/* AI Assistant Chat Bubble */}
      <button
        onClick={() => setAiAssistantOpen(!aiAssistantOpen)}
        className={cn(
          "fixed bottom-6 z-40 flex h-14 w-14 items-center justify-center rounded-full shadow-lg transition-all duration-300 hover:scale-105 hover:shadow-xl",
          aiAssistantOpen
            ? "right-[416px] bg-muted/90 text-foreground backdrop-blur-sm hover:bg-muted"
            : "right-6 bg-gradient-to-r from-teal to-blue-deep text-white"
        )}
        aria-label={aiAssistantOpen ? t("closeAiAssistant") : t("openAiAssistant")}
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
        contextType="trace"
        contextTitle={`${selectedTrace.id} - ${selectedTrace.sample}`}
        contextId={selectedTrace.id}
      />
    </div>
  );
}
