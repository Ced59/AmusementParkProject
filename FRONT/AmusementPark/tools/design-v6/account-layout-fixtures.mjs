// CI-only layout fixtures. Never imported by Angular or sent to a real server.
import { homeEvidenceApiPaths } from './home-network-evidence.mjs';
import { createAccountNotificationFixture, accountNotificationFixtureResponse } from './account-notification-fixtures.mjs';
import { createAccountTripFixture, accountTripFixtureResponse } from './account-trip-fixtures.mjs';
import { createAccountPassportStatisticsFixture, accountPassportStatisticsFixtureResponse } from './account-passport-statistics-fixtures.mjs';
import { createAccountVisitEditorFixture, accountVisitEditorFixtureResponse } from './account-visit-editor-fixtures.mjs';
export const accountLayoutFixtureOrigin = 'https://apf-account-layout.invalid';
export const accountLayoutRefreshMarker = 'amusementpark.hasRefreshSession';
export const accountLayoutResetQuery = '?token=APF-CI-UI-NOT-A-REAL-RESET-TOKEN';
const userId = '00000000-0000-4000-8000-000000000017';
const languages = ['fr', 'en', 'de', 'nl', 'it', 'es', 'pt', 'pl'];

export function accountLayoutCases(widths) {
  if (!Array.isArray(widths) || !widths.includes(390) || !widths.includes(1366) || widths.length > 13
      || new Set(widths).size !== widths.length || widths.some(width => !Number.isInteger(width) || width < 320 || width > 1920)) throw new Error('Account acceptance requires bounded widths including 390 and 1366.');
  return [
    ...['standard', 'incomplete', 'expired'].map(kind => ({ kind, language: 'fr', widths: [...widths] })),
    ...languages.slice(1).map(language => ({ kind: 'standard', language, widths: [390, 1366] })),
  ];
}

export function createAccountLayoutFixture(kind, nowSeconds, language = 'fr') {
  if (!['standard', 'incomplete', 'expired'].includes(kind) || !languages.includes(language) || !Number.isInteger(nowSeconds) || nowSeconds < 1) {
    throw new Error('Unknown or undated account layout fixture.');
  }
  const date = new Date(nowSeconds * 1000).toISOString();
  const user = { id: userId, email: 'layout-review@apf-fixture.invalid',
    firstName: kind === 'incomplete' ? '' : 'Camille', lastName: kind === 'incomplete' ? '' : 'Démonstration',
    publicDisplayName: kind === 'incomplete' ? null : 'Compte de recette — caractères longs Äéł',
    publicIdentifier: 'layout-review', isActivated: true, isBlocked: false, roles: ['USER'],
    preferredLanguage: language.toUpperCase(), preferredMeasurementSystem: 'Metric', avatarUrl: '', createdAt: date, updatedAt: date };
  const claims = { sub: user.id, nameidentifier: user.id, email: user.email, firstname: user.firstName,
    lastname: user.lastName, avatar: '', lastlogin: date, roles: [...user.roles], jti: 'apf-ci-layout-only',
    exp: nowSeconds + 3600, iss: 'urn:apf-ci-layout-fixture', aud: 'apf-ci-layout-only' };
  const encode = value => Buffer.from(JSON.stringify(value)).toString('base64url');
  // Deliberately unsigned, local-only UI input. This is not authentication proof.
  const accessToken = `${encode({ alg: 'none', typ: 'JWT' })}.${encode(claims)}.ci-layout-only`;
  const collections = kind !== 'standard' ? [] : ['Favorite', 'WantToVisit', 'Planned'].map((collectionKind, index) => ({
    entryId: `00000000-0000-4000-8000-00000000003${index}`, targetType: index === 1 ? 'ParkItem' : 'Park',
    targetId: `00000000-0000-4000-8000-00000000004${index}`, kind: collectionKind,
    targetStatus: index === 2 ? 'Unknown' : 'Available',
    targetName: index === 2 ? null : 'Lieu de recette uniquement — nom très long Äéł pour vérifier le retour à la ligne',
    parentParkId: index === 1 ? '00000000-0000-4000-8000-000000000050' : null,
    parentParkName: index === 1 ? 'Parc de recette uniquement' : null,
    mainImageId: null, privateNote: null, priority: null, preferredStartsOn: null, preferredEndsOn: null,
    createdAtUtc: date, updatedAtUtc: date, version: 1,
  }));
  const trips = kind !== 'standard' ? [] : ['None', 'Fixed', 'Range', 'Candidates'].map((proposalKind, index) => ({
    tripPlanId: `00000000-0000-4000-8000-00000000006${index}`,
    title: `Voyage de recette uniquement — titre long avec caractères Äéł (${proposalKind})`,
    dateProposal: { kind: proposalKind, startDate: ['Fixed', 'Range'].includes(proposalKind) ? '2026-12-20' : null,
      endDate: proposalKind === 'Range' ? '2026-12-22' : proposalKind === 'Fixed' ? '2026-12-20' : null,
      candidateDates: proposalKind === 'Candidates' ? ['2026-12-20', '2026-12-22'] : [] },
    destinationTimeZoneId: proposalKind === 'None' ? null : 'Europe/Paris', status: 'Draft', accessScope: 'Private',
    memberCount: 2, isOwner: true, effectiveRole: 'Owner', canEditPlan: true, canEditProgram: true,
    canInvite: true, canChangeRoles: true, createdAtUtc: date, updatedAtUtc: date, version: 1,
  }));
  const visits = kind !== 'standard' ? [] : ['Year', 'Month', 'Day'].map((precision, index) => ({
    id: `00000000-0000-4000-8000-00000000007${index}`, parkId: '00000000-0000-4000-8000-000000000050',
    parkName: index === 2 ? null : 'Parc de recette uniquement — nom long Äéł',
    date: { year: 2026, month: precision === 'Year' ? null : 9, day: precision === 'Day' ? 3 : null,
      precision, isApproximate: precision === 'Year' }, timeZoneId: precision === 'Day' ? 'Europe/Paris' : null,
    serviceDayConvention: 'VisitStartLocalDate', status: ['Draft', 'Completed', 'Archived'][index], privacy: 'Private',
    title: 'Visite de recette uniquement — sans donnée personnelle', privateNote: null, hasPrivateNote: index === 1,
    version: 1, createdAtUtc: date, updatedAtUtc: date, completedAtUtc: index === 1 ? date : null,
  }));
  const fitProfiles = kind !== 'standard' ? [] : [false, true].map((accompanied, index) => ({
    profileId: `00000000-0000-4000-8000-00000000008${index}`, alias: index ? 'Groupe de recette — Élise et Łukasz' : 'Profil de recette — données partielles',
    heightCentimeters: index ? 120 : null, ageYears: index ? 9 : null, canBeAccompanied: accompanied,
    companionAgeYears: accompanied ? 32 : null, createdAtUtc: date, updatedAtUtc: date, version: 1,
  }));
  return { kind, language, user, accessToken, collections, trips, visits, fitProfiles,
    notificationData: createAccountNotificationFixture(kind, date), tripData: createAccountTripFixture(kind, date, user, trips),
    passportStatisticsData: createAccountPassportStatisticsFixture(kind, date), visitEditorData: createAccountVisitEditorFixture(kind, date, visits) };
}

