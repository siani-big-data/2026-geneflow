"use client";

import { useQuery } from "@tanstack/react-query";
import { profileService } from "@/services/profile.service";

/**
 * Resolves a public display name for a given userId. Falls back to the raw
 * userId while the profile is loading or if it cannot be fetched (e.g. user
 * without a public profile). Shares its cache with `UserChip`.
 */
export function useUserDisplayName(userId: string | null | undefined): string {
  const { data } = useQuery({
    queryKey: ["profile", "by-user", userId ?? ""],
    queryFn: () => profileService.getProfileByUserId(userId!),
    enabled: !!userId,
    staleTime: 5 * 60 * 1000,
    retry: false,
  });

  return data?.fullName ?? userId ?? "";
}
