// CI-only private statistics/share input. Never served by a real API.
export const accountComparisonFixtureToken = 'APF-CI-UI-NOT-A-REAL-COMPARISON-TOKEN';

export function createAccountPassportStatisticsFixture(kind, date) {
  const filled = kind === 'standard';
  const count = filled ? 2 : 0;
  const parkId = '00000000-0000-4000-8000-000000000050';
  const itemId = '00000000-0000-4000-8000-000000000600';
  const visitId = '00000000-0000-4000-8000-000000000071';
  const parkName = 'Parc de recette uniquement — nom long Äéł';
  const itemName = 'Attraction de recette uniquement — nom long Äéł';
  const visitDate = { year: 2026, month: 9, day: 3, precision: 'Day', isApproximate: false };
  const distribution = filled ? { ratingCount: 2, average: 4, median: 4, minimum: 3, maximum: 5, populationStandardDeviation: 1 } : null;
  const coverage = { ratedCount: count, totalCount: count, rate: filled ? 1 : 0 };
  const summary = { visitCount: count, approximateVisitCount: 0, parkRatingCoverage: { ...coverage }, historicalParkRatings: distribution,
    firstVisit: filled ? { visitId, parkId, date: visitDate } : null, lastVisit: filled ? { visitId, parkId, date: visitDate } : null,
    rideOutcomes: { recordedOutcomeCount: count, completedRideCount: count, attemptedCount: 0, missedClosedCount: 0,
      missedUnavailableCount: 0, skippedByChoiceCount: 0 }, rideRatingCoverage: { ...coverage }, historicalRideRatings: distribution,
    distinctCompletedItemCount: filled ? 1 : 0, repeatedCompletedItemCount: filled ? 1 : 0,
    categoryCoverage: filled ? [{ category: 'Attraction', completedRideCount: count, distinctItemCount: 1,
      historicalReferenceRideCount: count, currentReferenceRideCount: 0, unknownReferenceRideCount: 0, completedRideRate: 1 }] : [] };
  const global = { selectedYear: null, selectedParkId: null, availableYears: filled ? [2026] : [],
    availableParks: filled ? [{ parkId, parkName }] : [], parkCount: filled ? 1 : 0, summary,
    activityByYear: filled ? [{ year: 2026, visitCount: count, recordedRideCount: count }] : [],
    topParks: filled ? [{ parkId, parkName, visitCount: count, recordedRideCount: count }] : [],
    topItems: filled ? [{ parkItemId: itemId, parkItemName: itemName, parkId, parkName, completedRideCount: count }] : [],
    ratingEvolution: filled ? [{ year: 2026, parkAverage: 4, ratedVisitCount: count, rideAverage: 4, ratedRideCount: count }] : [] };
  const history = { visitCount: count, firstVisitYear: filled ? 2026 : null, completedRideCount: count,
    canonicallyResolvedRideCount: count, canonicalCoverageRate: filled ? 1 : 0, parkCountAcrossMultipleEras: filled ? 1 : 0,
    parksAcrossEras: filled ? [{ parkName, firstVisitYear: 2026, lastVisitYear: 2026, visitCount: count, canonicalEraCount: 2 }] : [],
    disappearedAttractions: filled ? [{ parkName, attractionName: itemName, firstVisitYear: 2026, lastVisitYear: 2026, completedRideCount: count }] : [],
    transformations: [], historicalNames: filled ? [{ parkName, nameAtVisit: 'Ancien nom de recette uniquement', currentName: itemName,
      firstVisitYear: 2026, lastVisitYear: 2026, completedRideCount: count }] : [],
    historicalCategories: filled ? [{ category: 'Attraction', completedRideCount: count, distinctAttractionCount: 1 }] : [] };
  const item = { parkItemId: itemId, parkItemName: filled ? itemName : null, rideCount: count, visitCount: count,
    ratingCoverage: { ratedRideCount: count, totalRideCount: count, rate: filled ? 1 : 0 },
    firstExperience: filled ? { visitId, date: visitDate } : null, lastExperience: filled ? { visitId, date: visitDate } : null,
    historicalRatings: distribution, currentGlobalRating: null, currentGlobalMinusHistoricalAverage: null,
    byVisit: [], byYear: [], ratingTimeline: [], trend: null };
  const park = { parkId, parkName: filled ? parkName : null, summary, currentGlobalRating: null, currentGlobalMinusHistoricalAverage: null,
    assessmentTimeline: [], byYear: filled ? [{ year: 2026, summary }] : [], currentTopItems: [], historicalTopItems: [] };
  const year = { year: 2026, parkCount: filled ? 1 : 0, summary, byPark: filled ? [{ parkId, parkName, summary }] : [] };
  const shareSettings = { isPublic: false, isModerationSuspended: false, includedFields: [], visibility: 'Unlisted' };
  const shareSelection = { years: filled ? [{ year: 2026, visitCount: count }] : [],
    parks: filled ? [{ parkId, name: parkName, countryCode: 'FR', visitCount: count }] : [],
    ratings: filled ? [{ selectionKey: 'ci-rating-only', parkId, name: itemName, parkName, rating: 4 }] : [],
    maximumSelectedYears: 10, maximumSelectedParks: 20, savedSelectedYears: [], savedSelectedParkIds: [], savedSelectedRatingKeys: [],
    savedPublicCaption: null, savedVisibility: 'Unlisted', savedAllowsComparisons: false, hasSavedSnapshot: false };
  const comparisons = filled ? [{ shareId: 'ci-comparison-only', otherDisplayName: 'Autre compte de recette uniquement — nom long Äéł',
    createdAtUtc: date, categories: ['VisitedParks', 'YearlyActivity'], isModerationSuspended: false }] : [];
  const invitation = { status: filled ? 'Ready' : 'Expired', creatorDisplayName: 'Compte de recette uniquement',
    inviteeDisplayName: 'Autre compte de recette uniquement', expiresAtUtc: '2026-12-31T23:59:59Z', acceptedAtUtc: null,
    categories: ['VisitedParks', 'YearlyActivity'], canAccept: filled };
  return { parkId, itemId, global, history, item, park, year, shareSettings, shareSelection, comparisons, invitation };
}

