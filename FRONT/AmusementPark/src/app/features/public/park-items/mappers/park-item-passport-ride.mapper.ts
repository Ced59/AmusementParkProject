import {
  CreatePassportRideOccurrencesBatchRequest,
  PassportRideOccurrenceStatus
} from '@app/models/passport/passport-ride-occurrence.models';
import { PassportVisit } from '@app/models/passport/passport-visit.models';
import { ParkItem } from '@app/models/parks/park-item';
import { formatPassportVisitDate } from '@shared/utils/passport/passport-visit-date-formatter';
import {
  ParkItemPassportRideDraft,
  ParkItemPassportRideVisitOption
} from '../models/park-item-passport-ride.models';

const allowedStatuses: ReadonlySet<PassportRideOccurrenceStatus> = new Set<PassportRideOccurrenceStatus>([
  'Completed',
  'Attempted',
  'MissedClosed',
  'MissedUnavailable',
  'SkippedByChoice'
]);

export function canLogParkItemRide(item: ParkItem, resolvedParkId: string | null | undefined): boolean {
  return item.category === 'Attraction'
    && !!item.id?.trim()
    && !!(item.parkId?.trim() || resolvedParkId?.trim());
}

export function mapPassportVisitToParkItemRideVisitOption(
  visit: PassportVisit,
  language: string
): ParkItemPassportRideVisitOption | null {
  const id: string = visit.id?.trim() ?? '';
  if (!id || visit.status !== 'Draft') {
    return null;
  }

  return {
    id,
    dateLabel: formatPassportVisitDate(visit.date, language),
    title: visit.title?.trim() || null,
    acceptsLocalTime: visit.date.precision === 'Day' && !!visit.timeZoneId?.trim()
  };
}

export function mapParkItemRideDraftToRequest(
  parkItemId: string,
  draft: ParkItemPassportRideDraft,
  acceptsLocalTime: boolean
): CreatePassportRideOccurrencesBatchRequest | null {
  const normalizedParkItemId: string = parkItemId.trim();
  const normalizedCount: number = Number.isFinite(draft.count) ? Math.trunc(draft.count) : 0;
  if (!normalizedParkItemId
    || normalizedCount < 1
    || normalizedCount > 100
    || !allowedStatuses.has(draft.status)) {
    return null;
  }

  const localTime: string | null = acceptsLocalTime ? normalizeTime(draft.localTime) : null;
  if (acceptsLocalTime && draft.localTime.trim().length > 0 && localTime === null) {
    return null;
  }

  return {
    items: [{
      parkItemId: normalizedParkItemId,
      moment: {
        localTime,
        isApproximate: localTime !== null && draft.isApproximate
      },
      status: draft.status,
      privateNote: null,
      confirmHistoricalConflict: draft.confirmHistoricalConflict,
      count: normalizedCount
    }]
  };
}

export function isParkItemRideRatingValid(value: number | null): boolean {
  if (value === null) {
    return true;
  }

  return Number.isFinite(value)
    && value >= 0.5
    && value <= 5
    && Number.isInteger(value * 2);
}

export function formatParkItemRideReferenceDate(value: string | null, language: string): string {
  const match: RegExpMatchArray | null = value?.trim().match(/^(\d{4})-(\d{2})-(\d{2})$/) ?? null;
  if (!match) {
    return '—';
  }

  const year: number = Number(match[1]);
  const month: number = Number(match[2]);
  const day: number = Number(match[3]);
  const date: Date = new Date(Date.UTC(year, month - 1, day));
  if (date.getUTCFullYear() !== year
    || date.getUTCMonth() !== month - 1
    || date.getUTCDate() !== day) {
    return '—';
  }

  return new Intl.DateTimeFormat(language.trim() || 'en', {
    day: 'numeric',
    month: 'long',
    year: 'numeric',
    timeZone: 'UTC'
  }).format(date);
}

function normalizeTime(value: string): string | null {
  const normalized: string = value.trim();
  return /^([01]\d|2[0-3]):[0-5]\d$/.test(normalized)
    ? `${normalized}:00`
    : null;
}
