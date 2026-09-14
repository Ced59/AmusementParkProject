import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { TranslateModule } from '@ngx-translate/core';
import { of, Subject } from 'rxjs';

import { PublicHtmlSitemapNode } from '@app/models/seo/public-html-sitemap-node';
import { TranslationService } from '@app/services/translation.service';
import { SeoService } from '@core/seo/seo.service';
import { optimizeHtmlForRobotNoJs } from '@core/ssr/robot-html-optimizer';
import { PUBLIC_SITEMAP_DATA_PORT } from '../state/public-sitemap-data.ports';
import { PublicSitemapPageComponent } from './public-sitemap-page.component';

const publicPark: string = '/fr/park/park-1/parc-demo';
const rootNode: PublicHtmlSitemapNode = { id: 'parks', label: 'Parcs', relativeUrl: '/fr/parks', hasChildren: true };
const parkNode: PublicHtmlSitemapNode = { id: 'park:park-1', label: 'Parc Démo', relativeUrl: publicPark, hasChildren: true };
const itemsNode: PublicHtmlSitemapNode = { id: 'park-items:park-1', label: 'Attractions', relativeUrl: `${publicPark}/items`, hasChildren: true };
const historyNode: PublicHtmlSitemapNode = { id: 'park-history:park-1', label: 'Histoire', relativeUrl: `${publicPark}/history`, hasChildren: true };

