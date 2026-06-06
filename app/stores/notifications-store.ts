"use client";

import { create } from "zustand";

/**
 * Lightweight client-side store for the notifications bell badge.
 * The source of truth is the server (`GET /api/v1/notifications/unread-count`);
 * this store mirrors the count so SSE can increment it live without a refetch.
 */
interface NotificationsStore {
  unreadCount: number;
  setUnreadCount: (count: number) => void;
  incrementUnread: (by?: number) => void;
  decrementUnread: (by?: number) => void;
  markAllRead: () => void;
}

export const useNotificationsStore = create<NotificationsStore>((set) => ({
  unreadCount: 0,
  setUnreadCount: (count) => set({ unreadCount: Math.max(0, count) }),
  incrementUnread: (by = 1) =>
    set((state) => ({ unreadCount: state.unreadCount + by })),
  decrementUnread: (by = 1) =>
    set((state) => ({ unreadCount: Math.max(0, state.unreadCount - by) })),
  markAllRead: () => set({ unreadCount: 0 }),
}));
