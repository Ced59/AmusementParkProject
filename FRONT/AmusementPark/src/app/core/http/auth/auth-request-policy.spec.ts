import { shouldSkipAuthorizationHeader } from './auth-request-policy';

describe('shouldSkipAuthorizationHeader', () => {
  it('skips anonymous authentication endpoints with exact path suffixes', () => {
    expect(shouldSkipAuthorizationHeader('https://api.test/auth/login')).toBeTrue();
    expect(shouldSkipAuthorizationHeader('https://api.test/users')).toBeTrue();
    expect(shouldSkipAuthorizationHeader('https://api.test/users/forgot-password')).toBeTrue();
    expect(shouldSkipAuthorizationHeader('https://api.test/auth/refresh-token')).toBeTrue();
  });

  it('skips external auth and google response urls using contains patterns', () => {
    expect(shouldSkipAuthorizationHeader('https://api.test/auth/external/google')).toBeTrue();
    expect(shouldSkipAuthorizationHeader('https://client.test/google-response?code=123')).toBeTrue();
  });

  it('does not skip protected endpoints containing a protected suffix only in the middle', () => {
    expect(shouldSkipAuthorizationHeader('https://api.test/users/123')).toBeFalse();
    expect(shouldSkipAuthorizationHeader('https://api.test/admin/users')).toBeTrue();
  });

  it('keeps regular API calls protected', () => {
    expect(shouldSkipAuthorizationHeader('https://api.test/parks')).toBeFalse();
    expect(shouldSkipAuthorizationHeader('https://api.test/admin/audit-logs')).toBeFalse();
  });
});
