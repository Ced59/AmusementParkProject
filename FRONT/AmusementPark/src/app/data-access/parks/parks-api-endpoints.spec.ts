import { PARKS_API_ENDPOINTS } from './parks-api-endpoints';

describe('PARKS_API_ENDPOINTS', () => {
  it('builds paginated park urls with visibility, region and admin filters', () => {
    const endpoint: string = PARKS_API_ENDPOINTS.getParksPaginated(2, 25, true, 'europe', {
      isVisible: false,
      adminReviewStatus: 'ToReview',
      type: 'ThemePark',
      countryCode: ' be ',
      hasValidCoordinates: true
    });

    expect(endpoint).toBe('parks?page=2&size=25&visibleOnly=true&region=europe&isVisible=false&adminReviewStatus=ToReview&type=ThemePark&countryCode=be&hasValidCoordinates=true');
  });

  it('omits empty optional filters', () => {
    const endpoint: string = PARKS_API_ENDPOINTS.getParksPaginated(1, 10, false, null, {
      isVisible: null,
      adminReviewStatus: null,
      type: null,
      countryCode: '  '
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
