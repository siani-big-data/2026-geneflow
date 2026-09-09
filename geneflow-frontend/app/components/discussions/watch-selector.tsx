"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { Eye, EyeOff, BellRing, Loader2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { useSetWatchLevel } from "@/hooks";
import type { WatchLevel } from "@/types/notifications";

interface WatchSelectorProps {
  studyId: string;
  initialLevel?: WatchLevel;
}

const ICONS: Record<WatchLevel, React.ReactNode> = {
  All: <BellRing className="mr-1 h-3.5 w-3.5" />,
  Mentions: <Eye className="mr-1 h-3.5 w-3.5" />,
  None: <EyeOff className="mr-1 h-3.5 w-3.5" />,
};

export function WatchSelector({
  studyId,
  initialLevel = "None",
}: WatchSelectorProps) {
  const t = useTranslations("watch");
  const [level, setLevel] = useState<WatchLevel>(initialLevel);
  const mutation = useSetWatchLevel(studyId);

  const select = (next: WatchLevel) => {
    if (next === level) return;
    setLevel(next);
    mutation.mutate(next);
  };

  return (
    <div className="inline-flex items-center gap-1 rounded-md border bg-background p-0.5 text-xs">
      {(["All", "None"] as const).map((opt) => (
        <Button
          key={opt}
          type="button"
          size="sm"
          variant={level === opt ? "default" : "ghost"}
          onClick={() => select(opt)}
          disabled={mutation.isPending}
          className="h-7 px-2"
        >
          {mutation.isPending && level === opt ? (
            <Loader2 className="mr-1 h-3 w-3 animate-spin" />
          ) : (
            ICONS[opt]
          )}
          {t(`levels.${opt.toLowerCase()}`)}
        </Button>
      ))}
    </div>
  );
}
