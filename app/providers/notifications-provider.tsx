"use client";

import * as React from "react";
import { useQueryClient } from "@tanstack/react-query";
import { createSseSubscription } from "@/lib/sse-client";
import { useAuthStore, selectIsAuthenticated } from "@/stores/auth-store";
import { useNotificationsStore } from "@/stores/notifications-store";
import { notificationKeys } from "@/hooks/use-notifications";
import type { NotificationServerEvent } from "@/types/notifications";

/**
 * Subscribes to the per-user SSE notifications channel while the user is
 * authenticated. Each `notification.created` frame bumps the local unread
 * counter and invalidates the notifications list cache so any open consumer
 * (bell popover, /notifications page) refetches in the background.
 *
 * Intentionally renderless — it just wraps `children`.
 */
export function NotificationsProvider({
  children,
}: {
  children: React.ReactNode;
}) {
  const isAuthenticated = useAuthStore(selectIsAuthenticated);
  const queryClient = useQueryClient();
  const incrementUnread = useNotificationsStore((s) => s.incrementUnread);

  React.useEffect(() => {
    if (!isAuthenticated) return;

    const dispose = createSseSubscription({
      url: "/api/v1/notifications/stream",
      enabled: true,
      onFrame: (event, payload) => {
        if (event !== "notification.created") return;
        const _evt = payload as NotificationServerEvent;
        void _evt; // payload reserved for future use
        incrementUnread(1);
        queryClient.invalidateQueries({ queryKey: notificationKeys.lists() });
        queryClient.invalidateQueries({
          queryKey: notificationKeys.unreadCount(),
        });
      },
    });

    return dispose;
  }, [isAuthenticated, incrementUnread, queryClient]);

  return <>{children}</>;
}
