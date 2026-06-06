import { api } from "@/lib/api-client";
import type {
  CursorPaged,
  ExploreItem,
  ExploreParams,
  FeedItem,
  GlobalSearchParams,
  SearchHit,
} from "@/types/search";

/** Builds a URLSearchParams string omitting null/undefined/empty values. */
function qs(params: object): string {
  const sp = new URLSearchParams();
  for (const [k, v] of Object.entries(params as Record<string, unknown>)) {
    if (v === undefined || v === null || v === "") continue;
    sp.append(k, String(v));
  }
  const out = sp.toString();
  return out ? `?${out}` : "";
}

/**
 * Discovery service: full-text search, explore (trending/recent/featured)
 * and the personal feed.
 */
export const searchService = {
  search(params: GlobalSearchParams): Promise<CursorPaged<SearchHit>> {
    return api.get<CursorPaged<SearchHit>>(`/api/v1/search${qs(params)}`);
  },

  recent(params: ExploreParams = {}): Promise<CursorPaged<ExploreItem>> {
    return api.get<CursorPaged<ExploreItem>>(`/api/v1/explore/recent${qs(params)}`);
  },

  trending(
    params: { type?: ExploreParams["type"]; limit?: number } = {},
  ): Promise<ExploreItem[]> {
    return api.get<ExploreItem[]>(`/api/v1/explore/trending${qs(params)}`);
  },

  featured(params: { limit?: number } = {}): Promise<ExploreItem[]> {
    return api.get<ExploreItem[]>(`/api/v1/explore/featured${qs(params)}`);
  },

  feed(params: { cursor?: string; pageSize?: number } = {}): Promise<
    CursorPaged<FeedItem>
  > {
    return api.get<CursorPaged<FeedItem>>(`/api/v1/feed${qs(params)}`);
  },
};
