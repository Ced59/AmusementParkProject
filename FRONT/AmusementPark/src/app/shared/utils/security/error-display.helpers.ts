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
  const candidate: string | null = extractErrorMessageCandidate(error);
  if (candidate === null) {
    return fallback;
  }

  return sanitizeDisplayMessage(candidate, fallback);
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

function extractErrorMessageCandidate(error: unknown): string | null {
  if (typeof error === 'string') {
    return error;
  }

  if (typeof error !== 'object' || error === null) {
    return null;
  }

  const errorRecord: Record<string, unknown> = error as Record<string, unknown>;
  const nestedError: unknown = errorRecord['error'];

  if (typeof nestedError === 'string') {
    return nestedError;
  }

  if (typeof nestedError === 'object' && nestedError !== null) {
    const nestedRecord: Record<string, unknown> = nestedError as Record<string, unknown>;
    const nestedMessage: unknown = nestedRecord['message'] ?? nestedRecord['Message'];
    if (typeof nestedMessage === 'string') {
      return nestedMessage;
    }
  }

  const message: unknown = errorRecord['message'] ?? errorRecord['Message'];
  return typeof message === 'string' ? message : null;
}

function stripHtml(value: string): string {
  return value.replace(/<[^>]*>/g, '');
}
