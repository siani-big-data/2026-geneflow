"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { Loader2, Check, AlertCircle } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui";
import { Link } from "@/lib/navigation";
import {
  useNotifications,
  useMarkNotificationRead,
  useMarkAllNotificationsRead,
} from "@/hooks";

export default function NotificationsPage() {
  const t = useTranslations("notifications");
  const [page, setPage] = useState(1);
  const [unreadOnly, setUnreadOnly] = useState(false);

  const { data, isLoading, isError, refetch } = useNotifications(
    page,
    20,
    unreadOnly,
  );
  const markRead = useMarkNotificationRead();
  const markAllRead = useMarkAllNotificationsRead();

  const items = data?.items ?? [];

  return (
    <div className="mx-auto flex max-w-3xl flex-col gap-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-semibold">{t("pageTitle")}</h1>
        <div className="flex items-center gap-2">
          <Button
            type="button"
            size="sm"
            variant={unreadOnly ? "default" : "outline"}
            onClick={() => {
              setUnreadOnly((p) => !p);
              setPage(1);
            }}
          >
            {t("filter.unreadOnly")}
          </Button>
          <Button
            type="button"
            size="sm"
            variant="outline"
            onClick={() => markAllRead.mutate()}
            disabled={markAllRead.isPending}
          >
            <Check className="mr-1 h-3.5 w-3.5" />
            {t("markAllRead")}
          </Button>
        </div>
      </div>

      {isLoading && (
        <Card className="flex items-center justify-center py-12">
          <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
        </Card>
      )}

      {isError && !isLoading && (
        <Card className="flex flex-col items-center gap-3 py-12">
          <AlertCircle className="h-6 w-6 text-destructive" />
          <p className="text-sm text-destructive">{t("loadError")}</p>
          <Button variant="outline" size="sm" onClick={() => refetch()}>
            {t("retry")}
          </Button>
        </Card>
      )}

      {!isLoading && !isError && items.length === 0 && (
        <Card className="flex items-center justify-center py-12 text-sm text-muted-foreground">
          {t("empty")}
        </Card>
      )}

      {!isLoading && !isError && items.length > 0 && (
        <ul className="flex flex-col gap-2">
          {items.map((n) => (
            <li
              key={n.id}
              className={`rounded-md border bg-card p-3 text-sm shadow-sm ${
                n.isRead ? "" : "border-primary/40 bg-primary/5"
              }`}
            >
              <div className="flex items-start justify-between gap-3">
                <div className="min-w-0 flex-1">
                  {n.url ? (
                    <Link
                      href={n.url as never}
                      onClick={() => {
                        if (!n.isRead) markRead.mutate(n.id);
                      }}
                      className="font-medium hover:underline"
                    >
                      {n.subject}
                    </Link>
                  ) : (
                    <span className="font-medium">{n.subject}</span>
                  )}
                  <p className="mt-0.5 text-xs text-muted-foreground">
                    {new Date(n.createdAt).toLocaleString()}
                  </p>
                </div>
                {!n.isRead && (
                  <Button
                    type="button"
                    size="sm"
                    variant="ghost"
                    onClick={() => markRead.mutate(n.id)}
                  >
                    {t("markRead")}
                  </Button>
                )}
              </div>
            </li>
          ))}
        </ul>
      )}

      {data && data.totalPages > 1 && (
        <div className="flex items-center justify-center gap-2 py-2">
          <Button
            type="button"
            size="sm"
            variant="outline"
            disabled={!data.hasPreviousPage}
            onClick={() => setPage((p) => Math.max(1, p - 1))}
          >
            {t("prev")}
          </Button>
          <span className="text-xs text-muted-foreground">
            {t("pageOf", { page: data.pageNumber, total: data.totalPages })}
          </span>
          <Button
            type="button"
            size="sm"
            variant="outline"
            disabled={!data.hasNextPage}
            onClick={() => setPage((p) => p + 1)}
          >
            {t("next")}
          </Button>
        </div>
      )}
    </div>
  );
}
