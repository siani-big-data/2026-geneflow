/**
 * Activity feed types — mirror of the backend wire contract returned by
 * `GET /api/v1/activity/me`.
 */

export type ActivityVerb =
  | "Created"
  | "Updated"
  | "Deleted"
  | "Started"
  | "Completed"
  | "Failed"
  | "Joined"
  | "Left"
  | string;

export type ActivityObjectType =
  | "Study"
  | "Trace"
  | "Pipeline"
  | "PipelineExecution"
  | "Profile"
  | "User"
  | string;

export type ActivityVisibility =
  | "Public"
  | "StudyMembers"
  | "Private"
  | string;

/** Single activity event as returned by the API. */
export interface ActivityEventResponse {
  id: string;
  actorUserId: string;
  verb: ActivityVerb;
  objectType: ActivityObjectType;
  objectId: string;
  studyId: string | null;
  occurredAt: string;
  visibility: ActivityVisibility;
  payloadJson: string;
  sourceEventType: string;
}

/**
 * Cursor-paginated page of activity events. Opaque cursor — clients must
 * pass the value back unchanged to fetch the next page.
 */
export interface ActivityFeedResponse {
  items: ActivityEventResponse[];
  nextCursor: string | null;
  hasMore: boolean;
}