describe('PublicSitemapPageComponent crawlable navigation', () => {
  const dataPort = { getNodes: vi.fn() };

  beforeEach(() => {
    dataPort.getNodes.mockImplementation((_language: string, parent: string | null) => {
      const branches: Record<string, PublicHtmlSitemapNode[]> = {
        root: [rootNode],
        parks: [parkNode],
        'park:park-1': [itemsNode, historyNode],
        'park-items:park-1': [{ id: 'park-item:item-1', label: 'Grand Huit', relativeUrl: `${publicPark}/item/item-1/grand-huit`, hasChildren: true }],
        'park-history:park-1': [{ id: 'park-article:article-1', label: 'Les débuts', relativeUrl: `${publicPark}/history/article-1/les-debuts`, hasChildren: false }]
      };
      return of(branches[parent ?? 'root'] ?? []);
    });
    TestBed.configureTestingModule({
      imports: [PublicSitemapPageComponent, TranslateModule.forRoot()],
      providers: [
        provideRouter([{ path: ':lang/sitemap', component: PublicSitemapPageComponent }]),
        { provide: PUBLIC_SITEMAP_DATA_PORT, useValue: dataPort },
        { provide: TranslationService, useValue: { getCurrentLang: () => 'fr', languageChanged: new Subject<string>() } },
        { provide: SeoService, useValue: { applyRouteDefaults: vi.fn() } }
      ]
    });
  });

  it('renders actual root and child hrefs and follows query changes on the same component', async () => {
    const harness: RouterTestingHarness = await RouterTestingHarness.create();
    const initial: PublicSitemapPageComponent = await harness.navigateByUrl('/fr/sitemap', PublicSitemapPageComponent);
    const branchHref: string = readLink(harness, '.sitemap-tree__toggle');
    expect(new URL(branchHref, 'https://example.test').searchParams.get('node')).toBe('parks');
    expect(harness.routeNativeElement?.querySelector('.sitemap-tree__toggle')?.textContent?.trim()).toBeTruthy();
    const robotHtml: string = optimizeHtmlForRobotNoJs(harness.routeNativeElement!.outerHTML).html;
    const robotDocument: Document = new DOMParser().parseFromString(robotHtml, 'text/html');
    expect(robotDocument.querySelector('a[href*="node=parks"]')).not.toBeNull();

    const next: PublicSitemapPageComponent = await harness.navigateByUrl(branchHref, PublicSitemapPageComponent);
    expect(next).toBe(initial);
    expect(readLink(harness, '.sitemap-tree__link')).toBe(publicPark);
    expect(new URL(readLink(harness, '.sitemap-tree__toggle'), 'https://example.test').searchParams.get('node')).toBe('parks/park:park-1');
  });

  it('exposes item and article hrefs in a directly loaded branch', async () => {
    const harness: RouterTestingHarness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/fr/sitemap?node=parks%2Fpark:park-1%2Fpark-items:park-1', PublicSitemapPageComponent);
    expect(readLink(harness, '.sitemap-tree__link')).toBe(`${publicPark}/item/item-1/grand-huit`);
    expect(harness.routeNativeElement?.querySelector('.sitemap-navigation')?.textContent).toContain('Parc Démo');

    await harness.navigateByUrl('/fr/sitemap?node=parks%2Fpark:park-1%2Fpark-history:park-1', PublicSitemapPageComponent);
    expect(readLink(harness, '.sitemap-tree__link')).toBe(`${publicPark}/history/article-1/les-debuts`);
    expect(harness.routeNativeElement?.querySelector('.sitemap-tree__toggle')).toBeNull();
  });

  it('provides next and previous hrefs for bounded sibling pages', async () => {
    dataPort.getNodes.mockImplementation((_language: string, parent: string | null) => of(parent === 'parks'
      ? Array.from({ length: 105 }, (_, index): PublicHtmlSitemapNode => ({ ...parkNode, id: `park:${index}`, label: `Parc ${index}` }))
      : [rootNode]));
    const harness: RouterTestingHarness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/fr/sitemap?node=parks', PublicSitemapPageComponent);
    expect(harness.routeNativeElement?.querySelectorAll('.sitemap-tree__item')).toHaveLength(100);
    const nextHref: string = readLink(harness, 'a[rel="next"]');
    expect(new URL(nextHref, 'https://example.test').searchParams.get('page')).toBe('2');

    await harness.navigateByUrl(nextHref, PublicSitemapPageComponent);
    expect(harness.routeNativeElement?.querySelectorAll('.sitemap-tree__item')).toHaveLength(5);
    expect(new URL(readLink(harness, 'a[rel="prev"]'), 'https://example.test').searchParams.has('page')).toBe(false);
    expect(harness.routeNativeElement?.querySelector('a[rel="next"]')).toBeNull();
  });

  it.each([false, true])('resets language-scoped snapshot navigation when changing locale (header event: %s)', async (emitHeaderEvent: boolean) => {
    dataPort.getNodes.mockImplementation((language: string, parent: string | null) => {
      if (parent === null) {
        return of([{ id: 'snapshot-sections', label: 'Toutes les pages', hasChildren: true }]);
      }
      if (parent === 'snapshot-sections') {
        return of([{ id: `sitemap-section:parks-${language}`, label: `Parcs ${language}`, hasChildren: true }]);
      }
      return of(Array.from({ length: 105 }, (_, index): PublicHtmlSitemapNode => ({
        id: `park:${index}`, label: `Parc ${index}`, relativeUrl: `/${language}/park/${index}/parc`, hasChildren: false
      })));
    });
    const harness: RouterTestingHarness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/fr/sitemap?node=snapshot-sections%2Fsitemap-section:parks-fr&page=2', PublicSitemapPageComponent);
    expect(harness.routeNativeElement?.querySelectorAll('.sitemap-tree__item')).toHaveLength(5);
    dataPort.getNodes.mockClear();

    if (emitHeaderEvent) {
      TestBed.inject(TranslationService).languageChanged.next('en');
    }
    await harness.navigateByUrl('/en/sitemap?node=snapshot-sections%2Fsitemap-section:parks-fr&page=2', PublicSitemapPageComponent);
    await harness.fixture.whenStable();
    harness.detectChanges();

    expect(TestBed.inject(Router).url).toBe('/en/sitemap?node=snapshot-sections');
    expect(harness.routeNativeElement?.querySelector('.sitemap-tree__item')?.textContent).toContain('Parcs en');
    expect(new URL(readLink(harness, '.sitemap-tree__toggle'), 'https://example.test').searchParams.get('node')).toBe('snapshot-sections/sitemap-section:parks-en');
    expect(dataPort.getNodes).not.toHaveBeenCalledWith('en', 'sitemap-section:parks-fr', false);
  });
});

function readLink(harness: RouterTestingHarness, selector: string): string {
  const href: string | null | undefined = harness.routeNativeElement?.querySelector(selector)?.getAttribute('href');
  expect(href).toBeTruthy();
  return href!;
}
