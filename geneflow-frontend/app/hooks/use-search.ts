"use client";

import { useInfiniteQuery, useQuery } from "@tanstack/react-query";
import { searchService } from "@/services/search.service";
import type {
  CursorPaged,
  ExploreItem,
  ExploreParams,
  GlobalSearchParams,
  SearchHit,
  SearchObjectType,
} from "@/types/search";

// ============= QUERY KEYS =============
export const searchKeys = {
  all: ["search"] as const,
  global: (p: GlobalSearchParams) => [...searchKeys.all, "global", p] as const,
  trending: (type?: SearchObjectType, limit?: number) =>
    [...searchKeys.all, "trending", type ?? null, limit ?? null] as const,
  featured: (limit?: number) =>
    [...searchKeys.all, "featured", limit ?? null] as const,
  recent: (type?: SearchObjectType, pageSize?: number) =>
    [...searchKeys.all, "recent", type ?? null, pageSize ?? null] as const,
};

// ============= GLOBAL SEARCH =============
/**
 * Cursor-paginated global search. Disabled while the query is empty so
 * we don't fire a request on every keystroke before the user types.
 */
export function useGlobalSearch(
  params: GlobalSearchParams,
  options: { enabled?: boolean } = {},
) {
  const enabled = (options.enabled ?? true) && params.q.trim().length > 0;
  return useInfiniteQuery<CursorPaged<SearchHit>>({
    queryKey: searchKeys.global(params),
    queryFn: ({ pageParam }) =>
      searchService.search({ ...params, cursor: pageParam as string | undefined }),
    initialPageParam: undefined as string | undefined,
    getNextPageParam: (last) => last.nextCursor ?? undefined,
    enabled,
  });
}

// ============= EXPLORE =============
export function useTrending(type?: SearchObjectType, limit = 20) {
  return useQuery<ExploreItem[]>({
    queryKey: searchKeys.trending(type, limit),
    queryFn: () => searchService.trending({ type, limit }),
  });
}

export function useFeatured(limit = 20) {
  return useQuery<ExploreItem[]>({
    queryKey: searchKeys.featured(limit),
    queryFn: () => searchService.featured({ limit }),
  });
}

export function useRecent(params: ExploreParams = {}) {
  return useInfiniteQuery<CursorPaged<ExploreItem>>({
    queryKey: searchKeys.recent(params.type, params.pageSize),
    queryFn: ({ pageParam }) =>
      searchService.recent({ ...params, cursor: pageParam as string | undefined }),
    initialPageParam: undefined as string | undefined,
    getNextPageParam: (last) => last.nextCursor ?? undefined,
  });
}
