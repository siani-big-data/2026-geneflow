"use client";

import { useMemo, useState } from "react";
import { useTranslations } from "next-intl";
import {
  X,
  Play,
  ChevronDown,
  ChevronUp,
  Eye,
  EyeOff,
  Loader2,
} from "lucide-react";
import { cn } from "@/lib/utils";
import {
  useAnalysisList,
  useAnalysisResult,
  useTriggerAnalysis,
} from "@/hooks/use-analysis";
import type {
  AnalysisType,
  TrimmingResult,
  HeterozygoteResult,
  MotifSearchResult,
  TranslationResult,
  ORFDetectionResult,
  RestrictionAnalysisResult,
} from "@/types";

export interface LayerVisibility {
  motifs: boolean;
  restriction: boolean;
}

export interface AnalysisLayersPanelProps {
  traceId: string;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onNotify?: (message: string, type?: "success" | "error" | "info") => void;
  visibility: LayerVisibility;
  onVisibilityChange: (key: keyof LayerVisibility, value: boolean) => void;
  /**
   * Set of analyses that have been launched but have not yet emitted an SSE
   * completion event. Owned by the parent so toasts keep firing while the
   * panel is closed. Drives the per-row spinner.
   */
  pendingAnalyses?: ReadonlySet<AnalysisType>;
  /**
   * Notifies the parent that the user just launched an analysis. The parent is
   * responsible for adding the type to {@link pendingAnalyses} and clearing it
   * on SSE completion.
   */
  onAnalysisLaunched?: (type: AnalysisType) => void;
  /**
   * Notifies the parent that the launch POST itself failed (network/auth).
   * Parent should clear pending state for this type if previously added.
   */
  onAnalysisLaunchFailed?: (type: AnalysisType, error: Error) => void;
}

const ANALYSIS_TYPES: AnalysisType[] = [
  "trimming",
  "heterozygote",
  "motif",
  "translation",
  "orf",
  "restriction",
];

type Translator = ReturnType<typeof useTranslations>;

function buildSummary(
  type: AnalysisType,
  data: unknown,
  t: Translator,
): string | null {
  if (!data) return null;
  switch (type) {
    case "trimming": {
      const r = data as TrimmingResult;
      return t("summary.trimming", {
        original: r.originalLength,
        trimmed: r.trimmedLength,
      });
    }
    case "heterozygote": {
      const r = data as HeterozygoteResult;
      return t("summary.heterozygote", { count: r.heterozygoteCount });
    }
    case "motif": {
      const r = data as MotifSearchResult;
      return t("summary.motif", { count: r.matchCount });
    }
    case "translation": {
      const r = data as TranslationResult;
      return t("summary.translation", { length: r.proteinLength });
    }
    case "orf": {
      const r = data as ORFDetectionResult;
      return t("summary.orf", {
        count: r.totalOrfs,
        longest: r.longestOrfLength,
      });
    }
    case "restriction": {
      const r = data as RestrictionAnalysisResult;
      return t("summary.restriction", {
        enzymes: r.enzymeCount,
        sites: r.totalSites,
      });
    }
  }
}

interface DetailsBlockProps {
  type: AnalysisType;
  data: unknown;
  t: Translator;
}

