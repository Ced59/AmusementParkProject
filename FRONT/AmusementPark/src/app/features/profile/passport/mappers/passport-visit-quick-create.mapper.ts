import { CreatePassportVisitRequest, PassportVisitDatePrecision } from '@app/models/passport/passport-visit.models';
import { Park } from '@app/models/parks/park';
import { PassportParkOption, PassportVisitQuickCreateDraft } from '../models/passport-visit-quick-create.models';

export interface PassportVisitQuickCreateMappingResult {
  request: CreatePassportVisitRequest | null;
  errorKey: string | null;
}

export function mapPassportVisitQuickCreateDraft(
  draft: PassportVisitQuickCreateDraft
): PassportVisitQuickCreateMappingResult {
  const parkId: string = draft.parkId.trim();
  if (!parkId) {
    return invalid('passport.quickCreate.validation.parkRequired');
  }

  const year: number | null = normalizeInteger(draft.year);
  if (year === null || year < 1 || year > 9999) {
    return invalid('passport.quickCreate.validation.yearInvalid');
  }

  const precision: PassportVisitDatePrecision = draft.precision;
  const month: number | null = precision === 'Year' ? null : normalizeInteger(draft.month);
  if (precision !== 'Year' && (month === null || month < 1 || month > 12)) {
    return invalid('passport.quickCreate.validation.monthInvalid');
  }

  const day: number | null = precision === 'Day' ? normalizeInteger(draft.day) : null;
  if (precision === 'Day' && (day === null || month === null || day < 1 || day > daysInMonth(year, month))) {
    return invalid('passport.quickCreate.validation.dayInvalid');
  }

  const timeZoneId: string | null = normalizeOptional(draft.timeZoneId);
  if (timeZoneId && (timeZoneId.length > 128 || !isValidTimeZoneId(timeZoneId))) {
    return invalid('passport.quickCreate.validation.timeZoneInvalid');
  }

  const title: string | null = normalizeOptional(draft.title);
  if (title && title.length > 160) {
    return invalid('passport.quickCreate.validation.titleTooLong');
  }

  const privateNote: string | null = normalizeOptional(draft.privateNote);
  if (privateNote && privateNote.length > 4000) {
    return invalid('passport.quickCreate.validation.noteTooLong');
  }

  return {
    request: {
      parkId,
      date: {
        year,
        month,
        day,
        precision,
        isApproximate: draft.isApproximate
      },
      timeZoneId,
      serviceDayConvention: 'VisitStartLocalDate',
      title,
      privateNote
    },
    errorKey: null
  };
}

export function mapParkToPassportOption(park: Park): PassportParkOption | null {
  const id: string = park.id?.trim() ?? '';
  const name: string = park.name?.trim() ?? '';
  if (!id || !name) {
    return null;
  }

  const locationParts: string[] = [park.city?.trim(), park.countryCode?.trim()]
    .filter((value: string | undefined): value is string => !!value);

  return {
    id,
    name,
    location: locationParts.length > 0 ? locationParts.join(' · ') : null
  };
}

function normalizeInteger(value: number | null): number | null {
  return typeof value === 'number' && Number.isInteger(value) ? value : null;
}

function normalizeOptional(value: string): string | null {
  const normalizedValue: string = value.trim();
  return normalizedValue.length > 0 ? normalizedValue : null;
}

function isValidTimeZoneId(value: string): boolean {
  try {
    new Intl.DateTimeFormat('en', { timeZone: value }).format(0);
    return true;
  } catch {
    return false;
  }
}

function daysInMonth(year: number, month: number): number {
  if (month === 2) {
    const isLeapYear: boolean = year % 4 === 0 && (year % 100 !== 0 || year % 400 === 0);
    return isLeapYear ? 29 : 28;
  }

  return [4, 6, 9, 11].includes(month) ? 30 : 31;
}

function invalid(errorKey: string): PassportVisitQuickCreateMappingResult {
  return { request: null, errorKey };
}
