import { PARKS_API_ENDPOINTS } from './parks-api-endpoints';

describe('PARKS_API_ENDPOINTS', () => {
  it('builds paginated park urls with visibility, region and admin filters', () => {
    const endpoint: string = PARKS_API_ENDPOINTS.getParksPaginated(2, 25, true, 'europe', {
      isVisible: false,
      adminReviewStatus: 'ToReview',
      type: 'ThemePark',
      audienceClassification: 'National',
      countryCode: ' be ',
      hasValidCoordinates: true,
      openingHoursStatus: 'needsUpdate'
    });

    expect(endpoint).toBe('parks?page=2&size=25&visibleOnly=true&region=europe&isVisible=false&adminReviewStatus=ToReview&type=ThemePark&audienceClassification=National&countryCode=be&hasValidCoordinates=true&openingHoursStatus=needsUpdate');
  });

  it('omits empty optional filters', () => {
    const endpoint: string = PARKS_API_ENDPOINTS.getParksPaginated(1, 10, false, null, {
      isVisible: null,
      adminReviewStatus: null,
      type: null,
      countryCode: '  ',
      openingHoursStatus: 'all'
    });

    expect(endpoint).toBe('parks?page=1&size=10');
  });

  it('encodes search and filter values', () => {
    const endpoint: string = PARKS_API_ENDPOINTS.searchParks('walibi belgium', 1, 12, true, 'north-america', {
      countryCode: 'US',
      type: 'ThemePark'
    });

    expect(endpoint).toBe('parks?page=1&size=12&query=walibi%20belgium&visibleOnly=true&region=north-america&type=ThemePark&countryCode=US');
  });

  it('builds visible map point urls without a dangling question mark', () => {
    expect(PARKS_API_ENDPOINTS.getVisibleParkMapPoints()).toBe('parks/map-visible');
    expect(PARKS_API_ENDPOINTS.getVisibleParkMapPoints(' parc ', 'europe')).toBe('parks/map-visible?query=%20parc%20&region=europe');
    expect(PARKS_API_ENDPOINTS.getVisibleParkMapPoints(null, null, 'closedOnly')).toBe('parks/map-visible?closedFilter=closedOnly');
    expect(PARKS_API_ENDPOINTS.getVisibleParkMapPoints(null, null, null, 'Unspecified')).toBe('parks/map-visible?audienceClassification=Unspecified');
  });

  it('adds closed filters only when they differ from the default public scope', () => {
    expect(PARKS_API_ENDPOINTS.getParksPaginated(1, 10, true, null, null, null, 'openOnly')).toBe('parks?page=1&size=10&visibleOnly=true');
    expect(PARKS_API_ENDPOINTS.getParksPaginated(1, 10, true, null, null, null, 'all')).toBe('parks?page=1&size=10&visibleOnly=true&closedFilter=all');
    expect(PARKS_API_ENDPOINTS.getParkMapItems('park-1', 'closedOnly')).toBe('parks/park-1/map-items?closedFilter=closedOnly');
    expect(PARKS_API_ENDPOINTS.getParkExplorer('park-1', 'all')).toBe('park-zones/park/park-1/explorer?closedFilter=all');
  });

  it('adds admin sort parameters when a list sort is selected', () => {
    expect(PARKS_API_ENDPOINTS.getParksPaginated(1, 10, false, null, null, { sortBy: 'parkItemsTotalCount', sortDirection: 'desc' }))
      .toBe('parks?page=1&size=10&sortBy=parkItemsTotalCount&sortDirection=desc');
    expect(PARKS_API_ENDPOINTS.searchParks('parc', 1, 10, false, null, null, { sortBy: 'parkItemsVisibleCount', sortDirection: 'asc' }))
      .toBe('parks?page=1&size=10&query=parc&sortBy=parkItemsVisibleCount&sortDirection=asc');
    expect(PARKS_API_ENDPOINTS.getParksPaginated(1, 10, false, null, null, { sortBy: 'openingHoursStatus', sortDirection: 'desc' }))
      .toBe('parks?page=1&size=10&sortBy=openingHoursStatus&sortDirection=desc');
  });

  it('builds opening hours urls with optional date bounds', () => {
    expect(PARKS_API_ENDPOINTS.getParkOpeningHours('park 1', '2026-07-01', '2026-07-31'))
      .toBe('parks/park%201/opening-hours?from=2026-07-01&to=2026-07-31');
    expect(PARKS_API_ENDPOINTS.getParkOpeningHours('park 1')).toBe('parks/park%201/opening-hours');
    expect(PARKS_API_ENDPOINTS.getAdminParkOpeningHours('park 1')).toBe('admin/parks/park%201/opening-hours');
    expect(PARKS_API_ENDPOINTS.upsertAdminParkOpeningHours('park 1')).toBe('admin/parks/park%201/opening-hours');
  });

  it('adds displayed forecast dates to historical weather comparison urls', () => {
    expect(PARKS_API_ENDPOINTS.getParkWeatherHistoricalComparisons('park 1', 2, 5, ['2026-06-20', '2026-06-21']))
      .toBe('parks/park%201/weather/historical-comparisons?days=2&years=5&forecastDates=2026-06-20%2C2026-06-21');
  });

  it('builds distance urls with encoded ids and no empty query when targets are missing', () => {
    expect(PARKS_API_ENDPOINTS.getParkDistances('source park', ['target 1', 'target/2']))
      .toBe('parks/source%20park/distances?targetParkIds=target%201&targetParkIds=target%2F2');
    expect(PARKS_API_ENDPOINTS.getParkDistances('source', [])).toBe('parks/source/distances');
  });

  it('builds nearby urls and only includes finite max distance', () => {
    expect(PARKS_API_ENDPOINTS.getNearestParks('source', 3, 120.5)).toBe('parks/source/nearby?limit=3&maxDistanceKilometers=120.5');
    expect(PARKS_API_ENDPOINTS.getNearestParks('source', 3, Number.NaN)).toBe('parks/source/nearby?limit=3');
  });
});
