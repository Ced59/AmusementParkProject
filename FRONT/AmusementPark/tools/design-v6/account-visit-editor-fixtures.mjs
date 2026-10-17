// CI-only visit states. No persisted write or real park/private API call.
export function createAccountVisitEditorFixture(kind, date, visits) {
  const filled = kind === 'standard';
  const templates = filled ? visits.slice(0, 2) : [{ id: '00000000-0000-4000-8000-000000000070',
    parkId: '00000000-0000-4000-8000-000000000050', parkName: null,
    date: { year: 2026, month: null, day: null, precision: 'Year', isApproximate: true }, timeZoneId: null,
    serviceDayConvention: 'VisitStartLocalDate', status: 'Archived', privacy: 'Private',
    title: 'Visite de recette archivée uniquement — titre long Äéł', privateNote: null, hasPrivateNote: false,
    version: 1, createdAtUtc: date, updatedAtUtc: date, completedAtUtc: null }];
  return templates.map((template, visitIndex) => {
    const visit = structuredClone(template);
    const occurrences = !filled ? [] : [0, 1, 2].map(index => ({
      id: `00000000-0000-4000-8000-${String(1000 + visitIndex * 10 + index).padStart(12, '0')}`,
      visitId: visit.id, parkId: visit.parkId, parkItemId: `00000000-0000-4000-8000-${String(1100 + index).padStart(12, '0')}`,
      sortPosition: index, moment: { localTime: null, isApproximate: true }, status: index === 1 ? 'MissedClosed' : 'Completed',
      source: 'Manual', historicalConsistency: index === 1 ? 'Unverified' : 'Verified', historicalConflictConfirmed: false,
      privateNote: index === 0 ? 'Note privée de recette uniquement' : null, countsAsRide: index !== 1, version: 1,
      createdAtUtc: date, updatedAtUtc: date,
      target: { name: 'Attraction de recette uniquement — nom long Äéł', category: 'Attraction', lifecycleStatus: null,
        isHistoricalSnapshot: true, isResolved: true, openingDate: null, closingDate: null },
      assessment: index === 0 ? { value: 4, privateComment: null, revision: 1, createdAtUtc: date, updatedAtUtc: date } : null,
    }));
    const targets = !filled ? [] : occurrences.slice(0, 2).map(item => ({ parkItemId: item.parkItemId, name: item.target.name,
      category: 'Attraction', operationalState: 'KnownOpen', historicalConsistency: item.historicalConsistency,
      isHistoricalOnly: true, mainImageId: null, zoneId: null, lifecycleStatus: null, openingDate: null, closingDate: null }));
    const reports = filled ? [{ reportId: `ci-history-report-${visitIndex}`, parkId: visit.parkId,
      parkName: 'Parc de recette uniquement', visitDate: structuredClone(visit.date),
      claimedName: 'Signalement de recette uniquement — nom long Äéł', sourceUrl: null, sourceReference: 'CI-UI-ONLY',
      details: null, status: 'Pending', submittedAtUtc: date, reviewedAtUtc: null, decisionNote: null, revision: 1 }] : [];
    return { visit, occurrences, targets, reports, parentAvailable: filled };
  });
}

export function accountVisitEditorFixtureResponse(request, url, fixture) {
  if (fixture.kind === 'expired' || request.method !== 'GET') return null;
  const all = fixture.visitEditorData;
  const parkId = all[0].visit.parkId;
  if (url.pathname === `/api/parks/${parkId}` && !url.search) return fixture.kind === 'standard'
    ? { status: 200, body: { id: parkId, name: 'Parc de recette uniquement — nom long Äéł' } }
    : { status: 404, body: { title: 'Intentional missing parent in isolated visit UI' } };
  if (url.pathname === `/api/park-zones/park/${parkId}` && url.search === '?page=1&size=100') return { status: 200, body: [] };
  for (const data of all) {
    const prefix = `/api/me/passport/visits/${data.visit.id}`;
    if (!url.search && url.pathname === prefix) return { status: 200, body: structuredClone(data.visit) };
    if (!url.search && url.pathname === `${prefix}/historical-existence-reports`) return { status: 200, body: structuredClone(data.reports) };
    if (!url.search && url.pathname === `${prefix}/deletion-preview`) return { status: 200, body: {
      visitId: data.visit.id, expectedVersion: 1, occurrenceCount: data.occurrences.length,
      assessmentCount: data.occurrences.filter(item => item.assessment).length, retentionDays: 30 } };
    if (!url.search && url.pathname === `${prefix}/share` && data.visit.status === 'Completed') return { status: 200,
      body: { isPublic: false, isModerationSuspended: false, includedFields: [] } };
    if (url.pathname === `${prefix}/occurrences`) {
      if (url.search === '?limit=50') return { status: 200, body: { items: structuredClone(data.occurrences.slice(0, 2)),
        nextCursor: data.occurrences.length > 2 ? 'apf-editor-next' : null } };
      if (url.search === '?limit=50&cursor=apf-editor-next' && data.occurrences.length > 2) return { status: 200,
        body: { items: structuredClone(data.occurrences.slice(2)), nextCursor: null } };
    }
    if (url.pathname === `${prefix}/historical-ride-targets`
        && ['?page=1&pageSize=24&scope=KnownOpen', '?page=1&pageSize=24&scope=PossiblyOpen', '?page=1&pageSize=24&scope=AllHistory'].includes(url.search)) {
      const items = url.searchParams.get('scope') === 'PossiblyOpen' ? [] : data.targets;
      return { status: 200, body: { items: structuredClone(items), currentPage: 1, pageSize: 24, totalItems: items.length,
        totalPages: items.length ? 1 : 0, knownOpenCount: data.targets.length, possiblyOpenCount: 0, allHistoryCount: data.targets.length,
        coverageStatus: 'Partial', coveragePercent: 50, methodologyVersion: 'ci-ui-only' } };
    }
  }
  return null;
}
