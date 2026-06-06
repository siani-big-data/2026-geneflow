"use client";

import { useEffect, useRef } from "react";
import { useTranslations } from "next-intl";
import { History, Loader2, RefreshCw } from "lucide-react";
import { Button, Card } from "@/components/ui";
import { EmptyState } from "@/components/shared";
import { useMyActivityFeed } from "@/hooks";
import { ActivityFeedItem } from "@/components/activity/activity-feed-item";
import type { ActivityEventResponse } from "@/types";

const DEFAULT_PAGE_SIZE = 20;

interface MyActivityPanelProps {
  /** Page size requested per fetch. The server clamps between 1 and 100. */
  pageSize?: number;
  /** Optional extra className for the outer Card. */
  className?: string;
}

/**
 * Self-contained dashboard panel rendering the current user's activity feed.
 *
 * The feed is keyset-paginated server-side via an opaque cursor; this panel
 * owns the cursor lifecycle through `useMyActivityFeed` (TanStack
 * `useInfiniteQuery`) and exposes a "load more" button that walks the pages.
 *
 * Visual contract follows the sibling `RecentPipelinesPanel`: a `Card`
 * wrapper with a header (title + description) and a divided body with
 * dedicated loading / error / empty states.
 */
export function MyActivityPanel({
  pageSize = DEFAULT_PAGE_SIZE,
  className,
}: MyActivityPanelProps) {
  const t = useTranslations("activity");

  const {
    data,
    fetchNextPage,
    hasNextPage,
    isFetching,
    isFetchingNextPage,
    isError,
    refetch,
  } = useMyActivityFeed(pageSize);

  const events: ActivityEventResponse[] =
    data?.pages.flatMap((p) => p.items) ?? [];

  // Infinite scroll: a sentinel at the bottom of the scrollable list triggers
  // `fetchNextPage` as soon as it becomes visible. Replaces the explicit
  // "Load more" button with a continuous slide-up reveal.
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
    // The side column stacks two 380px panels with a 24px gap
    // (`space-y-6`), so the activity panel locks to 380 + 24 + 380 = 784px
    // to line up exactly with that pair. The internal `<ul>` keeps
    // `flex-1 overflow-y-auto`, so it absorbs the remaining space and
    // remains the only scrollable region.
    <Card
      className={`flex flex-col overflow-hidden shadow-sm ${
        className ?? ""
      }`.trim()}
      style={{ height: 784 }}
    >
      <header className="flex-shrink-0 border-b border-border p-5">
        <div className="flex items-start justify-between gap-3">
          <div className="min-w-0">
            <h2 className="truncate text-base font-semibold text-foreground">
              {t("title")}
            </h2>
            <p className="mt-1 truncate text-sm text-muted-foreground">
              {t("description")}
            </p>
          </div>
          {}
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
            title={t("empty.title")}
            description={t("empty.description")}
            className="px-5"
          />
        </div>
      ) : (
        <>
          {/*
            The list grows to fill all remaining vertical space inside the
            Card (`flex-1`) so the panel matches the height of the side
            column (Recent Studies + Recent Pipelines). Overflow becomes a
            scroll area; "Load more" still appends pages beneath the
            currently visible window.
          */}
          <ul
            ref={scrollRef}
            className="scrollbar-styled flex-1 divide-y divide-border overflow-y-auto scroll-smooth"
            data-testid="activity-feed"
          >
            {events.map((event) => (
              // The shared row component carries its own padding-friendly
              // styling; we strip the border/background to integrate cleanly
              // inside the panel's divider stack.
              <ActivityFeedItem
                key={event.id}
                event={event}
                className="rounded-none border-0 bg-transparent px-5 py-4 hover:bg-muted/30"
              />
            ))}
            {/*
              Sentinel + footer status sit at the end of the scrollable list
              so they slide into view together with the rows. The observer
              above triggers `fetchNextPage` when the sentinel intersects.
            */}
            <li
              ref={sentinelRef}
              aria-hidden
              className="h-1 list-none"
              data-testid="activity-feed-sentinel"
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
        </>
      )}
    </Card>
  );
}
