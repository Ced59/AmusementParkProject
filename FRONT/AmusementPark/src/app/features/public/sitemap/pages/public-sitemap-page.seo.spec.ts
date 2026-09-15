import { DOCUMENT } from '@angular/common';
import { TestBed } from '@angular/core/testing';
import { NavigationEnd, Router, provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { TranslateModule } from '@ngx-translate/core';
import { of, Subject, Subscription, throwError } from 'rxjs';

import { PublicHtmlSitemapNode } from '@app/models/seo/public-html-sitemap-node';
import { TranslationService } from '@app/services/translation.service';
import { SeoService } from '@core/seo/seo.service';
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import { PUBLIC_SITEMAP_DATA_PORT } from '../state/public-sitemap-data.ports';
import { PublicSitemapPageComponent } from './public-sitemap-page.component';

describe('PublicSitemapPageComponent validated SEO', () => {
  const dataPort = { getNodes: vi.fn() };
  const status = { setNotFound: vi.fn(), setStatus: vi.fn() };
  let documentRef: Document;
  let rootNavigation: Subscription;
  let languageChanged: Subject<string>;

  beforeEach(() => {
    vi.clearAllMocks();
    languageChanged = new Subject<string>();
    dataPort.getNodes.mockImplementation((language: string, parent: string | null) => of(parent === null
      ? [{ id: 'parks', label: language === 'fr' ? 'Parcs' : 'Parks', hasChildren: true }]
      : Array.from({ length: 105 }, (_, index): PublicHtmlSitemapNode => ({
        id: `park:${index}`, label: `Parc ${index}`, relativeUrl: `/${language}/park/${index}/parc`, hasChildren: false
      }))));
    TestBed.configureTestingModule({
      imports: [PublicSitemapPageComponent, TranslateModule.forRoot()],
      providers: [
        provideRouter([{ path: ':lang', children: [{ path: 'sitemap', component: PublicSitemapPageComponent }] }]),
        { provide: PUBLIC_SITEMAP_DATA_PORT, useValue: dataPort },
        { provide: TranslationService, useValue: { getCurrentLang: () => 'fr', languageChanged } },
        { provide: SsrHttpStatusService, useValue: status }
      ]
    });
    documentRef = TestBed.inject(DOCUMENT);
    clearSeo();
    // Reproduce AppComponent's earlier subscription; synchronous data must survive this reset.
    rootNavigation = TestBed.inject(Router).events.subscribe(event => {
      if (event instanceof NavigationEnd) {
        TestBed.inject(SeoService).applyRouteDefaults(event.urlAfterRedirects);
      }
    });
  });

  afterEach(() => {
    rootNavigation.unsubscribe();
    clearSeo();
  });

  it('indexes synchronously loaded root HTML after NavigationEnd and normalizes redundant page one', async () => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/fr/sitemap?node=&page=1', PublicSitemapPageComponent);
    expect(robots()).toBe('index,follow');
    expect(canonical()).toBe('http://localhost:4200/fr/sitemap');
    expect(documentRef.head.querySelectorAll('link[rel="alternate"]')).toHaveLength(9);
  });

  it('keeps each branch page canonical equal to its actual navigation href', async () => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/fr/sitemap?node=parks', PublicSitemapPageComponent);
    const nextHref: string = harness.routeNativeElement!.querySelector('a[rel="next"]')!.getAttribute('href')!;
    const firstTitle: string = documentRef.title;
    await harness.navigateByUrl(nextHref, PublicSitemapPageComponent);
    expect(robots()).toBe('index,follow');
    expect(canonical()).toBe(`http://localhost:4200${nextHref}`);
    expect(documentRef.title).not.toBe(firstTitle);
    expect(documentRef.title).toContain('Page 2');
    expect(documentRef.head.querySelectorAll('link[rel="alternate"]')).toHaveLength(0);
    expect(harness.routeNativeElement!.querySelectorAll('.sitemap-tree__item')).toHaveLength(5);
  });

  it.each([
    '?node=unknown', '?node=parks&page=3', '?node=parks&page=0',
    '?node=parks&node=parks', '?node=parks&page=1&page=2', '?search=park'
  ])('removes stale indexability and canonical for invalid location %s', async (query: string) => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/fr/sitemap', PublicSitemapPageComponent);
    expect(robots()).toBe('index,follow');
    await harness.navigateByUrl(`/fr/sitemap${query}`, PublicSitemapPageComponent);
    expect(robots()).toBe('noindex,follow');
    expect(canonical()).toBeNull();
    expect(documentRef.head.querySelectorAll('script[data-managed-by="amusementpark-seo"]')).toHaveLength(0);
    expect(status.setNotFound).toHaveBeenCalled();
  });

  it('keeps an empty branch noindex', async () => {
    dataPort.getNodes.mockImplementation((_language: string, parent: string | null) => of(parent === null
      ? [{ id: 'parks', label: 'Parcs', hasChildren: true }] : []));
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/fr/sitemap?node=parks', PublicSitemapPageComponent);
    expect(robots()).toBe('noindex,follow');
    expect(canonical()).toBeNull();
  });

  it('removes stale metadata while loading and after a temporary API failure', async () => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/fr/sitemap', PublicSitemapPageComponent);
    const response = new Subject<PublicHtmlSitemapNode[]>();
    dataPort.getNodes.mockReturnValue(response);
    await harness.navigateByUrl('/fr/sitemap?node=parks', PublicSitemapPageComponent);
    expect(robots()).toBe('noindex,follow');
    expect(canonical()).toBeNull();
    response.error(new Error('network'));
    harness.detectChanges();
    expect(robots()).toBe('noindex,follow');
    expect(canonical()).toBeNull();
    expect(status.setStatus).toHaveBeenCalledWith(503);
  });

  it('applies asynchronously loaded metadata without another NavigationEnd and ignores obsolete responses', async () => {
    const oldResponse = new Subject<PublicHtmlSitemapNode[]>();
    const newResponse = new Subject<PublicHtmlSitemapNode[]>();
    dataPort.getNodes.mockImplementation((language: string) => language === 'fr' ? oldResponse : newResponse);
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/fr/sitemap', PublicSitemapPageComponent);
    expect(robots()).toBe('noindex,follow');
    await harness.navigateByUrl('/en/sitemap', PublicSitemapPageComponent);
    newResponse.next([{ id: 'parks', label: 'Parks', hasChildren: true }]);
    harness.detectChanges();
    expect(robots()).toBe('index,follow');
    expect(canonical()).toBe('http://localhost:4200/en/sitemap');
    oldResponse.next([{ id: 'parks', label: 'Parcs', hasChildren: true }]);
    harness.detectChanges();
    expect(oldResponse.observed).toBe(false);
    expect(canonical()).toBe('http://localhost:4200/en/sitemap');
  });

  it.each([false, true])('keeps localized metadata after a language navigation (header event: %s)', async (emitHeaderEvent: boolean) => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/fr/sitemap?node=parks', PublicSitemapPageComponent);
    if (emitHeaderEvent) {
      languageChanged.next('en');
      harness.detectChanges();
      expect(canonical()).not.toBe('http://localhost:4200/en/sitemap?node=parks');
    }
    await harness.navigateByUrl('/en/sitemap?node=parks', PublicSitemapPageComponent);
    expect(canonical()).toBe('http://localhost:4200/en/sitemap?node=parks');
    expect(documentRef.title).toContain('Parks — Sitemap');
    expect(documentRef.head.querySelector('meta[property="og:locale"]')?.getAttribute('content')).toBe('en_US');
    expect(robots()).toBe('index,follow');
  });

  it('keeps a directly failing branch noindex after NavigationEnd', async () => {
    dataPort.getNodes.mockReturnValue(throwError(() => ({ status: 503 })));
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/fr/sitemap?node=parks', PublicSitemapPageComponent);
    expect(robots()).toBe('noindex,follow');
    expect(canonical()).toBeNull();
    expect(status.setStatus).toHaveBeenCalledWith(503);
  });

  function robots(): string | null {
    return documentRef.head.querySelector('meta[name="robots"]')?.getAttribute('content') ?? null;
  }

  function canonical(): string | null {
    return documentRef.head.querySelector('link[rel="canonical"]')?.getAttribute('href') ?? null;
  }

  function clearSeo(): void {
    documentRef.head.querySelectorAll('meta[name="description"],meta[name="robots"],meta[name="googlebot"],meta[name^="twitter:"],meta[property^="og:"],link[rel="canonical"],link[rel="alternate"],script[data-managed-by="amusementpark-seo"]').forEach(element => element.remove());
  }
});
