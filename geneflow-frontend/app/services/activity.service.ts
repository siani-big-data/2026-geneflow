/**
 * Activity Service — calls into the GeneFlow Activity bounded context.
 */

import { api } from "@/lib/api-client";
import type { ActivityFeedResponse } from "@/types";

export const activityService = {
  /**
   * Fetches the current user's activity feed (events authored by the user,
   * public events, and study-scoped events for studies the user can see).
   *
   * @param limit  Page size (server clamps to 1..100).
   * @param cursor Opaque cursor from a previous page, or `undefined` for the first page.
   */
  async getMyFeed(
    limit: number = 20,
    cursor?: string,
  ): Promise<ActivityFeedResponse> {
    const params = new URLSearchParams({ limit: limit.toString() });
    if (cursor) params.append("cursor", cursor);
    return api.get<ActivityFeedResponse>(`/api/v1/activity/me?${params}`);
  },

  /**
   * Fetches the activity timeline of a single study. The backend filters by
   * study id and excludes `Private` events, so the response includes both
   * `StudyMembers` and `Public` events for the study. Authorization (member
   * or public study) is enforced server-side; non-members of a private study
   * receive a 403.
   *
   * @param studyId Prefixed study identifier (e.g. `"S00000123"`).
   * @param limit   Page size (server clamps to 1..100).
   * @param cursor  Opaque cursor from a previous page, or `undefined` for the first page.
   */
  async getStudyTimeline(
    studyId: string,
    limit: number = 20,
    cursor?: string,
  ): Promise<ActivityFeedResponse> {
    const params = new URLSearchParams({ limit: limit.toString() });
    if (cursor) params.append("cursor", cursor);
    return api.get<ActivityFeedResponse>(
      `/api/v1/activity/studies/${encodeURIComponent(studyId)}?${params}`,
    );
  },
};
