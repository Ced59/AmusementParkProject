// CI-only layout fixtures. Never imported by Angular or sent to a real server.
export const accountLayoutFixtureOrigin = 'https://apf-account-layout.invalid';
export const accountLayoutRefreshMarker = 'amusementpark.hasRefreshSession';
const userId = '00000000-0000-4000-8000-000000000017';

export function createAccountLayoutFixture(kind, nowSeconds) {
  if (!['standard', 'incomplete', 'expired'].includes(kind) || !Number.isInteger(nowSeconds) || nowSeconds < 1) {
    throw new Error('Unknown or undated account layout fixture.');
  }
  const date = new Date(nowSeconds * 1000).toISOString();
  const user = { id: userId, email: 'layout-review@apf-fixture.invalid',
    firstName: kind === 'incomplete' ? '' : 'Camille', lastName: kind === 'incomplete' ? '' : 'Démonstration',
    publicDisplayName: kind === 'incomplete' ? null : 'Compte de recette — caractères longs Äéł',
    publicIdentifier: 'layout-review', isActivated: true, isBlocked: false, roles: ['USER'],
    preferredLanguage: 'FR', preferredMeasurementSystem: 'SI', avatarUrl: '', createdAt: date, updatedAt: date };
  const claims = { sub: user.id, nameidentifier: user.id, email: user.email, firstname: user.firstName,
    lastname: user.lastName, avatar: '', lastlogin: date, roles: [...user.roles], jti: 'apf-ci-layout-only',
    exp: nowSeconds + 3600, iss: 'urn:apf-ci-layout-fixture', aud: 'apf-ci-layout-only' };
  const encode = value => Buffer.from(JSON.stringify(value)).toString('base64url');
  // Deliberately unsigned, local-only UI input. This is not authentication proof.
  const accessToken = `${encode({ alg: 'none', typ: 'JWT' })}.${encode(claims)}.ci-layout-only`;
  return { kind, user, accessToken };
}

export function accountLayoutFixtureResponse(request, fixture) {
  const url = new URL(request.url);
  if (url.origin !== accountLayoutFixtureOrigin || url.search || url.hash) return null;
  if (request.method === 'POST' && url.pathname === '/api/auth/refresh-token') {
    return fixture.kind === 'expired'
      ? { status: 401, body: { status: 401, title: 'Expired isolated layout session' } }
      : { status: 200, body: { accessToken: fixture.accessToken } };
  }
  if (request.method === 'GET' && url.pathname === `/api/users/${fixture.user.id}` && fixture.kind !== 'expired') {
    return { status: 200, body: { ...fixture.user, roles: [...fixture.user.roles] } };
  }
  // No generic fallback, account creation, mutation, real cookie or network call.
  return null;
}
