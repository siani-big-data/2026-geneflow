import { api } from "@/lib/api-client";
import type {
  FollowUser,
  IsFollowingResponse,
  PagedFollowUsers,
  PinnedStudy,
  UpdatePinnedStudiesRequest,
} from "@/types/social";

/**
 * Social features service: Follow graph + Pinned studies.
 * Star endpoints live in `studies.service.ts`.
 */
export const socialService = {
  // ============================================================
  // Follow graph
  // ============================================================

  isFollowing(userId: string): Promise<IsFollowingResponse> {
    return api.get<IsFollowingResponse>(
      `/api/v1/users/${userId}/follow`,
    );
  },

  follow(userId: string): Promise<void> {
    return api.post<void>(`/api/v1/users/${userId}/follow`);
  },

  unfollow(userId: string): Promise<void> {
    return api.delete<void>(`/api/v1/users/${userId}/follow`);
  },

  getFollowers(
    userId: string,
    pageNumber = 1,
    pageSize = 20,
  ): Promise<PagedFollowUsers> {
    return api.get<PagedFollowUsers>(
      `/api/v1/users/${userId}/followers?pageNumber=${pageNumber}&pageSize=${pageSize}`,
    );
  },

  getFollowing(
    userId: string,
    pageNumber = 1,
    pageSize = 20,
  ): Promise<PagedFollowUsers> {
    return api.get<PagedFollowUsers>(
      `/api/v1/users/${userId}/following?pageNumber=${pageNumber}&pageSize=${pageSize}`,
    );
  },

  // ============================================================
  // Pinned studies
  // ============================================================

  getMyPinned(): Promise<PinnedStudy[]> {
    return api.get<PinnedStudy[]>(`/api/v1/profiles/me/pinned`);
  },

  getUserPinned(userId: string): Promise<PinnedStudy[]> {
    return api.get<PinnedStudy[]>(`/api/v1/profiles/${userId}/pinned`);
  },

  updateMyPinned(studyIds: string[]): Promise<void> {
    const body: UpdatePinnedStudiesRequest = { studyIds };
    return api.put<void>(`/api/v1/profiles/me/pinned`, body);
  },
};
