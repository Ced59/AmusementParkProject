import { convertToParamMap } from '@angular/router';
import { buildPublicParksPagePath, resolvePublicParksLocation } from './public-parks-location';

describe('public parks pagination URLs', () => {
  it.each([{}, { page: '1' }, { page: '2' }])('accepts only a bounded positive page: %j', params => {
    const result = resolvePublicParksLocation(convertToParamMap(params));
    expect(result).toEqual({ page: Number(params.page ?? 1), isValid: true, isIndexable: true });
  });

  it.each(['0', '-1', '', '1.5', '2e2', '02', ' 2', '1000000', 'NaN'])('rejects malformed page %s', page => {
    expect(resolvePublicParksLocation(convertToParamMap({ page })).isValid).toBe(false);
  });

  it('rejects repeated pages and does not index unrelated queries', () => {
    expect(resolvePublicParksLocation(convertToParamMap({ page: ['1', '2'] })).isValid).toBe(false);
    expect(resolvePublicParksLocation(convertToParamMap({ page: '2', search: 'parc' })))
      .toEqual({ page: 2, isValid: true, isIndexable: false });
    expect(resolvePublicParksLocation(convertToParamMap({ utm_source: 'newsletter' })))
      .toEqual({ page: 1, isValid: true, isIndexable: false });
  });

  it('links page one to the directory root', () => {
    expect(buildPublicParksPagePath('fr', 1)).toBe('/fr/parks');
    expect(buildPublicParksPagePath('pl', 3)).toBe('/pl/parks?page=3');
  });
});
