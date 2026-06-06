"use client";

import { useEffect, useRef, useState } from "react";
import { useTranslations } from "next-intl";
import { BellRing, Loader2, AlertCircle, Check } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Link } from "@/lib/navigation";
import {
  useNotifications,
  useUnreadCount,
  useMarkNotificationRead,
  useMarkAllNotificationsRead,
} from "@/hooks";
import { useNotificationsStore } from "@/stores/notifications-store";

/**
 * App-wide notifications bell.
 * Lives next to the existing invitations tray; both can coexist in the header.
 */
export function NotificationsBell() {
  const t = useTranslations("notifications");
  const unreadCount = useNotificationsStore((s) => s.unreadCount);

  // Boot: hydrate unread count + start poll.
  useUnreadCount();

  const [open, setOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement | null>(null);

  // Load the first page of notifications only when popover opens.
  const { data, isLoading, isError } = useNotifications(1, 10, false);
  const markRead = useMarkNotificationRead();
  const markAllRead = useMarkAllNotificationsRead();

  useEffect(() => {
    if (!open) return;

    const handlePointerDown = (event: MouseEvent) => {
      const target = event.target as Node | null;
      if (!target) return;
      if (containerRef.current && !containerRef.current.contains(target)) {
        setOpen(false);
      }
    };
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") setOpen(false);
    };

    document.addEventListener("mousedown", handlePointerDown);
    document.addEventListener("keydown", handleKeyDown);
    return () => {
      document.removeEventListener("mousedown", handlePointerDown);
      document.removeEventListener("keydown", handleKeyDown);
    };
  }, [open]);

  const items = data?.items ?? [];

  return (
    <div ref={containerRef} className="relative">
      <button
        type="button"
        aria-label={t("trayAriaLabel")}
        aria-expanded={open}
        aria-haspopup="dialog"
        onClick={() => setOpen((prev) => !prev)}
        className="relative flex min-h-[40px] min-w-[40px] items-center justify-center rounded-lg p-2.5 text-muted-foreground transition-all duration-200 hover:bg-muted/50 hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 active:scale-95"
      >
        <BellRing className="h-5 w-5" />
        {unreadCount > 0 && (
          <span
            aria-label={t("trayBadge", { count: unreadCount })}
            className="absolute right-1 top-1 flex min-h-[16px] min-w-[16px] items-center justify-center rounded-full bg-primary px-1 text-[10px] font-semibold leading-none text-primary-foreground"
          >
            {unreadCount > 9 ? "9+" : unreadCount}
          </span>
        )}
      </button>

      {open && (
        <div
          role="dialog"
          aria-label={t("trayTitle")}
          className="absolute right-0 top-full z-50 mt-2 w-[360px] max-w-[calc(100vw-1.5rem)] origin-top-right rounded-md border bg-popover text-popover-foreground shadow-md"
        >
          <div className="flex items-center justify-between border-b px-4 py-3 text-sm font-semibold">
            <span>{t("trayTitle")}</span>
            {unreadCount > 0 && (
              <Button
                type="button"
                size="sm"
                variant="ghost"
                onClick={() => markAllRead.mutate()}
                disabled={markAllRead.isPending}
              >
                <Check className="mr-1 h-3 w-3" aria-hidden />
                {t("markAllRead")}
              </Button>
            )}
          </div>

          <div className="max-h-[420px] overflow-y-auto">
            {isLoading && (
              <div className="flex items-center gap-2 px-4 py-6 text-xs text-muted-foreground">
                <Loader2 className="h-3.5 w-3.5 animate-spin" aria-hidden />
                {t("loading")}
              </div>
            )}

            {isError && !isLoading && (
              <div className="flex items-center gap-2 px-4 py-6 text-xs text-destructive">
                <AlertCircle className="h-3.5 w-3.5" aria-hidden />
                {t("loadError")}
              </div>
            )}

            {!isLoading && !isError && items.length === 0 && (
              <div className="px-4 py-8 text-center text-xs text-muted-foreground">
                {t("empty")}
              </div>
            )}

            {!isLoading && !isError && items.length > 0 && (
              <ul className="divide-y">
                {items.map((n) => (
                  <li
                    key={n.id}
                    className={`px-4 py-3 text-sm ${
                      n.isRead ? "" : "bg-primary/5"
                    }`}
                  >
                    <div className="flex items-start gap-2">
                      <div className="min-w-0 flex-1">
                        {n.url ? (
                          <Link
                            href={n.url as never}
                            onClick={() => {
                              if (!n.isRead) markRead.mutate(n.id);
                              setOpen(false);
                            }}
                            className="font-medium hover:underline"
                          >
                            {n.subject}
                          </Link>
                        ) : (
                          <span className="font-medium">{n.subject}</span>
                        )}
                        <p className="mt-0.5 text-[11px] text-muted-foreground">
                          {new Date(n.createdAt).toLocaleString()}
                        </p>
                      </div>
                      {!n.isRead && (
                        <button
                          type="button"
                          onClick={() => markRead.mutate(n.id)}
                          className="text-[11px] text-primary hover:underline"
                          aria-label={t("markRead")}
                        >
                          {t("markRead")}
                        </button>
                      )}
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </div>

          <div className="border-t px-4 py-2 text-right">
            <Link
              href={"/notifications" as never}
              onClick={() => setOpen(false)}
              className="text-xs text-primary hover:underline"
            >
              {t("seeAll")}
            </Link>
          </div>
        </div>
      )}
    </div>
  );
}
