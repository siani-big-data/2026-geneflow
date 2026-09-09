"use client";

import { useEffect, useRef } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { analysisKeys } from "@/hooks/use-analysis";
import { createSseSubscription } from "@/lib/sse-client";
import type { AnalysisType } from "@/types";

/**
 * Wire-format payload pushed by `AnalysisSseBroadcaster` for every
 * `*Completed` / `*Failed` event on the backend.
 */
export interface AnalysisServerEvent {
  eventType: string;
  analysisKey: AnalysisType;
  success: boolean;
  error: string | null;
  occurredAt: string;
}

export interface UseAnalysisEventsOptions {
  /** Disables the SSE connection without unmounting the host component. */
  enabled?: boolean;
  /** Invoked for any `*.success` event after caches are invalidated. */
  onSuccess?: (event: AnalysisServerEvent) => void;
  /** Invoked for any `*.failed` event. */
  onFailure?: (event: AnalysisServerEvent) => void;
}

/**
 * Subscribes to the trace's analysis SSE stream and invalidates the matching
 * React-Query caches when an analysis finishes. Transport (auth, framing,
 * exponential reconnect) is handled by {@link createSseSubscription}.
 *
 * The hook **must** live above the `AnalysisLayersPanel` (e.g. on the trace
 * page) so completion toasts still fire after the panel is closed.
 */
export function useAnalysisEvents(
  traceId: string,
  options: UseAnalysisEventsOptions = {},
): void {
  const { enabled = true, onSuccess, onFailure } = options;
  const queryClient = useQueryClient();

  // Hold latest callbacks in refs so the effect doesn't reconnect when the
  // parent re-renders with new closure identities.
  const onSuccessRef = useRef(onSuccess);
  const onFailureRef = useRef(onFailure);
  onSuccessRef.current = onSuccess;
  onFailureRef.current = onFailure;

  useEffect(() => {
    if (!enabled || !traceId) return;

    const dispose = createSseSubscription({
      url: `/api/v1/traces/${traceId}/analysis/events`,
      enabled: true,
      onFrame: (event, payload) => {
        // We only care about `<key>.success` and `<key>.failed` frames.
        if (!event.includes(".")) return;

        const parsed = payload as AnalysisServerEvent;
        const key = parsed.analysisKey;
        if (key) {
          queryClient.invalidateQueries({ queryKey: analysisKeys.list(traceId) });
          queryClient.invalidateQueries({
            queryKey: analysisKeys.result(traceId, key),
          });
        }

        if (parsed.success) onSuccessRef.current?.(parsed);
        else onFailureRef.current?.(parsed);
      },
    });

    return dispose;
  }, [traceId, enabled, queryClient]);
}
