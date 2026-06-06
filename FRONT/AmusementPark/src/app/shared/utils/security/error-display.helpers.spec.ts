import { HttpErrorResponse } from '@angular/common/http';

import { extractApiProblemDetails, extractSafeDisplayErrorMessage, sanitizeDisplayMessage } from './error-display.helpers';

describe('error-display helpers', () => {
  it('extracts problem details from plain objects and HttpErrorResponse bodies', () => {
    const problem: { status: number; title: string; detail: string } = { status: 400, title: 'Invalid', detail: 'Invalid field' };
    const httpError: HttpErrorResponse = new HttpErrorResponse({ status: 400, error: problem });

    expect(extractApiProblemDetails(problem)).toBe(problem);
    expect(extractApiProblemDetails(httpError)).toBe(problem);
  });

  it('rejects malformed problem detail candidates', () => {
    expect(extractApiProblemDetails({ status: '400', title: 'Invalid' })).toBeNull();
    expect(extractApiProblemDetails({ status: 400 })).toBeNull();
    expect(extractApiProblemDetails(null)).toBeNull();
  });

  it('prefers detail over title for safe display messages', () => {
    const message: string = extractSafeDisplayErrorMessage({ status: 409, title: 'Conflict', detail: 'Already exists' });

    expect(message).toBe('Already exists');
  });

  it('uses title when detail is missing', () => {
    expect(extractSafeDisplayErrorMessage({ status: 404, title: 'Not found' })).toBe('Not found');
  });

  it('sanitizes html, whitespace and control characters', () => {
    expect(sanitizeDisplayMessage('<strong>Hello</strong>\n\t world')).toBe('Hello world');
  });

  it('hides empty, too long and technical messages behind fallback text', () => {
    const fallback: string = 'Fallback';

    expect(sanitizeDisplayMessage('', fallback)).toBe(fallback);
    expect(sanitizeDisplayMessage('a'.repeat(241), fallback)).toBe(fallback);
    expect(sanitizeDisplayMessage('System.NullReferenceException at Service.Run()', fallback)).toBe(fallback);
    expect(sanitizeDisplayMessage('TypeError: Cannot read properties', fallback)).toBe(fallback);
  });

  it('returns the fallback when no problem details can be extracted', () => {
    expect(extractSafeDisplayErrorMessage('boom', 'Fallback')).toBe('Fallback');
  });
});
