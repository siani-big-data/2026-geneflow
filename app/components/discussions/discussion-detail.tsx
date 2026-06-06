"use client";

import { useTranslations } from "next-intl";
import { ArrowLeft, Lock, Unlock, Loader2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui";
import { useDiscussion, useLockDiscussion } from "@/hooks";
import { useAuthStore, selectUser } from "@/stores/auth-store";
import { CommentThread } from "./comment-thread";

interface DiscussionDetailProps {
  discussionId: string;
  onBack: () => void;
}

export function DiscussionDetail({
  discussionId,
  onBack,
}: DiscussionDetailProps) {
  const t = useTranslations("discussions");
  const user = useAuthStore(selectUser);
  const { data, isLoading, isError } = useDiscussion(discussionId);
  const lockMutation = useLockDiscussion();

  if (isLoading) {
    return (
      <Card className="flex items-center justify-center py-12">
        <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
      </Card>
    );
  }
  if (isError || !data) {
    return (
      <Card className="flex flex-col items-center gap-3 py-12">
        <p className="text-sm text-destructive">{t("loadError")}</p>
        <Button variant="outline" size="sm" onClick={onBack}>
          {t("back")}
        </Button>
      </Card>
    );
  }

  const isAuthor = !!user && data.authorId === user.id;
  const comments = data.comments ?? [];

  return (
    <div className="flex flex-col gap-4">
      <div className="flex items-center justify-between">
        <Button
          type="button"
          size="sm"
          variant="ghost"
          onClick={onBack}
          className="-ml-2"
        >
          <ArrowLeft className="mr-1 h-4 w-4" />
          {t("back")}
        </Button>
        {isAuthor && (
          <Button
            type="button"
            size="sm"
            variant="outline"
            onClick={() =>
              lockMutation.mutate({
                discussionId,
                lock: !data.isLocked,
              })
            }
            disabled={lockMutation.isPending}
          >
            {data.isLocked ? (
              <>
                <Unlock className="mr-1 h-3.5 w-3.5" />
                {t("actions.unlock")}
              </>
            ) : (
              <>
                <Lock className="mr-1 h-3.5 w-3.5" />
                {t("actions.lock")}
              </>
            )}
          </Button>
        )}
      </div>

      <header className="border-b pb-3">
        <div className="flex flex-wrap items-center gap-2">
          <h2 className="text-xl font-semibold">{data.title}</h2>
          {data.category && (
            <Badge variant="secondary">{data.category}</Badge>
          )}
          {data.isLocked && (
            <Badge variant="outline" className="gap-1">
              <Lock className="h-3 w-3" />
              {t("locked")}
            </Badge>
          )}
        </div>
        <p className="mt-1 text-xs text-muted-foreground">
          {t("createdBy", {
            name: data.authorId,
            date: new Date(data.createdAt).toLocaleString(),
          })}
        </p>
      </header>

      <CommentThread
        discussionId={discussionId}
        comments={comments}
        locked={data.isLocked}
      />
    </div>
  );
}
