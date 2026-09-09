import { api } from "@/lib/api-client";
import type {
  Notification,
  SetWatchLevelInput,
  UnreadCount,
  WatchLevel,
} from "@/types/notifications";
import type { PagedResponse } from "@/types/common";

/**
 * Notifications + Watch service.
 * (Watch is merged here per Phase 4 plan.)
 */
export const notificationsService = {
  // ============================================================
  // Notifications
  // ============================================================

  list(
    pageNumber = 1,
    pageSize = 20,
    unreadOnly = false,
  ): Promise<PagedResponse<Notification>> {
    const params = new URLSearchParams({
      pageNumber: String(pageNumber),
      pageSize: String(pageSize),
      unreadOnly: String(unreadOnly),
    });
    return api.get<PagedResponse<Notification>>(
      `/api/v1/notifications?${params.toString()}`,
    );
  },

  unreadCount(): Promise<UnreadCount> {
    return api.get<UnreadCount>(`/api/v1/notifications/unread-count`);
  },

  markRead(notificationId: string): Promise<Notification> {
    return api.post<Notification>(
      `/api/v1/notifications/${notificationId}/read`,
    );
  },

  markAllRead(): Promise<{ updatedCount: number }> {
    return api.post<{ updatedCount: number }>(
      `/api/v1/notifications/read-all`,
    );
  },

  // ============================================================
  // Watch
  // ============================================================

  setWatchLevel(studyId: string, level: WatchLevel): Promise<void> {
    const body: SetWatchLevelInput = { level };
    return api.put<void>(`/api/v1/studies/${studyId}/watch`, body);
  },
};
