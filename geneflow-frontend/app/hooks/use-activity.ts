"use client";

/**
 * Activity feed hooks. Uses React Query's `useInfiniteQuery` to model the
 * opaque cursor returned by the backend — `nextCursor` is treated as a
 * black-box token that is passed straight back on the next page request.
 */

import { useInfiniteQuery } from "@tanstack/react-query";
import { activityService } from "@/services";
import type { ActivityFeedResponse } from "@/types";

export const activityKeys = {
  all: ["activity"] as const,
  myFeed: (limit: number) => [...activityKeys.all, "me", limit] as const,
  studyTimeline: (studyId: string, limit: number) =>
    [...activityKeys.all, "studies", studyId, limit] as const,
};

/** Polling interval (ms) used while live SSE (1.E) is not yet wired up. */
const FEED_POLL_INTERVAL_MS = 15_000;

/**
 * Cursor-paginated infinite query over the current user's activity feed.
 *
 * Page boundaries are determined by the backend; the client never inspects
 * the cursor value. Stop conditions:
 *  - `hasMore === false` (server says: no more pages), or
 *  - `nextCursor === null` (no anchor to resume from).
 *
 * Freshness strategy until live SSE (1.E) lands:
 *  - `staleTime: 0` so revisits / focus always refetch the first page.
 *  - `refetchOnWindowFocus: true` (explicit; matches TanStack default).
 *  - `refetchInterval: 15s` while the tab is visible so new server-side
 *    events surface without user interaction. `refetchIntervalInBackground`
 *    stays `false` (the default) to avoid pointless work in hidden tabs.
 */
export function useMyActivityFeed(limit: number = 20) {
  return useInfiniteQuery({
    queryKey: activityKeys.myFeed(limit),
    initialPageParam: undefined as string | undefined,
    queryFn: ({ pageParam }) =>
      activityService.getMyFeed(limit, pageParam),
    getNextPageParam: (lastPage: ActivityFeedResponse) =>
      lastPage.hasMore && lastPage.nextCursor
        ? lastPage.nextCursor
        : undefined,
    staleTime: 0,
    refetchOnWindowFocus: true,
    refetchInterval: FEED_POLL_INTERVAL_MS,
  });
}

/**
 * Cursor-paginated infinite query over the activity timeline of a single
 * study. Mirrors {@link useMyActivityFeed} but scopes results to one study.
 *
 * The backend filters by `study_id` and excludes `Private` events, so this
 * hook surfaces both `StudyMembers` and `Public` events. Authorization
 * (member-or-public) is enforced server-side; a non-member request against a
 * private study results in a 403 and TanStack Query surfaces it as `isError`.
 *
 * The query is disabled until `studyId` is non-empty to avoid firing a
 * request during route hydration.
 */
export function useStudyTimeline(studyId: string, limit: number = 20) {
  return useInfiniteQuery({
    queryKey: activityKeys.studyTimeline(studyId, limit),
    initialPageParam: undefined as string | undefined,
    queryFn: ({ pageParam }) =>
      activityService.getStudyTimeline(studyId, limit, pageParam),
    getNextPageParam: (lastPage: ActivityFeedResponse) =>
      lastPage.hasMore && lastPage.nextCursor
        ? lastPage.nextCursor
        : undefined,
    enabled: studyId.length > 0,
    staleTime: 0,
    refetchOnWindowFocus: true,
    refetchInterval: FEED_POLL_INTERVAL_MS,
  });
}