function DetailsBlock({ type, data, t }: DetailsBlockProps) {
  if (!data) return null;
  switch (type) {
    case "trimming": {
      const r = data as TrimmingResult;
      return (
        <div className="space-y-1 text-xs text-muted-foreground">
          <div>
            {t("trimming.start")}: {r.trimStart} · {t("trimming.end")}: {r.trimEnd}
          </div>
          {r.trimmedSequence && (
            <pre className="mt-1 max-h-24 overflow-auto rounded bg-muted p-2 font-mono text-[10px] leading-tight">
              {r.trimmedSequence.slice(0, 240)}
              {r.trimmedSequence.length > 240 ? "…" : ""}
            </pre>
          )}
        </div>
      );
    }
    case "heterozygote": {
      const r = data as HeterozygoteResult;
      const top = r.calls.slice(0, 5);
      if (!top.length) return <p className="text-xs">{t("heterozygote.noCalls")}</p>;
      return (
        <div className="space-y-1 text-xs">
          <div className="text-muted-foreground">
            {t("heterozygote.preview", { count: top.length, total: r.heterozygoteCount })}
          </div>
          <ul className="space-y-0.5">
            {top.map((c) => (
              <li key={c.position} className="font-mono">
                {c.position}: {c.primaryBase}/{c.secondaryBase} ({c.ratio.toFixed(2)})
              </li>
            ))}
          </ul>
        </div>
      );
    }
    case "motif": {
      const r = data as MotifSearchResult;
      return (
        <div className="text-xs text-muted-foreground">
          {t("motif.previewPattern")}: <span className="font-mono">{r.pattern}</span>
        </div>
      );
    }
    case "translation": {
      const r = data as TranslationResult;
      return (
        <pre className="max-h-24 overflow-auto rounded bg-muted p-2 font-mono text-[10px] leading-tight">
          {r.proteinSequence.slice(0, 240)}
          {r.proteinSequence.length > 240 ? "…" : ""}
        </pre>
      );
    }
    case "orf": {
      const r = data as ORFDetectionResult;
      const top = r.orfs.slice(0, 5);
      if (!top.length) return <p className="text-xs">{t("orf.noOrfs")}</p>;
      return (
        <div className="space-y-1 text-xs">
          <div className="text-muted-foreground">
            {t("orf.previewTop", { count: top.length })}
          </div>
          <ul className="space-y-0.5 font-mono">
            {top.map((o, i) => (
              <li key={i}>
                {o.start}-{o.end} (frame {o.frame}, {o.length} bp)
              </li>
            ))}
          </ul>
        </div>
      );
    }
    case "restriction": {
      const r = data as RestrictionAnalysisResult;
      if (!r.sites.length) return <p className="text-xs">{t("restriction.noSites")}</p>;
      return (
        <div className="flex flex-wrap gap-1">
          {r.enzymesWithSites.map((enz) => (
            <span
              key={enz}
              className="rounded-full border border-border bg-muted px-2 py-0.5 text-[10px] font-mono"
            >
              {enz}
            </span>
          ))}
        </div>
      );
    }
  }
}

interface AnalysisRowProps {
  traceId: string;
  type: AnalysisType;
  available: boolean;
  visibilityKey?: keyof LayerVisibility;
  visibilityValue?: boolean;
  onVisibilityChange?: (value: boolean) => void;
  onNotify?: (message: string, type?: "success" | "error" | "info") => void;
  motifPattern: string;
  setMotifPattern: (v: string) => void;
  pending: boolean;
  onLaunched?: (type: AnalysisType) => void;
  onLaunchFailed?: (type: AnalysisType, error: Error) => void;
}

function AnalysisRow({
  traceId,
  type,
  available,
  visibilityKey,
  visibilityValue,
  onVisibilityChange,
  onNotify,
  motifPattern,
  setMotifPattern,
  pending,
  onLaunched,
  onLaunchFailed,
}: AnalysisRowProps) {
  const t = useTranslations("traces.analysis");
  const [expanded, setExpanded] = useState(false);

  const result = useAnalysisResult<unknown>(traceId, type, available);
  const trigger = useTriggerAnalysis(traceId, type);

  const summary = useMemo(
    () => buildSummary(type, result.data, t),
    [type, result.data, t],
  );

  const handleRun = () => {
    let payload: unknown = {};
    if (type === "motif") {
      if (!motifPattern.trim()) {
        onNotify?.(t("motif.patternRequired"), "error");
        return;
      }
      payload = { pattern: motifPattern.trim() };
    }
    onNotify?.(t("notifications.queued"), "info");
    trigger.mutate(payload, {
      // POST returned 202: the analysis is queued. Completion will arrive via
      // SSE — surface that to the parent so it can add `type` to the pending
      // set and listen for `<type>.success` / `<type>.failed` events.
      onSuccess: () => onLaunched?.(type),
      onError: (err: Error) => {
        onNotify?.(t("notifications.error"), "error");
        onLaunchFailed?.(type, err);
      },
    });
  };

  // Spinner = either the launch POST is in flight, OR the request is queued
  // and we're waiting on SSE completion.
  const isPending = trigger.isPending || pending;

  return (
    <div className="rounded-lg border border-border bg-card p-3">
      <div className="flex items-center justify-between gap-2">
        <div className="flex items-center gap-2">
          <span className="text-sm font-medium capitalize">
            {t(`tabs.${type}`)}
          </span>
          <span
            className={cn(
              "rounded-full px-2 py-0.5 text-[10px] font-medium",
              available
                ? "bg-emerald-500/10 text-emerald-600"
                : "bg-muted text-muted-foreground",
            )}
          >
            {available ? t("status.ready") : t("status.notRun")}
          </span>
        </div>
        <div className="flex items-center gap-1">
          {visibilityKey && available && onVisibilityChange && (
            <button
              type="button"
              onClick={() => onVisibilityChange(!visibilityValue)}
              className="flex h-7 w-7 items-center justify-center rounded-md border border-border text-muted-foreground hover:text-foreground"
              title={visibilityValue ? t("hideLayer") : t("showLayer")}
            >
              {visibilityValue ? (
                <Eye className="h-3.5 w-3.5" />
              ) : (
                <EyeOff className="h-3.5 w-3.5" />
              )}
            </button>
          )}
          <button
            type="button"
            onClick={handleRun}
            disabled={isPending}
            className="flex h-7 items-center gap-1 rounded-md border border-teal bg-teal/10 px-2 text-xs font-medium text-teal hover:bg-teal/20 disabled:opacity-50"
          >
            {isPending ? (
              <Loader2 className="h-3 w-3 animate-spin" />
            ) : (
              <Play className="h-3 w-3" />
            )}
            {available ? t("rerun") : t("run")}
          </button>
        </div>
      </div>

      {type === "motif" && (
        <div className="mt-2">
          <input
            value={motifPattern}
            onChange={(e) => setMotifPattern(e.target.value)}
            placeholder={t("motif.pattern")}
            className="w-full rounded-md border border-border bg-background px-2 py-1 text-xs font-mono"
          />
        </div>
      )}

      {summary && (
        <div className="mt-2 text-xs text-muted-foreground">{summary}</div>
      )}

      {available && (
        <button
          type="button"
          onClick={() => setExpanded((v) => !v)}
          className="mt-2 flex items-center gap-1 text-[10px] text-muted-foreground hover:text-foreground"
        >
          {expanded ? (
            <ChevronUp className="h-3 w-3" />
          ) : (
            <ChevronDown className="h-3 w-3" />
          )}
          {expanded ? t("collapse") : t("expand")}
        </button>
      )}

      {expanded && available && (
        <div className="mt-2 border-t border-border pt-2">
          <DetailsBlock type={type} data={result.data} t={t} />
        </div>
      )}
    </div>
  );
}

