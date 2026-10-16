// Synthetic UI-only history, never published or imported by Angular.
export const historyLayoutParkId = '00000000-0000-4000-8000-000000002000';
export const historyLayoutSubjectId = '00000000-0000-4000-8000-000000002001';
const parkName = 'Parc historique de recette';
const subjectName = 'Attraction historique de recette';
export const historyLayoutParkSlug = 'parc-historique-de-recette';

export function historyLayoutDocuments(language) {
  const prefix = `/${language}/park/${historyLayoutParkId}/${historyLayoutParkSlug}/history`;
  return [prefix, `${prefix}/page/2`, `${prefix}/2000`, `${prefix}/2000?month=1&day=1`,
    `${prefix}/compare/2000/2002`, `${prefix}/compare/2001/2002`,
    `/${language}/history/lineages/${historyLayoutParkId}/ParkItem/${historyLayoutSubjectId}/attraction-historique-de-recette`];
}

export function createHistoryLayoutFixture(kind, date) {
  const filled = kind === 'standard';
  const period = { start: { year: 2000, precision: 'Year' }, end: { year: 2002, precision: 'Year' },
    startConfidence: 'Confirmed', endConfidence: 'Confirmed' };
  const source = { type: 'Official', title: 'Référence fictive réservée à la recette CI', publisherOrAuthor: 'Source de recette uniquement',
    url: null, bibliographicReference: 'CI-UI-ONLY', publishedOn: null, accessedOn: date.slice(0, 10), languageCode: 'fr', archiveUrl: null, accessibility: 'Public' };
  const events = !filled ? [] : Array.from({ length: 26 }, (_, index) => ({ subjectType: 'ParkItem', subjectId: historyLayoutSubjectId,
    subjectLabel: `${subjectName} — repère ${index + 1}`, currentSubjectName: subjectName, hasPublishedLineage: true, factType: 'Opening',
    period: { ...structuredClone(period), start: { year: index < 25 ? 2000 : 2002, precision: 'Year' } },
    evidenceState: 'Verified', importance: 'Major', uncertaintyExplanations: [], sources: [structuredClone(source)], narrative: null }));
  const subjects = !filled ? [] : [['Park', 'KnownOpen', parkName], ['ParkItem', 'KnownOpen', subjectName],
    ['ParkItem', 'PossiblyOpen', 'Lieu historique de recette — période partielle'], ['ParkItem', 'Unknown', 'Lieu historique de recette — période inconnue'],
    ['ParkZone', 'KnownOpen', 'Zone historique de recette']].map(([subjectType, operationalState, displayName], index) => ({
    subjectType, subjectId: index === 0 ? historyLayoutParkId : index === 1 ? historyLayoutSubjectId : `ci-subject-${index}`, displayName,
    nameOrigin: 'Documented', operationalState, presenceExtent: 'WholePeriod', attributes: [], reasonCodes: [], supportingSourceCount: 1 }));
  const coverage = { totalSubjectCount: subjects.length, reliablePeriodSubjectCount: filled ? 3 : 0,
    partialPeriodSubjectCount: filled ? 1 : 0, undatedSubjectCount: filled ? 1 : 0,
    name: { documentedSubjectCount: subjects.length, applicableSubjectCount: subjects.length, percentage: filled ? 100 : 0, isComplete: filled },
    zone: { documentedSubjectCount: filled ? 1 : 0, applicableSubjectCount: subjects.length, percentage: filled ? 20 : 0, isComplete: false },
    lastReviewedAtUtc: date, status: 'Partial' };
  const compared = !filled ? [] : ['Opened', 'Closed', 'PresentAtBoth', 'PresentAtBoth', 'Uncertain'].map((presenceChange, index) => ({
    comparisonKey: `ci-comparison-${index}`, subjectType: 'ParkItem', displayName: `${subjectName} ${index + 1}`, nameOrigin: 'Documented',
    previousName: index === 2 ? 'Ancien nom de recette' : null, nextName: index === 2 ? subjectName : null,
    presenceChange, isRenamed: index === 2, isMoved: false, previousZoneName: null, nextZoneName: null,
    previousCategory: 'Attraction', nextCategory: 'Attraction', fromOperationalState: 'Unknown', toOperationalState: 'Unknown',
    fromSupportingSourceCount: 1, toSupportingSourceCount: 1 }));
  const root = { key: 'ci-root', type: 'ParkItem', label: subjectName, isHistoricalOnly: false };
  const successor = { key: 'ci-successor', type: 'HistoricalSubject', label: 'Successeur fictif de recette uniquement', isHistoricalOnly: true };
  const lineage = { root, contextPark: { id: historyLayoutParkId, name: parkName }, subjects: filled ? [root, successor] : [root],
    relations: filled ? [{ sourceKey: root.key, targetKey: successor.key, type: 'RenamedTo', direction: 'Directed', period,
      evidenceState: 'Verified', uncertaintyExplanations: [], sources: [source] }] : [], hasDirectedCycle: false, isTruncated: false, maximumDepth: 4 };
  return { events, subjects, coverage, compared, lineage, park: { id: historyLayoutParkId, name: parkName, isVisible: true } };
}

