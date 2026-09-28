export type HistoricalDiagnosticSeverity = 'Information' | 'Warning' | 'Error';

export interface AdminHistoricalDiagnosticIssue {
  code: string;
  severity: HistoricalDiagnosticSeverity;
  subjectType: string | null;
  subjectId: string | null;
  subjectLabel: string | null;
  factId: string | null;
  relationId: string | null;
}

export interface AdminHistoricalDecadeCoverage {
  decade: number;
  resourceCount: number;
  sourcedResourceCount: number;
  publishedResourceCount: number;
  subjectCount: number;
  sourceCoveragePercentage: number;
}

export interface AdminHistoricalWorkflowStage {
  stage: string;
  resourceCount: number;
}

export interface AdminHistoricalVisitDiagnostics {
  potentiallyInconsistentVisitCount: number;
  confirmedConflictVisitCount: number;
  unverifiedVisitCount: number;
}

export interface AdminHistoricalParkDiagnostics {
  parkId: string;
  parkName: string;
  factCount: number;
  relationCount: number;
  blockingIssueCount: number;
  issues: AdminHistoricalDiagnosticIssue[];
  decadeCoverage: AdminHistoricalDecadeCoverage[];
  workflow: AdminHistoricalWorkflowStage[];
  visits: AdminHistoricalVisitDiagnostics;
}