export function AnalysisLayersPanel({
  traceId,
  open,
  onOpenChange,
  onNotify,
  visibility,
  onVisibilityChange,
  pendingAnalyses,
  onAnalysisLaunched,
  onAnalysisLaunchFailed,
}: AnalysisLayersPanelProps) {
  const t = useTranslations("traces.analysis");
  const [motifPattern, setMotifPattern] = useState("");
  const list = useAnalysisList(traceId);

  const availableSet = useMemo(
    () =>
      new Set(
        (list.data?.availableTypes ?? []).map((s) => s.toLowerCase()),
      ),
    [list.data],
  );

  if (!open) return null;

  return (
    <>
      <div
        className="fixed inset-0 z-40 bg-black/30"
        onClick={() => onOpenChange(false)}
        aria-hidden
      />
      <aside
        className="fixed right-0 top-0 z-50 flex h-full w-[360px] flex-col border-l border-border bg-background shadow-xl"
        role="dialog"
        aria-label={t("title")}
      >
        <header className="flex items-center justify-between border-b border-border px-4 py-3">
          <h2 className="text-sm font-semibold">{t("title")}</h2>
          <button
            type="button"
            onClick={() => onOpenChange(false)}
            className="flex h-7 w-7 items-center justify-center rounded-md text-muted-foreground hover:bg-muted hover:text-foreground"
            title={t("close")}
          >
            <X className="h-4 w-4" />
          </button>
        </header>
        <div className="flex-1 space-y-2 overflow-y-auto p-3">
          {ANALYSIS_TYPES.map((type) => {
            const visibilityKey: keyof LayerVisibility | undefined =
              type === "motif"
                ? "motifs"
                : type === "restriction"
                  ? "restriction"
                  : undefined;
            return (
              <AnalysisRow
                key={type}
                traceId={traceId}
                type={type}
                available={availableSet.has(type)}
                visibilityKey={visibilityKey}
                visibilityValue={
                  visibilityKey ? visibility[visibilityKey] : undefined
                }
                onVisibilityChange={
                  visibilityKey
                    ? (v) => onVisibilityChange(visibilityKey, v)
                    : undefined
                }
                onNotify={onNotify}
                motifPattern={motifPattern}
                setMotifPattern={setMotifPattern}
                pending={pendingAnalyses?.has(type) ?? false}
                onLaunched={onAnalysisLaunched}
                onLaunchFailed={onAnalysisLaunchFailed}
              />
            );
          })}
        </div>
      </aside>
    </>
  );
}
