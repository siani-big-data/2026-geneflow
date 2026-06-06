"use client";

import Link from "next/link";
import { Users } from "lucide-react";
import { useTranslations } from "next-intl";
import { useFollowers, useFollowing } from "@/hooks/use-follows";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";

interface FollowersDialogProps {
  userId: string;
  mode: "followers" | "following";
  triggerLabel: string;
  count?: number;
}

/**
 * Reusable dialog that lists either followers or following for a user.
 */
export function FollowersDialog({
  userId,
  mode,
  triggerLabel,
  count,
}: FollowersDialogProps) {
  const t = useTranslations("follows");
  const followersQ = useFollowers(mode === "followers" ? userId : undefined, 1, 50);
  const followingQ = useFollowing(mode === "following" ? userId : undefined, 1, 50);
  const data = mode === "followers" ? followersQ.data : followingQ.data;
  const isLoading = mode === "followers" ? followersQ.isLoading : followingQ.isLoading;

  return (
    <Dialog>
      <DialogTrigger asChild>
        <Button variant="ghost" size="sm" className="gap-2">
          <Users className="h-4 w-4" />
          <span>
            {triggerLabel}
            {typeof count === "number" && (
              <span className="ml-1 tabular-nums">({count})</span>
            )}
          </span>
        </Button>
      </DialogTrigger>
      <DialogContent className="max-h-[80vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>{t(mode)}</DialogTitle>
        </DialogHeader>
        <div className="space-y-2">
          {isLoading && (
            <div className="space-y-2">
              {Array.from({ length: 3 }).map((_, i) => (
                <div
                  key={i}
                  className="h-10 animate-pulse rounded bg-muted/40"
                />
              ))}
            </div>
          )}
          {!isLoading && data?.items.length === 0 && (
            <p className="py-6 text-center text-sm text-muted-foreground">
              {t("empty")}
            </p>
          )}
          {data?.items.map((u) => (
            <Link
              key={u.userId}
              href={`/users/${u.userId}`}
              className="flex items-center justify-between rounded p-2 hover:bg-accent"
            >
              <div className="min-w-0">
                <div className="truncate text-sm font-medium">{u.username}</div>
                <div className="truncate text-xs text-muted-foreground">
                  {u.email}
                </div>
              </div>
            </Link>
          ))}
        </div>
      </DialogContent>
    </Dialog>
  );
}
