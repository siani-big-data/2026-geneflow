/**
 * Ownership Service — Phase 7.
 *
 * Handles transferring ownership of resources (currently studies) between
 * Users and Orgs.
 */

import { api } from "@/lib/api-client";
import type { TransferStudyOwnershipInput } from "@/types";

export const ownershipService = {
  /**
   * Transfer a study to a new owner (User or Org).
   */
  async transferStudy(
    studyId: string,
    input: TransferStudyOwnershipInput,
  ): Promise<void> {
    return api.post<void>(`/api/v1/studies/${studyId}/transfer`, input);
  },
};
