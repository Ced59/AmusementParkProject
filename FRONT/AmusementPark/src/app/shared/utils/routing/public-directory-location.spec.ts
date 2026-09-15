import { convertToParamMap } from '@angular/router';
import { buildPublicDirectoryPagePath, resolvePublicDirectoryLocation } from './public-directory-location';

describe('public directory pagination URLs', () => {
  it.each([{}, { page: '1' }, { page: '2' }])('accepts only a bounded positive page: %j', params => {
    const result = resolvePublicDirectoryLocation(convertToParamMap(params));
    expect(result).toEqual({ page: Number(params.page ?? 1), isValid: true, isIndexable: true });
  });

  it.each(['0', '-1', '', '1.5', '2e2', '02', ' 2', '1000000', 'NaN'])('rejects malformed page %s', page => {
    expect(resolvePublicDirectoryLocation(convertToParamMap({ page })).isValid).toBe(false);
  });

  it('rejects repeated pages and does not index unrelated queries', () => {
    expect(resolvePublicDirectoryLocation(convertToParamMap({ page: ['1', '2'] })).isValid).toBe(false);
    expect(resolvePublicDirectoryLocation(convertToParamMap({ page: '2', search: 'parc' })))
      .toEqual({ page: 2, isValid: true, isIndexable: false });
    expect(resolvePublicDirectoryLocation(convertToParamMap({ utm_source: 'newsletter' })))
      .toEqual({ page: 1, isValid: true, isIndexable: false });
  });

  it('links page one to the directory root', () => {
    expect(buildPublicDirectoryPagePath('fr', 'parks', 1)).toBe('/fr/parks');
    expect(buildPublicDirectoryPagePath('pl', 'parks', 3)).toBe('/pl/parks?page=3');
    expect(buildPublicDirectoryPagePath('fr', 'manufacturers', 1)).toBe('/fr/manufacturers');
    expect(buildPublicDirectoryPagePath('pl', 'manufacturers', 3)).toBe('/pl/manufacturers?page=3');
  });
});
