import { shouldSkipAuthorizationHeader } from './auth-request-policy';

describe('shouldSkipAuthorizationHeader', () => {
  it('skips public POST endpoints with exact API paths', () => {
    expect(shouldSkipAuthorizationHeader('https://api.test/auth/login', 'POST')).toBeTrue();
    expect(shouldSkipAuthorizationHeader('https://api.test/users', 'POST')).toBeTrue();
    expect(shouldSkipAuthorizationHeader('https://api.test/users/forgot-password', 'POST')).toBeTrue();
    expect(shouldSkipAuthorizationHeader('https://api.test/auth/refresh-token', 'POST')).toBeTrue();
  });

  it('supports public POST endpoints behind the front API prefix', () => {
    expect(shouldSkipAuthorizationHeader('/api/auth/login', 'POST')).toBeTrue();
    expect(shouldSkipAuthorizationHeader('/api/users', 'POST')).toBeTrue();
    expect(shouldSkipAuthorizationHeader('/api/users/reset-password', 'POST')).toBeTrue();
  });

  it('skips external login and google response urls using dedicated matchers', () => {
    expect(shouldSkipAuthorizationHeader('https://api.test/auth/external/google', 'POST')).toBeTrue();
    expect(shouldSkipAuthorizationHeader('https://client.test/google-response?code=123')).toBeTrue();
  });

  it('does not skip user list and user profile reads', () => {
    expect(shouldSkipAuthorizationHeader('https://api.test/users?page=1&size=10', 'GET')).toBeFalse();
    expect(shouldSkipAuthorizationHeader('/api/users?page=1&size=10', 'GET')).toBeFalse();
    expect(shouldSkipAuthorizationHeader('https://api.test/users/123', 'GET')).toBeFalse();
    expect(shouldSkipAuthorizationHeader('https://api.test/admin/users', 'GET')).toBeFalse();
    expect(shouldSkipAuthorizationHeader('https://api.test/admin/users', 'POST')).toBeFalse();
  });

  it('keeps regular API calls protected', () => {
    expect(shouldSkipAuthorizationHeader('https://api.test/parks')).toBeFalse();
    expect(shouldSkipAuthorizationHeader('https://api.test/admin/audit-logs')).toBeFalse();
  });
});
