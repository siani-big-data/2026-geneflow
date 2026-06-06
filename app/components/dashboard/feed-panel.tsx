"use client";

import { useTranslations } from "next-intl";
import {
  ArrowUpRight,
  FileText,
  FlaskConical,
  Inbox,
  Loader2,
  MessageSquare,
  Rss,
  User,
} from "lucide-react";
import { Card } from "@/components/ui";
import { Link } from "@/lib/navigation";
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

interface FeedPanelProps {
  /** How many feed items to request on the first page. */
  pageSize?: number;
  /** Optional fixed card height; defaults to the side-column convention. */
  height?: number;
}

/**
 * Dashboard panel projection of the personal feed.
 *
 * Renders the first page of `/api/v1/feed` inside a fixed-height card so
 * it slots into the dashboard side column next to `RecentStudiesPanel`
 * and `RecentPipelinesPanel`. The full-page `/feed` view stays the place
 * to paginate; the panel only links there via "View all".
 */
export function FeedPanel({ pageSize = 8, height = 380 }: FeedPanelProps) {
  const t = useTranslations("feed");
  const { data, isLoading, isError } = useFeed(pageSize);

  // Only render the first page in the panel — pagination lives on /feed.
  const items: FeedItem[] = data?.pages[0]?.items ?? [];

  return (
    <Card
      className="flex flex-col overflow-hidden shadow-sm"
      style={{ height }}
    >
      <div className="flex-shrink-0 border-b border-border p-5">
        <div className="flex items-center justify-between">
          <div className="min-w-0">
            <h2 className="truncate text-base font-semibold text-foreground">
              {t("title")}
            </h2>
            <p className="mt-1 truncate text-sm text-muted-foreground">
              {t("subtitle")}
            </p>
          </div>
          <Link
            href="/feed"
            className="flex flex-shrink-0 items-center gap-1.5 text-sm font-medium text-teal transition-colors duration-200 hover:text-teal/80"
          >
            {t("loadMore")}
            <ArrowUpRight className="h-4 w-4" />
          </Link>
        </div>
      </div>

      <div className="scrollbar-styled flex-1 overflow-y-auto">
        {isLoading ? (
          <div className="flex items-center justify-center py-12">
            <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
          </div>
        ) : isError ? (
          <div className="m-5 rounded-lg border border-destructive/40 bg-destructive/10 p-4 text-sm text-destructive">
            {t("error")}
          </div>
        ) : items.length === 0 ? (
          <div className="flex flex-col items-center justify-center px-5 py-12 text-center">
            <div className="mb-3 rounded-full bg-muted/50 p-3">
              <Inbox className="h-6 w-6 text-muted-foreground" />
            </div>
            <h3 className="mb-1 text-sm font-medium text-foreground">
              {t("empty.title")}
            </h3>
            <p className="max-w-xs text-xs text-muted-foreground">
              {t("empty.description")}
            </p>
          </div>
        ) : (
          <ul className="divide-y divide-border">
            {items.map((item) => (
              <FeedPanelRow key={feedKey(item)} item={item} t={t} />
            ))}
          </ul>
        )}
      </div>
    </Card>
  );
}

function feedKey(item: FeedItem): string {
  return `${item.objectType}:${item.objectId}:${item.updatedAt}`;
}

interface FeedPanelRowProps {
  item: FeedItem;
  t: (key: string) => string;
}

function FeedPanelRow({ item, t }: FeedPanelRowProps) {
  const Icon = TYPE_ICON[item.objectType] ?? Rss;
  return (
    <li>
      <Link
        href={hrefFor(item)}
        className="group block px-5 py-3 transition-all duration-200 hover:bg-muted/30"
      >
        <div className="flex items-start gap-3">
          <Icon className="mt-0.5 h-4 w-4 flex-shrink-0 text-teal" />
          <div className="min-w-0 flex-1">
            <div className="flex items-start justify-between gap-2">
              <h3 className="truncate text-sm font-medium text-foreground transition-colors duration-200 group-hover:text-teal">
                {item.title}
              </h3>
              <span
                className={cn(
                  "flex-shrink-0 rounded-md px-1.5 py-0.5 text-[10px] font-medium uppercase tracking-wide",
                  REASON_CLASS[item.reason],
                )}
              >
                {t(`reason.${item.reason}`)}
              </span>
            </div>
            {item.body && (
              <p className="mt-0.5 line-clamp-1 text-xs text-muted-foreground">
                {item.body}
              </p>
            )}
            <p className="mt-1 text-[11px] text-muted-foreground/80">
              {new Date(item.updatedAt).toLocaleString()}
            </p>
          </div>
        </div>
      </Link>
    </li>
  );
}
