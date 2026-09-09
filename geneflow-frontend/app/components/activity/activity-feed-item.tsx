"use client";

/**
 * Single activity feed row. Pure presentational — receives an event DTO
 * and renders an icon, a human-readable headline and a relative timestamp.
 */

import { useTranslations } from "next-intl";
import {
  Activity as ActivityIcon,
  Beaker,
  FileText,
  GitBranch,
  Play,
  User as UserIcon,
  type LucideIcon,
} from "lucide-react";
import { Link } from "@/lib/navigation";
import { cn } from "@/lib/utils";
import type { ActivityEventResponse } from "@/types";

interface ActivityFeedItemProps {
  event: ActivityEventResponse;
  className?: string;
}

const OBJECT_ICONS: Record<string, LucideIcon> = {
  Study: Beaker,
  Trace: FileText,
  Pipeline: GitBranch,
  PipelineExecution: Play,
  Profile: UserIcon,
  User: UserIcon,
};

/**
 * Calls `t(key)` and returns `fallback` if the key is missing or throws.
 * next-intl logs missing keys but throws when configured strict, hence the
 * try/catch.
 */
function safeTranslate(
  t: (key: string) => string,
  key: string,
  fallback: string,
): string {
  try {
    const value = t(key);
    // When a key is missing, next-intl in lenient mode returns the key path
    // itself; we treat that as "not translated" and fall back.
    return value === key ? fallback : value;
  } catch {
    return fallback;
  }
}

/** Maps an object type to an in-app deep link, when one exists. */
function buildObjectHref(objectType: string, objectId: string): string | null {
  switch (objectType) {
    case "Study":
      return `/studies/${objectId}`;
    case "Trace":
      return `/traces/${objectId}`;
    case "Pipeline":
      return `/pipelines/${objectId}`;
    default:
      return null;
  }
}

/**
 * Renders an ISO timestamp as an absolute locale-formatted date+time
 * (e.g. "5 jun 2026, 18:42"). We deliberately avoid relative formatting
 * because client/server clock skew was producing future-leaning strings
 * like "dentro de 6761 segundos" for events that had just happened.
 */
function formatAbsolute(iso: string, locale: string): string {
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return iso;
  return date.toLocaleString(locale, {
    day: "2-digit",
    month: "short",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });
}

export function ActivityFeedItem({ event, className }: ActivityFeedItemProps) {
  const t = useTranslations("activity");
  const Icon = OBJECT_ICONS[event.objectType] ?? ActivityIcon;
  const href = buildObjectHref(event.objectType, event.objectId);

  // The headline uses i18n keys keyed by verb + object type. Unknown
  // server-emitted values fall back to the raw token so the row still renders.
  const verbKey = `verbs.${event.verb}`;
  const typeKey = `objects.${event.objectType}`;
  const verb = safeTranslate(t, verbKey, event.verb);
  const objectLabel = safeTranslate(t, typeKey, event.objectType);

  const objectNode = href ? (
    <Link href={href} className="font-medium text-foreground hover:underline">
      {objectLabel}
    </Link>
  ) : (
    <span className="font-medium text-foreground">{objectLabel}</span>
  );

  return (
    <li
      className={cn(
        "flex items-start gap-3 rounded-md border border-border bg-card p-3 transition-colors hover:bg-accent/40",
        className,
      )}
      data-testid="activity-feed-item"
      data-event-id={event.id}
    >
      <div className="mt-0.5 rounded-full bg-muted p-2">
        <Icon className="h-4 w-4 text-muted-foreground" aria-hidden />
      </div>
      <div className="flex min-w-0 flex-1 flex-col">
        <p className="text-sm text-foreground">
          <span className="text-muted-foreground">{verb}</span> {objectNode}
        </p>
        <p className="mt-1 text-xs text-muted-foreground">
          <time dateTime={event.occurredAt} title={event.occurredAt}>
            {formatAbsolute(event.occurredAt, t("locale"))}
          </time>
          {event.studyId && (
            <>
              <span className="mx-1">·</span>
              <Link
                href={`/studies/${event.studyId}`}
                className="hover:underline"
              >
                {t("inStudy")}
              </Link>
            </>
          )}
        </p>
      </div>
    </li>
  );
}
