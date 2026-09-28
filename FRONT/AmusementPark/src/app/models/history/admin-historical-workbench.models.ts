import { AdminHistoricalParkDiagnostics, AdminHistoricalVisitDiagnostics } from './admin-historical-park-diagnostics.models';

export type HistoricalEditorialResourceType = 'Fact' | 'Source' | 'Relation';
export type HistoricalWorkflowState = 'Draft' | 'SourcesAttached' | 'EditorialReview' | 'StructuredValidation' | 'Published' | 'Corrected' | 'Retracted';
export type HistoricalPublicationState = 'Draft' | 'Published' | 'LegacyPublishedPendingReview' | 'Withdrawn' | 'Suppressed';
export type HistoricalFactState = 'Verified' | 'Probable' | 'Disputed' | 'Unverified' | 'Retracted';
export type HistoricalSubjectType = 'Park' | 'ParkItem' | 'ParkZone' | 'StandaloneAttraction' | 'ParkOperator' | 'AttractionManufacturer';

export interface AdminHistoricalDate {
  year: number;
  month: number | null;
  day: number | null;
  precision: 'Year' | 'Month' | 'Day';
  isApproximate: boolean;
  qualifier: string | null;
}

export interface AdminHistoricalPeriod {
  start: AdminHistoricalDate | null;
  end: AdminHistoricalDate | null;
  startConfidence: string;
  endConfidence: string;
}

export interface AdminHistoricalSubject {
  type: HistoricalSubjectType;
  id: string;
  label: string;
  publicationPolicy: string;
}

export interface AdminHistoricalLocalizedText {
  languageCode: string;
  value: string;
}

export interface AdminHistoricalEvidence {
  sourceId: string;
  revision: number;
  position: 'Supports' | 'Contradicts';
}

export interface AdminHistoricalSource {
  id: string;
  revision: number;
  type: string;
  title: string;
  publisherOrAuthor: string;
  url: string | null;
  bibliographicReference: string | null;
  publishedOn: string | null;
  accessedOn: string;
  languageCode: string | null;
  archiveUrl: string | null;
  scopes: string[];
  adminNote: string | null;
  accessibility: string;
  workflowState: HistoricalWorkflowState;
  publicationState: HistoricalPublicationState;
}

export interface AdminHistoricalFact {
  id: string;
  revision: number;
  subject: AdminHistoricalSubject;
  type: string;
  period: AdminHistoricalPeriod;
  state: HistoricalFactState;
  importance: 'Standard' | 'Major';
  workflowState: HistoricalWorkflowState;
  publicationState: HistoricalPublicationState;
  publicUncertaintyExplanation: AdminHistoricalLocalizedText[];
  lifecycleBoundaryMeaning: string | null;
  attributeKind: string | null;
  attributeBoundaryMeaning: string | null;
  sequenceWithinDate: number | null;
  sources: AdminHistoricalEvidence[];
  structuredValue: string | null;
  otherTypeLabel: string | null;
  narrativeContentId: string | null;
}

export interface AdminHistoricalRelation {
  id: string;
  revision: number;
  source: AdminHistoricalSubject;
  target: AdminHistoricalSubject;
  type: string;
  direction: string;
  period: AdminHistoricalPeriod;
  state: HistoricalFactState;
  workflowState: HistoricalWorkflowState;
  publicationState: HistoricalPublicationState;
  publicUncertaintyExplanation: AdminHistoricalLocalizedText[];
  sources: AdminHistoricalEvidence[];
  editorialNote: string | null;
}

export interface AdminHistoricalParkWorkbench {
  parkId: string;
  parkName: string;
  subjects: AdminHistoricalSubject[];
  facts: AdminHistoricalFact[];
  relations: AdminHistoricalRelation[];
  sources: AdminHistoricalSource[];
  diagnostics: AdminHistoricalParkDiagnostics;
}

export interface HistoricalDateRequest {
  year: number;
  month: number | null;
  day: number | null;
  precision: 'Year' | 'Month' | 'Day';
  isApproximate: boolean;
  qualifier: string | null;
}

export interface HistoricalPeriodRequest {
  start: HistoricalDateRequest | null;
  end: HistoricalDateRequest | null;
  startConfidence: string;
  endConfidence: string;
}

export interface HistoricalEvidenceSourceRequest {
  sourceId: string;
  revision: number;
  position: 'Supports' | 'Contradicts';
}

export interface SaveHistoricalSourceRequest {
  expectedRevision: number | null;
  type: string;
  title: string;
  publisherOrAuthor: string;
  url: string | null;
  bibliographicReference: string | null;
  publishedOn: string | null;
  accessedOn: string;
  languageCode: string | null;
  archiveUrl: string | null;
  scopes: string[];
  adminNote: string | null;
  accessibility: string;
  reviewNote: string | null;
}

export interface SaveHistoricalFactRequest {
  expectedRevision: number | null;
  subjectType: HistoricalSubjectType;
  subjectId: string;
  type: string;
  period: HistoricalPeriodRequest;
  state: HistoricalFactState;
  importance: 'Standard' | 'Major';
  publicUncertaintyExplanation: AdminHistoricalLocalizedText[];
  lifecycleBoundaryMeaning: string | null;
  attributeKind: string | null;
  attributeBoundaryMeaning: string | null;
  sequenceWithinDate: number | null;
  sources: HistoricalEvidenceSourceRequest[];
  structuredValue: string | null;
  otherTypeLabel: string | null;
  narrativeContentId: string | null;
  reviewNote: string | null;
}

export interface SaveHistoricalRelationRequest {
  expectedRevision: number | null;
  sourceSubjectType: HistoricalSubjectType;
  sourceSubjectId: string;
  targetSubjectType: HistoricalSubjectType;
  targetSubjectId: string;
  type: string;
  direction: string;
  period: HistoricalPeriodRequest;
  state: HistoricalFactState;
  publicUncertaintyExplanation: AdminHistoricalLocalizedText[];
  sources: HistoricalEvidenceSourceRequest[];
  editorialNote: string | null;
  reviewNote: string | null;
}

export interface HistoricalEditorialMutation {
  resourceType: HistoricalEditorialResourceType;
  resourceId: string;
  revision: number;
  workflowState: HistoricalWorkflowState;
  publicationState: HistoricalPublicationState;
  factState: HistoricalFactState | null;
}

export interface HistoricalSnapshotImpactSummary {
  knownOpenSubjectCount: number;
  ambiguityCount: number;
  reliablePeriodSubjectCount: number;
  partialPeriodSubjectCount: number;
  undatedSubjectCount: number;
}

export interface HistoricalPublicationImpactPreview {
  resourceType: HistoricalEditorialResourceType;
  resourceId: string;
  resourceLabel: string;
  previewYear: number;
  canPublish: boolean;
  blockingReasons: string[];
  affectedFromYear: number | null;
  affectedToYear: number | null;
  affectedSnapshotYearCount: number | null;
  changedSubjectCount: number;
  before: HistoricalSnapshotImpactSummary;
  after: HistoricalSnapshotImpactSummary;
  visits: AdminHistoricalVisitDiagnostics;
}
