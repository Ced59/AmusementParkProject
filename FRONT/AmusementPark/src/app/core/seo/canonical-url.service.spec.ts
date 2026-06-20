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

  it('normalizes legacy video share routes to canonical video routes', () => {
    expect(service.buildCanonicalFromCurrentUrl('/fr/park/park-1/demo-park/video/s/video-1/demo-video'))
      .toBe('http://localhost:4200/fr/park/park-1/demo-park/videos/video-1/demo-video');
    expect(service.buildCanonicalFromCurrentUrl('/fr/park/park-1/demo-park/video/video-1/demo-video'))
      .toBe('http://localhost:4200/fr/park/park-1/demo-park/videos/video-1/demo-video');
    expect(service.buildCanonicalFromCurrentUrl('/fr/park/park-1/demo-park/item/item-1/demo-item/video/s/video-1/demo-video'))
      .toBe('http://localhost:4200/fr/park/park-1/demo-park/item/item-1/demo-item/videos/video-1/demo-video');
    expect(service.buildCanonicalFromCurrentUrl('/fr/park/park-1/demo-park/item/item-1/demo-item/video/video-1/demo-video'))
      .toBe('http://localhost:4200/fr/park/park-1/demo-park/item/item-1/demo-item/videos/video-1/demo-video');
  });

  it('builds alternates from normalized legacy video routes', () => {
    expect(service.replaceLanguage('/fr/park/park-1/demo-park/item/item-1/demo-item/video/s/video-1/demo-video', 'en'))
      .toBe('/en/park/park-1/demo-park/item/item-1/demo-item/videos/video-1/demo-video');
  });

  it('uses language home when there is no path segment', () => {
    expect(service.replaceLanguage('/', 'de')).toBe('/de/home');
    expect(service.replaceLanguage('', 'pt')).toBe('/pt/home');
  });
});
