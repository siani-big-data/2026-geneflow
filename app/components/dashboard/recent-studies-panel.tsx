"use client";

import { useTranslations } from "next-intl";
import {
  ArrowUpRight,
  Beaker,
  FolderOpen,
  Loader2,
  Users,
} from "lucide-react";
import { Card } from "@/components/ui";
import { StatusBadge } from "@/components/shared";
import { Link } from "@/lib/navigation";
import { useMyStudies } from "@/hooks";
import type { StudySummary } from "@/types";

interface RecentStudiesPanelProps {
  /** How many studies to render. The hook still requests page 1. */
  limit?: number;
}

/**
 * Lateral panel for the dashboard showing the most recent studies the
 * current user has access to. Mirrors the layout, paddings and header
 * conventions of <see cref="RecentPipelinesPanel"/> so both side panels
 * look symmetric.
 *
 * Each row links into the study detail page.
 */
export function RecentStudiesPanel({ limit = 5 }: RecentStudiesPanelProps) {
  const t = useTranslations("dashboard.recentStudies");
  const { data, isLoading } = useMyStudies(1, limit);

  const studies = data?.items ?? [];

  return (
    // Fixed height + scrollable body. The activity panel mirrors the
    // combined height of this card and the pipelines card with the gap in
    // between (see `my-activity-panel.tsx`).
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
            href="/studies"
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
        ) : studies.length === 0 ? (
          <div className="flex flex-col items-center justify-center px-5 py-12 text-center">
            <div className="mb-3 rounded-full bg-muted/50 p-3">
              <FolderOpen className="h-6 w-6 text-muted-foreground" />
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
            {studies.map((study) => (
              <RecentStudyRow key={study.id} study={study} />
            ))}
          </div>
        )}
      </div>
    </Card>
  );
}

interface RecentStudyRowProps {
  study: StudySummary;
}

function RecentStudyRow({ study }: RecentStudyRowProps) {
  // Defensive: lower-case the status so the badge variant always resolves.
  const status = study.statusName.toLowerCase() as
    | "active"
    | "draft"
    | "completed"
    | "archived"
    | "pending"
    | "processing";

  return (
    <Link
      href={`/studies/${study.id}`}
      className="group block px-5 py-4 transition-all duration-200 hover:bg-muted/30"
    >
      <div className="flex items-start gap-3">
        <Beaker className="mt-0.5 h-4 w-4 flex-shrink-0 text-teal" />
        <div className="min-w-0 flex-1">
          <div className="flex items-start justify-between gap-2">
            <h3 className="truncate text-sm font-medium text-foreground transition-colors duration-200 group-hover:text-teal">
              {study.title}
            </h3>
            <StatusBadge status={status} />
          </div>
          {study.principalInvestigator && (
            <p className="mt-0.5 truncate text-xs text-muted-foreground">
              {study.principalInvestigator}
            </p>
          )}
          <div className="mt-1.5 flex items-center gap-2 text-xs text-muted-foreground">
            <span className="inline-flex items-center gap-1">
              <Users className="h-3 w-3" />
              {study.memberCount}
            </span>
            {study.researchFieldName && (
              <>
                <span>·</span>
                <span className="truncate">{study.researchFieldName}</span>
              </>
            )}
          </div>
        </div>
      </div>
    </Link>
  );
}
