"use client";

import { useTranslations } from "next-intl";
import { useRecentExecutions } from "@/hooks/use-pipelines";
import { Link } from "@/lib/navigation";
import { Card } from "@/components/ui";
import {
  ArrowUpRight,
  CheckCircle2,
  Clock,
  Loader2,
  Workflow,
  XCircle,
  AlertCircle,
} from "lucide-react";
import type { RecentPipelineExecution } from "@/types/pipeline";

/**
 * Lateral panel for the dashboard showing the current user's most recent
 * pipeline executions across all studies they are a member of.
 *
 * Each item links into the originating study's pipeline executions tab so
 * the user can drill into details.
 */
export function RecentPipelinesPanel() {
  const t = useTranslations("dashboard.recentPipelines");
  const { data, isLoading } = useRecentExecutions(5);

  const executions = data ?? [];

  return (
    // Fixed height + scrollable body, matching `RecentStudiesPanel` so the
    // pair stacks into a predictable column the activity panel can mirror.
    <Card
      className="flex flex-col overflow-hidden shadow-sm"
      style={{ height: 380 }}
    >
      <div className="flex-shrink-0 border-b border-border p-5">
        <div className="flex items-center justify-between">
          <div className="min-w-0">
            <h2 className="truncate text-base font-semibold text-foreground">
              {t("title")}
            </h2>
            <p className="mt-1 truncate text-sm text-muted-foreground">
              {t("description")}
            </p>
          </div>
          <Link
            href="/pipelines"
            className="flex flex-shrink-0 items-center gap-1.5 text-sm font-medium text-teal transition-colors duration-200 hover:text-teal/80"
          >
            {t("viewAll")}
            <ArrowUpRight className="h-4 w-4" />
          </Link>
        </div>
      </div>

      <div className="scrollbar-styled flex-1 overflow-y-auto">
        {isLoading ? (
          <div className="flex items-center justify-center py-12">
            <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
          </div>
        ) : executions.length === 0 ? (
          <div className="flex flex-col items-center justify-center px-5 py-12 text-center">
            <div className="mb-3 rounded-full bg-muted/50 p-3">
              <Workflow className="h-6 w-6 text-muted-foreground" />
            </div>
            <h3 className="mb-1 text-sm font-medium text-foreground">
              {t("empty.title")}
            </h3>
            <p className="max-w-xs text-xs text-muted-foreground">
              {t("empty.description")}
            </p>
          </div>
        ) : (
          <div className="divide-y divide-border">
            {executions.map((execution) => (
              <RecentPipelineRow key={execution.id} execution={execution} />
            ))}
          </div>
        )}
      </div>
    </Card>
  );
}

interface RecentPipelineRowProps {
  execution: RecentPipelineExecution;
}

function RecentPipelineRow({ execution }: RecentPipelineRowProps) {
  const t = useTranslations("dashboard.recentPipelines");
  const StatusIcon = getStatusIcon(execution.statusId);
  const statusColor = getStatusColor(execution.statusId);
  const showProgress =
    execution.statusId === EXECUTION_STATUS.RUNNING ||
    execution.statusId === EXECUTION_STATUS.PENDING;

  return (
    <Link
      href={`/studies/${execution.studyId}?tab=pipelines`}
      className="group block px-5 py-4 transition-all duration-200 hover:bg-muted/30"
    >
      <div className="flex items-start gap-3">
        <StatusIcon
          className={`mt-0.5 h-4 w-4 flex-shrink-0 ${statusColor}`}
        />
        <div className="min-w-0 flex-1">
          <div className="flex items-start justify-between gap-2">
            <h3 className="truncate text-sm font-medium text-foreground transition-colors duration-200 group-hover:text-teal">
              {execution.pipelineName}
            </h3>
            <span className="flex-shrink-0 text-xs font-medium text-muted-foreground">
              {execution.progressPercentage}%
            </span>
          </div>
          <p className="mt-0.5 truncate text-xs text-muted-foreground">
            {execution.studyTitle}
          </p>

          {showProgress && (
            <div className="mt-2 h-1 w-full overflow-hidden rounded-full bg-muted">
              <div
                className="h-full bg-teal transition-all duration-300"
                style={{ width: `${execution.progressPercentage}%` }}
              />
            </div>
          )}

          <div className="mt-1.5 flex items-center gap-2 text-xs text-muted-foreground">
            <span>
              {t("steps", {
                completed: execution.completedSteps,
                total: execution.totalSteps,
              })}
            </span>
            {execution.durationSeconds != null && (
              <>
                <span>·</span>
                <span>
                  {t("duration", {
                    seconds: Math.round(execution.durationSeconds),
                  })}
                </span>
              </>
            )}
          </div>
        </div>
      </div>
    </Link>
  );
}

const EXECUTION_STATUS = {
  PENDING: 1,
  RUNNING: 2,
  COMPLETED: 3,
  FAILED: 4,
  CANCELLED: 5,
} as const;

function getStatusIcon(statusId: number) {
  switch (statusId) {
    case EXECUTION_STATUS.COMPLETED:
      return CheckCircle2;
    case EXECUTION_STATUS.RUNNING:
      return Loader2;
    case EXECUTION_STATUS.FAILED:
      return XCircle;
    case EXECUTION_STATUS.CANCELLED:
      return AlertCircle;
    case EXECUTION_STATUS.PENDING:
    default:
      return Clock;
  }
}

function getStatusColor(statusId: number) {
  switch (statusId) {
    case EXECUTION_STATUS.COMPLETED:
      return "text-emerald-500";
    case EXECUTION_STATUS.RUNNING:
      return "text-teal animate-spin";
    case EXECUTION_STATUS.FAILED:
      return "text-red-500";
    case EXECUTION_STATUS.CANCELLED:
      return "text-muted-foreground";
    case EXECUTION_STATUS.PENDING:
    default:
      return "text-amber-500";
  }
}
