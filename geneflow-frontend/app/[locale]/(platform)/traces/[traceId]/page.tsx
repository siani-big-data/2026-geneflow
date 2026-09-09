"use client";

import { useState, useCallback, useEffect, useMemo, useRef, Suspense } from "react";
import { useParams, useSearchParams } from "next/navigation";
import { useTranslations } from "next-intl";
import { Link, useRouter } from "@/lib/navigation";
import {
  ArrowLeft,
  Download,
  Share2,
  Printer,
  CheckCircle2,
  Info,
  AlertCircle,
  Sparkles,
  ZoomIn,
  ZoomOut,
  Maximize2,
  Activity,
  List,
  BarChart3,
  Copy,
  Scissors,
  MessageSquare,
  Loader2,
  Undo2,
  Pencil,
  Trash2,
  Dna,
  ArrowLeftRight,
  ChevronDown,
  Search,
} from "lucide-react";
import { cn } from "@/lib/utils";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Button,
} from "@/components/ui";
import { Chromatogram, TrimDialog, AnnotateDialog, AnalysisLayersPanel, AIAssistant } from "@/components/shared";
import type { TraceContext } from "@/services/ai.service";
import type {
  ChromatogramControls,
  ChromatogramStats,
  ChromatogramData,
  ChromatogramAnnotation,
  ChromatogramTrimRegion,
  ChromatogramMotifMatch,
  ChromatogramRestrictionSite,
} from "@/components/shared";
import type { LayerVisibility } from "@/components/shared/analysis";
import {
  reverseComplementChromatogramData,
  flipAnnotations,
  flipTrims,
  flipMotifMatches,
  flipRestrictionSites,
} from "@/components/shared/analysis";
import { tracesService } from "@/services/traces.service";
import { useTraceTrims, useUndoAllTrims, useTraceAnnotations, useDeleteAnnotation, useStudyTraces } from "@/hooks/use-traces";
import { useAnalysisList, useAnalysisResult } from "@/hooks/use-analysis";
import { useAnalysisEvents } from "@/hooks/use-analysis-events";
import { useTraceProcessingEvents } from "@/hooks/use-trace-processing-events";
import type {
  Trace,
  SequencePage,
  TraceTrim,
  TraceAnnotation,
  MotifSearchResult,
  RestrictionAnalysisResult,
  AnalysisType,
  TraceSummary,
} from "@/types";