export function accountPassportStatisticsFixtureResponse(request, url, fixture) {
  if (fixture.kind === 'expired' || request.method !== 'GET') return null;
  const data = fixture.passportStatisticsData;
  if (url.pathname === '/api/me/passport/stats') {
    const allowedQueries = ['', '?year=2026', `?parkId=${data.parkId}`, `?year=2026&parkId=${data.parkId}`];
    if (!allowedQueries.includes(url.search)) return null;
    return { status: 200, body: { ...structuredClone(data.global), selectedYear: url.searchParams.has('year') ? 2026 : null,
      selectedParkId: url.searchParams.get('parkId') } };
  }
  if (url.search) return null;
  const pages = new Map([['/api/me/passport/stats/history', data.history], [`/api/me/passport/items/${data.itemId}/stats`, data.item],
    [`/api/me/passport/parks/${data.parkId}/stats`, data.park], ['/api/me/passport/years/2026/stats', data.year],
    ['/api/me/passport/share', data.shareSettings], ['/api/me/passport/share/selection', data.shareSelection],
    ['/api/me/passport/years/2026/share', data.shareSettings], ['/api/me/passport/years/2026/share/selection', { hasSavedSnapshot: false, savedPublicCaption: null }],
    ['/api/me/profile-comparisons', data.comparisons], [`/api/me/comparisons/invitations/${accountComparisonFixtureToken}/preview`, data.invitation]]);
  return pages.has(url.pathname) ? { status: 200, body: structuredClone(pages.get(url.pathname)) } : null;
}
