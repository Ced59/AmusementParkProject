import { CanonicalUrlService } from './canonical-url.service';
import { HreflangService } from './hreflang.service';
import { SEO_LANGUAGES } from './seo-languages';


describe('HreflangService', () => {
  let service: HreflangService;

  beforeEach(() => {
    service = new HreflangService(new CanonicalUrlService());
  });

  it('builds one alternate per SEO language plus x-default', () => {
    const alternates = service.buildAlternates('/fr/parks/1');

    expect(alternates.length).toBe(SEO_LANGUAGES.length + 1);
    expect(alternates.map((alternate) => alternate.hreflang)).toContain('x-default');
  });

  it('replaces the route language in every alternate url', () => {
    const alternates = service.buildAlternates('/fr/parks/1');
    const english = alternates.find((alternate) => alternate.hreflang === 'en');
    const french = alternates.find((alternate) => alternate.hreflang === 'fr');
    const xDefault = alternates.find((alternate) => alternate.hreflang === 'x-default');

    expect(english?.href).toBe('http://localhost:4200/en/parks/1');
    expect(french?.href).toBe('http://localhost:4200/fr/parks/1');
    expect(xDefault?.href).toBe('http://localhost:4200/en/parks/1');
  });
});
