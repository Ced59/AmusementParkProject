export type PassportVisitDatePrecision = 'Year' | 'Month' | 'Day';

export type PassportLocalServiceDayConvention = 'VisitStartLocalDate' | 'UserSelectedServiceDate';

export type PassportVisitStatus = 'Draft' | 'Completed' | 'Archived';

export type PassportVisitPrivacy = 'Private' | 'Unlisted' | 'Public';

export interface PassportVisitDate {
  year: number;
  month: number | null;
  day: number | null;
  precision: PassportVisitDatePrecision;
  isApproximate: boolean;
}

export interface CreatePassportVisitRequest {
  parkId: string;
  date: PassportVisitDate;
  timeZoneId: string | null;
  serviceDayConvention: PassportLocalServiceDayConvention;
  title: string | null;
  privateNote: string | null;
}

export interface UpdatePassportVisitRequest {
  date: PassportVisitDate;
  timeZoneId: string | null;
  serviceDayConvention: PassportLocalServiceDayConvention;
  title: string | null;
  privateNote: string | null;
  expectedVersion: number;
}

export interface MutatePassportVisitStatusRequest {
  expectedVersion: number;
}

export interface PassportVisitParkAssessment {
  value: number;
  privateComment: string | null;
  revision: number;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface UpsertPassportVisitParkAssessmentRequest {
  value: number;
  privateComment: string | null;
  expectedVersion: number;
}

export interface PassportVisit {
  id: string;
  parkId: string;
  date: PassportVisitDate;
  timeZoneId: string | null;
  serviceDayConvention: PassportLocalServiceDayConvention;
  status: PassportVisitStatus;
  privacy: PassportVisitPrivacy;
  title: string | null;
  privateNote: string | null;
  parkAssessment?: PassportVisitParkAssessment | null;
  version: number;
  createdAtUtc: string;
  updatedAtUtc: string;
  completedAtUtc: string | null;
}
