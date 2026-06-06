"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { Loader2, MessageSquare, Plus, Lock } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui";
import { useStudyDiscussions, useUserDisplayName } from "@/hooks";
import type { Discussion } from "@/types/discussions";
import { NewDiscussionDialog } from "./new-discussion-dialog";
import { DiscussionDetail } from "./discussion-detail";
import { WatchSelector } from "./watch-selector";

interface DiscussionsTabProps {
  studyId: string;
}

export function DiscussionsTab({ studyId }: DiscussionsTabProps) {
  const t = useTranslations("discussions");
  const [page, setPage] = useState(1);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [selectedId, setSelectedId] = useState<string | null>(null);

  const { data, isLoading, isError, refetch } = useStudyDiscussions(
    studyId,
    page,
    20,
  );

  if (selectedId) {
    return (
      <DiscussionDetail
        discussionId={selectedId}
        onBack={() => setSelectedId(null)}
      />
    );
  }

  if (isLoading) {
    return (
      <Card className="flex items-center justify-center py-12">
        <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
      </Card>
    );
  }
  if (isError) {
    return (
      <Card className="flex flex-col items-center gap-3 py-12">
        <p className="text-sm text-destructive">{t("loadError")}</p>
        <Button variant="outline" size="sm" onClick={() => refetch()}>
          {t("retry")}
        </Button>
      </Card>
    );
  }

  const items = data?.items ?? [];

  return (
    <div className="flex flex-col gap-4">
      <div className="flex items-center justify-between gap-2">
        <div className="flex items-center gap-2">
          <h3 className="text-lg font-semibold">{t("listTitle")}</h3>
          {data?.totalCount !== undefined && (
            <Badge variant="secondary">{data.totalCount}</Badge>
          )}
        </div>
        <div className="flex items-center gap-2">
          <WatchSelector studyId={studyId} />
          <Button type="button" size="sm" onClick={() => setDialogOpen(true)}>
            <Plus className="mr-1 h-4 w-4" />
            {t("new.title")}
          </Button>
        </div>
      </div>

      {items.length === 0 ? (
        <Card className="flex flex-col items-center gap-3 py-12 text-center">
          <MessageSquare className="h-8 w-8 text-muted-foreground" />
          <p className="text-sm text-muted-foreground">{t("empty")}</p>
          <Button size="sm" onClick={() => setDialogOpen(true)}>
            {t("new.title")}
          </Button>
        </Card>
      ) : (
        <ul className="flex flex-col gap-2">
          {items.map((d) => (
            <DiscussionListRow
              key={d.id}
              discussion={d}
              onOpen={() => setSelectedId(d.id)}
            />
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

      <NewDiscussionDialog
        studyId={studyId}
        open={dialogOpen}
        onOpenChange={setDialogOpen}
        onCreated={(id) => setSelectedId(id)}
      />
    </div>
  );
}

interface DiscussionListRowProps {
  discussion: Discussion;
  onOpen: () => void;
}

function DiscussionListRow({ discussion: d, onOpen }: DiscussionListRowProps) {
  const t = useTranslations("discussions");
  const authorName = useUserDisplayName(d.authorId);

  return (
    <li>
      <button
        type="button"
        onClick={onOpen}
        className="w-full rounded-md border bg-card p-3 text-left text-sm shadow-sm transition hover:border-primary/50 hover:bg-accent"
      >
        <div className="flex flex-wrap items-center gap-2">
          <span className="font-medium">{d.title}</span>
          {d.category && (
            <Badge variant="secondary" className="text-[10px]">
              {d.category}
            </Badge>
          )}
          {d.isLocked && (
            <Badge variant="outline" className="gap-1 text-[10px]">
              <Lock className="h-3 w-3" />
              {t("locked")}
            </Badge>
          )}
        </div>
        <p className="mt-0.5 text-xs text-muted-foreground">
          {t("createdBy", {
            name: authorName,
            date: new Date(d.createdAt).toLocaleString(),
          })}
        </p>
      </button>
    </li>
  );
}
