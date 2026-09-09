"use client";

import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { profileService } from "@/services/profile.service";
import { cn } from "@/lib/utils";

interface UserChipProps {
  userId: string;
  /** Avatar size in pixels (default 24). */
  size?: number;
  /** Hide the name and only render the avatar. */
  avatarOnly?: boolean;
  /** Extra classes for the outer link. */
  className?: string;
}

/**
 * Renders a clickable user chip (avatar + display name) for a given userId.
 * Resolves the user's profile via /api/v1/profiles/{userId} and links to
 * /users/{userId}. Falls back to the raw userId while the profile loads or
 * if the fetch fails (e.g. user without a public profile).
 */
export function UserChip({
  userId,
  size = 24,
  avatarOnly = false,
  className,
}: UserChipProps) {
  const { data: profile } = useQuery({
    queryKey: ["profile", "by-user", userId],
    queryFn: () => profileService.getProfileByUserId(userId),
    enabled: !!userId,
    staleTime: 5 * 60 * 1000,
    retry: false,
  });

  const displayName = profile?.fullName ?? userId;
  const initials =
    profile?.initials ||
    displayName.slice(0, 2).toUpperCase();
  const photo = profileService.resolveStorageUrl(
    profile?.photoThumbnailUrl ?? profile?.photoUrl,
  );

  const sizePx = { width: size, height: size };

  return (
    <Link
      href={`/users/${userId}`}
      className={cn(
        "inline-flex items-center gap-1.5 rounded-full hover:underline focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
        className,
      )}
      title={displayName}
    >
      <Avatar
        className="shrink-0"
        style={sizePx}
      >
        {photo ? <AvatarImage src={photo} alt={displayName} /> : null}
        <AvatarFallback className="text-[10px] font-semibold">
          {initials}
        </AvatarFallback>
      </Avatar>
      {!avatarOnly && (
        <span className="truncate text-sm font-medium text-foreground">
          {displayName}
        </span>
      )}
    </Link>
  );
}
