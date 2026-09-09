// =============================================================================
// DASHBOARD TYPES - Aligned with GeneFlow.ApiNet Backend API
// =============================================================================

/** Study count statistics for dashboard overview */
export interface DashboardStudyStats {
  totalCount: number;
  activeCount: number;
  draftCount: number;
  archivedCount: number;
  publishedCount: number;
}

/** Trace statistics for dashboard */
export interface TraceStats {
  totalCount: number;
  processedCount: number;
  pendingCount: number;
  processingCount: number;
  failedCount: number;
}

/** Alignment statistics for dashboard */
export interface AlignmentStats {
  totalCount: number;
  completedCount: number;
  pendingCount: number;
  processingCount: number;
  failedCount: number;
}

/** Recent study summary for dashboard */
export interface RecentStudy {
  studyId: string;
  title: string;
  status: string;
  tracesCount: number;
  lastUpdated: string;
}

/** Dashboard statistics response */
export interface DashboardOverview {
  studies: DashboardStudyStats;
  traces: TraceStats;
  alignments: AlignmentStats;
  recentStudies: RecentStudy[];
}

/** Activity item for activity feed */
export interface ActivityItem {
  type: string;
  action: string;
  description: string;
  studyId?: string;
  studyTitle?: string;
  entityId?: string;
  entityName?: string;
  timestamp: string;
}
