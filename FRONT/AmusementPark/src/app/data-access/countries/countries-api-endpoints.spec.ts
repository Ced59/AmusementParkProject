import { COUNTRIES_API_ENDPOINTS } from './countries-api-endpoints';

describe('COUNTRIES_API_ENDPOINTS', () => {
  it('encodes language and keeps pagination values', () => {
    expect(COUNTRIES_API_ENDPOINTS.getCountries('pt-BR', 2, 50)).toBe('countries?lang=pt-BR&page=2&size=50');
    expect(COUNTRIES_API_ENDPOINTS.getCountries('fr test', 1, 10)).toBe('countries?lang=fr%20test&page=1&size=10');
  });
});
