"use client";

import { useMutation } from "@tanstack/react-query";
import { notificationsService } from "@/services/notifications.service";
import type { WatchLevel } from "@/types/notifications";

export function useSetWatchLevel(studyId: string) {
  return useMutation({
    mutationFn: (level: WatchLevel) =>
      notificationsService.setWatchLevel(studyId, level),
  });
}
