/**
 * Profile Service for GeneFlow.
 *
 * Handles all profile-related API calls:
 * - Get current user profile
 * - Get profile by user ID
 * - Update profile information
 * - Update research identifiers
 * - Update/delete profile photo
 * - Get profile statistics
 */

import { api } from "@/lib/api-client";
import type {
  Profile,
  ProfileSummary,
  ProfileStats,
  UpdateProfileRequest,
  UpdateResearchIdentifiersRequest,
  UpdateProfilePhotoRequest,
} from "@/types";

export const profileService = {
  // ===========================================================================
  // GET PROFILE
  // ===========================================================================

  /**
   * Get the current authenticated user's profile.
   */
  async getCurrentProfile(): Promise<Profile> {
    return api.get<Profile>("/api/v1/profiles/me");
  },

  /**
   * Get a profile by user ID.
   */
  async getProfileByUserId(userId: string): Promise<Profile> {
    return api.get<Profile>(`/api/v1/profiles/${userId}`);
  },

  /**
   * Get multiple profiles by user IDs.
   */
  async getProfilesByUserIds(userIds: string[]): Promise<ProfileSummary[]> {
    return api.post<ProfileSummary[]>("/api/v1/profiles/batch", { userIds });
  },

  // ===========================================================================
  // PROFILE STATS
  // ===========================================================================

  /**
   * Get statistics for the current user's profile.
   */
  async getCurrentProfileStats(): Promise<ProfileStats> {
    return api.get<ProfileStats>("/api/v1/profiles/me/stats");
  },

  // ===========================================================================
  // UPDATE PROFILE
  // ===========================================================================

  /**
   * Update the current user's basic profile information.
   */
  async updateProfile(data: UpdateProfileRequest): Promise<Profile> {
    return api.put<Profile>("/api/v1/profiles/me", data);
  },

  /**
   * Update the current user's research identifiers (ORCID, Website).
   */
  async updateResearchIdentifiers(
    data: UpdateResearchIdentifiersRequest
  ): Promise<Profile> {
    return api.put<Profile>("/api/v1/profiles/me/research-identifiers", data);
  },

  // ===========================================================================
  // PROFILE PHOTO
  // ===========================================================================

  /**
   * Update the current user's profile photo.
   */
  async updateProfilePhoto(data: UpdateProfilePhotoRequest): Promise<Profile> {
    return api.put<Profile>("/api/v1/profiles/me/photo", data);
  },

  /**
   * Delete the current user's profile photo.
   */
  async deleteProfilePhoto(): Promise<void> {
    return api.delete("/api/v1/profiles/me/photo");
  },

  // ===========================================================================
  // HELPERS
  // ===========================================================================

  /**
   * Generate initials from a name.
   * @param firstName - First name
   * @param lastName - Last name (optional)
   * @returns Initials (e.g., "JD" for "John Doe")
   */
  getInitials(firstName: string, lastName?: string | null): string {
    const first = firstName?.[0]?.toUpperCase() || "";
    const last = lastName?.[0]?.toUpperCase() || "";
    return first + last;
  },

  /**
   * Generate full name from parts.
   */
  getFullName(firstName: string, lastName?: string | null): string {
    return lastName ? `${firstName} ${lastName}` : firstName;
  },

  /**
   * Format ORCID ID to URL.
   */
  getOrcidUrl(orcidId: string | null): string | null {
    if (!orcidId) return null;
    return `https://orcid.org/${orcidId}`;
  },

  /**
   * Validate ORCID ID format.
   * Format: 0000-0000-0000-000X (where X is digit or X)
   */
  isValidOrcidId(orcidId: string): boolean {
    const pattern = /^\d{4}-\d{4}-\d{4}-\d{3}[\dX]$/;
    return pattern.test(orcidId);
  },

  /**
   * Validate website URL.
   */
  isValidWebsite(url: string): boolean {
    try {
      const parsed = new URL(url);
      return parsed.protocol === "http:" || parsed.protocol === "https:";
    } catch {
      return false;
    }
  },
};
