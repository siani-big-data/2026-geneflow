/**
 * Notifications + Watch — frontend types mirroring backend DTOs.
 */

export type NotificationType =
  | "CommentCreated"
  | "DiscussionCreated"
  | "MentionReceived"
  | "StudyInvitation";

export type WatchLevel = "All" | "Mentions" | "None";

export interface Notification {
  id: string;
  recipientId: string;
  type: NotificationType;
  subject: string;
  url: string | null;
  isRead: boolean;
  readAt: string | null;
  createdAt: string;
}

export interface UnreadCount {
  unreadCount: number;
}

export interface SetWatchLevelInput {
  level: WatchLevel;
}

/** SSE payload pushed via `/api/v1/notifications/stream`. */
export interface NotificationServerEvent {
  notificationId: string;
  recipientId: string;
  type: NotificationType;
  subject: string;
  url: string | null;
  createdAt: string;
}