export function accountLayoutFixtureResponse(request, fixture, homeFixtures = new Map()) {
  const url = new URL(request.url);
  if (url.origin !== accountLayoutFixtureOrigin || url.hash) return null;
  const notificationResponse = accountNotificationFixtureResponse(request, url, fixture);
  if (notificationResponse) return notificationResponse;
  const tripResponse = accountTripFixtureResponse(request, url, fixture);
  if (tripResponse) return tripResponse;
  const passportStatisticsResponse = accountPassportStatisticsFixtureResponse(request, url, fixture);
  if (passportStatisticsResponse) return passportStatisticsResponse;
  const visitEditorResponse = accountVisitEditorFixtureResponse(request, url, fixture);
  if (visitEditorResponse) return visitEditorResponse;
  if (fixture.kind === 'expired' && request.method === 'GET' && homeEvidenceApiPaths.has(url.pathname)
      && homeFixtures instanceof Map && homeFixtures.size <= 16) {
    const record = homeFixtures.get(url.pathname + url.search);
    if (record?.status === 200 && record.contentType?.includes('application/json') && Buffer.isBuffer(record.bytes)
        && record.bytes.length <= 2097152) return { status: 200, body: JSON.parse(record.bytes.toString('utf8')) };
    return null;
  }
  if (request.method === 'GET' && fixture.kind !== 'expired' && url.pathname === '/api/me/passport/visits') {
    if (url.search === '?limit=20') return { status: 200, body: { items: structuredClone(fixture.visits.slice(0, 2)), nextCursor: fixture.visits.length > 2 ? 'apf-layout-next-page' : null } };
    if (url.search === '?limit=20&cursor=apf-layout-next-page' && fixture.visits.length > 2) return {
      status: 200, body: { items: structuredClone([fixture.visits[0], ...fixture.visits.slice(2)]), nextCursor: null },
    };
    return null;
  }
  if (url.search) return null;
  if (request.method === 'POST' && url.pathname === '/api/auth/refresh-token') {
    return fixture.kind === 'expired'
      ? { status: 401, body: { status: 401, title: 'Expired isolated layout session' } }
      : { status: 200, body: { accessToken: fixture.accessToken } };
  }
  if (request.method === 'GET' && url.pathname === `/api/users/${fixture.user.id}` && fixture.kind !== 'expired') {
    return { status: 200, body: { ...fixture.user, roles: [...fixture.user.roles] } };
  }
  if (request.method === 'GET' && fixture.kind !== 'expired' && ['/api/me/collections', '/api/me/trips'].includes(url.pathname)) {
    return { status: 200, body: structuredClone(url.pathname === '/api/me/collections' ? fixture.collections : fixture.trips) };
  }
  if (request.method === 'GET' && fixture.kind !== 'expired' && url.pathname === '/api/me/park-fit/group-profiles') {
    return { status: 200, body: structuredClone(fixture.fitProfiles) };
  }
  // No generic fallback, account creation, mutation, real cookie or network call.
  return null;
}
