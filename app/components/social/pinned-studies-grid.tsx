"use client";

import Link from "next/link";
import { Pin, PinOff } from "lucide-react";
import { useTranslations } from "next-intl";
import { useStudy } from "@/hooks/use-studies";
import {
  useMyPinnedStudies,
  useUpdateMyPinnedStudies,
  useUserPinnedStudies,
} from "@/hooks/use-pinned-studies";
import { MAX_PINNED_STUDIES } from "@/types/social";
import { Button } from "@/components/ui/button";

interface PinnedStudiesGridProps {
  /** When provided, shows the pinned grid for that user (read-only). */
  userId?: string;
  /** Whether to render the unpin controls (only for owner). */
  editable?: boolean;
}

/**
 * Renders a grid of up to 6 pinned studies for a user's profile.
 * If `editable`, allows the owner to unpin entries.
 */
export function PinnedStudiesGrid({
  userId,
  editable = false,
}: PinnedStudiesGridProps) {
  const t = useTranslations("pinned");

  const myQ = useMyPinnedStudies();
  const userQ = useUserPinnedStudies(userId);
  const data = userId ? userQ.data : myQ.data;
  const isLoading = userId ? userQ.isLoading : myQ.isLoading;

  const updateM = useUpdateMyPinnedStudies();

  const onUnpin = (studyId: string) => {
    if (!data) return;
    const next = data
      .filter((p) => p.studyId !== studyId)
      .sort((a, b) => a.order - b.order)
      .map((p) => p.studyId);
    updateM.mutate(next);
  };

  if (isLoading) {
    return (
      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
        {Array.from({ length: 3 }).map((_, i) => (
          <div
            key={i}
            className="h-24 animate-pulse rounded-lg border bg-muted/40"
          />
        ))}
      </div>
    );
  }

  if (!data || data.length === 0) {
    return (
      <div className="rounded-lg border border-dashed p-6 text-center text-sm text-muted-foreground">
        {editable
          ? t("emptyOwner", { max: MAX_PINNED_STUDIES })
          : t("emptyOther")}
      </div>
    );
  }

  return (
    <div>
      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
        {data
          .slice()
          .sort((a, b) => a.order - b.order)
          .map((p) => (
            <PinnedStudyCard
              key={p.studyId}
              studyId={p.studyId}
              editable={editable}
              disabled={updateM.isPending}
              onUnpin={onUnpin}
            />
          ))}
      </div>
      {editable && (
        <p className="mt-3 text-xs text-muted-foreground">
          {t("limitHint", { max: MAX_PINNED_STUDIES, count: data.length })}
        </p>
      )}
    </div>
  );
}

function PinnedStudyCard({
  studyId,
  editable,
  disabled,
  onUnpin,
}: {
  studyId: string;
  editable: boolean;
  disabled: boolean;
  onUnpin: (id: string) => void;
}) {
  const t = useTranslations("pinned");
  const { data: study } = useStudy(studyId);

  return (
    <div className="group relative rounded-lg border bg-card p-3 transition hover:bg-accent">
      <Link href={`/studies/${studyId}`} className="block">
        <div className="flex items-start gap-2">
          <Pin className="mt-0.5 h-4 w-4 text-muted-foreground" />
          <div className="min-w-0">
            <div className="truncate text-sm font-medium">
              {study?.title ?? studyId}
            </div>
            {study?.description && (
              <p className="mt-1 line-clamp-2 text-xs text-muted-foreground">
                {study.description}
              </p>
            )}
          </div>
        </div>
      </Link>
      {editable && (
        <Button
          type="button"
          variant="ghost"
          size="sm"
          className="absolute right-1 top-1 opacity-0 transition-opacity group-hover:opacity-100"
          onClick={() => onUnpin(studyId)}
          disabled={disabled}
          aria-label={t("unpin")}
        >
          <PinOff className="h-4 w-4" />
        </Button>
      )}
    </div>
  );
}
