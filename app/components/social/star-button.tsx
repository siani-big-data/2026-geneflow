"use client";

import { Star } from "lucide-react";
import { useTranslations } from "next-intl";
import { Button } from "@/components/ui/button";
import {
  useIsStudyStarred,
  useStarStudy,
  useStudyStats,
  useUnstarStudy,
} from "@/hooks/use-studies";
import { cn } from "@/lib/utils";

interface StarButtonProps {
  studyId: string;
  showCount?: boolean;
  size?: "sm" | "default" | "lg";
  variant?: "default" | "outline" | "ghost";
}

/**
 * Star/Unstar toggle button for a study. Optimistically reflects starred state.
 */
export function StarButton({
  studyId,
  showCount = true,
  size = "default",
  variant = "outline",
}: StarButtonProps) {
  const t = useTranslations("studies.stars");
  const starredQ = useIsStudyStarred(studyId);
  const statsQ = useStudyStats(studyId);
  const starM = useStarStudy();
  const unstarM = useUnstarStudy();

  const isStarred = starredQ.data?.isStarred ?? false;
  const count = statsQ.data?.starsCount ?? 0;
  const pending = starM.isPending || unstarM.isPending;

  const onClick = () => {
    if (pending) return;
    if (isStarred) unstarM.mutate(studyId);
    else starM.mutate(studyId);
  };

  return (
    <Button
      type="button"
      variant={variant}
      size={size}
      onClick={onClick}
      disabled={pending || starredQ.isLoading}
      aria-pressed={isStarred}
      aria-label={isStarred ? t("unstar") : t("star")}
    >
      <Star
        className={cn(
          "h-4 w-4",
          isStarred ? "fill-yellow-400 text-yellow-500" : "text-muted-foreground",
        )}
      />
      <span>{isStarred ? t("starred") : t("star")}</span>
      {showCount && (
        <span className="ml-1 rounded bg-muted px-1.5 py-0.5 text-xs tabular-nums">
          {count}
        </span>
      )}
    </Button>
  );
}