export function historyLayoutFixtureResponse(request, url, fixture) {
  if (fixture.kind === 'expired' || request.method !== 'GET') return null;
  const data = fixture.historyLayoutData;
  // The unchanged navigation-trail parser probes this nonexistent article on
  // a compare route. Preserve its catchError/null fallback, never invent an article.
  if (url.pathname === '/api/history/articles/compare' && !url.search) return { status: 404,
    body: { title: 'No article named compare in the isolated history fixture' } };
  if (url.pathname === `/api/parks/${historyLayoutParkId}/detail-summary` && !url.search) return { status: 200, body: { park: structuredClone(data.park) } };
  const prefix = `/api/public/parks/${historyLayoutParkId}/history`;
  if (url.pathname === `${prefix}/timeline` && ['?page=1&pageSize=25', '?page=2&pageSize=25'].includes(url.search)) {
    const page = Number(url.searchParams.get('page'));
    return { status: 200, body: { parkId: historyLayoutParkId, parkName, hasDecisionSnapshots: fixture.kind === 'standard',
      events: structuredClone(data.events.slice((page - 1) * 25, page * 25)), pagination: { currentPage: page, itemsPerPage: 25,
        totalItems: data.events.length, totalPages: Math.max(1, Math.ceil(data.events.length / 25)) } } };
  }
  if (url.pathname === `${prefix}/snapshot` && ['?year=2000', '?year=2000&month=1&day=1'].includes(url.search)) return { status: 200,
    body: { parkId: historyLayoutParkId, parkName, requestedInstant: { year: 2000, month: url.searchParams.has('month') ? 1 : null,
      day: url.searchParams.has('day') ? 1 : null, precision: url.searchParams.has('day') ? 'Day' : 'Year' }, subjects: structuredClone(data.subjects),
      coverage: structuredClone(data.coverage), ambiguities: [], methodologyVersion: 'ci-ui-only', isIndexableKeyYear: false } };
  if (url.pathname === `${prefix}/compare` && ['?fromYear=2000&toYear=2002', '?fromYear=2001&toYear=2002'].includes(url.search)) return { status: 200,
    body: { parkId: historyLayoutParkId, parkName, fromInstant: { year: Number(url.searchParams.get('fromYear')), precision: 'Year' },
      toInstant: { year: 2002, precision: 'Year' }, subjects: structuredClone(data.compared), categoryNetChanges: [],
      fromCoverage: structuredClone(data.coverage), toCoverage: structuredClone(data.coverage), fromUnclassifiedOpenItemCount: 0,
      toUnclassifiedOpenItemCount: 0, isCategoryComparisonComplete: false, methodologyVersion: 'ci-ui-only' } };
  if (url.pathname === `/api/public/history/subjects/ParkItem/${historyLayoutSubjectId}/lineage` && url.search === `?contextParkId=${historyLayoutParkId}`) return { status: 200, body: structuredClone(data.lineage) };
  return null;
}
