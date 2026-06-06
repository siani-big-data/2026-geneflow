"use client";

import { useEffect, useRef } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { createSseSubscription } from "@/lib/sse-client";

/**
 * Wire-format payload pushed by `TraceProcessingSseBroadcaster` for every
 * trace processing transition (started / completed / failed).
 */
export interface TraceProcessingServerEvent {
  eventType:
    | "trace.processing.started"
    | "trace.processing.completed"
    | "trace.processing.failed";
  traceId: string;
  studyId: string | null;
  success: boolean;
  error: string | null;
  qualityScore: number | null;
  occurredAt: string;
}

export interface UseTraceProcessingEventsOptions {
  /** Disables the SSE connection without unmounting the host component. */
  enabled?: boolean;
  /** Parent study id; used to build cache keys for invalidation. */
  studyId?: string | null;
  /** Invoked once when processing starts on the server. */
  onStarted?: (event: TraceProcessingServerEvent) => void;
  /** Invoked when processing completes successfully. */
  onCompleted?: (event: TraceProcessingServerEvent) => void;
  /** Invoked when processing fails. */
  onFailed?: (event: TraceProcessingServerEvent) => void;
  /**
   * Invoked when the SSE connection becomes live. Useful for callers that
   * keep their own state outside React Query (e.g. a `useState` cache of
   * the trace) and need to refetch on connect to cover the race where the
   * worker completes before the subscription is established.
   */
  onReady?: () => void;
}

/**
 * Subscribes to the trace's processing-status SSE stream and invalidates the
 * matching React-Query caches whenever the trace transitions state.
 *
 * Default `enabled` policy is left to the caller — the trace page passes
 * `enabled: trace?.status === "Processing"` so the connection is only open
 * while there's work to track.
 */
export function useTraceProcessingEvents(
  traceId: string,
  options: UseTraceProcessingEventsOptions = {},
): void {
  const {
    enabled = true,
    studyId,
    onStarted,
    onCompleted,
    onFailed,
    onReady,
  } = options;
  const queryClient = useQueryClient();

  const onStartedRef = useRef(onStarted);
  const onCompletedRef = useRef(onCompleted);
  const onFailedRef = useRef(onFailed);
  const onReadyRef = useRef(onReady);
  onStartedRef.current = onStarted;
  onCompletedRef.current = onCompleted;
  onFailedRef.current = onFailed;
  onReadyRef.current = onReady;

  useEffect(() => {
    if (!enabled || !traceId) return;

    const invalidateTraceCaches = (effectiveStudyId: string | undefined) => {
      queryClient.invalidateQueries({
        queryKey: ["trace", effectiveStudyId, traceId],
      });
      if (effectiveStudyId) {
        queryClient.invalidateQueries({
          queryKey: ["traces", effectiveStudyId],
        });
        queryClient.invalidateQueries({
          queryKey: ["traces", effectiveStudyId, "counts"],
        });
      }
    };

    const dispose = createSseSubscription({
      url: `/api/v1/traces/${traceId}/processing/events`,
      enabled: true,
      // The worker often finishes before the SSE connection is established.
      // When the broadcaster publishes with no live subscriber the frame is
      // dropped (no replay). Invalidate on `ready` so React Query refetches
      // the current server state and the page reconciles whatever transitions
      // happened during the connect handshake.
      onReady: () => {
        invalidateTraceCaches(studyId ?? undefined);
        onReadyRef.current?.();
      },
      onFrame: (event, payload) => {
        if (!event.startsWith("trace.processing.")) return;

        const parsed = payload as TraceProcessingServerEvent;
        const effectiveStudyId = parsed.studyId ?? studyId ?? undefined;

        // Always invalidate the single-trace cache; list/counts only when
        // we know which study to invalidate (avoid blasting unrelated caches).
        queryClient.invalidateQueries({
          queryKey: ["trace", effectiveStudyId, traceId],
        });
        if (effectiveStudyId) {
          queryClient.invalidateQueries({
            queryKey: ["traces", effectiveStudyId],
          });
          queryClient.invalidateQueries({
            queryKey: ["traces", effectiveStudyId, "counts"],
          });
        }

        if (event === "trace.processing.started") {
          onStartedRef.current?.(parsed);
        } else if (event === "trace.processing.completed") {
          onCompletedRef.current?.(parsed);
        } else if (event === "trace.processing.failed") {
          onFailedRef.current?.(parsed);
        }
      },
    });

    return dispose;
  }, [traceId, enabled, studyId, queryClient]);
}
