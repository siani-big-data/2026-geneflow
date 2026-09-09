"use client";

/**
 * Study Timeline Tab — chronological activity feed scoped to a single study.
 *
 * Reuses the same building blocks as the dashboard's `MyActivityPanel`
 * (TanStack `useInfiniteQuery` over an opaque server cursor + intersection
 * observer for infinite scroll + `ActivityFeedItem` row component) but the
 * data source is the study-scoped endpoint `GET /api/v1/activity/studies/{id}`.
 *
 * Server-side authorization (member, or non-member on public studies) is
 * enforced by `StudyMembershipBehavior`. A 403 surfaces here as `isError`.
 */

import { useEffect, useRef } from "react";
import { useTranslations } from "next-intl";
import { History, Loader2, RefreshCw } from "lucide-react";
import { Button, Card } from "@/components/ui";
import { EmptyState } from "@/components/shared";
import { useStudyTimeline } from "@/hooks";
import { ActivityFeedItem } from "@/components/activity/activity-feed-item";
import type { ActivityEventResponse } from "@/types";

const DEFAULT_PAGE_SIZE = 30;

interface StudyTimelineTabProps {
  studyId: string;
  /** Page size requested per fetch. The server clamps between 1 and 100. */
  pageSize?: number;
}

export function StudyTimelineTab({
  studyId,
  pageSize = DEFAULT_PAGE_SIZE,
}: StudyTimelineTabProps) {
  const t = useTranslations("activity");

  const {
    data,
    fetchNextPage,
    hasNextPage,
    isFetching,
    isFetchingNextPage,
    isError,
    refetch,
  } = useStudyTimeline(studyId, pageSize);

  const events: ActivityEventResponse[] =
    data?.pages.flatMap((p) => p.items) ?? [];

  // Infinite scroll: a sentinel at the bottom of the scrollable list triggers
  // `fetchNextPage` as soon as it becomes visible.
  const sentinelRef = useRef<HTMLLIElement | null>(null);
  const scrollRef = useRef<HTMLUListElement | null>(null);

  useEffect(() => {
    const sentinel = sentinelRef.current;
    const root = scrollRef.current;
    if (!sentinel || !root) return;
    if (!hasNextPage || isFetchingNextPage) return;

    const observer = new IntersectionObserver(
      (entries) => {
        const [entry] = entries;
        if (entry?.isIntersecting) {
          fetchNextPage();
        }
      },
      { root, rootMargin: "0px 0px 200px 0px", threshold: 0 },
    );
    observer.observe(sentinel);
    return () => observer.disconnect();
  }, [hasNextPage, isFetchingNextPage, fetchNextPage, events.length]);

  return (
    <Card
      className="flex flex-col overflow-hidden shadow-sm"
      style={{ height: 720 }}
    >
      <header className="flex-shrink-0 border-b border-border p-5">
        <div className="flex items-start justify-between gap-3">
          <div className="min-w-0">
            <h2 className="truncate text-base font-semibold text-foreground">
              {t("studyTimeline.title")}
            </h2>
            <p className="mt-1 truncate text-sm text-muted-foreground">
              {t("studyTimeline.description")}
            </p>
          </div>
          <Button
            variant="ghost"
            size="sm"
            onClick={() => refetch()}
            disabled={isFetching}
            aria-label={t("refresh")}
            title={t("refresh")}
            className="h-8 w-8 flex-shrink-0 p-0"
          >
            <RefreshCw
              className={`h-4 w-4 ${isFetching ? "animate-spin" : ""}`}
            />
          </Button>
        </div>
      </header>

      {isError ? (
        <div className="m-5 flex-1 rounded-md border border-destructive/40 bg-destructive/5 p-4 text-sm text-destructive">
          <p>{t("error")}</p>
          <Button
            variant="outline"
            size="sm"
            className="mt-3"
            onClick={() => refetch()}
          >
            {t("retry")}
          </Button>
        </div>
      ) : isFetching && events.length === 0 ? (
        <div
          className="flex flex-1 items-center justify-center py-12"
          aria-live="polite"
        >
          <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
        </div>
      ) : events.length === 0 ? (
        <div className="flex flex-1 items-center justify-center">
          <EmptyState
            icon={History}
            title={t("studyTimeline.empty.title")}
            description={t("studyTimeline.empty.description")}
            className="px-5"
          />
        </div>
      ) : (
        <ul
          ref={scrollRef}
          className="scrollbar-styled flex-1 divide-y divide-border overflow-y-auto scroll-smooth"
          data-testid="study-timeline-feed"
        >
          {events.map((event) => (
            <ActivityFeedItem
              key={event.id}
              event={event}
              className="rounded-none border-0 bg-transparent px-5 py-4 hover:bg-muted/30"
            />
          ))}
          <li
            ref={sentinelRef}
            aria-hidden
            className="h-1 list-none"
            data-testid="study-timeline-sentinel"
          />
          <li className="list-none px-5 py-3 text-center text-xs text-muted-foreground">
            {isFetchingNextPage ? (
              <span className="inline-flex items-center gap-2">
                <Loader2 className="h-3 w-3 animate-spin" />
                {t("loadMore")}
              </span>
            ) : hasNextPage ? null : (
              t("endOfFeed")
            )}
          </li>
        </ul>
      )}
    </Card>
  );
}
