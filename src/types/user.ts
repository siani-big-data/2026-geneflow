export interface User {
  id: string;
  email: string;
  name: string;
  avatar?: string;
  role: UserRole;
  plan: PlanTier;
  organization?: string;
  createdAt: string;
  updatedAt: string;
}

export type UserRole = "user" | "admin" | "researcher";

export type PlanTier = "free" | "pro" | "enterprise";

export interface UserProfile extends User {
  bio?: string;
  location?: string;
  website?: string;
  studiesCount: number;
  tracesCount: number;
}

export interface UserSettings {
  theme: "light" | "dark" | "system";
  notifications: NotificationSettings;
  privacy: PrivacySettings;
}

export interface NotificationSettings {
  email: boolean;
  pipelineCompleted: boolean;
  studyShared: boolean;
  weeklyDigest: boolean;
}

export interface PrivacySettings {
  profilePublic: boolean;
  showActivity: boolean;
}
