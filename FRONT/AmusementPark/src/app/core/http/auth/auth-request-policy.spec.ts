import { shouldSkipAuthorizationHeader } from './auth-request-policy';

describe('shouldSkipAuthorizationHeader', () => {
  it('skips public POST endpoints with exact API paths', () => {
    expect(
      shouldSkipAuthorizationHeader('https://api.test/auth/login', 'POST'),
    ).toBe(true);
    expect(
      shouldSkipAuthorizationHeader('https://api.test/users', 'POST'),
    ).toBe(true);
    expect(
      shouldSkipAuthorizationHeader(
        'https://api.test/users/forgot-password',
        'POST',
      ),
    ).toBe(true);
    expect(
      shouldSkipAuthorizationHeader(
        'https://api.test/auth/refresh-token',
        'POST',
      ),
    ).toBe(true);
  });

  it('supports public POST endpoints behind the front API prefix', () => {
    expect(shouldSkipAuthorizationHeader('/api/auth/login', 'POST')).toBe(true);
    expect(shouldSkipAuthorizationHeader('/api/users', 'POST')).toBe(true);
    expect(
      shouldSkipAuthorizationHeader('/api/users/reset-password', 'POST'),
    ).toBe(true);
  });

  it('skips external login and google response urls using dedicated matchers', () => {
    expect(
      shouldSkipAuthorizationHeader(
        'https://api.test/auth/external/google',
        'POST',
      ),
    ).toBe(true);
    expect(
      shouldSkipAuthorizationHeader(
        'https://client.test/google-response?code=123',
      ),
    ).toBe(true);
  });

  it('does not skip user list and user profile reads', () => {
    expect(
      shouldSkipAuthorizationHeader(
        'https://api.test/users?page=1&size=10',
        'GET',
      ),
    ).toBe(false);
    expect(
      shouldSkipAuthorizationHeader('/api/users?page=1&size=10', 'GET'),
    ).toBe(false);
    expect(
      shouldSkipAuthorizationHeader('https://api.test/users/123', 'GET'),
    ).toBe(false);
    expect(
      shouldSkipAuthorizationHeader('https://api.test/admin/users', 'GET'),
    ).toBe(false);
    expect(
      shouldSkipAuthorizationHeader('https://api.test/admin/users', 'POST'),
    ).toBe(false);
  });

  it('keeps regular API calls protected', () => {
    expect(shouldSkipAuthorizationHeader('https://api.test/parks')).toBe(false);
    expect(
      shouldSkipAuthorizationHeader('https://api.test/admin/audit-logs'),
    ).toBe(false);
  });
});
