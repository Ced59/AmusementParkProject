import { HttpErrorResponse } from '@angular/common/http';

export function hasHttpStatus(error: unknown, statusCode: number): boolean {
  if (error instanceof HttpErrorResponse) {
    return error.status === statusCode;
  }

  if (typeof error !== 'object' || error === null || !('status' in error)) {
    return false;
  }

  const candidate = (error as { status?: unknown }).status;
  return typeof candidate === 'number' && candidate === statusCode;
}