function TraceViewerContent() {
  const t = useTranslations("traces.detail");
  const tAnalysis = useTranslations("traces.analysis");
  const tTraceProcessing = useTranslations("traces.processing");
  const params = useParams();
  const searchParams = useSearchParams();
  const router = useRouter();
  const traceId = params.traceId as string;
  const studyId = searchParams.get("studyId");
  const backUrl = studyId ? `/studies/${studyId}` : "/studies";

  // Sibling traces in the same study, used by the trace selector in the
  // header. Only enabled when we know which study we belong to. Loads up to
  // 200 entries — studies with more should fall back to the studies page.
  const { data: studyTracesData } = useStudyTraces(studyId || "", 1, 200);
  const studyTraces = studyTracesData?.items ?? [];

  // Data state
  const [trace, setTrace] = useState<Trace | null>(null);
  const [sequenceData, setSequenceData] = useState<SequencePage | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // UI state
  const [notification, setNotification] = useState<{
    message: string;
    type: "success" | "error" | "info";
  } | null>(null);

  // Chromatogram controls and stats
  const [chromControls, setChromControls] = useState<ChromatogramControls | null>(null);
  const [chromStats, setChromStats] = useState<ChromatogramStats | null>(null);

  // Dialog state
  const [trimDialogOpen, setTrimDialogOpen] = useState(false);
  const [annotateDialogOpen, setAnnotateDialogOpen] = useState(false);
  const [analysisOpen, setAnalysisOpen] = useState(false);
  const [aiOpen, setAiOpen] = useState(false);

  // Per-analysis pending state. An entry is added when the user launches an
  // analysis via `AnalysisLayersPanel` and removed when the SSE stream emits
  // a `<key>.success` or `<key>.failed` event for that key. Owned at this
  // level so completion toasts still fire when the panel is closed.
  const [pendingAnalyses, setPendingAnalyses] = useState<ReadonlySet<AnalysisType>>(
    () => new Set(),
  );
  const removePending = useCallback((type: AnalysisType) => {
    setPendingAnalyses((prev) => {
      if (!prev.has(type)) return prev;
      const next = new Set(prev);
      next.delete(type);
      return next;
    });
  }, []);
  const handleAnalysisLaunched = useCallback((type: AnalysisType) => {
    setPendingAnalyses((prev) => {
      const next = new Set(prev);
      next.add(type);
      return next;
    });
  }, []);
  const handleAnalysisLaunchFailed = useCallback(
    (type: AnalysisType) => removePending(type),
    [removePending],
  );

  // Reverse-complement view (5'→3' vs 3'→5'). Pure client-side transformation.
  const [rcMode, setRcMode] = useState(false);
  // Compare mode: render forward + reverse-complement stacked for visual alignment.
  const [compareMode, setCompareMode] = useState(false);

  // Analysis layer visibility (chromatogram overlays).
  const [layerVisibility, setLayerVisibility] = useState<LayerVisibility>({
    motifs: true,
    restriction: true,
  });
  const handleLayerVisibilityChange = useCallback(
    (key: keyof LayerVisibility, value: boolean) => {
      setLayerVisibility((prev) => ({ ...prev, [key]: value }));
    },
    [],
  );

  // Read spatial analysis results so the chromatogram can overlay them.
  const analysisList = useAnalysisList(traceId);
  const availableAnalysis = useMemo(
    () =>
      new Set(
        (analysisList.data?.availableTypes ?? []).map((s) => s.toLowerCase()),
      ),
    [analysisList.data],
  );
  const motifResult = useAnalysisResult<MotifSearchResult>(
    traceId,
    "motif",
    availableAnalysis.has("motif"),
  );
  const restrictionResult = useAnalysisResult<RestrictionAnalysisResult>(
    traceId,
    "restriction",
    availableAnalysis.has("restriction"),
  );

  const motifLayer = useMemo<ChromatogramMotifMatch[]>(() => {
    if (!motifResult.data) return [];
    return (motifResult.data.matches ?? []).map((m) => ({
      start: m.start,
      end: m.end,
      matchedSequence: m.matchedSequence,
      strand: m.strand,
    }));
  }, [motifResult.data]);

  const restrictionLayer = useMemo<ChromatogramRestrictionSite[]>(() => {
    if (!restrictionResult.data) return [];
    return (restrictionResult.data.sites ?? []).map((s) => ({
      enzyme: s.enzyme,
      position: s.position,
      cutPosition: s.cutPosition,
      recognitionSequence: s.recognitionSequence,
    }));
  }, [restrictionResult.data]);

  // Trims - fetch from API
  const { data: activeTrims = [], refetch: refetchTrims } = useTraceTrims(studyId || "", traceId, true);
  const undoAllTrimsMutation = useUndoAllTrims(studyId || "", traceId);

  // Fetch annotations
  const { data: annotations = [], refetch: refetchAnnotations } = useTraceAnnotations(studyId || "", traceId);

  // Delete annotation mutation
  const deleteAnnotationMutation = useDeleteAnnotation(studyId || "", traceId);

  // Annotation being edited
  const [editingAnnotation, setEditingAnnotation] = useState<TraceAnnotation | null>(null);

  // Annotation being deleted (for confirmation dialog)
  const [deletingAnnotation, setDeletingAnnotation] = useState<TraceAnnotation | null>(null);

  // Build the trace payload injected into the AI Assistant context so the
  // agent can analyze the active chromatogram (sequence + quality + key
  // metadata + active trims + annotations) without having to re-fetch.
  const tracePayload = useMemo<TraceContext | null>(() => {
    if (!trace || !sequenceData?.bases) return null;
    return {
      traceId: trace.id,
      name: trace.name,
      format: trace.format,
      length: sequenceData.bases.length,
      averageQuality: trace.qualityMetrics?.averageQuality,
      gcContent: trace.qualityMetrics?.gcContent,
      bases: sequenceData.bases,
      qualityScores: sequenceData.qualityScores,
      trims: activeTrims.map((tr) => ({
        end: (tr.trimEnd === "ThreePrime" ? "3'" : "5'") as "3'" | "5'",
        start: tr.startPosition,
        stop: tr.endPosition,
        length: tr.length,
        reason: tr.reason,
      })),
      annotations: annotations.map((a) => ({
        label: a.label,
        start: a.startPosition,
        end: a.endPosition,
        type: a.type,
        description: a.description,
      })),
    };
  }, [trace, sequenceData, activeTrims, annotations]);

  // Fetch trace data — extracted into a callback so the SSE handlers can
  // re-run it after the worker emits processing.completed / .failed.
  // Without this, the `trace` useState below is loaded once on mount and
  // the page never reflects the new status (React Query cache invalidation
  // doesn't help because `trace` is local state, not bound to a query).
  const fetchTraceData = useCallback(async (silent = false) => {
    if (!traceId || !studyId) return;

    if (!silent) {
      setIsLoading(true);
      setError(null);
    }

    try {
      const [traceData, sequencePage] = await Promise.all([
        tracesService.getById(studyId, traceId).catch(() => null),
        tracesService.getSequencePage(traceId, 1, 10000).catch(() => null),
      ]);

      if (traceData) {
        setTrace(traceData);
      }

      if (sequencePage) {
        setSequenceData(sequencePage);
      }
    } catch (err) {
      console.error("Failed to fetch trace data:", err);
      if (!silent) {
        setError("Failed to load trace data. Please try again.");
      }
    } finally {
      if (!silent) {
        setIsLoading(false);
      }
    }
  }, [traceId, studyId]);

  useEffect(() => {
    fetchTraceData();
  }, [fetchTraceData]);

// Convert API sequence data to Chromatogram format (memoized to prevent infinite re-renders)
  const chromatogramData: ChromatogramData | undefined = useMemo(() => {
    if (!sequenceData) return undefined;

    const sequenceLength = sequenceData.bases.length;

    // Get raw chromatogram data (backend sends aChannel, tChannel, etc.)
    const chrom = sequenceData.chromatogram;
    const rawA = chrom?.aChannel || chrom?.a || [];
    const rawT = chrom?.tChannel || chrom?.t || [];
    const rawG = chrom?.gChannel || chrom?.g || [];
    const rawC = chrom?.cChannel || chrom?.c || [];
    const peakPositions = chrom?.peakPositions || [];

    // Find max value across all channels for normalization
    const allValues = [...rawA, ...rawT, ...rawG, ...rawC];
    const maxValue = allValues.length > 0 ? Math.max(...allValues, 1) : 1;

    // Extract peak values at each base position using peakPositions
    // Ensure output array matches sequence length
    const extractPeaksAtPositions = (rawArr: number[]): number[] => {
      const result = new Array(sequenceLength).fill(0);

      if (peakPositions.length > 0 && rawArr.length > 0) {
        // Use peakPositions to get the correct value for each base
        for (let i = 0; i < Math.min(peakPositions.length, sequenceLength); i++) {
          const pos = peakPositions[i];
          result[i] = (rawArr[pos] || 0) / maxValue;
        }
      } else if (rawArr.length > 0) {
        // Fallback: if no peakPositions, assume 1:1 mapping
        for (let i = 0; i < Math.min(rawArr.length, sequenceLength); i++) {
          result[i] = rawArr[i] / maxValue;
        }
      }

      return result;
    };

    return {
      sequence: sequenceData.bases,
      quality: sequenceData.qualityScores || [],
      peaks: {
        A: extractPeaksAtPositions(rawA),
        T: extractPeaksAtPositions(rawT),
        G: extractPeaksAtPositions(rawG),
        C: extractPeaksAtPositions(rawC),
      },
    };
  }, [sequenceData]);

  // Apply reverse-complement transformation across data + overlays when active.
  const sequenceLength = sequenceData?.totalBases ?? 0;
  const displayChromatogramData = useMemo<ChromatogramData | undefined>(() => {
    if (!chromatogramData) return undefined;
    return rcMode
      ? reverseComplementChromatogramData(chromatogramData)
      : chromatogramData;
  }, [chromatogramData, rcMode]);

  const displayAnnotations = useMemo<ChromatogramAnnotation[]>(() => {
    const base = annotations.map((a): ChromatogramAnnotation => ({
      id: a.id,
      label: a.label,
      startPosition: a.startPosition,
      endPosition: a.endPosition,
      color: a.color,
      type: a.type,
    }));
    return rcMode && sequenceLength > 0 ? flipAnnotations(base, sequenceLength) : base;
  }, [annotations, rcMode, sequenceLength]);

  const displayTrims = useMemo<ChromatogramTrimRegion[]>(() => {
    const base = activeTrims.map((t): ChromatogramTrimRegion => ({
      id: t.id,
      startPosition: t.startPosition,
      endPosition: t.endPosition,
      trimEnd: t.trimEnd,
      isActive: t.isActive,
    }));
    return rcMode && sequenceLength > 0 ? flipTrims(base, sequenceLength) : base;
  }, [activeTrims, rcMode, sequenceLength]);

  const displayMotifLayer = useMemo<ChromatogramMotifMatch[]>(
    () => (rcMode && sequenceLength > 0 ? flipMotifMatches(motifLayer, sequenceLength) : motifLayer),
    [motifLayer, rcMode, sequenceLength],
  );

  const displayRestrictionLayer = useMemo<ChromatogramRestrictionSite[]>(
    () =>
      rcMode && sequenceLength > 0
        ? flipRestrictionSites(restrictionLayer, sequenceLength)
        : restrictionLayer,
    [restrictionLayer, rcMode, sequenceLength],
  );

  // Compare overlay: opposite strand of the currently-displayed view. Only
  // computed when compareMode is on; reverseComplement is its own inverse so
  // applying flip* to display* yields the complement strand.
  const compareChromatogramData = useMemo<ChromatogramData | undefined>(() => {
    if (!compareMode || !displayChromatogramData) return undefined;
    return reverseComplementChromatogramData(displayChromatogramData);
  }, [compareMode, displayChromatogramData]);

  const compareAnnotations = useMemo<ChromatogramAnnotation[]>(
    () =>
      compareMode && sequenceLength > 0
        ? flipAnnotations(displayAnnotations, sequenceLength)
        : [],
    [compareMode, displayAnnotations, sequenceLength],
  );

  const compareTrims = useMemo<ChromatogramTrimRegion[]>(
    () =>
      compareMode && sequenceLength > 0
        ? flipTrims(displayTrims, sequenceLength)
        : [],
    [compareMode, displayTrims, sequenceLength],
  );

  const compareMotifLayer = useMemo<ChromatogramMotifMatch[]>(
    () =>
      compareMode && sequenceLength > 0
        ? flipMotifMatches(displayMotifLayer, sequenceLength)
        : [],
    [compareMode, displayMotifLayer, sequenceLength],
  );

  const compareRestrictionLayer = useMemo<ChromatogramRestrictionSite[]>(
    () =>
      compareMode && sequenceLength > 0
        ? flipRestrictionSites(displayRestrictionLayer, sequenceLength)
        : [],
    [compareMode, displayRestrictionLayer, sequenceLength],
  );

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

  // Subscribe to the trace's analysis SSE stream. Lives at the page level so
  // completion toasts still fire after the user closes `AnalysisLayersPanel`.
  // Cache invalidation happens inside the hook; we only handle UX side-effects.
  useAnalysisEvents(traceId, {
    onSuccess: (event) => {
      removePending(event.analysisKey);
      showNotification(
        tAnalysis(`notifications.complete.${event.analysisKey}`),
        "success",
      );
    },
    onFailure: (event) => {
      removePending(event.analysisKey);
      showNotification(
        event.error
          ? tAnalysis("notifications.failedWithReason", {
              type: tAnalysis(`tabs.${event.analysisKey}`),
              error: event.error,
            })
          : tAnalysis("notifications.failed", {
              type: tAnalysis(`tabs.${event.analysisKey}`),
            }),
        "error",
      );
    },
  });

  // Subscribe to trace processing transitions (Uploaded → Validating → Processing →
  // Processed/Failed). The hook stays open during all non-terminal statuses so we
  // don't miss the worker emitting `trace.processing.completed` between the upload
  // response and the first status refetch. Status comparison is case-insensitive
  // because the backend serializes the enum as PascalCase ("Uploaded") but other
  // surfaces sometimes lowercase it.
  useTraceProcessingEvents(traceId, {
    enabled: (() => {
      const s = trace?.status?.toLowerCase();
      return s === "uploaded" || s === "validating" || s === "processing";
    })(),
    studyId,
    onReady: () => {
      // Connect race: worker may have already finished by the time SSE
      // connects. Refetch on `ready` so the local `trace` state reflects
      // any transitions that happened before we subscribed.
      void fetchTraceData(true);
    },
    onStarted: () => {
      void fetchTraceData(true);
    },
    onCompleted: () => {
      void fetchTraceData(true);
      showNotification(
        tTraceProcessing("notifications.completed"),
        "success",
      );
    },
    onFailed: (event) => {
      void fetchTraceData(true);
      showNotification(
        event.error
          ? tTraceProcessing("notifications.failedWithReason", { reason: event.error })
          : tTraceProcessing("notifications.failed"),
        "error",
      );
    },
  });

  const handleExport = () => {
    showNotification(t("notifications.preparingExport"), "info");
  };

  const handleTrimSuccess = (trim: TraceTrim) => {
    refetchTrims();
    showNotification("Trim applied successfully", "success");
  };

  const handleUndoAllTrims = async () => {
    if (!studyId) {
      showNotification("Study ID required", "error");
      return;
    }
    try {
      await undoAllTrimsMutation.mutateAsync();
      refetchTrims();
      showNotification("All trims undone successfully", "success");
    } catch (err) {
      showNotification(err instanceof Error ? err.message : "Failed to undo trims", "error");
    }
  };

  const handleAnnotationSuccess = (annotation: TraceAnnotation) => {
    showNotification(`Annotation "${annotation.label}" created`, "success");
    setEditingAnnotation(null);
    refetchAnnotations();
  };

  const handleEditAnnotation = (annotation: TraceAnnotation) => {
    setEditingAnnotation(annotation);
    setAnnotateDialogOpen(true);
  };

  const handleDeleteAnnotation = (annotation: TraceAnnotation) => {
    setDeletingAnnotation(annotation);
  };

  const confirmDeleteAnnotation = async () => {
    if (!deletingAnnotation) return;

    try {
      await deleteAnnotationMutation.mutateAsync(deletingAnnotation.id);
      showNotification(`Annotation "${deletingAnnotation.label}" deleted`, "success");
      refetchAnnotations();
    } catch (err) {
      showNotification("Failed to delete annotation", "error");
    } finally {
      setDeletingAnnotation(null);
    }
  };

  const handleAnnotateDialogClose = (open: boolean) => {
    setAnnotateDialogOpen(open);
    if (!open) {
      setEditingAnnotation(null);
    }
  };

  if (isLoading) {
    return (
      <div className="-mx-16 -mt-10 flex h-[calc(100vh-0px)] flex-col items-center justify-center bg-background">
        <Loader2 className="h-12 w-12 animate-spin text-teal" />
        <p className="mt-4 text-muted-foreground">Loading trace data...</p>
      </div>
    );
  }

  if (error) {
    return (
      <div className="-mx-16 -mt-10 flex h-[calc(100vh-0px)] flex-col items-center justify-center bg-background">
        <AlertCircle className="h-12 w-12 text-red-500" />
        <p className="mt-4 text-red-500">{error}</p>
        <Link
          href={backUrl}
          className="mt-4 text-sm text-muted-foreground hover:text-foreground"
        >
          {t("backToStudy")}
        </Link>
      </div>
    );
  }

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
            href={backUrl}
            className="mb-3 inline-flex items-center gap-2 text-sm text-muted-foreground transition-colors hover:text-foreground"
          >
            <ArrowLeft className="h-4 w-4" />
            {t("backToStudy")}
          </Link>

          {/* Main Header Row */}
          <div className="flex items-center justify-between gap-4">
            {/* Left: Trace ID + Stats */}
            <div className="flex items-center gap-6">
              {/* Trace ID + sibling-trace selector */}
              {studyId && studyTraces.length > 1 ? (
                <TraceSelector
                  currentId={traceId}
                  currentName={trace?.name}
                  currentQuality={trace?.qualityMetrics?.averageQuality ?? chromStats?.averageQuality ?? null}
                  studyId={studyId}
                  traces={studyTraces}
                  onSelect={(nextId) => {
                    if (nextId !== traceId) {
                      router.push(`/traces/${nextId}?studyId=${studyId}`);
                    }
                  }}
                  searchPlaceholder={t("searchTraces")}
                  ariaLabel={t("traceSelector.label")}
                  qualityLabel={t("stats.quality")}
                />
              ) : (
                <div className="flex flex-col">
                  <span className="text-sm font-medium text-blue-deep">{traceId}</span>
                  {trace && (
                    <span className="font-mono text-xs text-muted-foreground">{trace.name}</span>
                  )}
                </div>
              )}

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
                      qualityToneClass(chromStats.averageQuality),
                    )}>
                      {formatQScore(chromStats.averageQuality)}
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

                {/* Reverse-complement toggle */}
                <button
                  onClick={() => setRcMode((prev) => !prev)}
                  aria-pressed={rcMode}
                  className={cn(
                    "flex h-8 items-center gap-1.5 rounded-lg border px-2.5 text-xs font-medium transition-all",
                    rcMode
                      ? "border-teal bg-teal text-white"
                      : "border-border text-muted-foreground hover:border-teal hover:text-foreground",
                  )}
                  title={t("controls.reverseComplement")}
                >
                  <ArrowLeftRight className="h-3.5 w-3.5" />
                  {rcMode ? "3'→5'" : "5'→3'"}
                </button>

                {/* Compare (forward + reverse stacked) */}
                <button
                  onClick={() => setCompareMode((prev) => !prev)}
                  aria-pressed={compareMode}
                  className={cn(
                    "flex h-8 items-center gap-1.5 rounded-lg border px-2.5 text-xs font-medium transition-all",
                    compareMode
                      ? "border-teal bg-teal text-white"
                      : "border-border text-muted-foreground hover:border-teal hover:text-foreground",
                  )}
                  title={t("controls.compareStrands")}
                >
                  <BarChart3 className="h-3.5 w-3.5" />
                  {t("controls.compareStrands")}
                </button>

                <div className="h-6 w-px bg-border" />

                {/* Tools */}
                <div className="flex items-center gap-1">
                  <button
                    onClick={() => {
                      if (!studyId) {
                        showNotification("Study ID required. Access trace from study page.", "error");
                        return;
                      }
                      setTrimDialogOpen(true);
                    }}
                    disabled={!sequenceData}
                    className={cn(
                      "flex h-8 items-center gap-1.5 rounded-lg border px-2.5 text-xs font-medium transition-all",
                      activeTrims.length > 0
                        ? "border-teal bg-teal/10 text-teal rounded-r-none"
                        : "border-border text-muted-foreground hover:border-teal hover:text-foreground",
                      !sequenceData && "opacity-50 cursor-not-allowed"
                    )}
                    title={t("controls.trim")}
                  >
                    <Scissors className="h-3.5 w-3.5" />
                    {t("controls.trim")}
                    {activeTrims.length > 0 && (
                      <span className="ml-1 rounded-full bg-teal px-1.5 text-[10px] text-white">
                        {activeTrims.length}
                      </span>
                    )}
                  </button>
                  {activeTrims.length > 0 && (
                    <button
                      onClick={handleUndoAllTrims}
                      disabled={undoAllTrimsMutation.isPending}
                      className="flex h-8 items-center gap-1 rounded-lg rounded-l-none border border-l-0 border-teal bg-teal/10 px-2 text-xs font-medium text-teal transition-all hover:bg-teal/20"
                      title="Undo all trims"
                    >
                      {undoAllTrimsMutation.isPending ? (
                        <Loader2 className="h-3.5 w-3.5 animate-spin" />
                      ) : (
                        <Undo2 className="h-3.5 w-3.5" />
                      )}
                    </button>
                  )}
                </div>

                <button
                  onClick={() => {
                    if (!studyId) {
                      showNotification("Study ID required. Access trace from study page.", "error");
                      return;
                    }
                    setAnnotateDialogOpen(true);
                  }}
                  disabled={!sequenceData}
                  className={cn(
                    "flex h-8 items-center gap-1.5 rounded-lg border border-border px-2.5 text-xs font-medium text-muted-foreground transition-all hover:border-teal hover:text-foreground",
                    !sequenceData && "opacity-50 cursor-not-allowed"
                  )}
                  title={t("controls.annotate")}
                >
                  <MessageSquare className="h-3.5 w-3.5" />
                  {t("controls.annotate")}
                </button>

                <button
                  onClick={() => setAnalysisOpen(true)}
                  disabled={!sequenceData}
                  className={cn(
                    "flex h-8 items-center gap-1.5 rounded-lg border px-2.5 text-xs font-medium transition-all",
                    analysisOpen
                      ? "border-teal bg-teal/10 text-teal"
                      : "border-border text-muted-foreground hover:border-teal hover:text-foreground",
                    !sequenceData && "opacity-50 cursor-not-allowed",
                  )}
                  title={tAnalysis("title")}
                >
                  <Dna className="h-3.5 w-3.5" />
                  {tAnalysis("title")}
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
                onClick={() => setAiOpen(true)}
                disabled={!tracePayload}
                className="flex h-8 items-center gap-1.5 rounded-lg bg-gradient-to-r from-teal to-blue-deep px-3 text-xs font-medium text-white transition-all hover:opacity-90 disabled:opacity-50 disabled:cursor-not-allowed"
                title={tracePayload ? t("aiAssistant") : "Loading trace…"}
                aria-label={t("aiAssistant")}
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
        {compareMode && (
          <div className="mb-1 flex items-center gap-2 text-xs font-medium text-muted-foreground">
            <span className="rounded-md border border-border bg-card px-2 py-0.5 font-mono">
              {rcMode ? "3'→5'" : "5'→3'"}
            </span>
            <span>{t("controls.topStrand")}</span>
          </div>
        )}
        <Chromatogram
          data={displayChromatogramData}
          className="w-full"
          style={{ height: compareMode ? 500 : 1000 }}
          hideHeader
          compact={compareMode}
          onControlsReady={compareMode ? undefined : handleControlsReady}
          onStatsReady={compareMode ? undefined : handleStatsReady}
          annotations={displayAnnotations}
          trims={displayTrims}
          motifMatches={displayMotifLayer}
          restrictionSites={displayRestrictionLayer}
          showMotifs={layerVisibility.motifs}
          showRestriction={layerVisibility.restriction}
        />

        {compareMode && (
          <>
            <div className="mt-3 mb-1 flex items-center gap-2 text-xs font-medium text-muted-foreground">
              <span className="rounded-md border border-border bg-card px-2 py-0.5 font-mono">
                {rcMode ? "5'→3'" : "3'→5'"}
              </span>
              <span>{t("controls.bottomStrand")}</span>
            </div>
            <Chromatogram
              data={compareChromatogramData}
              className="w-full"
              style={{ height: 500 }}
              hideHeader
              onControlsReady={handleControlsReady}
              onStatsReady={handleStatsReady}
              annotations={compareAnnotations}
              trims={compareTrims}
              motifMatches={compareMotifLayer}
              restrictionSites={compareRestrictionLayer}
              showMotifs={layerVisibility.motifs}
              showRestriction={layerVisibility.restriction}
            />
          </>
        )}

        {/* Annotations Panel */}
        {annotations.length > 0 && (
          <div className="mt-4 rounded-lg border border-border bg-card p-4">
            <h3 className="mb-3 flex items-center gap-2 text-sm font-medium">
              <MessageSquare className="h-4 w-4 text-teal" />
              Annotations ({annotations.length})
            </h3>
            <div className="space-y-2">
              {annotations.map((annotation) => (
                <div
                  key={annotation.id}
                  className="flex items-center justify-between rounded-md border border-border/50 bg-muted/30 px-3 py-2 text-sm group"
                >
                  <div className="flex items-center gap-3">
                    <span
                      className="h-3 w-3 rounded-full flex-shrink-0"
                      style={{ backgroundColor: annotation.color || "#14b8a6" }}
                    />
                    <span className="font-medium">{annotation.label}</span>
                    <span className="text-muted-foreground">
                      {annotation.type} @ {annotation.startPosition}
                      {annotation.endPosition !== annotation.startPosition && `-${annotation.endPosition}`}
                    </span>
                    {annotation.description && (
                      <span className="text-xs text-muted-foreground hidden sm:inline">— {annotation.description}</span>
                    )}
                  </div>
                  <div className="flex items-center gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
                    <button
                      onClick={() => handleEditAnnotation(annotation)}
                      className="p-1.5 rounded-md hover:bg-muted text-muted-foreground hover:text-foreground transition-colors"
                      title="Edit annotation"
                    >
                      <Pencil className="h-3.5 w-3.5" />
                    </button>
                    <button
                      onClick={() => handleDeleteAnnotation(annotation)}
                      className="p-1.5 rounded-md hover:bg-red-100 dark:hover:bg-red-900/30 text-muted-foreground hover:text-red-600 transition-colors"
                      title="Delete annotation"
                    >
                      <Trash2 className="h-3.5 w-3.5" />
                    </button>
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}
      </div>

      {/* Trim Dialog */}
      {studyId && sequenceData && (
        <TrimDialog
          open={trimDialogOpen}
          onOpenChange={setTrimDialogOpen}
          studyId={studyId}
          traceId={traceId}
          sequenceLength={sequenceData.totalBases}
          existingTrims={activeTrims}
          onSuccess={handleTrimSuccess}
        />
      )}

      {/* Annotate Dialog */}
      {studyId && sequenceData && (
        <AnnotateDialog
          open={annotateDialogOpen}
          onOpenChange={handleAnnotateDialogClose}
          studyId={studyId}
          traceId={traceId}
          sequenceLength={sequenceData.totalBases}
          onSuccess={handleAnnotationSuccess}
          editingAnnotation={editingAnnotation}
        />
      )}

      {/* Analysis Layers Panel */}
      <AnalysisLayersPanel
        traceId={traceId}
        open={analysisOpen}
        onOpenChange={setAnalysisOpen}
        onNotify={showNotification}
        visibility={layerVisibility}
        onVisibilityChange={handleLayerVisibilityChange}
        pendingAnalyses={pendingAnalyses}
        onAnalysisLaunched={handleAnalysisLaunched}
        onAnalysisLaunchFailed={handleAnalysisLaunchFailed}
      />

      {/* Delete Annotation Confirmation Dialog */}
      <Dialog open={!!deletingAnnotation} onOpenChange={(open) => !open && setDeletingAnnotation(null)}>
        <DialogContent className="sm:max-w-[425px]">
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2 text-red-600">
              <Trash2 className="h-5 w-5" />
              Delete Annotation
            </DialogTitle>
            <DialogDescription>
              Are you sure you want to delete the annotation{" "}
              <span className="font-semibold text-foreground">"{deletingAnnotation?.label}"</span>?
              This action cannot be undone.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter className="gap-2 sm:gap-0">
            <Button variant="outline" onClick={() => setDeletingAnnotation(null)}>
              Cancel
            </Button>
            <Button
              variant="destructive"
              onClick={confirmDeleteAnnotation}
              disabled={deleteAnnotationMutation.isPending}
            >
              {deleteAnnotationMutation.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              Delete
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* AI Assistant Panel — chats with the molecular biology agent and
          ships the active trace (bases + quality + metadata) as context. */}
      <AIAssistant
        isOpen={aiOpen}
        onClose={() => setAiOpen(false)}
        contextType="trace"
        contextTitle={trace?.name || traceId}
        contextId={traceId}
        traceContext={tracePayload}
      />
    </div>
  );
}

// Wrap with Suspense for useSearchParams
export default function TraceViewerPage() {
  return (
    <Suspense
      fallback={
        <div className="-mx-16 -mt-10 flex h-[calc(100vh-0px)] flex-col items-center justify-center bg-background">
          <Loader2 className="h-12 w-12 animate-spin text-teal" />
          <p className="mt-4 text-muted-foreground">Loading trace viewer...</p>
        </div>
      }
    >
      <TraceViewerContent />
    </Suspense>
  );
}

// =============================================================================
// TraceSelector — sibling-trace combobox in the header.
// =============================================================================

interface TraceSelectorProps {
  currentId: string;
  currentName: string | undefined;
  currentQuality: number | null;
  studyId: string;
  traces: TraceSummary[];
  onSelect: (id: string) => void;
  searchPlaceholder: string;
  ariaLabel: string;
  qualityLabel: string;
}

function qualityToneClass(quality: number | null | undefined): string {
  if (quality == null || !Number.isFinite(quality)) {
    return "text-muted-foreground";
  }
  if (quality >= 30) return "text-emerald-500";
  if (quality >= 20) return "text-amber-500";
  return "text-red-500";
}

/**
 * Phred Q-score display for trace headers and the trace selector.
 * Returns an em-dash for traces without quality data (FASTA, etc.) so we
 * never surface "QNaN" to the user.
 */
function formatQScore(quality: number | null | undefined): string {
  if (quality == null || !Number.isFinite(quality)) return "—";
  return `Q${Math.round(quality)}`;
}

function TraceSelector({
  currentId,
  currentName,
  currentQuality,
  studyId: _studyId,
  traces,
  onSelect,
  searchPlaceholder,
  ariaLabel,
  qualityLabel,
}: TraceSelectorProps) {
  const [open, setOpen] = useState(false);
  const [query, setQuery] = useState("");
  const containerRef = useRef<HTMLDivElement | null>(null);
  const inputRef = useRef<HTMLInputElement | null>(null);

  // Close on outside click + Escape.
  useEffect(() => {
    if (!open) return;

    const onPointerDown = (e: MouseEvent) => {
      if (!containerRef.current) return;
      if (!containerRef.current.contains(e.target as Node)) setOpen(false);
    };
    const onKeyDown = (e: KeyboardEvent) => {
      if (e.key === "Escape") setOpen(false);
    };

    document.addEventListener("mousedown", onPointerDown);
    document.addEventListener("keydown", onKeyDown);
    return () => {
      document.removeEventListener("mousedown", onPointerDown);
      document.removeEventListener("keydown", onKeyDown);
    };
  }, [open]);

  // Auto-focus search input when the dropdown opens.
  useEffect(() => {
    if (open) inputRef.current?.focus();
    else setQuery("");
  }, [open]);

  const filtered = useMemo(() => {
    const q = query.trim().toLowerCase();
    if (!q) return traces;
    return traces.filter(
      (t) =>
        t.id.toLowerCase().includes(q) ||
        t.name.toLowerCase().includes(q),
    );
  }, [traces, query]);

  const handleSelect = (id: string) => {
    setOpen(false);
    onSelect(id);
  };

  return (
    <div ref={containerRef} className="relative">
      {/* Trigger */}
      <button
        type="button"
        aria-label={ariaLabel}
        aria-haspopup="listbox"
        aria-expanded={open}
        onClick={() => setOpen((prev) => !prev)}
        className={cn(
          "flex min-w-[240px] items-center justify-between gap-3 rounded-md border border-border bg-background px-3 py-2 text-left transition-colors",
          "hover:bg-muted/50 focus:outline-none focus:ring-2 focus:ring-ring",
        )}
      >
        <div className="flex flex-col leading-tight">
          <div className="flex items-center gap-1.5">
            <span className="text-sm font-semibold text-blue-deep">
              {currentId}
            </span>
            {currentName && (
              <>
                <span className="text-muted-foreground">·</span>
                <span className="font-mono text-xs text-muted-foreground">
                  {currentName}
                </span>
              </>
            )}
          </div>
          <div className="text-xs">
            <span className="text-muted-foreground">{qualityLabel}: </span>
            <span className={cn("font-semibold", qualityToneClass(currentQuality))}>
              {formatQScore(currentQuality)}
            </span>
          </div>
        </div>
        <ChevronDown
          className={cn(
            "h-4 w-4 flex-shrink-0 text-muted-foreground transition-transform",
            open && "rotate-180",
          )}
        />
      </button>

      {/* Dropdown */}
      {open && (
        <div
          role="listbox"
          aria-label={ariaLabel}
          className="absolute left-0 top-full z-50 mt-1 w-[320px] overflow-hidden rounded-md border border-border bg-popover shadow-lg"
        >
          {/* Search input */}
          <div className="border-b border-border p-2">
            <div className="relative">
              <Search className="pointer-events-none absolute left-2.5 top-1/2 h-3.5 w-3.5 -translate-y-1/2 text-muted-foreground" />
              <input
                ref={inputRef}
                type="text"
                value={query}
                onChange={(e) => setQuery(e.target.value)}
                placeholder={searchPlaceholder}
                className="w-full rounded-md border border-border bg-background py-1.5 pl-8 pr-2 text-xs placeholder:text-muted-foreground focus:outline-none focus:ring-1 focus:ring-ring"
              />
            </div>
          </div>

          {/* List */}
          <ul className="max-h-72 overflow-y-auto py-1">
            {filtered.length === 0 ? (
              <li className="px-3 py-4 text-center text-xs text-muted-foreground">
                —
              </li>
            ) : (
              filtered.map((t) => {
                const isActive = t.id === currentId;
                return (
                  <li key={t.id} role="option" aria-selected={isActive}>
                    <button
                      type="button"
                      onClick={() => handleSelect(t.id)}
                      className={cn(
                        "relative flex w-full flex-col gap-0.5 px-3 py-2 text-left transition-colors",
                        isActive
                          ? "bg-emerald-500/5"
                          : "hover:bg-muted/60",
                      )}
                    >
                      {isActive && (
                        <span
                          aria-hidden
                          className="absolute inset-y-1 left-0 w-0.5 rounded-r bg-emerald-500"
                        />
                      )}
                      <div className="flex items-center gap-1.5">
                        <span className="text-sm font-semibold text-blue-deep">
                          {t.id}
                        </span>
                        <span className="font-mono text-xs text-muted-foreground">
                          {t.name}
                        </span>
                      </div>
                      <div className="text-xs">
                        <span className="text-muted-foreground">
                          {qualityLabel}:{" "}
                        </span>
                        <span
                          className={cn(
                            "font-semibold",
                            qualityToneClass(t.averageQualityScore ?? null),
                          )}
                        >
                          {formatQScore(t.averageQualityScore ?? null)}
                        </span>
                      </div>
                    </button>
                  </li>
                );
              })
            )}
          </ul>
        </div>
      )}
    </div>
  );
}
