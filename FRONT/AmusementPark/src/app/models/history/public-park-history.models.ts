import { PaginationContract } from '@shared/models/contracts';

export interface PublicHistoricalDate {
  year: number;
  month?: number | null;
  day?: number | null;
  precision: string;
  isApproximate?: boolean;
  qualifier?: string | null;
}

export interface PublicHistoricalPeriod {
  start?: PublicHistoricalDate | null;
  end?: PublicHistoricalDate | null;
  startConfidence: string;
  endConfidence: string;
}

export interface PublicHistoricalLocalizedText {
  languageCode: string;
  value: string;
}

export interface PublicHistoricalSource {
  type: string;
  title: string;
  publisherOrAuthor: string;
  url?: string | null;
  bibliographicReference?: string | null;
  publishedOn?: string | null;
  accessedOn: string;
  languageCode?: string | null;
  archiveUrl?: string | null;
  accessibility: string;
}

export interface PublicHistoricalTimelineEntry {
  subjectType: string;
  subjectId: string;
  subjectLabel: string;
  currentSubjectName?: string | null;
  hasPublishedLineage?: boolean;
  factType: string;
  period: PublicHistoricalPeriod;
  evidenceState: string;
  importance: string;
  attributeKind?: string | null;
  previousDisplayValue?: string | null;
  nextDisplayValue?: string | null;
  otherTypeLabel?: string | null;
  narrative?: {
    eventId: string;
    slug?: string | null;
    titles: PublicHistoricalLocalizedText[];
  } | null;
  uncertaintyExplanations: PublicHistoricalLocalizedText[];
  sources: PublicHistoricalSource[];
}

export interface PublicParkHistoricalTimeline {
  parkId: string;
  parkName: string;
  events: PublicHistoricalTimelineEntry[];
  pagination: PaginationContract;
}

export interface PublicHistoricalAttribute {
  kind: string;
  state: string;
  displayValue?: string | null;
  displayCandidates: string[];
  isDisplayResolved: boolean;
}

export interface PublicHistoricalSubjectSnapshot {
  subjectType: string;
  subjectId: string;
  displayName: string;
  nameOrigin: string;
  operationalState: string;
  presenceExtent: string;
  attributes: PublicHistoricalAttribute[];
  reasonCodes: string[];
  supportingSourceCount: number;
}

export interface PublicHistoricalFieldCoverage {
  documentedSubjectCount: number;
  applicableSubjectCount: number;
  percentage: number;
  isComplete: boolean;
}

export interface PublicHistoricalCoverage {
  totalSubjectCount: number;
  reliablePeriodSubjectCount: number;
  partialPeriodSubjectCount: number;
  undatedSubjectCount: number;
  name: PublicHistoricalFieldCoverage;
  zone: PublicHistoricalFieldCoverage;
  lastReviewedAtUtc?: string | null;
  status: string;
}

export interface PublicHistoricalAmbiguity {
  subjectType: string;
  subjectId: string;
  subjectLabel: string;
  nameOrigin: string;
  code: string;
  attributeKind?: string | null;
}

export interface PublicHistoricalLineageSubject {
  key: string;
  type: string;
  label: string;
  isHistoricalOnly: boolean;
}

export interface PublicHistoricalLineageRelation {
  sourceKey: string;
  targetKey: string;
  type: string;
  direction: string;
  period: PublicHistoricalPeriod;
  evidenceState: string;
  uncertaintyExplanations: PublicHistoricalLocalizedText[];
  sources: PublicHistoricalSource[];
}

export interface PublicHistoricalLineageContextPark {
  id: string;
  name: string;
}

export interface PublicHistoricalLineage {
  root: PublicHistoricalLineageSubject;
  contextPark: PublicHistoricalLineageContextPark | null;
  subjects: PublicHistoricalLineageSubject[];
  relations: PublicHistoricalLineageRelation[];
  hasDirectedCycle: boolean;
  isTruncated: boolean;
  maximumDepth: number;
}

export interface PublicParkHistoricalSnapshot {
  parkId: string;
  parkName: string;
  requestedInstant: PublicHistoricalDate;
  subjects: PublicHistoricalSubjectSnapshot[];
  coverage: PublicHistoricalCoverage;
  ambiguities: PublicHistoricalAmbiguity[];
  methodologyVersion: string;
}

export interface PublicHistoricalSubjectComparison {
  comparisonKey: string;
  subjectType: string;
  displayName: string;
  nameOrigin: string;
  previousName?: string | null;
  nextName?: string | null;
  presenceChange: string;
  isRenamed: boolean;
  isMoved: boolean;
  previousZoneName?: string | null;
  nextZoneName?: string | null;
  previousCategory?: string | null;
  nextCategory?: string | null;
  fromOperationalState: string;
  toOperationalState: string;
  fromSupportingSourceCount: number;
  toSupportingSourceCount: number;
}

export interface PublicHistoricalCategoryNetChange {
  category: string;
  fromCount: number;
  toCount: number;
  netChange: number;
}

export interface PublicParkHistoricalComparison {
  parkId: string;
  parkName: string;
  fromInstant: PublicHistoricalDate;
  toInstant: PublicHistoricalDate;
  subjects: PublicHistoricalSubjectComparison[];
  categoryNetChanges: PublicHistoricalCategoryNetChange[];
  fromCoverage: PublicHistoricalCoverage;
  toCoverage: PublicHistoricalCoverage;
  fromUnclassifiedOpenItemCount: number;
  toUnclassifiedOpenItemCount: number;
  isCategoryComparisonComplete: boolean;
  methodologyVersion: string;
}
