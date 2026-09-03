import { PassportVisitDatePrecision } from '@app/models/passport/passport-visit.models';

export interface PassportVisitQuickCreateDraft {
  parkId: string;
  precision: PassportVisitDatePrecision;
  year: number | null;
  month: number | null;
  day: number | null;
  isApproximate: boolean;
  timeZoneId: string;
  title: string;
  privateNote: string;
}

export interface PassportParkOption {
  id: string;
  name: string;
  location: string | null;
}
