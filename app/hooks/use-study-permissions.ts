"use client";

import { useMemo } from "react";
import type { Study, CurrentUserPermissions, StudyStatus } from "@/types/study";

const ALL_TABS = ["overview", "traces", "pipelines", "papers", "timeline", "team", "settings"] as const;
type TabId = (typeof ALL_TABS)[number];

// Tabs visible to everyone (including public visitors on published studies).
// `timeline` mirrors the backend's GetStudyTimelineQuery (MinimumRole = null):
// members + non-members on public studies can read the study activity feed.
const PUBLIC_TABS: TabId[] = ["overview", "traces", "pipelines", "papers", "timeline"];

// Tabs only visible to study members
const MEMBER_ONLY_TABS: TabId[] = ["team", "settings"];

/**
 * Check if a tab should be visible based on permissions
 */
function canViewTab(
  tabId: string,
  permissions: CurrentUserPermissions | undefined
): boolean {
  if (PUBLIC_TABS.includes(tabId as TabId)) {
    return true;
  }
  if (MEMBER_ONLY_TABS.includes(tabId as TabId)) {
    return permissions?.isMember ?? false;
  }
  return true;
}

/**
 * Get filtered tabs based on permissions
 */
function getVisibleTabs(permissions: CurrentUserPermissions | undefined): TabId[] {
  return ALL_TABS.filter(tab => canViewTab(tab, permissions));
}

export interface UseStudyPermissionsResult {
  /** Raw permissions object from API */
  permissions: CurrentUserPermissions | undefined;
  /** Whether the current user is a member of the study */
  isMember: boolean;
  /** Whether viewing as public (non-member on a published study) */
  isPublicView: boolean;
  /** User's role ID (null if not a member) */
  roleId: number | null;
  /** User's role name (null if not a member) */
  roleName: string | null;
  /** Can manage members (invite, remove, change roles) - Owner/Admin */
  canManageMembers: boolean;
  /** Can edit study metadata (title, description) - Owner/Admin/Editor */
  canEditStudy: boolean;
  /** Can edit study content (traces, papers) - Owner/Admin/Editor */
  canEditContent: boolean;
  /** Can change study status - Owner/Admin */
  canChangeStatus: boolean;
  /** Can delete the study - Owner only */
  canDeleteStudy: boolean;
  /** Can transfer ownership - Owner only */
  canTransferOwnership: boolean;
  /** List of tab IDs visible to the current user */
  visibleTabs: TabId[];
  /** Check if a specific tab is visible */
  canViewTab: (tabId: string) => boolean;
}

/**
 * Hook to access and compute study permissions for the current user.
 *
 * @param study - The study object containing currentUserPermissions
 * @returns Permission flags and computed values for conditional UI rendering
 *
 * @example
 * ```tsx
 * const { canEditContent, canManageMembers, visibleTabs } = useStudyPermissions(study);
 *
 * // Conditional rendering
 * {canEditContent && <UploadButton />}
 * {canManageMembers && <InviteMemberButton />}
 * ```
 */
export function useStudyPermissions(study: Study | undefined): UseStudyPermissionsResult {
  const permissions = study?.currentUserPermissions;

  return useMemo(() => {
    const isMember = permissions?.isMember ?? false;

    return {
      permissions,
      isMember,
      isPublicView: !isMember,
      roleId: permissions?.roleId ?? null,
      roleName: permissions?.roleName ?? null,
      canManageMembers: permissions?.canManageMembers ?? false,
      canEditStudy: permissions?.canEditStudy ?? false,
      canEditContent: permissions?.canEditContent ?? false,
      canChangeStatus: permissions?.canChangeStatus ?? false,
      canDeleteStudy: permissions?.canDeleteStudy ?? false,
      canTransferOwnership: permissions?.canTransferOwnership ?? false,
      visibleTabs: getVisibleTabs(permissions),
      canViewTab: (tabId: string) => canViewTab(tabId, permissions),
    };
  }, [permissions]);
}
