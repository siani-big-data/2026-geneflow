"use client";

import { useTraceProcessingEvents } from "@/hooks/use-trace-processing-events";

/**
 * Headless component: opens an SSE subscription to the trace processing
 * stream for a single trace and renders nothing. The hook itself takes
 * care of invalidating the React-Query caches (`["trace", studyId, traceId]`,
 * `["traces", studyId]`, `["traces", studyId, "counts"]`) on every frame
 * and on connect (`onReady`), so the parent list refetches automatically
 * once the worker transitions the trace.
 *
 * Used by the studies page to wire one connection per visible trace that
 * is still in a non-terminal state (Uploaded / Validating / Processing).
 * Once the trace transitions to a terminal state the parent stops
 * rendering this component, which disposes the subscription.
 */
export function TraceProcessingSubscriber({
  traceId,
  studyId,
}: {
  traceId: string;
  studyId?: string | null;
}) {
  useTraceProcessingEvents(traceId, {
    enabled: true,
    studyId: studyId ?? null,
  });
  return null;
}
