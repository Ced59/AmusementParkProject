import { HttpErrorResponse } from '@angular/common/http';

import { ApiProblemDetails } from '@shared/models/contracts';

const GENERIC_ERROR_MESSAGE: string = 'Une erreur est survenue.';
const TECHNICAL_MESSAGE_PATTERNS: RegExp[] = [
  /\bat\s+[\w$.<>]+\(/i,
  /\bSystem\./i,
  /\bMicrosoft\./i,
  /\bMongo(DB)?\b/i,
  /\bNullReferenceException\b/i,
  /\bStackTrace\b/i,
  /\bHttpErrorResponse\b/i,
  /\bTypeError\b/i
];

export function extractSafeDisplayErrorMessage(error: unknown, fallback: string = GENERIC_ERROR_MESSAGE): string {
  const problemDetails: ApiProblemDetails | null = extractApiProblemDetails(error);
  if (problemDetails === null) {
    return fallback;
  }

  const candidate: string | null = problemDetails.detail ?? problemDetails.title ?? null;
  return sanitizeDisplayMessage(candidate, fallback);
}

export function extractApiProblemDetails(error: unknown): ApiProblemDetails | null {
  if (error instanceof HttpErrorResponse) {
    return isApiProblemDetails(error.error) ? error.error : null;
  }

  return isApiProblemDetails(error) ? error : null;
}

export function sanitizeDisplayMessage(value: string | null | undefined, fallback: string = GENERIC_ERROR_MESSAGE): string {
  const normalizedValue: string = stripHtml(value ?? '')
    .replace(/[\r\n\t]+/g, ' ')
    .replace(/\s{2,}/g, ' ')
    .trim();

  if (!normalizedValue) {
    return fallback;
  }

  if (normalizedValue.length > 240) {
    return fallback;
  }

  if (TECHNICAL_MESSAGE_PATTERNS.some((pattern: RegExp) => pattern.test(normalizedValue))) {
    return fallback;
  }

  return normalizedValue;
}

function isApiProblemDetails(value: unknown): value is ApiProblemDetails {
  if (typeof value !== 'object' || value === null) {
    return false;
  }

  const candidate: Record<string, unknown> = value as Record<string, unknown>;
  const status: unknown = candidate['status'];
  const title: unknown = candidate['title'];
  const detail: unknown = candidate['detail'];

  return typeof status === 'number'
    && (typeof title === 'string' || typeof detail === 'string');
}

function stripHtml(value: string): string {
  return value.replace(/<[^>]*>/g, '');
}
