import { PASSPORT_VISITS_API_ENDPOINTS } from './passport-visits-api-endpoints';

describe('PASSPORT_VISITS_API_ENDPOINTS', () => {
  it('keeps the owner-scoped visit creation route stable', () => {
    expect(PASSPORT_VISITS_API_ENDPOINTS.create).toBe('me/passport/visits');
  });

  it('encodes the visit identifier in the owner-scoped detail route', () => {
    expect(PASSPORT_VISITS_API_ENDPOINTS.getById('visit/one')).toBe('me/passport/visits/visit%2Fone');
  });
});
