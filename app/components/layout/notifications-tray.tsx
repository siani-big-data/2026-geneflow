"use client";

import { useEffect, useRef, useState } from "react";
import { useTranslations, useLocale } from "next-intl";
import { Bell, Loader2, AlertCircle } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Link, useRouter } from "@/lib/navigation";
import { useAuthStore, selectUser } from "@/stores/auth-store";
import {
  useMyInvitations,
  useAcceptInvitation,
  useDeclineInvitation,
} from "@/hooks";
import { ApiClientError } from "@/lib/api-client";
import type { StudyInvitation } from "@/types";

type Feedback =
  | { type: "success"; message: string }
  | { type: "error"; message: string }
  | null;

function getErrorMessage(error: unknown, fallback: string): string {
  if (error instanceof ApiClientError) {
    return error.message || fallback;
  }
  if (error instanceof Error) {
    return error.message || fallback;
  }
  return fallback;
}

export function NotificationsTray() {
  const t = useTranslations("invitations");
  const locale = useLocale();
  const router = useRouter();
  const user = useAuthStore(selectUser);
  const email = user?.email ?? "";

  const { data, isLoading, isError } = useMyInvitations(email, 1, 20);
  const acceptMutation = useAcceptInvitation();
  const declineMutation = useDeclineInvitation();

  const [open, setOpen] = useState(false);
  const [feedback, setFeedback] = useState<Feedback>(null);
  const [pendingId, setPendingId] = useState<string | null>(null);
  // Capture a stable "now" reference once on mount so render stays pure.
  const [now] = useState(() => Date.now());

  const containerRef = useRef<HTMLDivElement | null>(null);

  // Close the popover on outside click or Escape.
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

  const dateFormatter = new Intl.DateTimeFormat(locale, {
    dateStyle: "medium",
  });

  const items = data?.items ?? [];
  const count = items.length;

  const handleAccept = (invitation: StudyInvitation) => {
    setPendingId(invitation.id);
    setFeedback(null);
    acceptMutation.mutate(invitation.token, {
      onSuccess: () => {
        setFeedback({
          type: "success",
          message: t("feedback.accepted", { study: invitation.studyTitle }),
        });
        setPendingId(null);
        setOpen(false);
        router.push(`/studies/${invitation.studyId}`);
      },
      onError: (error) => {
        console.error("[NotificationsTray] accept failed", error);
        setFeedback({
          type: "error",
          message: getErrorMessage(error, t("feedback.acceptError")),
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
        setFeedback({ type: "success", message: t("feedback.declined") });
        setPendingId(null);
      },
      onError: (error) => {
        console.error("[NotificationsTray] decline failed", error);
        setFeedback({
          type: "error",
          message: getErrorMessage(error, t("feedback.declineError")),
        });
        setPendingId(null);
      },
    });
  };

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
        <Bell className="h-5 w-5" />
        {count > 0 && (
          <span
            aria-label={t("trayBadge", { count })}
            className="absolute right-1 top-1 flex min-h-[16px] min-w-[16px] items-center justify-center rounded-full bg-primary px-1 text-[10px] font-semibold leading-none text-primary-foreground"
          >
            {count > 9 ? "9+" : count}
          </span>
        )}
      </button>

      {open && (
        <div
          role="dialog"
          aria-label={t("trayTitle")}
          className="absolute right-0 top-full z-50 mt-2 w-[360px] max-w-[calc(100vw-1.5rem)] origin-top-right rounded-md border bg-popover text-popover-foreground shadow-md"
        >
          <div className="border-b px-4 py-3 text-sm font-semibold">
            {t("trayTitle")}
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
                <AlertCircle
                  className="mt-0.5 h-3.5 w-3.5 shrink-0"
                  aria-hidden
                />
              )}
              <span className="break-words">{feedback.message}</span>
            </div>
          )}

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
                {items.map((invitation) => {
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
                            {t("card.role", { role: invitation.roleName })}
                          </Badge>
                          {isExpired && (
                            <Badge
                              variant="destructive"
                              className="text-[10px]"
                            >
                              {t("card.expired")}
                            </Badge>
                          )}
                        </div>
                        <p className="mt-0.5 truncate text-xs text-muted-foreground">
                          {t("card.invitedBy", {
                            name: invitation.invitedByName,
                          })}
                        </p>
                        <p className="text-[11px] text-muted-foreground">
                          {t("card.expires", {
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
                              {t("actions.declining")}
                            </>
                          ) : (
                            t("actions.decline")
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
                              {t("actions.accepting")}
                            </>
                          ) : (
                            t("actions.accept")
                          )}
                        </Button>
                      </div>
                    </li>
                  );
                })}
              </ul>
            )}
          </div>
        </div>
      )}
    </div>
  );
}
