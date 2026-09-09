"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import { ownershipService } from "@/services";
import type { TransferStudyOwnershipInput } from "@/types";

/** Transfer a study to a new owner (User or Org). */
export function useTransferStudyOwnership() {
  const qc = useQueryClient();

  return useMutation({
    mutationFn: ({
      studyId,
      input,
    }: {
      studyId: string;
      input: TransferStudyOwnershipInput;
    }) => ownershipService.transferStudy(studyId, input),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["studies"] });
    },
  });
}
