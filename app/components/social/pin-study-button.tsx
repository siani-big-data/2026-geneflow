"use client";

import { Pin, PinOff } from "lucide-react";
import { useTranslations } from "next-intl";
import { Button } from "@/components/ui/button";
import {
  useMyPinnedStudies,
  useUpdateMyPinnedStudies,
} from "@/hooks/use-pinned-studies";
import { MAX_PINNED_STUDIES } from "@/types/social";

interface PinStudyButtonProps {
  studyId: string;
  size?: "sm" | "default" | "lg";
  variant?: "default" | "outline" | "ghost";
}

/**
 * Toggle the current user's pin for a study (max 6).
 */
export function PinStudyButton({
  studyId,
  size = "default",
  variant = "outline",
}: PinStudyButtonProps) {
  const t = useTranslations("pinned");
  const pinnedQ = useMyPinnedStudies();
  const updateM = useUpdateMyPinnedStudies();

  const pinned = pinnedQ.data ?? [];
  const isPinned = pinned.some((p) => p.studyId === studyId);
  const atLimit = pinned.length >= MAX_PINNED_STUDIES;
  const pending = updateM.isPending;

  const onClick = () => {
    if (pending) return;
    let next: string[];
    if (isPinned) {
      next = pinned
        .filter((p) => p.studyId !== studyId)
        .sort((a, b) => a.order - b.order)
        .map((p) => p.studyId);
    } else {
      if (atLimit) return;
      next = [
        ...pinned.sort((a, b) => a.order - b.order).map((p) => p.studyId),
        studyId,
      ];
    }
    updateM.mutate(next);
  };

  return (
    <Button
      type="button"
      variant={variant}
      size={size}
      onClick={onClick}
      disabled={pending || pinnedQ.isLoading || (!isPinned && atLimit)}
      aria-pressed={isPinned}
      title={!isPinned && atLimit ? t("atLimitHint", { max: MAX_PINNED_STUDIES }) : undefined}
    >
      {isPinned ? (
        <>
          <PinOff className="h-4 w-4" />
          <span>{t("unpin")}</span>
        </>
      ) : (
        <>
          <Pin className="h-4 w-4" />
          <span>{t("pin")}</span>
        </>
      )}
    </Button>
  );
}
