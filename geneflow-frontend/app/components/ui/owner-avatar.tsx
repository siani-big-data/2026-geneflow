"use client";

import * as React from "react";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { cn } from "@/lib/utils";
import type { OwnerRef } from "@/types";

export interface OwnerAvatarProps {
  owner: OwnerRef;
  /** Size in px (mapped to width/height). Defaults to 32. */
  size?: number;
  className?: string;
}

/** Derive up to two-letter initials from a display name. */
function initialsOf(name: string): string {
  const parts = name.trim().split(/\s+/).filter(Boolean);
  if (parts.length === 0) return "?";
  if (parts.length === 1) {
    return parts[0].slice(0, 2).toUpperCase();
  }
  return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
}

/**
 * Renders an avatar for any study owner (User or Org). Falls back to two-letter
 * initials. Orgs render with a square-ish rounded tile to visually distinguish
 * them from circular user avatars.
 */
export const OwnerAvatar = React.forwardRef<HTMLSpanElement, OwnerAvatarProps>(
  ({ owner, size = 32, className }, ref) => {
    const isOrg = owner.type === "Org";
    const shapeClass = isOrg ? "rounded-md" : "rounded-full";
    const gradientClass = isOrg
      ? "bg-gradient-to-br from-blue-deep to-teal"
      : "bg-gradient-to-br from-teal to-blue-deep";

    return (
      <Avatar
        ref={ref}
        className={cn(shapeClass, "shrink-0", className)}
        style={{ width: size, height: size }}
        aria-label={`${owner.type === "Org" ? "Organization" : "User"}: ${owner.displayName}`}
      >
        {owner.avatarUrl ? (
          <AvatarImage
            src={owner.avatarUrl}
            alt={owner.displayName}
            className={shapeClass}
          />
        ) : null}
        <AvatarFallback
          className={cn(
            shapeClass,
            gradientClass,
            "text-xs font-medium text-white",
          )}
        >
          {initialsOf(owner.displayName)}
        </AvatarFallback>
      </Avatar>
    );
  },
);
OwnerAvatar.displayName = "OwnerAvatar";
