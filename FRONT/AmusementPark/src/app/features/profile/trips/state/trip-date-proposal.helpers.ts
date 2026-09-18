import { TripDateProposal } from '@app/models/trips/trip.models';

export const areTripDateInputsValid = (startDate: string, endDate: string): boolean => {
  const normalizedStart: string = startDate.trim();
  const normalizedEnd: string = endDate.trim();
  if (normalizedEnd && !normalizedStart) {
    return false;
  }
  if (!normalizedStart || !normalizedEnd) {
    return true;
  }

  const startTimestamp: number = Date.parse(`${normalizedStart}T00:00:00Z`);
  const endTimestamp: number = Date.parse(`${normalizedEnd}T00:00:00Z`);
  const inclusiveDayCount: number = ((endTimestamp - startTimestamp) / 86_400_000) + 1;
  return Number.isFinite(inclusiveDayCount)
    && inclusiveDayCount >= 1
    && inclusiveDayCount <= 366;
};

export const doesTripDateRangeExceedMaximum = (startDate: string, endDate: string): boolean => {
  const normalizedStart: string = startDate.trim();
  const normalizedEnd: string = endDate.trim();
  if (!normalizedStart || !normalizedEnd) {
    return false;
  }

  const startTimestamp: number = Date.parse(`${normalizedStart}T00:00:00Z`);
  const endTimestamp: number = Date.parse(`${normalizedEnd}T00:00:00Z`);
  const inclusiveDayCount: number = ((endTimestamp - startTimestamp) / 86_400_000) + 1;
  return Number.isFinite(inclusiveDayCount) && inclusiveDayCount > 366;
};

export const buildConfirmedTripDates = (startDate: string, endDate: string): TripDateProposal => {
  if (!areTripDateInputsValid(startDate, endDate)) {
    throw new Error('Trip date inputs are invalid.');
  }
  const normalizedStart: string = startDate.trim();
  const normalizedEnd: string = endDate.trim();
  if (!normalizedStart) {
    return {
      kind: 'None',
      startDate: null,
      endDate: null,
      candidateDates: []
    };
  }

  return {
    kind: 'Fixed',
    startDate: normalizedStart,
    endDate: normalizedEnd || normalizedStart,
    candidateDates: []
  };
};

export const resolvedBrowserTimeZone = (): string =>
  Intl.DateTimeFormat().resolvedOptions().timeZone || 'UTC';

export const enumerateTripDates = (proposal: TripDateProposal): string[] => {
  if (proposal.kind !== 'Fixed' || !proposal.startDate || !proposal.endDate) {
    return [];
  }

  const dates: string[] = [];
  const current: Date = new Date(`${proposal.startDate}T00:00:00Z`);
  const end: Date = new Date(`${proposal.endDate}T00:00:00Z`);
  while (current <= end && dates.length < 366) {
    dates.push(current.toISOString().slice(0, 10));
    current.setUTCDate(current.getUTCDate() + 1);
  }
  return dates;
};
