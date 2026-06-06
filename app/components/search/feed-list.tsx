"use client";

import { useTranslations } from "next-intl";
import { Link } from "@/lib/navigation";
import {
  FileText,
  MessageSquare,
  FlaskConical,
  User,
  Loader2,
  Inbox,
} from "lucide-react";
import { useFeed } from "@/hooks/use-search";
import type { FeedItem, FeedReason, SearchObjectType } from "@/types/search";
import { cn } from "@/lib/utils";

const TYPE_ICON: Record<SearchObjectType, typeof FileText> = {
  Study: FlaskConical,
  Trace: FileText,
  Discussion: MessageSquare,
  User,
};

function hrefFor(item: FeedItem): string {
  switch (item.objectType) {
    case "Study":
      return `/studies/${item.objectId}`;
    case "Trace":
      return `/traces/${item.objectId}`;
    case "Discussion":
      return item.tags
        ? `/studies/${item.tags}/discussions/${item.objectId}`
        : "/";
    case "User":
      return `/users/${item.objectId}`;
  }
}

const REASON_CLASS: Record<FeedReason, string> = {
  self: "bg-teal/10 text-teal",
  follow: "bg-blue-deep/10 text-blue-deep",
  watch: "bg-amber-500/10 text-amber-600",
  other: "bg-muted text-muted-foreground",
};

/**
 * Personal feed. Renders items from /api/v1/feed via cursor-paginated
 * useInfiniteQuery. Each row links to the underlying object route.
 */
export function FeedList() {
  const t = useTranslations("feed");
  const {
    data,
    isLoading,
    isError,
    hasNextPage,
    isFetchingNextPage,
    fetchNextPage,
  } = useFeed();

  const items = data?.pages.flatMap((page) => page.items) ?? [];

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-16">
        <Loader2 className="h-6 w-6 animate-spin text-teal" />
      </div>
    );
  }

  if (isError) {
    return (
      <div className="rounded-xl border border-destructive/40 bg-destructive/10 p-6 text-sm text-destructive">
        {t("error")}
      </div>
    );
  }

  if (items.length === 0) {
    return (
      <div className="rounded-xl border border-border bg-card p-10 text-center">
        <div className="mb-3 inline-flex items-center justify-center rounded-full bg-muted/50 p-3">
          <Inbox className="h-6 w-6 text-muted-foreground" />
        </div>
        <h3 className="mb-1 text-base font-semibold text-foreground">
          {t("empty.title")}
        </h3>
        <p className="text-sm text-muted-foreground">{t("empty.description")}</p>
      </div>
    );
  }

  return (
    <div className="space-y-3">
      <ul className="space-y-3">
        {items.map((item) => {
          const Icon = TYPE_ICON[item.objectType];
          return (
            <li key={`${item.objectType}:${item.objectId}:${item.updatedAt}`}>
              <Link
                href={hrefFor(item)}
                className="flex gap-3 rounded-xl border border-border bg-card p-4 transition-all hover:border-teal/40 hover:shadow-sm"
              >
                <div className="rounded-lg bg-muted p-2">
                  <Icon className="h-5 w-5 text-teal" />
                </div>
                <div className="min-w-0 flex-1">
                  <div className="mb-1 flex flex-wrap items-center gap-2">
                    <h4 className="truncate text-sm font-medium text-foreground">
                      {item.title}
                    </h4>
                    <span
                      className={cn(
                        "rounded-md px-2 py-0.5 text-[10px] font-medium uppercase tracking-wide",
                        REASON_CLASS[item.reason],
                      )}
                    >
                      {t(`reason.${item.reason}`)}
                    </span>
                  </div>
                  {item.body && (
                    <p className="line-clamp-2 text-xs text-muted-foreground">
                      {item.body}
                    </p>
                  )}
                  <p className="mt-1 text-[11px] text-muted-foreground/80">
                    {new Date(item.updatedAt).toLocaleString()}
                  </p>
                </div>
              </Link>
            </li>
          );
        })}
      </ul>

      {hasNextPage && (
        <div className="flex justify-center pt-2">
          <button
            type="button"
            onClick={() => fetchNextPage()}
            disabled={isFetchingNextPage}
            className={cn(
              "rounded-lg border border-border bg-card px-4 py-2 text-sm font-medium transition-all",
              isFetchingNextPage
                ? "cursor-not-allowed opacity-60"
                : "hover:bg-muted/60",
            )}
          >
            {isFetchingNextPage ? t("loadingMore") : t("loadMore")}
          </button>
        </div>
      )}
    </div>
  );
}
