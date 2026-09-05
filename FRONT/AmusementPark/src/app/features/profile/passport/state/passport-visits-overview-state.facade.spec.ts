import { DestroyRef } from '@angular/core';
import { PassportProductAnalyticsPort } from '@core/analytics/passport-product-analytics.port';
import { Subject, of, throwError } from 'rxjs';

import { PassportVisit, PassportVisitPage } from '@app/models/passport/passport-visit.models';
import { TranslationService } from '@app/services/translation.service';
import { PassportVisitsOverviewApiPort } from './passport-visits-overview-state-data.ports';
import { PassportVisitsOverviewStateFacade } from './passport-visits-overview-state.facade';

describe('PassportVisitsOverviewStateFacade', () => {
  it('loads and maps the first private cursor page', () => {
    const calls: Array<{ limit: number; cursor: string | null }> = [];
    const api: PassportVisitsOverviewApiPort = {
      listVisits: (limit: number, cursor: string | null) => {
        calls.push({ limit, cursor });
        return of(page([createVisit()], 'next-page'));
      }
    };
    const facade: PassportVisitsOverviewStateFacade = createFacade(api);

    facade.load();

    expect(calls).toEqual([{ limit: 20, cursor: null }]);
    expect(facade.visits()[0].parkName).toBe('Parc test');
    expect(facade.hasMore()).toBe(true);
    expect(facade.errorKey()).toBeNull();
  });

  it('appends unique visits and retains them when a later cursor request fails', () => {
    const responses = [
      of(page([createVisit()], 'next-page')),
      of(page([createVisit(), createVisit({ id: 'visit-2' })], 'last-page')),
      throwError(() => new Error('offline'))
    ];
    const api: PassportVisitsOverviewApiPort = {
      listVisits: () => responses.shift()!
    };
    const facade: PassportVisitsOverviewStateFacade = createFacade(api);
    vi.spyOn(console, 'error').mockImplementation(() => undefined);

    facade.load();
    facade.loadMore();
    facade.loadMore();

    expect(facade.visits().map((visit) => visit.id)).toEqual(['visit-1', 'visit-2']);
    expect(facade.loadMoreErrorKey()).toBe('passport.overview.errors.loadMore');
    expect(facade.hasMore()).toBe(true);
  });

  it('remaps partial dates when the active language changes without reloading the API', () => {
    const languageChanged = new Subject<string>();
    const api: PassportVisitsOverviewApiPort = {
      listVisits: () => of(page([createVisit({
        date: { year: 2026, month: 9, day: null, precision: 'Month', isApproximate: false }
      })], null))
    };
    const facade: PassportVisitsOverviewStateFacade = createFacade(api, languageChanged);

    facade.load();
    expect(facade.visits()[0].dateLabel).toBe('septembre 2026');

    languageChanged.next('en');

    expect(facade.visits()[0].dateLabel).toBe('September 2026');
  });

  it('ignores an in-flight pagination response after the visit list is refreshed', () => {
    const stalePage = new Subject<PassportVisitPage>();
    const calls: Array<{ limit: number; cursor: string | null }> = [];
    const responses = [
      of(page([createVisit()], 'old-cursor')),
      stalePage,
      of(page([createVisit({ id: 'new-visit' })], 'fresh-cursor')),
      of(page([createVisit({ id: 'older-visit' })], null))
    ];
    const api: PassportVisitsOverviewApiPort = {
      listVisits: (limit: number, cursor: string | null) => {
        calls.push({ limit, cursor });
        return responses.shift()!;
      }
    };
    const facade: PassportVisitsOverviewStateFacade = createFacade(api);

    facade.load();
    facade.loadMore();
    facade.load();
    stalePage.next(page([createVisit({ id: 'stale-visit' })], null));
    facade.loadMore();

    expect(calls).toEqual([
      { limit: 20, cursor: null },
      { limit: 20, cursor: 'old-cursor' },
      { limit: 20, cursor: null },
      { limit: 20, cursor: 'fresh-cursor' }
    ]);
    expect(facade.visits().map((visit) => visit.id)).toEqual(['new-visit', 'older-visit']);
    expect(facade.hasMore()).toBe(false);
    expect(facade.loadMoreErrorKey()).toBeNull();
  });

  it('runs a refresh requested while the initial visit page is still loading', () => {
    const initialPage = new Subject<PassportVisitPage>();
    const responses = [
      initialPage,
      of(page([createVisit({ id: 'new-visit' })], null))
    ];
    const api: PassportVisitsOverviewApiPort = {
      listVisits: () => responses.shift()!
    };
    const facade: PassportVisitsOverviewStateFacade = createFacade(api);

    facade.load();
    facade.load();
    initialPage.next(page([createVisit({ id: 'stale-visit' })], null));

    expect(facade.visits().map((visit) => visit.id)).toEqual(['new-visit']);
    expect(facade.loading()).toBe(false);
  });
});

function createFacade(
  api: PassportVisitsOverviewApiPort,
  languageChanged: Subject<string> = new Subject<string>()
): PassportVisitsOverviewStateFacade {
  const translationService = {
    getCurrentLang: (): string => 'fr',
    languageChanged
  } as unknown as TranslationService;
  const destroyRef = {
    onDestroy: (): (() => void) => (): void => undefined
  } as unknown as DestroyRef;
  const analytics: PassportProductAnalyticsPort = { track: vi.fn() };
  return new PassportVisitsOverviewStateFacade(api, analytics, translationService, destroyRef);
}

function page(items: PassportVisit[], nextCursor: string | null): PassportVisitPage {
  return { items, nextCursor };
}

function createVisit(overrides: Partial<PassportVisit> = {}): PassportVisit {
  return {
    id: 'visit-1',
    parkId: 'park-1',
    parkName: 'Parc test',
    date: { year: 2026, month: 9, day: 3, precision: 'Day', isApproximate: false },
    timeZoneId: null,
    serviceDayConvention: 'VisitStartLocalDate',
    status: 'Draft',
    privacy: 'Private',
    title: null,
    privateNote: null,
    version: 1,
    createdAtUtc: '2026-09-03T12:00:00Z',
    updatedAtUtc: '2026-09-03T12:00:00Z',
    completedAtUtc: null,
    ...overrides
  };
}
