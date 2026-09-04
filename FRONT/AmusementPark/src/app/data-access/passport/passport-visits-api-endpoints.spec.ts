import { PASSPORT_VISITS_API_ENDPOINTS } from './passport-visits-api-endpoints';

describe('PASSPORT_VISITS_API_ENDPOINTS', () => {
  it('keeps the owner-scoped visit creation route stable', () => {
    expect(PASSPORT_VISITS_API_ENDPOINTS.create).toBe('me/passport/visits');
  });

  it('encodes the visit identifier in the owner-scoped detail route', () => {
    expect(PASSPORT_VISITS_API_ENDPOINTS.getById('visit/one')).toBe('me/passport/visits/visit%2Fone');
  });

  it('encodes cursor pagination on the owner-scoped visit list', () => {
    expect(PASSPORT_VISITS_API_ENDPOINTS.list(20, null)).toBe('me/passport/visits?limit=20');
    expect(PASSPORT_VISITS_API_ENDPOINTS.list(20, 'next+page'))
      .toBe('me/passport/visits?limit=20&cursor=next%2Bpage');
  });

  it('encodes the optional park, year and status filters', () => {
    expect(PASSPORT_VISITS_API_ENDPOINTS.list(100, null, {
      parkId: 'park/one',
      year: 2026,
      status: 'Draft'
    })).toBe('me/passport/visits?limit=100&parkId=park%2Fone&year=2026&status=Draft');
  });

  it('keeps the park assessment nested under its owned visit', () => {
    expect(PASSPORT_VISITS_API_ENDPOINTS.assessment('visit/one'))
      .toBe('me/passport/visits/visit%2Fone/assessment');
  });

  it('keeps visit corrections owner-scoped and encodes the identifier', () => {
    expect(PASSPORT_VISITS_API_ENDPOINTS.update('visit/one')).toBe('me/passport/visits/visit%2Fone');
    expect(PASSPORT_VISITS_API_ENDPOINTS.complete('visit/one')).toBe('me/passport/visits/visit%2Fone/complete');
    expect(PASSPORT_VISITS_API_ENDPOINTS.reopen('visit/one')).toBe('me/passport/visits/visit%2Fone/reopen');
    expect(PASSPORT_VISITS_API_ENDPOINTS.archive('visit/one')).toBe('me/passport/visits/visit%2Fone/archive');
  });

  it('keeps deletion preview and mutation scoped to the encoded visit', () => {
    expect(PASSPORT_VISITS_API_ENDPOINTS.deletionPreview('visit/one'))
      .toBe('me/passport/visits/visit%2Fone/deletion-preview');
    expect(PASSPORT_VISITS_API_ENDPOINTS.delete('visit/one'))
      .toBe('me/passport/visits/visit%2Fone');
  });
});
