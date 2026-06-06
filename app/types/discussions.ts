/**
 * Discussions / Comments / Reactions — frontend types mirroring backend DTOs.
 * Polymorphic `parentType` lets comments later attach to Annotations / Traces.
 */

export type CommentParentType = "Discussion" | "Annotation" | "Trace";

export interface Discussion {
  id: string;
  studyId: string;
  authorId: string;
  title: string;
  category: string | null;
  isLocked: boolean;
  createdAt: string;
  modifiedAt: string | null;
  comments?: Comment[];
}

export interface ReactionSummary {
  emoji: string;
  count: number;
  reactedByMe: boolean;
}

export interface Comment {
  id: string;
  parentType: CommentParentType;
  parentId: string;
  authorId: string;
  bodyMarkdown: string;
  isDeleted: boolean;
  editedAt: string | null;
  createdAt: string;
  modifiedAt: string | null;
  reactions: ReactionSummary[];
}

// ============================================================
// Request payloads
// ============================================================

export interface CreateDiscussionInput {
  title: string;
  category?: string | null;
  firstCommentBody: string;
}

export interface LockDiscussionInput {
  lock: boolean;
}

export interface CreateCommentInput {
  body: string;
}

export interface EditCommentInput {
  body: string;
}
