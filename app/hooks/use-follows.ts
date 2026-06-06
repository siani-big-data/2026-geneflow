"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { socialService } from "@/services/social.service";

export const followKeys = {
  all: ["follows"] as const,
  isFollowing: (userId: string) =>
    [...followKeys.all, "is-following", userId] as const,
  followers: (userId: string, page: number, pageSize: number) =>
    [...followKeys.all, "followers", userId, page, pageSize] as const,
  following: (userId: string, page: number, pageSize: number) =>
    [...followKeys.all, "following", userId, page, pageSize] as const,
};

export function useIsFollowing(userId: string | undefined) {
  return useQuery({
    queryKey: followKeys.isFollowing(userId ?? ""),
    queryFn: () => socialService.isFollowing(userId!),
    enabled: !!userId,
  });
}

export function useFollowers(
  userId: string | undefined,
  pageNumber = 1,
  pageSize = 20,
) {
  return useQuery({
    queryKey: followKeys.followers(userId ?? "", pageNumber, pageSize),
    queryFn: () => socialService.getFollowers(userId!, pageNumber, pageSize),
    enabled: !!userId,
  });
}

export function useFollowing(
  userId: string | undefined,
  pageNumber = 1,
  pageSize = 20,
) {
  return useQuery({
    queryKey: followKeys.following(userId ?? "", pageNumber, pageSize),
    queryFn: () => socialService.getFollowing(userId!, pageNumber, pageSize),
    enabled: !!userId,
  });
}

export function useFollowUser() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (userId: string) => socialService.follow(userId),
    onSuccess: (_, userId) => {
      queryClient.invalidateQueries({ queryKey: followKeys.isFollowing(userId) });
      queryClient.invalidateQueries({ queryKey: followKeys.all });
    },
  });
}

export function useUnfollowUser() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (userId: string) => socialService.unfollow(userId),
    onSuccess: (_, userId) => {
      queryClient.invalidateQueries({ queryKey: followKeys.isFollowing(userId) });
      queryClient.invalidateQueries({ queryKey: followKeys.all });
    },
  });
}
