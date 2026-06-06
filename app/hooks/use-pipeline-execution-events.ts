"use client";

import { useEffect, useRef } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { pipelineKeys } from "@/hooks/use-pipelines";
import { createSseSubscription } from "@/lib/sse-client";

/**
 * Wire-format payload pushed by `PipelineExecutionSseBroadcaster` for every
 * pipeline execution lifecycle event.
 */
export interface PipelineExecutionServerEvent {
  eventType:
    | "execution.started"
    | "execution.step.completed"
    | "execution.completed"
    | "execution.failed";
  executionId: string;
  pipelineId: string | null;
  traceId: string | null;
  completedSteps: number | null;
  totalSteps: number | null;
  stepType: string | null;
  success: boolean;
  error: string | null;
  occurredAt: string;
}

export interface UsePipelineExecutionEventsOptions {
  /** Disables the SSE connection without unmounting the host component. */
  enabled?: boolean;
  /** Invoked once when the execution starts on the server. */
  onStarted?: (event: PipelineExecutionServerEvent) => void;
  /** Invoked each time a step completes. */
  onStepCompleted?: (event: PipelineExecutionServerEvent) => void;
  /** Invoked when the execution finishes successfully. */
  onCompleted?: (event: PipelineExecutionServerEvent) => void;
  /** Invoked when the execution fails. */
  onFailed?: (event: PipelineExecutionServerEvent) => void;
}

/**
 * Subscribes to the pipeline execution's SSE stream and invalidates the
 * matching React-Query caches as steps complete and the execution finishes.
 *
 * Callers typically gate `enabled` on `execution.statusId === Running` to
 * avoid keeping a connection open for terminal states. On step completion
 * we only invalidate the execution detail; on terminal events we also
 * invalidate the executions list so dashboards re-render without refresh.
 */
export function usePipelineExecutionEvents(
  executionId: string,
  options: UsePipelineExecutionEventsOptions = {},
): void {
  const {
    enabled = true,
    onStarted,
    onStepCompleted,
    onCompleted,
    onFailed,
  } = options;
  const queryClient = useQueryClient();

  const onStartedRef = useRef(onStarted);
  const onStepCompletedRef = useRef(onStepCompleted);
  const onCompletedRef = useRef(onCompleted);
  const onFailedRef = useRef(onFailed);
  onStartedRef.current = onStarted;
  onStepCompletedRef.current = onStepCompleted;
  onCompletedRef.current = onCompleted;
  onFailedRef.current = onFailed;

  useEffect(() => {
    if (!enabled || !executionId) return;

    const dispose = createSseSubscription({
      url: `/api/v1/pipeline-executions/${executionId}/events`,
      enabled: true,
      // The worker can complete a fast pipeline before the SSE connection is
      // established. When that happens the broadcaster publishes with no
      // subscriber and the frame is dropped. Invalidate on `ready` so React
      // Query refetches the current execution state on connect.
      onReady: () => {
        queryClient.invalidateQueries({
          queryKey: pipelineKeys.execution(executionId),
        });
        queryClient.invalidateQueries({ queryKey: pipelineKeys.executions() });
      },
      onFrame: (event, payload) => {
        if (!event.startsWith("execution.")) return;

        const parsed = payload as PipelineExecutionServerEvent;

        // Always invalidate the per-execution cache so the detail panel /
        // expandable row re-renders progress immediately.
        queryClient.invalidateQueries({
          queryKey: pipelineKeys.execution(executionId),
        });

        if (event === "execution.started") {
          onStartedRef.current?.(parsed);
          return;
        }

        if (event === "execution.step.completed") {
          onStepCompletedRef.current?.(parsed);
          return;
        }

        // Terminal events: also invalidate the broader executions caches so
        // any list / dashboard view re-renders the new status.
        queryClient.invalidateQueries({ queryKey: pipelineKeys.executions() });

        if (event === "execution.completed") {
          onCompletedRef.current?.(parsed);
        } else if (event === "execution.failed") {
          onFailedRef.current?.(parsed);
        }
      },
    });

    return dispose;
  }, [executionId, enabled, queryClient]);
}
