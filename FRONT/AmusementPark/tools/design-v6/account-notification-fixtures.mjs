// Synthetic private UI input, used only by the denied-network CI browser.
const parkId = '00000000-0000-4000-8000-000000000050';
const otherParkId = '00000000-0000-4000-8000-000000000051';

export function createAccountNotificationFixture(kind, date) {
  const notifications = kind !== 'standard' ? [] : Array.from({ length: 13 }, (_, index) => ({
    notificationId: `00000000-0000-4000-8000-${String(100 + index).padStart(12, '0')}`,
    eventType: index % 2 ? 'ParkNameChanged' : 'TicketPricePublishedOrChanged',
    status: index % 2 ? 'Read' : 'Delivered',
    noticeKind: ['Update', 'Correction', 'Superseded', 'Retraction'][index % 4],
    factualStatus: index % 4 === 3 ? 'Retracted' : index % 4 === 1 ? 'Corrected' : 'Published',
    lifecycleAtUtc: index % 4 ? date : null, retractionReasonCode: null,
    target: { type: index % 2 ? 'ParkItem' : 'Park', targetId: `00000000-0000-4000-8000-${String(200 + index).padStart(12, '0')}`,
      parkId: index % 2 ? otherParkId : parkId, name: index === 3 ? null : 'Lieu de recette uniquement — titre long Äéł',
      parentParkName: 'Parc de recette uniquement', mainImageId: null },
    previousValue: { kind: 'Text', canonicalValue: 'Valeur de recette précédente', unitCode: null },
    newValue: { kind: 'Text', canonicalValue: 'Valeur de recette corrigée', unitCode: null },
    source: { type: 'Official', publisherName: 'Source de recette uniquement', title: null, url: null,
      publishedAtUtc: null, verifiedAtUtc: date },
    occurredAtUtc: date, deliveredAtUtc: date, readAtUtc: index % 2 ? date : null,
    expiresAtUtc: '2026-12-31T23:59:59Z', version: 1, isReportedMisleading: index === 2,
    canManageSubscription: index === 0, subscriptionVersion: index === 0 ? 1 : null,
  }));
  const emailPreference = { emailDigestEnabled: false, emailAvailable: kind === 'standard',
    maskedEmail: kind === 'standard' ? 'l***@apf-fixture.invalid' : null,
    consentTextVersion: 'ci-ui-only', consentGrantedAtUtc: null, revokedAtUtc: null, version: 1 };
  const liveDashboard = { subscriptions: [], notifications: [], unreadCount: 0, retentionDays: 1 };
  if (kind === 'standard') {
    const target = { targetId: '00000000-0000-4000-8000-000000000250', parkId,
      targetName: 'Attraction de recette uniquement — titre long Äéł', parkName: 'Parc de recette uniquement', mainImageId: null,
      type: 'Reopened', thresholdMinutes: null, expiresAtUtc: '2026-12-31T23:59:59Z', version: 1 };
    liveDashboard.subscriptions.push({ ...target, subscriptionId: '00000000-0000-4000-8000-000000000350',
      createdAtUtc: date, lastObservedAtUtc: date, lastStatus: 'Open', lastWaitMinutes: null, cooldownMinutes: 15, hysteresisMinutes: 5 });
    liveDashboard.notifications.push({ ...target, notificationId: '00000000-0000-4000-8000-000000000450',
      subscriptionId: liveDashboard.subscriptions[0].subscriptionId, previousStatus: 'Down', currentStatus: 'Open',
      previousWaitMinutes: null, currentWaitMinutes: null, sourceId: 'ci-ui-only', sourceName: 'Source de recette uniquement',
      attributionText: null, attributionUrl: null, observedAtUtc: date, deliveredAtUtc: date, ageSecondsAtDelivery: 30,
      status: 'Delivered', readAtUtc: null });
    liveDashboard.unreadCount = 1;
  }
  return { notifications, emailPreference, liveDashboard };
}

export function accountNotificationFixtureResponse(request, url, fixture) {
  if (fixture.kind === 'expired') return null;
  const data = fixture.notificationData;
  if (request.method === 'POST' && url.pathname === '/api/me/notifications/pilot-interactions' && !url.search) {
    // This automatic page-open telemetry is consumed locally, never forwarded.
    // No SourceOpened, report, read, dismiss, subscription or preference write.
    let body;
    try { body = JSON.parse(request.postData ?? ''); } catch { return null; }
    if (!body || Object.keys(body).sort().join(',') !== 'interactionKind,notificationId'
        || body.interactionKind !== 'NotificationCenterOpened' || body.notificationId !== null) return null;
    return { status: 200, body: null };
  }
  if (request.method !== 'GET') return null;
  if (!url.search && url.pathname === '/api/me/notification-preferences') return { status: 200, body: structuredClone(data.emailPreference) };
  if (!url.search && url.pathname === '/api/me/live-alerts') return { status: 200, body: structuredClone(data.liveDashboard) };
  if (url.pathname !== '/api/me/notifications') return null;
  const query = url.search;
  const allowedQueries = new Set(['?page=1&size=12&unreadOnly=false', '?page=2&size=12&unreadOnly=false',
    '?page=1&size=12&unreadOnly=true', `?page=1&size=12&unreadOnly=false&parkId=${parkId}`,
    '?page=1&size=12&unreadOnly=false&eventType=ParkNameChanged',
    '?page=1&size=12&unreadOnly=false&eventType=VerifiedSourceAdded']);
  if (!allowedQueries.has(query)) return null;
  if (url.searchParams.get('eventType') === 'VerifiedSourceAdded') return { status: 503, body: { title: 'Intentional isolated UI error state' } };
  const page = Number(url.searchParams.get('page'));
  const items = data.notifications.filter(item => (url.searchParams.get('unreadOnly') !== 'true' || item.status === 'Delivered')
    && (!url.searchParams.has('parkId') || item.target.parkId === url.searchParams.get('parkId'))
    && (!url.searchParams.has('eventType') || item.eventType === url.searchParams.get('eventType')));
  return { status: 200, body: { items: structuredClone(items.slice((page - 1) * 12, page * 12)), page, pageSize: 12,
    totalItems: items.length, totalPages: Math.ceil(items.length / 12), unreadCount: data.notifications.filter(item => item.status === 'Delivered').length,
    retentionDays: 365, parkFilters: [{ parkId, parkName: 'Parc de recette uniquement — nom long Äéł' },
      { parkId: otherParkId, parkName: 'Autre parc de recette uniquement' }] } };
}
