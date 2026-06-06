"use client";

import { UserMinus, UserPlus } from "lucide-react";
import { useTranslations } from "next-intl";
import { Button } from "@/components/ui/button";
import {
  useFollowUser,
  useIsFollowing,
  useUnfollowUser,
} from "@/hooks/use-follows";

interface FollowButtonProps {
  userId: string;
  /** Hide entirely when this is the current user's own id. */
  currentUserId?: string;
  size?: "sm" | "default" | "lg";
}

/**
 * Follow/Unfollow toggle. Hidden when `userId === currentUserId`.
 */
export function FollowButton({
  userId,
  currentUserId,
  size = "default",
}: FollowButtonProps) {
  const t = useTranslations("follows");
  const isSelf = currentUserId === userId;
  const followingQ = useIsFollowing(isSelf ? undefined : userId);
  const followM = useFollowUser();
  const unfollowM = useUnfollowUser();

  if (isSelf) return null;

  const isFollowing = followingQ.data?.isFollowing ?? false;
  const pending = followM.isPending || unfollowM.isPending;

  const onClick = () => {
    if (pending) return;
    if (isFollowing) unfollowM.mutate(userId);
    else followM.mutate(userId);
  };

  return (
    <Button
      type="button"
      variant={isFollowing ? "outline" : "default"}
      size={size}
      onClick={onClick}
      disabled={pending || followingQ.isLoading}
      aria-pressed={isFollowing}
    >
      {isFollowing ? (
        <>
          <UserMinus className="h-4 w-4" />
          <span>{t("unfollow")}</span>
        </>
      ) : (
        <>
          <UserPlus className="h-4 w-4" />
          <span>{t("follow")}</span>
        </>
      )}
    </Button>
  );
}
