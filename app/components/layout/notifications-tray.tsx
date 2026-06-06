"use client";

import { useEffect, useRef, useState } from "react";
import { useTranslations, useLocale } from "next-intl";
import {
  Bell,
  Loader2,
  AlertCircle,
  Check,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Link, useRouter } from "@/lib/navigation";
import { useAuthStore, selectUser } from "@/stores/auth-store";
import {
  useMyInvitations,
  useAcceptInvitation,
  useDeclineInvitation,
  useNotifications,
  useUnreadCount,
  useMarkNotificationRead,
  useMarkAllNotificationsRead,
} from "@/hooks";
import { useNotificationsStore } from "@/stores/notifications-store";
import { ApiClientError } from "@/lib/api-client";
import type { StudyInvitation } from "@/types";

type Feedback =
  | { type: "success"; message: string }
  | { type: "error"; message: string }
  | null;

type Tab = "notifications" | "invitations";

function getErrorMessage(error: unknown, fallback: string): string {
  if (error instanceof ApiClientError) return error.message || fallback;
  if (error instanceof Error) return error.message || fallback;
  return fallback;
}

/**
 * Unified header bell: shows notifications + pending study invitations
 * behind a single popover with two tabs.
 *
 * Badge count = unread notifications + pending invitations.
 */
