import { PassportVisit, UpdatePassportVisitRequest } from '@app/models/passport/passport-visit.models';
import { PassportVisitMetadataDraft } from '../models/passport-visit-editor.models';

export interface PassportVisitMetadataMappingResult {
  request: UpdatePassportVisitRequest | null;
  errorKey: string | null;
}

export function createPassportVisitMetadataDraft(visit: PassportVisit): PassportVisitMetadataDraft {
  return {
    precision: visit.date.precision,
    year: visit.date.year,
    month: visit.date.month,
    day: visit.date.day,
    isApproximate: visit.date.isApproximate,
    timeZoneId: visit.timeZoneId ?? '',
    title: visit.title ?? '',
    privateNote: visit.privateNote ?? ''
  };
}

export function mapPassportVisitMetadataDraft(
  draft: PassportVisitMetadataDraft,
  expectedVersion: number
): PassportVisitMetadataMappingResult {
  const year: number | null = normalizeInteger(draft.year);
  if (year === null || year < 1 || year > 9999) {
    return invalid('passport.editor.visit.validation.yearInvalid');
  }

  const month: number | null = draft.precision === 'Year' ? null : normalizeInteger(draft.month);
  if (draft.precision !== 'Year' && (month === null || month < 1 || month > 12)) {
    return invalid('passport.editor.visit.validation.monthInvalid');
  }

  const day: number | null = draft.precision === 'Day' ? normalizeInteger(draft.day) : null;
  if (draft.precision === 'Day'
    && (day === null || month === null || day < 1 || day > daysInMonth(year, month))) {
    return invalid('passport.editor.visit.validation.dayInvalid');
  }

  const timeZoneId: string | null = normalizeOptional(draft.timeZoneId);
  if (timeZoneId && timeZoneId.length > 128) {
    return invalid('passport.editor.visit.validation.timeZoneInvalid');
  }

  const title: string | null = normalizeOptional(draft.title);
  if (title && title.length > 160) {
    return invalid('passport.editor.visit.validation.titleTooLong');
  }

  const privateNote: string | null = normalizeOptional(draft.privateNote);
  if (privateNote && privateNote.length > 4000) {
    return invalid('passport.editor.visit.validation.noteTooLong');
  }

  return {
    request: {
      date: {
        year,
        month,
        day,
        precision: draft.precision,
        isApproximate: draft.isApproximate
      },
      timeZoneId,
      serviceDayConvention: 'VisitStartLocalDate',
      title,
      privateNote,
      expectedVersion
    },
    errorKey: null
  };
}

function normalizeInteger(value: number | null): number | null {
  return typeof value === 'number' && Number.isInteger(value) ? value : null;
}

function normalizeOptional(value: string): string | null {
  const normalizedValue: string = value.trim();
  return normalizedValue.length > 0 ? normalizedValue : null;
}

function daysInMonth(year: number, month: number): number {
  if (month === 2) {
    const isLeapYear: boolean = year % 4 === 0 && (year % 100 !== 0 || year % 400 === 0);
    return isLeapYear ? 29 : 28;
  }

  return [4, 6, 9, 11].includes(month) ? 30 : 31;
}

function invalid(errorKey: string): PassportVisitMetadataMappingResult {
  return { request: null, errorKey };
}
