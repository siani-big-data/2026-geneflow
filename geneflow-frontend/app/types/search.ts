/**
 * Search / Explore / Feed — frontend types mirroring backend DTOs.
 *
 * Object type values mirror SearchObjectType.Name on the backend
 * (Study, Trace, Discussion, User).
 */

export type SearchObjectType = "Study" | "Trace" | "Discussion" | "User";

export interface SearchHit {
  objectType: SearchObjectType;
  objectId: string;
  ownerId: string | null;
  title: string;
  snippet: string | null;
  tags: string | null;
  isPublic: boolean;
  updatedAt: string;
  rank: number;
}

export interface ExploreItem {
  objectType: SearchObjectType;
  objectId: string;
  ownerId: string | null;
  title: string;
  body: string | null;
  tags: string | null;
  updatedAt: string;
  score: number;
}

/** Reason describing why an item ended up in the personal feed. */
export type FeedReason = "self" | "follow" | "watch" | "other";

export interface FeedItem {
  objectType: SearchObjectType;
  objectId: string;
  ownerId: string | null;
  title: string;
  body: string | null;
  tags: string | null;
  updatedAt: string;
  reason: FeedReason;
}

/** Wire envelope returned by cursor-paginated endpoints. */
export interface CursorPaged<T> {
  items: T[];
  nextCursor: string | null;
  hasMore: boolean;
}

/** Filters accepted by GET /api/v1/search. */
export interface GlobalSearchParams {
  q: string;
  type?: SearchObjectType;
  owner?: string;
  tag?: string;
  cursor?: string;
  pageSize?: number;
}

export interface ExploreParams {
  type?: SearchObjectType;
  cursor?: string;
  pageSize?: number;
}
