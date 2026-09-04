import { PassportRideOccurrenceStatus } from '@app/models/passport/passport-ride-occurrence.models';
import { CreatePassportVisitRequest, PassportVisitDate } from '@app/models/passport/passport-visit.models';
import {
  PASSPORT_ANONYMOUS_DRAFT_MAX_RIDE_COUNT,
  PASSPORT_ANONYMOUS_DRAFT_SCHEMA_VERSION,
  PassportAnonymousDraft,
  PassportAnonymousRideDraft
} from './passport-anonymous-draft.models';

const rideStatuses: ReadonlySet<PassportRideOccurrenceStatus> = new Set([
  'Completed',
  'Attempted',
  'MissedClosed',
  'MissedUnavailable',
  'SkippedByChoice'
]);

export function isSupportedPassportAnonymousDraft(value: unknown): value is PassportAnonymousDraft {
  if (!isRecord(value)) {
    return false;
  }

  const rides: unknown = value['rides'];
  return value['schemaVersion'] === PASSPORT_ANONYMOUS_DRAFT_SCHEMA_VERSION
    && isBoundedRequiredString(value['id'], 128)
    && isBoundedRequiredString(value['visitOperationId'], 128)
    && isBoundedRequiredString(value['rideOperationId'], 128)
    && isBoundedRequiredString(value['parkName'], 300)
    && isVisit(value['visit'])
    && Array.isArray(rides)
    && rides.every(isRide)
    && rides.reduce(
      (total: number, ride: PassportAnonymousRideDraft): number => total + ride.count,
      0
    ) <= PASSPORT_ANONYMOUS_DRAFT_MAX_RIDE_COUNT
    && isPendingImport(value['pendingImport'])
    && isIsoDate(value['createdAtUtc'])
    && isIsoDate(value['updatedAtUtc']);
}

function isPendingImport(value: unknown): boolean {
  if (value === undefined || value === null) {
    return true;
  }

  if (!isRecord(value)
    || (value['choice'] !== 'Separate' && value['choice'] !== 'Merge')
    || !isNullableBoundedString(value['targetVisitId'], 128)
    || (value['metadataChoice'] !== 'KeepServer' && value['metadataChoice'] !== 'UseLocal')
    || !isIsoDate(value['startedAtUtc'])) {
    return false;
  }

  return value['choice'] === 'Separate'
    || isBoundedRequiredString(value['targetVisitId'], 128);
}

function isVisit(value: unknown): value is CreatePassportVisitRequest {
  if (!isRecord(value) || !isVisitDate(value['date'])) {
    return false;
  }

  return isBoundedRequiredString(value['parkId'], 128)
    && isNullableBoundedString(value['timeZoneId'], 128)
    && (value['serviceDayConvention'] === 'VisitStartLocalDate'
      || value['serviceDayConvention'] === 'UserSelectedServiceDate')
    && isNullableBoundedString(value['title'], 160)
    && isNullableBoundedString(value['privateNote'], 4000);
}

function isVisitDate(value: unknown): value is PassportVisitDate {
  if (!isRecord(value)
    || !Number.isInteger(value['year'])
    || (value['year'] as number) < 1
    || (value['year'] as number) > 9999
    || typeof value['isApproximate'] !== 'boolean') {
    return false;
  }

  const precision: unknown = value['precision'];
  const month: unknown = value['month'];
  const day: unknown = value['day'];
  if (precision === 'Year') {
    return month === null && day === null;
  }

  if ((precision !== 'Month' && precision !== 'Day')
    || !Number.isInteger(month)
    || (month as number) < 1
    || (month as number) > 12) {
    return false;
  }

  if (precision === 'Month') {
    return day === null;
  }

  return Number.isInteger(day)
    && (day as number) >= 1
    && (day as number) <= daysInMonth(value['year'] as number, month as number);
}

function isRide(value: unknown): value is PassportAnonymousRideDraft {
  if (!isRecord(value) || !isRecord(value['moment'])) {
    return false;
  }

  const moment: Record<string, unknown> = value['moment'];
  const count: unknown = value['count'];
  return isBoundedRequiredString(value['id'], 128)
    && isBoundedRequiredString(value['parkItemId'], 128)
    && isBoundedRequiredString(value['attractionName'], 300)
    && (moment['localTime'] === null
      || (typeof moment['localTime'] === 'string'
        && /^([01]\d|2[0-3]):[0-5]\d$/.test(moment['localTime'])))
    && typeof moment['isApproximate'] === 'boolean'
    && typeof value['status'] === 'string'
    && rideStatuses.has(value['status'] as PassportRideOccurrenceStatus)
    && isNullableBoundedString(value['privateNote'], 2000)
    && typeof value['confirmHistoricalConflict'] === 'boolean'
    && Number.isInteger(count)
    && (count as number) >= 1
    && (count as number) <= 100;
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return !!value && typeof value === 'object' && !Array.isArray(value);
}

function isBoundedRequiredString(value: unknown, maximumLength: number): value is string {
  return typeof value === 'string'
    && value.trim().length > 0
    && value.length <= maximumLength;
}

function isNullableBoundedString(value: unknown, maximumLength: number): value is string | null {
  return value === null || (typeof value === 'string' && value.length <= maximumLength);
}

function isIsoDate(value: unknown): value is string {
  return typeof value === 'string'
    && value.length <= 40
    && !Number.isNaN(Date.parse(value));
}

function daysInMonth(year: number, month: number): number {
  return new Date(Date.UTC(year, month, 0)).getUTCDate();
}
