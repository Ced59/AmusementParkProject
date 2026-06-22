import { isApiHeaderHiddenFromPublicProxy } from './public-api-header-policy';

describe('public API header policy', () => {
  it('hides upstream headers owned by the public edge or SSR layer', () => {
    expect(isApiHeaderHiddenFromPublicProxy('Content-Security-Policy')).toBeTrue();
    expect(isApiHeaderHiddenFromPublicProxy('Content-Security-Policy-Report-Only')).toBeTrue();
    expect(isApiHeaderHiddenFromPublicProxy('Strict-Transport-Security')).toBeTrue();
    expect(isApiHeaderHiddenFromPublicProxy('X-Powered-By')).toBeTrue();
  });

  it('keeps regular API response headers visible', () => {
    expect(isApiHeaderHiddenFromPublicProxy('Cache-Control')).toBeFalse();
    expect(isApiHeaderHiddenFromPublicProxy('Content-Type')).toBeFalse();
    expect(isApiHeaderHiddenFromPublicProxy('ETag')).toBeFalse();
  });

  it('matches header names case-insensitively', () => {
    expect(isApiHeaderHiddenFromPublicProxy('strict-transport-security')).toBeTrue();
    expect(isApiHeaderHiddenFromPublicProxy('STRICT-TRANSPORT-SECURITY')).toBeTrue();
  });
});
