import { api } from "@/lib/api-client";
import type {
  Comment,
  CreateCommentInput,
  CreateDiscussionInput,
  Discussion,
  EditCommentInput,
  ReactionSummary,
} from "@/types/discussions";
import type { PagedResponse } from "@/types/common";

/**
 * Discussions + Comments + Reactions service.
 */
export const discussionsService = {
  // ============================================================
  // Discussions
  // ============================================================

  list(
    studyId: string,
    pageNumber = 1,
    pageSize = 20,
  ): Promise<PagedResponse<Discussion>> {
    return api.get<PagedResponse<Discussion>>(
      `/api/v1/studies/${studyId}/discussions?pageNumber=${pageNumber}&pageSize=${pageSize}`,
    );
  },

  get(discussionId: string): Promise<Discussion> {
    return api.get<Discussion>(`/api/v1/discussions/${discussionId}`);
  },

  create(studyId: string, input: CreateDiscussionInput): Promise<Discussion> {
    return api.post<Discussion>(
      `/api/v1/studies/${studyId}/discussions`,
      input,
    );
  },

  lock(discussionId: string, lock: boolean): Promise<Discussion> {
    return api.post<Discussion>(
      `/api/v1/discussions/${discussionId}/lock`,
      { lock },
    );
  },

  // ============================================================
  // Comments
  // ============================================================

  createComment(
    discussionId: string,
    input: CreateCommentInput,
  ): Promise<Comment> {
    return api.post<Comment>(
      `/api/v1/discussions/${discussionId}/comments`,
      input,
    );
  },

  editComment(commentId: string, input: EditCommentInput): Promise<Comment> {
    return api.put<Comment>(`/api/v1/comments/${commentId}`, input);
  },

  deleteComment(commentId: string): Promise<void> {
    return api.delete<void>(`/api/v1/comments/${commentId}`);
  },

  // ============================================================
  // Reactions
  // ============================================================

  addReaction(commentId: string, emoji: string): Promise<ReactionSummary[]> {
    return api.post<ReactionSummary[]>(
      `/api/v1/comments/${commentId}/reactions/${encodeURIComponent(emoji)}`,
    );
  },

  removeReaction(commentId: string, emoji: string): Promise<ReactionSummary[]> {
    return api.delete<ReactionSummary[]>(
      `/api/v1/comments/${commentId}/reactions/${encodeURIComponent(emoji)}`,
    );
  },
};