export function NotificationsTray() {
  const tInv = useTranslations("invitations");
  const tNotif = useTranslations("notifications");
  const locale = useLocale();
  const router = useRouter();
  const user = useAuthStore(selectUser);
  const email = user?.email ?? "";

  const unreadNotifCount = useNotificationsStore((s) => s.unreadCount);

  // Boot: hydrate unread count + start poll.
  useUnreadCount();

  // Invitations (loaded only when needed, but small so always-on is fine).
  const {
    data: invData,
    isLoading: invLoading,
    isError: invError,
  } = useMyInvitations(email, 1, 20);
  const acceptMutation = useAcceptInvitation();
  const declineMutation = useDeclineInvitation();

  // Notifications page (first page).
  const {
    data: notifData,
    isLoading: notifLoading,
    isError: notifError,
  } = useNotifications(1, 10, false);
  const markRead = useMarkNotificationRead();
  const markAllRead = useMarkAllNotificationsRead();

  const [open, setOpen] = useState(false);
  const [tab, setTab] = useState<Tab>("notifications");
  const [feedback, setFeedback] = useState<Feedback>(null);
  const [pendingId, setPendingId] = useState<string | null>(null);
  const [now] = useState(() => Date.now());

  const containerRef = useRef<HTMLDivElement | null>(null);

  // Close on outside click / Escape.
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

  const dateFormatter = new Intl.DateTimeFormat(locale, { dateStyle: "medium" });
  const invitations = invData?.items ?? [];
  const notifications = notifData?.items ?? [];

  const invCount = invitations.length;
  const totalBadge = unreadNotifCount + invCount;

  const handleAccept = (invitation: StudyInvitation) => {
    setPendingId(invitation.id);
    setFeedback(null);
    acceptMutation.mutate(invitation.token, {
      onSuccess: () => {
        setFeedback({
          type: "success",
          message: tInv("feedback.accepted", { study: invitation.studyTitle }),
        });
        setPendingId(null);
        setOpen(false);
        router.push(`/studies/${invitation.studyId}`);
      },
      onError: (error) => {
        setFeedback({
          type: "error",
          message: getErrorMessage(error, tInv("feedback.acceptError")),
        });
        setPendingId(null);
      },
    });
  };

  const handleDecline = (invitation: StudyInvitation) => {
    setPendingId(invitation.id);
    setFeedback(null);
    declineMutation.mutate(invitation.token, {
      onSuccess: () => {
        setFeedback({ type: "success", message: tInv("feedback.declined") });
        setPendingId(null);
      },
      onError: (error) => {
        setFeedback({
          type: "error",
          message: getErrorMessage(error, tInv("feedback.declineError")),
        });
        setPendingId(null);
      },
    });
  };

  return (
    <div ref={containerRef} className="relative">
      <button
        type="button"
        aria-label={tNotif("trayAriaLabel")}
        aria-expanded={open}
        aria-haspopup="dialog"
        onClick={() => setOpen((prev) => !prev)}
        className="relative flex min-h-[40px] min-w-[40px] items-center justify-center rounded-lg p-2.5 text-muted-foreground transition-all duration-200 hover:bg-muted/50 hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 active:scale-95"
      >
        <Bell className="h-5 w-5" />
        {totalBadge > 0 && (
          <span
            aria-label={tNotif("trayBadge", { count: totalBadge })}
            className="absolute right-1 top-1 flex min-h-[16px] min-w-[16px] items-center justify-center rounded-full bg-primary px-1 text-[10px] font-semibold leading-none text-primary-foreground"
          >
            {totalBadge > 9 ? "9+" : totalBadge}
          </span>
        )}
      </button>

      {open && (
        <div
          role="dialog"
          aria-label={tNotif("trayTitle")}
          className="absolute right-0 top-full z-50 mt-2 w-[380px] max-w-[calc(100vw-1.5rem)] origin-top-right rounded-md border bg-popover text-popover-foreground shadow-md"
        >
          {/* Tabs */}
          <div
            role="tablist"
            aria-label={tNotif("trayTitle")}
            className="flex border-b"
          >
            <button
              type="button"
              role="tab"
              aria-selected={tab === "notifications"}
              onClick={() => setTab("notifications")}
              className={`flex-1 px-3 py-2 text-xs font-medium transition ${
                tab === "notifications"
                  ? "border-b-2 border-primary text-foreground"
                  : "text-muted-foreground hover:text-foreground"
              }`}
            >
              {tNotif("trayTitle")}
              {unreadNotifCount > 0 && (
                <Badge variant="secondary" className="ml-1 text-[10px]">
                  {unreadNotifCount}
                </Badge>
              )}
            </button>
            <button
              type="button"
              role="tab"
              aria-selected={tab === "invitations"}
              onClick={() => setTab("invitations")}
              className={`flex-1 px-3 py-2 text-xs font-medium transition ${
                tab === "invitations"
                  ? "border-b-2 border-primary text-foreground"
                  : "text-muted-foreground hover:text-foreground"
              }`}
            >
              {tInv("trayTitle")}
              {invCount > 0 && (
                <Badge variant="secondary" className="ml-1 text-[10px]">
                  {invCount}
                </Badge>
              )}
            </button>
          </div>

          {feedback && (
            <div
              role="status"
              className={`flex items-start gap-2 px-4 py-2 text-xs ${
                feedback.type === "success"
                  ? "bg-emerald-50 text-emerald-900 dark:bg-emerald-950/40 dark:text-emerald-100"
                  : "bg-destructive/5 text-destructive"
              }`}
            >
              {feedback.type === "error" && (
                <AlertCircle className="mt-0.5 h-3.5 w-3.5 shrink-0" aria-hidden />
              )}
              <span className="break-words">{feedback.message}</span>
            </div>
          )}

          {/* ============= NOTIFICATIONS TAB ============= */}
          {tab === "notifications" && (
            <>
              {unreadNotifCount > 0 && (
                <div className="flex items-center justify-end border-b px-3 py-1.5">
                  <Button
                    type="button"
                    size="sm"
                    variant="ghost"
                    className="h-7 text-xs"
                    onClick={() => markAllRead.mutate()}
                    disabled={markAllRead.isPending}
                  >
                    <Check className="mr-1 h-3 w-3" aria-hidden />
                    {tNotif("markAllRead")}
                  </Button>
                </div>
              )}

              <div className="max-h-[380px] overflow-y-auto">
                {notifLoading && (
                  <div className="flex items-center gap-2 px-4 py-6 text-xs text-muted-foreground">
                    <Loader2 className="h-3.5 w-3.5 animate-spin" aria-hidden />
                    {tNotif("loading")}
                  </div>
                )}

                {notifError && !notifLoading && (
                  <div className="flex items-center gap-2 px-4 py-6 text-xs text-destructive">
                    <AlertCircle className="h-3.5 w-3.5" aria-hidden />
                    {tNotif("loadError")}
                  </div>
                )}

                {!notifLoading && !notifError && notifications.length === 0 && (
                  <div className="px-4 py-8 text-center text-xs text-muted-foreground">
                    {tNotif("empty")}
                  </div>
                )}

                {!notifLoading && !notifError && notifications.length > 0 && (
                  <ul className="divide-y">
                    {notifications.map((n) => (
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
                              aria-label={tNotif("markRead")}
                            >
                              {tNotif("markRead")}
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
                  {tNotif("seeAll")}
                </Link>
              </div>
            </>
          )}

          {/* ============= INVITATIONS TAB ============= */}
          {tab === "invitations" && (
            <div className="max-h-[420px] overflow-y-auto">
              {invLoading && (
                <div className="flex items-center gap-2 px-4 py-6 text-xs text-muted-foreground">
                  <Loader2 className="h-3.5 w-3.5 animate-spin" aria-hidden />
                  {tInv("loading")}
                </div>
              )}

              {invError && !invLoading && (
                <div className="flex items-center gap-2 px-4 py-6 text-xs text-destructive">
                  <AlertCircle className="h-3.5 w-3.5" aria-hidden />
                  {tInv("loadError")}
                </div>
              )}

              {!invLoading && !invError && invitations.length === 0 && (
                <div className="px-4 py-8 text-center text-xs text-muted-foreground">
                  {tInv("empty")}
                </div>
              )}

              {!invLoading && !invError && invitations.length > 0 && (
                <ul className="divide-y">
                  {invitations.map((invitation) => {
                    const isRowPending = pendingId === invitation.id;
                    const expiresDate = new Date(invitation.expiresAt);
                    const isExpired = expiresDate.getTime() < now;
                    return (
                      <li key={invitation.id} className="px-4 py-3 text-sm">
                        <div className="min-w-0">
                          <div className="flex flex-wrap items-center gap-1.5">
                            <Link
                              href={`/studies/${invitation.studyId}`}
                              onClick={() => setOpen(false)}
                              className="truncate font-medium text-foreground hover:underline"
                            >
                              {invitation.studyTitle}
                            </Link>
                            <Badge variant="secondary" className="text-[10px]">
                              {tInv("card.role", { role: invitation.roleName })}
                            </Badge>
                            {isExpired && (
                              <Badge variant="destructive" className="text-[10px]">
                                {tInv("card.expired")}
                              </Badge>
                            )}
                          </div>
                          <p className="mt-0.5 truncate text-xs text-muted-foreground">
                            {tInv("card.invitedBy", {
                              name: invitation.invitedByName,
                            })}
                          </p>
                          <p className="text-[11px] text-muted-foreground">
                            {tInv("card.expires", {
                              date: dateFormatter.format(expiresDate),
                            })}
                          </p>
                          {invitation.message && (
                            <p className="mt-1 line-clamp-2 whitespace-pre-wrap text-xs text-foreground/80">
                              {invitation.message}
                            </p>
                          )}
                        </div>
                        <div className="mt-2 flex justify-end gap-2">
                          <Button
                            type="button"
                            size="sm"
                            variant="outline"
                            onClick={() => handleDecline(invitation)}
                            disabled={isRowPending}
                          >
                            {isRowPending && declineMutation.isPending ? (
                              <>
                                <Loader2
                                  className="mr-1.5 h-3 w-3 animate-spin"
                                  aria-hidden
                                />
                                {tInv("actions.declining")}
                              </>
                            ) : (
                              tInv("actions.decline")
                            )}
                          </Button>
                          <Button
                            type="button"
                            size="sm"
                            onClick={() => handleAccept(invitation)}
                            disabled={isRowPending || isExpired}
                          >
                            {isRowPending && acceptMutation.isPending ? (
                              <>
                                <Loader2
                                  className="mr-1.5 h-3 w-3 animate-spin"
                                  aria-hidden
                                />
                                {tInv("actions.accepting")}
                              </>
                            ) : (
                              tInv("actions.accept")
                            )}
                          </Button>
                        </div>
                      </li>
                    );
                  })}
                </ul>
              )}
            </div>
          )}
        </div>
      )}
    </div>
  );
}
