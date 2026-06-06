import { CanonicalUrlService } from './canonical-url.service';

describe('CanonicalUrlService', () => {
  let service: CanonicalUrlService;

  beforeEach(() => {
    service = new CanonicalUrlService();
  });

  it('builds absolute urls from relative paths', () => {
    expect(service.buildAbsoluteUrl('/fr/parks')).toBe('http://localhost:4200/fr/parks');
    expect(service.buildAbsoluteUrl('fr/parks')).toBe('http://localhost:4200/fr/parks');
  });

  it('normalizes empty, duplicate slashes, query and hash fragments', () => {
    expect(service.buildAbsoluteUrl('')).toBe('http://localhost:4200/');
    expect(service.buildAbsoluteUrl('/fr//parks/?page=2#top')).toBe('http://localhost:4200/fr/parks');
  });

  it('extracts the path from already absolute urls', () => {
    expect(service.buildCanonicalFromCurrentUrl('https://other.test/fr/park/1?x=1#hash')).toBe('http://localhost:4200/fr/park/1');
  });

  it('replaces the first URL segment with the target language', () => {
    expect(service.replaceLanguage('/fr/parks/1?x=1#top', 'en')).toBe('/en/parks/1');
  });

  it('uses language home when there is no path segment', () => {
    expect(service.replaceLanguage('/', 'de')).toBe('/de/home');
    expect(service.replaceLanguage('', 'pt')).toBe('/pt/home');
  });
});
