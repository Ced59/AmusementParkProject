import { HttpErrorResponse } from '@angular/common/http';

import { hasHttpStatus } from './http-error-status.helpers';

describe('hasHttpStatus', () => {
  it('returns true for a matching HttpErrorResponse status', () => {
    const error: HttpErrorResponse = new HttpErrorResponse({ status: 404 });

    expect(hasHttpStatus(error, 404)).toBeTrue();
  });

  it('returns false for a non matching HttpErrorResponse status', () => {
    const error: HttpErrorResponse = new HttpErrorResponse({ status: 500 });

    expect(hasHttpStatus(error, 404)).toBeFalse();
  });

  it('supports plain error-like objects containing a numeric status', () => {
    const error: { status: number } = { status: 401 };

    expect(hasHttpStatus(error, 401)).toBeTrue();
  });

  it('ignores objects whose status is not numeric', () => {
    const error: { status: string } = { status: '404' };

    expect(hasHttpStatus(error, 404)).toBeFalse();
  });

  it('returns false for null, primitives and unrelated objects', () => {
    expect(hasHttpStatus(null, 404)).toBeFalse();
    expect(hasHttpStatus(undefined, 404)).toBeFalse();
    expect(hasHttpStatus('404', 404)).toBeFalse();
    expect(hasHttpStatus({ message: 'not found' }, 404)).toBeFalse();
  });
});
