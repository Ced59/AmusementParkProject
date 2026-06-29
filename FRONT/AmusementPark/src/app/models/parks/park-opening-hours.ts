export interface ParkOpeningHoursTimeRange {
  opensAt: string;
  closesAt: string;
  closesNextDay: boolean;
  lastAdmissionAt?: string | null;
  lastAdmissionNextDay: boolean;
}

export interface ParkOpeningHoursRule {
  id?: string | null;
  startDate: string;
  endDate: string;
  daysOfWeek: string[];
  isClosed: boolean;
  label?: string | null;
  reason?: string | null;
  sortOrder: number;
  timeRanges: ParkOpeningHoursTimeRange[];
}

export interface ParkOpeningHoursDateOverride {
  localDate: string;
  isClosed: boolean;
  label?: string | null;
  reason?: string | null;
  timeRanges: ParkOpeningHoursTimeRange[];
}

export interface ParkOpeningHoursSchedule {
  parkId: string;
  timeZoneId: string;
  sourceUrl?: string | null;
  notes?: string | null;
  lastVerifiedAtUtc?: string | null;
  createdAtUtc?: string | null;
  updatedAtUtc?: string | null;
  regularRules: ParkOpeningHoursRule[];
  dateOverrides: ParkOpeningHoursDateOverride[];
}

export interface ParkOpeningHoursDay {
  localDate: string;
  isClosed: boolean;
  isDefined: boolean;
  sourceKind: string;
  label?: string | null;
  reason?: string | null;
  timeRanges: ParkOpeningHoursTimeRange[];
}

export interface ParkOpeningHoursCalendar {
  parkId: string;
  timeZoneId: string;
  sourceUrl?: string | null;
  notes?: string | null;
  lastVerifiedAtUtc?: string | null;
  updatedAtUtc: string;
  firstDate?: string | null;
  lastDate?: string | null;
  fromDate: string;
  toDate: string;
  days: ParkOpeningHoursDay[];
}

export type ParkOpeningHoursAdminStatus = 'NotConfigured' | 'Expired' | 'NeedsUpdate' | 'UpToDate';
export type ParkOpeningHoursAdminFilter = 'all' | 'configured' | 'notConfigured' | 'upToDate' | 'needsUpdate' | 'expired';

export interface ParkOpeningHoursAdminSummary {
  hasOpeningHours: boolean;
  status: ParkOpeningHoursAdminStatus;
  timeZoneId?: string | null;
  firstDate?: string | null;
  lastDate?: string | null;
  completeUntilDate?: string | null;
  completeForDays?: number | null;
  warningThresholdDays?: number | null;
  lastVerifiedAtUtc?: string | null;
  updatedAtUtc?: string | null;
}
