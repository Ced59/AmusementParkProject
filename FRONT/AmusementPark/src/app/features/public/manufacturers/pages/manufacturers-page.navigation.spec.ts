import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { Location } from '@angular/common';
import { provideLocationMocks } from '@angular/common/testing';
import { PLATFORM_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { NavigationEnd, Router, provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { TranslateModule } from '@ngx-translate/core';
import { Subject, filter, firstValueFrom, of, take, throwError } from 'rxjs';

import { AttractionManufacturer } from '@app/models/parks/attraction-manufacturer';
import { TranslationService } from '@app/services/translation.service';
import { SeoService } from '@core/seo/seo.service';
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import { optimizeHtmlForRobotNoJs } from '@core/ssr/robot-html-optimizer';
import { PagedResult } from '@shared/models/contracts';
import { ManufacturersPageComponent } from './manufacturers-page.component';
import { PublicManufacturersStateFacade } from '../state/public-manufacturers-state.facade';
import { PUBLIC_MANUFACTURERS_PORT } from '../state/public-manufacturers-state-data.ports';

describe('Manufacturer directory URL navigation and resolved SEO', () => {
  const port = { getAttractionManufacturersPage: vi.fn(), getAllAttractionManufacturers: vi.fn() };
  const seo = { applyManufacturersListSeo: vi.fn() };
  const httpStatus = { setNotFound: vi.fn(), setStatus: vi.fn() };
  let languageChanged: Subject<string>;

  beforeEach(() => {
    vi.clearAllMocks();
    languageChanged = new Subject<string>();
    port.getAttractionManufacturersPage.mockImplementation((page: number, size: number) => of(pageResponse(page, size)));
    TestBed.configureTestingModule({
      imports: [ManufacturersPageComponent, TranslateModule.forRoot()],
      providers: [
        provideRouter([{ path: ':lang', children: [{ path: 'manufacturers', component: ManufacturersPageComponent }] }]),
        provideLocationMocks(), provideHttpClient(), provideHttpClientTesting(),
        { provide: PLATFORM_ID, useValue: 'server' },
        { provide: PUBLIC_MANUFACTURERS_PORT, useValue: port },
        { provide: TranslationService, useValue: { getCurrentLang: () => 'fr', languageChanged } },
        { provide: SeoService, useValue: seo },
        { provide: SsrHttpStatusService, useValue: httpStatus }
      ]
    });
    // Match AppComponent's earlier route-default listener, including synchronous API results.
    TestBed.inject(Router).events.subscribe(event => {
      if (event instanceof NavigationEnd) {
        seo.applyManufacturersListSeo('defaults', event.urlAfterRedirects, null);
      }
    });
  });

  it('loads 24 different cards on direct page two with real links retained for no-JS bots', async () => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/fr/manufacturers?page=2', ManufacturersPageComponent);
    harness.detectChanges();
    expect(port.getAttractionManufacturersPage).toHaveBeenCalledOnce();
    expect(port.getAttractionManufacturersPage).toHaveBeenCalledWith(2, 24, '');
    expect(port.getAllAttractionManufacturers).not.toHaveBeenCalled();
    expect(harness.routeNativeElement?.querySelectorAll('.manufacturer-card')).toHaveLength(24);
    expect(harness.routeNativeElement?.querySelector('a[href="/fr/park-manufacturer/manufacturer-25/fabricant-25"]')).not.toBeNull();
    expect(harness.routeNativeElement?.querySelector('a[href="/fr/park-manufacturer/manufacturer-1/fabricant-1"]')).toBeNull();
    expect(link(harness, 'a[rel="prev"]')).toBe('/fr/manufacturers');
    expect(link(harness, 'a[rel="next"]')).toBe('/fr/manufacturers?page=3');
    expect(harness.routeNativeElement?.querySelector('.manufacturers-pagination-trail')?.textContent).toContain('Page 2');
    expect(seo.applyManufacturersListSeo).toHaveBeenLastCalledWith('fr', '/fr/manufacturers?page=2', 2);
    const stripped = new DOMParser().parseFromString(optimizeHtmlForRobotNoJs(harness.routeNativeElement!.outerHTML).html, 'text/html');
    expect(stripped.querySelector('a[href="/fr/manufacturers?page=3"]')).not.toBeNull();
    expect(stripped.querySelector('a[href="/fr/park-manufacturer/manufacturer-25/fabricant-25"]')).not.toBeNull();
  });

  it('navigates through the page event and restores root cards without a catalogue request', async () => {
    const harness = await RouterTestingHarness.create();
    const page = await harness.navigateByUrl('/fr/manufacturers', ManufacturersPageComponent);
    page.onPageChanged({ page: 1, rows: 24 });
    await harness.fixture.whenStable();
    harness.detectChanges();
    expect(TestBed.inject(Router).url).toBe('/fr/manufacturers?page=2');
    expect(port.getAttractionManufacturersPage.mock.calls.map(call => call[0])).toEqual([1, 2]);
    expect(await harness.navigateByUrl('/fr/manufacturers', ManufacturersPageComponent)).toBe(page);
    expect(port.getAttractionManufacturersPage.mock.calls.map(call => call[0])).toEqual([1, 2, 1]);
    expect(port.getAllAttractionManufacturers).not.toHaveBeenCalled();
  });

  it('preserves search while removing its previous page URL', async () => {
    const harness = await RouterTestingHarness.create();
    const page = await harness.navigateByUrl('/fr/manufacturers?page=2', ManufacturersPageComponent);
    const input = harness.routeNativeElement!.querySelector('input')!;
    input.value = 'ride';
    input.dispatchEvent(new Event('input'));
    await harness.fixture.whenStable();
    await new Promise(resolve => setTimeout(resolve, 300));
    harness.detectChanges();
    expect(TestBed.inject(Router).url).toBe('/fr/manufacturers');
    expect(harness.routeDebugElement!.injector.get(PublicManufacturersStateFacade).searchTerm()).toBe('ride');
    expect(port.getAttractionManufacturersPage).toHaveBeenLastCalledWith(1, 24, 'ride');
    expect(harness.routeNativeElement?.querySelector('.app-pagination a')).toBeNull();
    expect(seo.applyManufacturersListSeo).toHaveBeenLastCalledWith('fr', '/fr/manufacturers', null);
    page.clearSearch();
    await new Promise(resolve => setTimeout(resolve, 300));
    harness.detectChanges();
    expect(link(harness, 'a[rel="next"]')).toBe('/fr/manufacturers?page=2');
  });

  it('keeps custom sizes interactive and restores links on returning to 24 rows', async () => {
    const harness = await RouterTestingHarness.create();
    const page = await harness.navigateByUrl('/fr/manufacturers?page=2', ManufacturersPageComponent);
    page.onPageChanged({ page: 0, rows: 12 });
    await harness.fixture.whenStable();
    harness.detectChanges();
    expect(TestBed.inject(Router).url).toBe('/fr/manufacturers');
    expect(harness.routeNativeElement?.querySelectorAll('.manufacturer-card')).toHaveLength(12);
    expect(harness.routeNativeElement?.querySelector('.app-pagination a')).toBeNull();
    page.onPageChanged({ page: 0, rows: 24 });
    harness.detectChanges();
    expect(link(harness, 'a[rel="next"]')).toBe('/fr/manufacturers?page=2');
  });

  it('does not persist the temporary filter marker when navigating Back', async () => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/fr/manufacturers', ManufacturersPageComponent);
    const page = await harness.navigateByUrl('/fr/manufacturers?page=2', ManufacturersPageComponent);
    page.onPageChanged({ page: 0, rows: 12 });
    await harness.fixture.whenStable();
    await harness.navigateByUrl('/fr/manufacturers?page=3', ManufacturersPageComponent);
    const router = TestBed.inject(Router);
    router.setUpLocationChangeListener();
    const returned = firstValueFrom(router.events.pipe(filter(event => event instanceof NavigationEnd), take(1)));
    TestBed.inject(Location).back();
    await returned;
    await harness.fixture.whenStable();
    harness.detectChanges();
    expect(router.url).toBe('/fr/manufacturers');
    expect(harness.routeNativeElement?.querySelectorAll('.manufacturer-card')).toHaveLength(24);
    expect(harness.routeNativeElement?.querySelector('a[href="/fr/park-manufacturer/manufacturer-1/fabricant-1"]')).not.toBeNull();
    expect(seo.applyManufacturersListSeo).toHaveBeenLastCalledWith('fr', '/fr/manufacturers', 1);
  });

  it.each([false, true])('keeps page two across a language change without refetching multilingual data (header first: %s)', async headerFirst => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/fr/manufacturers?page=2', ManufacturersPageComponent);
    if (headerFirst) {
      languageChanged.next('de');
    }
    await harness.navigateByUrl('/de/manufacturers?page=2', ManufacturersPageComponent);
    harness.detectChanges();
    expect(link(harness, 'a[rel="next"]')).toBe('/de/manufacturers?page=3');
    expect(harness.routeNativeElement?.querySelector('.manufacturers-pagination-trail')?.textContent).toContain('Seite 2');
    expect(seo.applyManufacturersListSeo).toHaveBeenLastCalledWith('de', '/de/manufacturers?page=2', 2);
    expect(port.getAttractionManufacturersPage).toHaveBeenCalledOnce();
  });

  it('waits for asynchronous data and ignores an abandoned page response', async () => {
    const pending = new Subject<PagedResult<AttractionManufacturer>>();
    port.getAttractionManufacturersPage.mockImplementation((page: number, size: number) => page === 2 ? pending : of(pageResponse(page, size)));
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/fr/manufacturers?page=2', ManufacturersPageComponent);
    expect(seo.applyManufacturersListSeo).toHaveBeenLastCalledWith('fr', '/fr/manufacturers?page=2', null);
    await harness.navigateByUrl('/fr/manufacturers?page=3', ManufacturersPageComponent);
    pending.next(pageResponse(2));
    harness.detectChanges();
    expect(seo.applyManufacturersListSeo).toHaveBeenLastCalledWith('fr', '/fr/manufacturers?page=3', 3);
    expect(harness.routeNativeElement?.querySelectorAll('.manufacturer-card')).toHaveLength(1);
  });

  it('cancels a queued search when an addressable page replaces it', async () => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/fr/manufacturers?page=2', ManufacturersPageComponent);
    const input = harness.routeNativeElement!.querySelector('input')!;
    input.value = 'ride';
    input.dispatchEvent(new Event('input'));
    await harness.fixture.whenStable();
    await harness.navigateByUrl('/fr/manufacturers?page=3', ManufacturersPageComponent);
    await new Promise(resolve => setTimeout(resolve, 300));
    harness.detectChanges();
    expect(port.getAttractionManufacturersPage.mock.calls.map(call => call[0])).toEqual([2, 3]);
    expect(seo.applyManufacturersListSeo).toHaveBeenLastCalledWith('fr', '/fr/manufacturers?page=3', 3);
  });

  it.each(['/fr/manufacturers?page=0', '/fr/manufacturers?page=99'])('excludes invalid or nonexistent pages: %s', async url => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl(url, ManufacturersPageComponent);
    expect(httpStatus.setNotFound).toHaveBeenCalled();
    expect(harness.routeNativeElement?.querySelectorAll('.manufacturer-card')).toHaveLength(0);
    expect(seo.applyManufacturersListSeo).toHaveBeenLastCalledWith('fr', url, null);
    if (url.endsWith('=0')) {
      expect(port.getAttractionManufacturersPage).not.toHaveBeenCalled();
    }
  });

  it('keeps tracking queries functional with a validated clean canonical decision', async () => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/fr/manufacturers?utm_source=mail', ManufacturersPageComponent);
    expect(harness.routeNativeElement?.querySelectorAll('.manufacturer-card')).toHaveLength(24);
    expect(httpStatus.setNotFound).not.toHaveBeenCalled();
    expect(seo.applyManufacturersListSeo).toHaveBeenLastCalledWith('fr', '/fr/manufacturers?utm_source=mail', 1);
  });

  it('keeps an API failure unavailable after NavigationEnd', async () => {
    port.getAttractionManufacturersPage.mockReturnValue(throwError(() => ({ status: 500 })));
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/fr/manufacturers?page=2', ManufacturersPageComponent);
    expect(httpStatus.setStatus).toHaveBeenCalledWith(503);
    expect(seo.applyManufacturersListSeo).toHaveBeenLastCalledWith('fr', '/fr/manufacturers?page=2', null);
  });

  it('allows the contextual trail to wrap on narrow viewports and long localized labels', () => {
    const styles = (ManufacturersPageComponent as unknown as { ɵcmp: { styles: string[] } }).ɵcmp.styles.join('\n');
    expect(styles).toContain('.manufacturers-pagination-trail');
    expect(styles).toContain('flex-wrap: wrap');
    expect(styles).toContain('min-width: 0');
    expect(styles).toContain('overflow-wrap: anywhere');
  });
});

function pageResponse(page: number, size: number = 24): PagedResult<AttractionManufacturer> {
  const start: number = (page - 1) * size;
  const items: AttractionManufacturer[] = Array.from({ length: Math.max(0, Math.min(size, 49 - start)) }, (_, index) => ({
    id: `manufacturer-${start + index + 1}`, name: `Fabricant ${start + index + 1}`,
    biography: [{ languageCode: 'fr', value: '<p>Un constructeur d’attractions avec sa fiche publique et son histoire.</p>' }]
  }));
  return { items, pagination: { currentPage: page, itemsPerPage: size, totalItems: 49, totalPages: Math.ceil(49 / size) } };
}

function link(harness: RouterTestingHarness, selector: string): string | null {
  const element = harness.routeNativeElement?.querySelector(selector);
  expect(element).not.toBeNull();
  return element?.getAttribute('href') ?? null;
}
