import { convertToParamMap } from '@angular/router';
import { buildPublicSitemapQuery, resolvePublicSitemapLocation } from './public-sitemap-location';

describe('public sitemap locations', () => {
  it('keeps the root URL and first page free of redundant parameters', () => {
    expect(resolvePublicSitemapLocation(convertToParamMap({}))).toEqual({ nodeIds: [], page: 1, isValid: true });
    expect(buildPublicSitemapQuery([])).toEqual({});
    expect(buildPublicSitemapQuery(['parks'])).toEqual({ node: 'parks' });
  });

  it('preserves a finite hierarchical location and numbered page', () => {
    expect(resolvePublicSitemapLocation(convertToParamMap({ node: 'parks/park:park-1/park-items:park-1', page: '2' }))).toEqual({
      nodeIds: ['parks', 'park:park-1', 'park-items:park-1'], page: 2, isValid: true
    });
    expect(buildPublicSitemapQuery(['parks', 'park:park-1'], 2)).toEqual({ node: 'parks/park:park-1', page: 2 });
  });

  it.each(['../admin', '/parks', 'parks/', 'parks//references', 'parks?foo', 'park:<script>', `park:${'x'.repeat(81)}`, Array(7).fill('parks').join('/')])('rejects malformed node %s', (node: string) => {
    expect(resolvePublicSitemapLocation(convertToParamMap({ node })).isValid).toBe(false);
  });

  it.each(['0', '-1', '2.5', '01', '1e2', '1000000', 'NaN', ''])('rejects invalid page %s', (page: string) => {
    expect(resolvePublicSitemapLocation(convertToParamMap({ page })).isValid).toBe(false);
  });

  it('rejects repeated location parameters', () => {
    expect(resolvePublicSitemapLocation(convertToParamMap({ node: ['parks', 'references'] })).isValid).toBe(false);
    expect(resolvePublicSitemapLocation(convertToParamMap({ page: ['1', '2'] })).isValid).toBe(false);
  });

  it('rejects unsupported query keys instead of creating cacheable root variants', () => {
    expect(resolvePublicSitemapLocation(convertToParamMap({ node: 'parks', sort: 'random' })).isValid).toBe(false);
    expect(resolvePublicSitemapLocation(convertToParamMap({ arbitrary: 'value' })).isValid).toBe(false);
  });
});
