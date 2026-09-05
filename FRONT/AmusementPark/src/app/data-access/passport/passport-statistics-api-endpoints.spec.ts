import { PASSPORT_STATISTICS_API_ENDPOINTS } from './passport-statistics-api-endpoints';

describe('PASSPORT_STATISTICS_API_ENDPOINTS', () => {
  it('encodes private statistic scope identifiers', () => {
    expect(PASSPORT_STATISTICS_API_ENDPOINTS.global).toBe('me/passport/stats');
    expect(PASSPORT_STATISTICS_API_ENDPOINTS.item('item/one')).toBe('me/passport/items/item%2Fone/stats');
    expect(PASSPORT_STATISTICS_API_ENDPOINTS.park('park/one')).toBe('me/passport/parks/park%2Fone/stats');
    expect(PASSPORT_STATISTICS_API_ENDPOINTS.year(2026)).toBe('me/passport/years/2026/stats');
  });
});
