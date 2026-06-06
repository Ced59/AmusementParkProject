import { SEARCH_API_ENDPOINTS } from './search-api-endpoints';

describe('SEARCH_API_ENDPOINTS', () => {
  it('builds search urls with encoded query and pagination', () => {
    expect(SEARCH_API_ENDPOINTS.getSearch('parc astérix', [], 2, 24))
      .toBe('search?query=parc%20ast%C3%A9rix&page=2&pageSize=24');
  });

  it('adds comma-separated categories when provided', () => {
    expect(SEARCH_API_ENDPOINTS.getSearch('ride', ['parks', 'parkItems'], 1, 10))
      .toBe('search?query=ride&categories=parks,parkItems&page=1&pageSize=10');
  });
});
