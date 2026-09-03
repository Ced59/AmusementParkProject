import { PASSPORT_RIDE_OCCURRENCES_API_ENDPOINTS } from './passport-ride-occurrences-api-endpoints';

describe('PASSPORT_RIDE_OCCURRENCES_API_ENDPOINTS', () => {
  it('encodes visit, occurrence and cursor values', () => {
    expect(PASSPORT_RIDE_OCCURRENCES_API_ENDPOINTS.list('visit/1', 50, 'cursor+1')).toBe(
      'me/passport/visits/visit%2F1/occurrences?limit=50&cursor=cursor%2B1'
    );
    expect(PASSPORT_RIDE_OCCURRENCES_API_ENDPOINTS.update('visit/1', 'ride/2')).toBe(
      'me/passport/visits/visit%2F1/occurrences/ride%2F2'
    );
    expect(PASSPORT_RIDE_OCCURRENCES_API_ENDPOINTS.get('visit/1', 'ride/2')).toBe(
      'me/passport/visits/visit%2F1/occurrences/ride%2F2'
    );
  });

  it('keeps the batch, delete and reorder routes owner-scoped', () => {
    expect(PASSPORT_RIDE_OCCURRENCES_API_ENDPOINTS.addBatch('visit-1')).toBe(
      'me/passport/visits/visit-1/occurrences:batch'
    );
    expect(PASSPORT_RIDE_OCCURRENCES_API_ENDPOINTS.delete('visit-1', 'ride-1', 3)).toBe(
      'me/passport/visits/visit-1/occurrences/ride-1?expectedVersion=3'
    );
    expect(PASSPORT_RIDE_OCCURRENCES_API_ENDPOINTS.reorder('visit-1')).toBe(
      'me/passport/visits/visit-1/occurrences:reorder'
    );
    expect(PASSPORT_RIDE_OCCURRENCES_API_ENDPOINTS.assessment('ride/1')).toBe(
      'me/passport/occurrences/ride%2F1/assessment'
    );
    expect(PASSPORT_RIDE_OCCURRENCES_API_ENDPOINTS.deleteAssessment('ride/1', 3)).toBe(
      'me/passport/occurrences/ride%2F1/assessment?expectedVersion=3'
    );
  });
});
