// Synthetic browser-acceptance input only. Never imported by Angular, never
// forwarded to a server, and not evidence of scoring or authorization correctness.
import { isDeepStrictEqual } from 'node:util';

export const publicFitLayoutDate = '2026-12-20';
export function publicFitLayoutRequest() {
  return { evaluationDate: publicFitLayoutDate,
    members: [{ heightCentimeters: 120, minimumAgeYears: 9, maximumAgeYears: 9,
      canBeAccompanied: false, companionMinimumAgeYears: null, companionMaximumAgeYears: null }],
    preferredAttractionTypes: [], preferIndoor: false, countryCode: null,
    originLatitude: null, originLongitude: null, unknownDataPolicy: 'KeepWithWarning', maximumResults: 10 };
}

export function createPublicFitLayoutFixture(date) {
  const parks = ['Available', 'Capped', 'Suspended', 'Available', 'Available', 'Excluded'].map((scoreState, index) => ({
    parkId: `00000000-0000-4000-8000-00000000300${index}`,
    parkName: `Parc fictif CI ${index + 1} — nom long Äéł pour la recette uniquement`,
    countryCode: index === 4 ? null : 'FR', parkType: 'ThemePark', scoreState,
    comparativeScore: scoreState === 'Suspended' || scoreState === 'Excluded' ? null : 85 - index * 5,
    rawKnownScore: scoreState === 'Suspended' || scoreState === 'Excluded' ? null : 85 - index * 5,
    coveragePercent: index === 1 ? 65 : 90, knownWeightPercent: index === 1 ? 65 : 90,
    scoreCeilingPercent: scoreState === 'Capped' ? 80 : null, confidence: index === 1 ? 'Medium' : 'High',
    dateAvailabilityState: index === 2 ? 'Unknown' : 'Available',
    calendarState: index === 2 ? 'CalendarNotPublished' : 'OpenConfirmed',
    openingTimeRanges: index === 2 ? [] : [{ opensAt: '10:00', closesAt: '19:00', closesNextDay: false,
      lastAdmissionAt: '18:00', lastAdmissionNextDay: false }],
    calendarTimeZoneId: index === 2 ? null : 'Europe/Paris',
    calendarSourceUrl: index === 2 ? null : 'https://apf-fixture.invalid/ci-calendar',
    calendarLastVerifiedAtUtc: index === 2 ? null : date,
    distanceKilometers: null, distanceMethod: null, distanceEvaluatedAtUtc: null,
    unknownCount: index === 1 ? 3 : 0, everyoneTogetherAttractionCount: 12 - index,
    splitRequiredAttractionCount: index, partialAttractionCount: 0,
    noCompatibleMemberAttractionCount: 1, unknownAttractionCount: index === 1 ? 2 : 0,
    dataQualityStatus: index === 2 ? 'Suspended' : 'EligibleForFitComparison',
    dataQualityCoveragePercent: index === 1 ? 65 : 90, lastVerifiedAtUtc: index === 2 ? null : date,
    reasons: scoreState === 'Capped' ? ['IncompleteCoverageCapApplied']
      : scoreState === 'Suspended' ? ['CriticalDataSuspended'] : ['ScoreAvailable'],
    components: ['GroupCompatibility', 'PreferenceCoverage', 'IndoorResilience', 'BudgetFit'].map((kind, componentIndex) => ({
      kind, state: componentIndex === 3 ? 'Unknown' : 'Known', value: componentIndex === 3 ? null : 90 - index * 5,
      coveragePercent: componentIndex === 3 ? 0 : 90, confidence: componentIndex === 3 ? 'Unknown' : 'High',
      baseWeightPercent: 25, applicableWeightPercent: 25,
      knownScoreWeightPercent: componentIndex === 3 ? null : 25,
      contribution: componentIndex === 3 ? null : 20, reasons: componentIndex === 3 ? ['NoKnownFact'] : ['KnownFactsNormalized'] })),
    memberSummaries: [{ memberNumber: 1, compatibleAloneAttractionCount: 12 - index,
      compatibleWithCompanionAttractionCount: 0, incompatibleAttractionCount: index,
      unknownAttractionCount: index === 1 ? 2 : 0, notApplicableAttractionCount: 0 }],
    criticalSources: index === 2 ? [] : [{ kind: 'Official', url: 'https://apf-fixture.invalid/ci-source',
      reference: 'CI-ONLY-NOT-A-REAL-SOURCE', languageCode: 'fr', collectedAtUtc: date, verifiedAtUtc: date,
      confidence: 'High', summaries: [{ languageCode: 'fr', value: 'Source fictive de recette CI uniquement — aucun fait public.' }] }],
  }));
  return { methodVersion: 'park-fit-2026-01', evaluationDate: publicFitLayoutDate, evaluatedAtUtc: date,
    totalCandidateCount: 6, inspectedCandidateCount: 6, qualityEligibleCandidateCount: 4,
    qualityRejectedCandidateCount: 1, operationallySuspendedCandidateCount: 1, notActivatedCandidateCount: 0,
    candidatePoolTruncated: false, qualityStatusCounts: {}, qualityIssueCounts: {}, parks };
}

function validTelemetry(body) {
  const exact = value => isDeepStrictEqual(body, value);
  if (exact({ eventKind: 'SearchStarted' }) || exact({ eventKind: 'ExplanationViewed' })) return true;
  if (['Two', 'Four'].some(comparisonSize => exact({ eventKind: 'ComparisonOpened', comparisonSize }))) return true;
  return ['UnderHalfSecond', 'UnderOneAndHalfSeconds', 'UnderThreeSeconds', 'ThreeSecondsOrMore'].some(durationBand =>
    exact({ eventKind: 'SearchCompleted', resultBand: 'FiveOrMore', unknownLevel: 'Significant', durationBand,
      methodVersion: 'park-fit-2026-01', qualityIssues: [] }));
}

export function publicFitLayoutFixtureResponse(request, url, fixture) {
  if (!['standard', 'incomplete'].includes(fixture.kind) || url.origin !== 'https://apf-account-layout.invalid'
      || url.search || url.hash || request.method !== 'POST' || typeof request.postData !== 'string'
      || request.postData.length > 2048) return null;
  let body;
  try { body = JSON.parse(request.postData); } catch { return null; }
  if (url.pathname === '/api/public/park-fit/search' && isDeepStrictEqual(body, publicFitLayoutRequest())) {
    return { status: 200, body: structuredClone(fixture.publicFitLayoutData) };
  }
  if (url.pathname === '/api/public/park-fit/pilot-events' && validTelemetry(body)) return { status: 200, body: null };
  // No reports, saved criteria, arbitrary telemetry, geolocation, other search or forwarding.
  return null;
}
