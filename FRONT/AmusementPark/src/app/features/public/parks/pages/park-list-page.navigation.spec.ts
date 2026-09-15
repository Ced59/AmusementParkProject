import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { Location } from '@angular/common';
import { provideLocationMocks } from '@angular/common/testing';
import { PLATFORM_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { NavigationEnd, Router, provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { TranslateModule } from '@ngx-translate/core';
import { Subject, of, throwError } from 'rxjs';

import { Park } from '@app/models/parks/park';
import { ParksApiResponse } from '@app/models/parks/parks_api_response';
import { TranslationService } from '@app/services/translation.service';
import { SeoService } from '@core/seo/seo.service';
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import { optimizeHtmlForRobotNoJs } from '@core/ssr/robot-html-optimizer';
import { ParkListPageComponent } from './park-list-page.component';
import { ParkListMapComponent } from '../ui/park-list-map.component';
import { ParkListStateFacade } from '../state/park-list-state.facade';
import { PARK_LIST_STATE_PARKS_API_SERVICE_PORT, PARK_LIST_STATE_SEARCH_API_SERVICE_PORT, PARK_LIST_STATE_STANDALONE_ATTRACTIONS_API_SERVICE_PORT } from '../state/park-list-state-data.ports';

describe('Park directory URL navigation and resolved SEO', () => {
  const parksPort = { getParksPaginated: vi.fn(), searchParks: vi.fn(), getVisibleParkMapPoints: vi.fn() };
  const searchPort = { getSearch: vi.fn() };
  const seo = { applyParkListSeo: vi.fn() };
  const httpStatus = { setNotFound: vi.fn(), setStatus: vi.fn() };
  let languageChanged: Subject<string>;

  beforeEach(() => {
    vi.clearAllMocks();
    languageChanged = new Subject<string>();
    parksPort.getParksPaginated.mockImplementation((page: number, size: number) => of(pageResponse(page, size)));
    parksPort.searchParks.mockImplementation((_term: string, page: number, size: number) => of(pageResponse(page, size)));
    parksPort.getVisibleParkMapPoints.mockReturnValue(of([]));
    searchPort.getSearch.mockReturnValue(of({ data: [], pagination: { currentPage: 1, itemsPerPage: 9, totalItems: 0, totalPages: 0 } }));
    TestBed.configureTestingModule({
      imports: [ParkListPageComponent, TranslateModule.forRoot()],
      providers: [
        provideRouter([{ path: ':lang', children: [{ path: 'parks', component: ParkListPageComponent }] }]),
        provideLocationMocks(),
        provideHttpClient(), provideHttpClientTesting(),
        { provide: PLATFORM_ID, useValue: 'server' },
        { provide: PARK_LIST_STATE_PARKS_API_SERVICE_PORT, useValue: parksPort },
        { provide: PARK_LIST_STATE_SEARCH_API_SERVICE_PORT, useValue: searchPort },
        { provide: PARK_LIST_STATE_STANDALONE_ATTRACTIONS_API_SERVICE_PORT, useValue: { getVisibleMapPoints: () => of([]) } },
        { provide: TranslationService, useValue: { getCurrentLang: () => 'fr', languageChanged } },
        { provide: SeoService, useValue: seo },
        { provide: SsrHttpStatusService, useValue: httpStatus }
      ]
    }).overrideComponent(ParkListMapComponent, { set: { template: '', imports: [] } });
    // AppComponent's earlier NavigationEnd listener applies route defaults before the page restores validated SEO.
    TestBed.inject(Router).events.subscribe(event => {
      if (event instanceof NavigationEnd) {
        seo.applyParkListSeo('defaults', event.urlAfterRedirects, null);
      }
    });
  });

  it('loads page two directly with different cards, real hrefs and final metadata', async () => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/fr/parks?page=2', ParkListPageComponent);
    harness.detectChanges();
    expect(parksPort.getParksPaginated).toHaveBeenCalledOnce();
    expect(parksPort.getParksPaginated.mock.calls[0].slice(0, 3)).toEqual([2, 9, true]);
    expect(harness.routeNativeElement?.querySelectorAll('.park-card-wrapper')).toHaveLength(9);
    expect(harness.routeNativeElement?.querySelector('a[href="/fr/park/park-10/parc-10"]')).not.toBeNull();
    expect(harness.routeNativeElement?.querySelector('a[href="/fr/park/park-1/parc-1"]')).toBeNull();
    expect(link(harness, 'a[rel="prev"]')).toBe('/fr/parks');
    expect(link(harness, 'a[rel="next"]')).toBe('/fr/parks?page=3');
    expect(harness.routeNativeElement?.querySelector('.parks-pagination-trail')?.textContent).toContain('Page 2');
    expect(seo.applyParkListSeo).toHaveBeenLastCalledWith('fr', '/fr/parks?page=2', 2);
    const stripped = new DOMParser().parseFromString(optimizeHtmlForRobotNoJs(harness.routeNativeElement!.outerHTML).html, 'text/html');
    expect(stripped.querySelector('a[href="/fr/parks?page=3"]')).not.toBeNull();
    expect(stripped.querySelector('a[href="/fr/park/park-10/parc-10"]')).not.toBeNull();
  });

  it('navigates through the existing page-change event without reloading the map', async () => {
    const harness = await RouterTestingHarness.create();
    const page = await harness.navigateByUrl('/fr/parks', ParkListPageComponent);
    page.onPageChange({ page: 1, rows: 9 });
    await harness.fixture.whenStable();
    harness.detectChanges();
    expect(TestBed.inject(Router).url).toBe('/fr/parks?page=2');
    expect(parksPort.getVisibleParkMapPoints).toHaveBeenCalledOnce();
    expect(parksPort.getParksPaginated.mock.calls.map(call => call[0])).toEqual([1, 2]);
    const reused = await harness.navigateByUrl('/fr/parks', ParkListPageComponent);
    expect(reused).toBe(page);
    expect(parksPort.getParksPaginated.mock.calls.map(call => call[0])).toEqual([1, 2, 1]);
    expect(parksPort.getVisibleParkMapPoints).toHaveBeenCalledOnce();
  });

  it('preserves a status filter while removing the old standard-page URL', async () => {
    const harness = await RouterTestingHarness.create();
    const page = await harness.navigateByUrl('/fr/parks?page=2', ParkListPageComponent);
    page.onStatusFilterChanged('Planned');
    await harness.fixture.whenStable();
    harness.detectChanges();
    expect(TestBed.inject(Router).url).toBe('/fr/parks');
    expect(harness.routeDebugElement!.injector.get(ParkListStateFacade).selectedStatus()).toBe('Planned');
    expect(parksPort.getParksPaginated).toHaveBeenCalledTimes(2);
    expect(parksPort.getParksPaginated.mock.calls[1][5]).toMatchObject({ status: 'Planned' });
    expect(harness.routeNativeElement?.querySelector('.app-pagination a')).toBeNull();
    expect(seo.applyParkListSeo).toHaveBeenLastCalledWith('fr', '/fr/parks', null);
  });

  it('keeps custom sizes interactive and restores link mode when returning to nine rows', async () => {
    const harness = await RouterTestingHarness.create();
    const page = await harness.navigateByUrl('/fr/parks?page=2', ParkListPageComponent);
    page.onPageChange({ page: 0, rows: 18 });
    await harness.fixture.whenStable();
    harness.detectChanges();
    expect(TestBed.inject(Router).url).toBe('/fr/parks');
    expect(harness.routeNativeElement?.querySelectorAll('.park-card-wrapper')).toHaveLength(18);
    expect(harness.routeNativeElement?.querySelector('.app-pagination a')).toBeNull();
    expect(parksPort.getVisibleParkMapPoints).toHaveBeenCalledOnce();
    page.onPageChange({ page: 0, rows: 9 });
    harness.detectChanges();
    expect(link(harness, 'a[rel="next"]')).toBe('/fr/parks?page=2');
    expect(parksPort.getVisibleParkMapPoints).toHaveBeenCalledOnce();
  });

  it('does not preserve a temporary filter-removal marker in browser history', async () => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/fr/parks', ParkListPageComponent);
    const page = await harness.navigateByUrl('/fr/parks?page=2', ParkListPageComponent);
    page.onStatusFilterChanged('Planned');
    await harness.fixture.whenStable();
    await harness.navigateByUrl('/fr/parks?page=3', ParkListPageComponent);
    TestBed.inject(Location).back();
    await harness.fixture.whenStable();
    harness.detectChanges();
    expect(TestBed.inject(Router).url).toBe('/fr/parks');
    expect(harness.routeDebugElement!.injector.get(ParkListStateFacade).selectedStatus()).toBe('Operating');
    expect(harness.routeNativeElement?.querySelector('a[href="/fr/park/park-1/parc-1"]')).not.toBeNull();
    expect(seo.applyParkListSeo).toHaveBeenLastCalledWith('fr', '/fr/parks', 1);
  });

  it.each([false, true])('keeps the requested page through a language change (header first: %s)', async headerFirst => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/fr/parks?page=2', ParkListPageComponent);
    if (headerFirst) {
      languageChanged.next('en');
    }
    await harness.navigateByUrl('/en/parks?page=2', ParkListPageComponent);
    harness.detectChanges();
    expect(TestBed.inject(Router).url).toBe('/en/parks?page=2');
    expect(link(harness, 'a[rel="next"]')).toBe('/en/parks?page=3');
    expect(seo.applyParkListSeo).toHaveBeenLastCalledWith('en', '/en/parks?page=2', 2);
    expect(parksPort.getVisibleParkMapPoints).toHaveBeenCalledTimes(2);
  });

  it('waits for asynchronous data and never restores metadata from an abandoned page', async () => {
    const pending = new Subject<ParksApiResponse>();
    parksPort.getParksPaginated.mockImplementation((page: number, size: number) => page === 2 ? pending : of(pageResponse(page, size)));
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/fr/parks?page=2', ParkListPageComponent);
    expect(seo.applyParkListSeo).toHaveBeenLastCalledWith('fr', '/fr/parks?page=2', null);
    await harness.navigateByUrl('/fr/parks?page=3', ParkListPageComponent);
    pending.next(pageResponse(2));
    harness.detectChanges();
    expect(seo.applyParkListSeo).toHaveBeenLastCalledWith('fr', '/fr/parks?page=3', 3);
    expect(harness.routeNativeElement?.querySelectorAll('.park-card-wrapper')).toHaveLength(2);
  });

  it.each(['/fr/parks?page=0', '/fr/parks?page=99'])('excludes invalid or nonexistent pages: %s', async url => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl(url, ParkListPageComponent);
    expect(httpStatus.setNotFound).toHaveBeenCalled();
    expect(harness.routeNativeElement?.querySelectorAll('.park-card-wrapper')).toHaveLength(0);
    expect(seo.applyParkListSeo).toHaveBeenLastCalledWith('fr', url, null);
    if (url.endsWith('=0')) {
      expect(parksPort.getParksPaginated).not.toHaveBeenCalled();
      expect(parksPort.getVisibleParkMapPoints).not.toHaveBeenCalled();
    }
  });

  it('keeps incoming tracking queries functional but excluded from the standard-page index', async () => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/fr/parks?utm_source=mail', ParkListPageComponent);
    expect(harness.routeNativeElement?.querySelectorAll('.park-card-wrapper')).toHaveLength(9);
    expect(httpStatus.setNotFound).not.toHaveBeenCalled();
    expect(seo.applyParkListSeo).toHaveBeenLastCalledWith('fr', '/fr/parks?utm_source=mail', null);
  });

  it('keeps an API failure unavailable after NavigationEnd', async () => {
    parksPort.getParksPaginated.mockReturnValue(throwError(() => ({ status: 500 })));
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/fr/parks?page=2', ParkListPageComponent);
    expect(httpStatus.setStatus).toHaveBeenCalledWith(503);
    expect(seo.applyParkListSeo).toHaveBeenLastCalledWith('fr', '/fr/parks?page=2', null);
  });
});

function pageResponse(page: number, size: number = 9): ParksApiResponse {
  const start: number = (page - 1) * size;
  const data: Park[] = Array.from({ length: Math.max(0, Math.min(size, 20 - start)) }, (_, index): Park => ({
    id: `park-${start + index + 1}`, name: `Parc ${start + index + 1}`, isVisible: true, countryCode: 'FR',
    status: 'Operating', descriptions: [{ languageCode: 'fr', value: '<p>Un parc avec des attractions et des jardins.</p>' }]
  }));
  return { data, pagination: { currentPage: page, itemsPerPage: size, totalItems: 20, totalPages: Math.ceil(20 / size) } };
}

function link(harness: RouterTestingHarness, selector: string): string | null {
  const element = harness.routeNativeElement?.querySelector(selector);
  expect(element).not.toBeNull();
  return element?.getAttribute('href') ?? null;
}
