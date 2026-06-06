"use client";

import { use } from "react";
import { useTranslations } from "next-intl";
import { useQuery } from "@tanstack/react-query";
import {
  MapPin,
  Briefcase,
  BookOpen,
  Globe,
  Calendar,
  Loader2,
  AlertCircle,
} from "lucide-react";
import { profileService } from "@/services/profile.service";
import { useAuthStore } from "@/stores/auth-store";
import { useTranslatedResearchFields } from "@/hooks";
import { FollowButton } from "@/components/social/follow-button";
import { FollowersDialog } from "@/components/social/followers-dialog";
import { PinnedStudiesGrid } from "@/components/social/pinned-studies-grid";
import { useFollowers, useFollowing } from "@/hooks/use-follows";

interface PublicUserProfilePageProps {
  params: Promise<{ locale: string; userId: string }>;
}

/**
 * Public user profile page. Mirrors the layout of the own-profile page
 * but without any edit/upload controls. Surfaces basic identity, "about",
 * follower/following dialogs and the user's pinned studies.
 */
export default function PublicUserProfilePage({
  params,
}: PublicUserProfilePageProps) {
  const { userId } = use(params);
  const t = useTranslations("profile");
  const tFollows = useTranslations("follows");
  const tPinned = useTranslations("pinned");
  const currentUser = useAuthStore((s) => s.user);
  const { getFieldLabel } = useTranslatedResearchFields();

  const profileQ = useQuery({
    queryKey: ["profile", "by-user", userId],
    queryFn: () => profileService.getProfileByUserId(userId),
    enabled: !!userId,
  });

  // Page size 1 is enough to read totalCount for the chips.
  const followersQ = useFollowers(userId, 1, 1);
  const followingQ = useFollowing(userId, 1, 1);

  const profile = profileQ.data;

  if (profileQ.isLoading) {
    return (
      <div className="flex min-h-[400px] items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-teal" />
      </div>
    );
  }

  if (profileQ.isError || !profile) {
    return (
      <div className="flex min-h-[400px] flex-col items-center justify-center gap-4">
        <AlertCircle className="h-12 w-12 text-destructive" />
        <p className="text-lg font-medium text-foreground">
          {profileQ.error instanceof Error
            ? profileQ.error.message
            : "Profile not found"}
        </p>
      </div>
    );
  }

  return (
    <div className="-mx-16 -mt-10 min-h-full bg-background">
      {/* Header Section */}
      <div className="border-b border-border bg-card">
        <div className="mx-auto max-w-[1400px] px-8 py-8">
          <div className="flex items-start gap-8">
            {/* Avatar */}
            <div className="relative">
              {profile.photoUrl ? (
                <img
                  src={
                    profileService.resolveStorageUrl(profile.photoThumbnailUrl) ||
                    profileService.resolveStorageUrl(profile.photoUrl) ||
                    undefined
                  }
                  alt={profile.fullName}
                  className="h-28 w-28 rounded-2xl object-cover shadow-lg"
                />
              ) : (
                <div className="flex h-28 w-28 items-center justify-center rounded-2xl bg-gradient-to-br from-teal to-blue-deep text-3xl font-semibold text-white shadow-lg">
                  {profile.initials}
                </div>
              )}
            </div>

            {/* Profile Info */}
            <div className="flex-1">
              <div className="mb-3 flex items-start justify-between gap-4">
                <div className="min-w-0">
                  <h1 className="mb-1 truncate text-2xl font-semibold text-foreground">
                    {profile.fullName}
                  </h1>
                  <p className="text-base text-muted-foreground">
                    {profile.professionalRole || t("noRole")}
                  </p>
                </div>
                <FollowButton
                  userId={userId}
                  currentUserId={currentUser?.id}
                />
              </div>

              <div className="mb-3 flex flex-wrap gap-2">
                <FollowersDialog
                  userId={userId}
                  mode="followers"
                  triggerLabel={tFollows("followers")}
                  count={followersQ.data?.totalCount}
                />
                <FollowersDialog
                  userId={userId}
                  mode="following"
                  triggerLabel={tFollows("following")}
                  count={followingQ.data?.totalCount}
                />
              </div>

              <div className="grid grid-cols-1 gap-3 text-sm md:grid-cols-2">
                {profile.institutionName && (
                  <div className="flex items-center gap-2 text-muted-foreground">
                    <Briefcase className="h-4 w-4" />
                    <span>
                      {profile.institutionDisplayName || profile.institutionName}
                    </span>
                  </div>
                )}
                {profile.researchField && (
                  <div className="flex items-center gap-2 text-muted-foreground">
                    <BookOpen className="h-4 w-4" />
                    <span>{getFieldLabel(profile.researchField)}</span>
                  </div>
                )}
                {profile.location && (
                  <div className="flex items-center gap-2 text-muted-foreground">
                    <MapPin className="h-4 w-4" />
                    <span>{profile.location}</span>
                  </div>
                )}
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Content */}
      <div className="mx-auto max-w-[1400px] px-8 py-8">
        <div className="space-y-6">
            {/* About */}
            <div className="rounded-xl border border-border bg-card p-6">
              <h2 className="mb-4 text-lg font-semibold text-foreground">
                {t("about.title")}
              </h2>
              <p className="mb-5 text-sm leading-relaxed text-muted-foreground">
                {profile.bio || t("about.noBio")}
              </p>
              <div className="flex flex-wrap gap-6 border-t border-border pt-4">
                {profile.orcidId && (
                  <div>
                    <p className="mb-1 text-xs text-muted-foreground">
                      {t("about.orcid")}
                    </p>
                    <a
                      href={profile.orcidUrl || `https://orcid.org/${profile.orcidId}`}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="font-mono text-sm text-blue-deep hover:underline"
                    >
                      {profile.orcidId}
                    </a>
                  </div>
                )}
                {profile.website && (
                  <div>
                    <p className="mb-1 text-xs text-muted-foreground">
                      {t("about.website")}
                    </p>
                    <a
                      href={profile.website}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="flex items-center gap-1 text-sm text-blue-deep hover:underline"
                    >
                      <Globe className="h-3.5 w-3.5" />
                      {t("about.labWebsite")}
                    </a>
                  </div>
                )}
                <div>
                  <p className="mb-1 text-xs text-muted-foreground">
                    {t("about.memberSince")}
                  </p>
                  <p className="flex items-center gap-1.5 text-sm text-foreground">
                    <Calendar className="h-3.5 w-3.5 text-muted-foreground" />
                    {new Date(profile.createdAt).toLocaleDateString("en-US", {
                      month: "long",
                      year: "numeric",
                    })}
                  </p>
                </div>
              </div>
            </div>

            {/* Pinned Studies */}
            <div className="rounded-xl border border-border bg-card p-6">
              <h2 className="mb-4 text-lg font-semibold text-foreground">
                {tPinned("title")}
              </h2>
              <PinnedStudiesGrid userId={userId} editable={false} />
            </div>
        </div>
      </div>
    </div>
  );
}
