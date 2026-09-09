/**
 * Social features types (Follow / Pin).
 * Star-related types live in `./study.ts` (StudyStats, StudyMetrics).
 */

export interface FollowUser {
  userId: string;
  username: string;
  email: string;
  createdAt: string;
}

export interface PagedFollowUsers {
  items: FollowUser[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface IsFollowingResponse {
  isFollowing: boolean;
}

export interface PinnedStudy {
  studyId: string;
  order: number;
  pinnedAt: string;
}

export interface UpdatePinnedStudiesRequest {
  studyIds: string[];
}

export const MAX_PINNED_STUDIES = 6;
