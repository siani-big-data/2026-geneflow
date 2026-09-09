"use client";

import { useEffect } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { notificationsService } from "@/services/notifications.service";
import { useNotificationsStore } from "@/stores/notifications-store";
import type {
  Notification,
  UnreadCount,
} from "@/types/notifications";
import type { PagedResponse } from "@/types/common";

// ============= QUERY KEYS =============
export const notificationKeys = {
  all: ["notifications"] as const,
  lists: () => [...notificationKeys.all, "list"] as const,
  list: (page: number, pageSize: number, unreadOnly: boolean) =>
    [...notificationKeys.lists(), page, pageSize, unreadOnly] as const,
  unreadCount: () => [...notificationKeys.all, "unread-count"] as const,
};

// ============= QUERIES =============
export function useNotifications(
  page = 1,
  pageSize = 20,
  unreadOnly = false,
) {
  return useQuery<PagedResponse<Notification>>({
    queryKey: notificationKeys.list(page, pageSize, unreadOnly),
    queryFn: () => notificationsService.list(page, pageSize, unreadOnly),
  });
}

/**
 * Polls unread count and mirrors it into the Zustand store so the bell badge
 * stays consistent across the app. SSE may bump the store directly between polls.
 */
export function useUnreadCount(pollMs = 60_000) {
  const setUnreadCount = useNotificationsStore((s) => s.setUnreadCount);

  const query = useQuery<UnreadCount>({
    queryKey: notificationKeys.unreadCount(),
    queryFn: () => notificationsService.unreadCount(),
    refetchInterval: pollMs,
  });

  useEffect(() => {
    if (query.data) setUnreadCount(query.data.unreadCount);
  }, [query.data, setUnreadCount]);

  return query;
}

// ============= MUTATIONS =============
export function useMarkNotificationRead() {
  const qc = useQueryClient();
  const decrement = useNotificationsStore((s) => s.decrementUnread);

  return useMutation({
    mutationFn: (notificationId: string) =>
      notificationsService.markRead(notificationId),
    onSuccess: () => {
      decrement(1);
      qc.invalidateQueries({ queryKey: notificationKeys.lists() });
      qc.invalidateQueries({ queryKey: notificationKeys.unreadCount() });
    },
  });
}

export function useMarkAllNotificationsRead() {
  const qc = useQueryClient();
  const markAllRead = useNotificationsStore((s) => s.markAllRead);

  return useMutation({
    mutationFn: () => notificationsService.markAllRead(),
    onSuccess: () => {
      markAllRead();
      qc.invalidateQueries({ queryKey: notificationKeys.lists() });
      qc.invalidateQueries({ queryKey: notificationKeys.unreadCount() });
    },
  });
}
