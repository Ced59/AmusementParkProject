import { HttpErrorResponse, HttpRequest } from '@angular/common/http';
import { Observable, throwError, timer } from 'rxjs';

export const TRANSIENT_HTTP_READ_RETRY_COUNT: number = 2;

const TRANSIENT_RETRY_BASE_DELAY_MILLISECONDS: number = 250;
const TRANSIENT_RETRY_MAX_DELAY_MILLISECONDS: number = 2_000;
const TRANSIENT_HTTP_STATUS_CODES: ReadonlySet<number> = new Set<number>([
  0,
  408,
  429,
  500,
  502,
  503,
  504
]);

export function resolveTransientHttpReadRetryDelay(
  error: unknown,
  request: HttpRequest<unknown>,
  retryCount: number
): Observable<number> {
  if (!shouldRetryTransientHttpRead(error, request)) {
    return throwError(() => error);
  }

  return timer(resolveRetryDelayMilliseconds(error, retryCount));
}

function shouldRetryTransientHttpRead(error: unknown, request: HttpRequest<unknown>): boolean {
  if (!isSafeHttpMethod(request.method)) {
    return false;
  }

  if (!(error instanceof HttpErrorResponse)) {
    return true;
  }

  return TRANSIENT_HTTP_STATUS_CODES.has(error.status);
}

function isSafeHttpMethod(method: string): boolean {
  const normalizedMethod: string = method.toUpperCase();
  return normalizedMethod === 'GET' || normalizedMethod === 'HEAD';
}

function resolveRetryDelayMilliseconds(error: unknown, retryCount: number): number {
  const retryAfterDelayMilliseconds: number | null = tryReadRetryAfterDelayMilliseconds(error);
  if (retryAfterDelayMilliseconds !== null) {
    return retryAfterDelayMilliseconds;
  }

  return Math.min(
    TRANSIENT_RETRY_BASE_DELAY_MILLISECONDS * retryCount,
    TRANSIENT_RETRY_MAX_DELAY_MILLISECONDS
  );
}

function tryReadRetryAfterDelayMilliseconds(error: unknown): number | null {
  if (!(error instanceof HttpErrorResponse)) {
    return null;
  }

  const retryAfterHeader: string | null = error.headers.get('Retry-After');
  if (retryAfterHeader === null) {
    return null;
  }

  const retryAfterSeconds: number = Number.parseInt(retryAfterHeader, 10);
  if (!Number.isFinite(retryAfterSeconds) || retryAfterSeconds < 0) {
    return null;
  }

  return Math.min(
    retryAfterSeconds * 1000,
    TRANSIENT_RETRY_MAX_DELAY_MILLISECONDS
  );
}
