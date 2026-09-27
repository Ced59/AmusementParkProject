import { PassportVisitDate } from './passport-visit.models';

export type PassportHistoricalExistenceReportStatus =
  'Pending' | 'AcceptedForResearch' | 'Dismissed';

export interface PassportHistoricalExistenceReport {
  reportId: string;
  parkId: string;
  parkName: string;
  visitDate: PassportVisitDate;
  claimedName: string;
  sourceUrl: string | null;
  sourceReference: string | null;
  details: string | null;
  status: PassportHistoricalExistenceReportStatus;
  submittedAtUtc: string;
  reviewedAtUtc: string | null;
  decisionNote: string | null;
  revision: number;
}

export interface SubmitPassportHistoricalExistenceReportRequest {
  claimedName: string;
  sourceUrl: string | null;
  sourceReference: string | null;
  details: string | null;
}
