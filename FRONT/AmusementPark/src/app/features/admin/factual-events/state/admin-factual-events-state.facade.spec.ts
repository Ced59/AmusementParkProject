import type { MockedObject } from 'vitest';
import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { Subject, of, throwError } from 'rxjs';

import { FactualChangeEventAdmin } from '@app/models/admin/factual-events/factual-event-administration.models';
import { provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { PagedResult } from '@shared/models/contracts';
import {
  ADMIN_FACTUAL_EVENTS_STATE_PORT,
  AdminFactualEventsStatePort,
} from './admin-factual-events-state-data.ports';
import { AdminFactualEventsStateFacade } from './admin-factual-events-state.facade';

describe('AdminFactualEventsStateFacade', (): void => {
  let facade: AdminFactualEventsStateFacade;
  let port: MockedObject<AdminFactualEventsStatePort>;

  beforeEach((): void => {
    port = {
      search: vi.fn().mockName('AdminFactualEventsStatePort.search'),
      verify: vi.fn().mockName('AdminFactualEventsStatePort.verify'),
      publish: vi.fn().mockName('AdminFactualEventsStatePort.publish'),
    } as unknown as MockedObject<AdminFactualEventsStatePort>;
    TestBed.configureTestingModule({
      providers: [
        provideCommonTestDependencies(),
        AdminFactualEventsStateFacade,
        { provide: ADMIN_FACTUAL_EVENTS_STATE_PORT, useValue: port },
      ],
    });
    facade = TestBed.inject(AdminFactualEventsStateFacade);
  });

  it('loads the human review queue', (): void => {
    const page: PagedResult<FactualChangeEventAdmin> = createPage(createEvent('Draft', 1));
    port.search.mockReturnValue(of(page));

    facade.load({ page: 1, size: 20, status: 'Draft' });

    expect(facade.events()).toEqual(page.items);
    expect(facade.loading()).toBe(false);
  });

  it('verifies against the displayed version then refreshes the active queue', (): void => {
    const event: FactualChangeEventAdmin = createEvent('Draft', 4);
    port.search.mockReturnValue(of(createPage(event)));
    port.verify.mockReturnValue(of(undefined));
    facade.load({ page: 1, size: 20, status: 'Draft' });

    facade.changeStatus(event, 'verify');

    expect(port.verify).toHaveBeenCalledWith('event-1', { expectedVersion: 4 });
    expect(port.search).toHaveBeenCalledTimes(2);
    expect(facade.actionEventId()).toBeNull();
  });

  it('publishes only through the dedicated publication operation', (): void => {
    const event: FactualChangeEventAdmin = createEvent('Verified', 5);
    port.search.mockReturnValue(of(createPage(event)));
    port.publish.mockReturnValue(of(undefined));
    facade.load({ page: 1, size: 20, status: 'Verified' });

    facade.changeStatus(event, 'publish');

    expect(port.publish).toHaveBeenCalledWith('event-1', { expectedVersion: 5 });
    expect(port.verify).not.toHaveBeenCalled();
  });

  it('does not replace a newer filtered page with a stale mutation refresh', (): void => {
    const draftEvent: FactualChangeEventAdmin = createEvent('Draft', 4);
    const verifiedEvent: FactualChangeEventAdmin = createEvent('Verified', 5);
    const mutation = new Subject<void>();
    const filterBeforeMutation = new Subject<PagedResult<FactualChangeEventAdmin>>();
    port.search
      .mockReturnValueOnce(of(createPage(draftEvent)))
      .mockReturnValueOnce(filterBeforeMutation)
      .mockReturnValueOnce(of(createPage(verifiedEvent)));
    port.verify.mockReturnValue(mutation);
    facade.load({ page: 1, size: 20, status: 'Draft' });

    facade.changeStatus(draftEvent, 'verify');
    facade.load({ page: 1, size: 20, status: 'Verified' });
    mutation.next();
    mutation.complete();
    filterBeforeMutation.next(createPage(draftEvent));
    filterBeforeMutation.complete();

    expect(port.search).toHaveBeenLastCalledWith({ page: 1, size: 20, status: 'Verified' });
    expect(facade.events()[0]?.status).toBe('Verified');
  });

  it('accepts a new filter while the previous search is still loading', (): void => {
    const staleSearch = new Subject<PagedResult<FactualChangeEventAdmin>>();
    const verifiedEvent: FactualChangeEventAdmin = createEvent('Verified', 5);
    port.search
      .mockReturnValueOnce(staleSearch)
      .mockReturnValueOnce(of(createPage(verifiedEvent)));

    facade.load({ page: 1, size: 20, status: 'Draft' });
    facade.load({ page: 1, size: 20, status: 'Verified' });
    staleSearch.next(createPage(createEvent('Draft', 4)));
    staleSearch.complete();

    expect(port.search).toHaveBeenCalledTimes(2);
    expect(facade.events()[0]?.status).toBe('Verified');
    expect(facade.loading()).toBe(false);
  });

  it('removes stale cards when a later filtered search fails', (): void => {
    const draftEvent: FactualChangeEventAdmin = createEvent('Draft', 4);
    port.search
      .mockReturnValueOnce(of(createPage(draftEvent)))
      .mockReturnValueOnce(throwError((): HttpErrorResponse => new HttpErrorResponse({ status: 503 })));
    facade.load({ page: 1, size: 20, status: 'Draft' });

    facade.load({ page: 1, size: 20, status: 'Verified' });

    expect(facade.events()).toEqual([]);
    expect(facade.pagination()).toBeNull();
    expect(facade.loadError()).toBe(true);
  });

  it('reports a refresh failure as a load error after a successful mutation', (): void => {
    const draftEvent: FactualChangeEventAdmin = createEvent('Draft', 4);
    port.search
      .mockReturnValueOnce(of(createPage(draftEvent)))
      .mockReturnValueOnce(throwError((): HttpErrorResponse => new HttpErrorResponse({ status: 503 })));
    port.verify.mockReturnValue(of(undefined));
    facade.load({ page: 1, size: 20, status: 'Draft' });

    facade.changeStatus(draftEvent, 'verify');

    expect(facade.actionError()).toBeNull();
    expect(facade.loadError()).toBe(true);
  });

  it('keeps actions locked until the successful mutation refresh completes', (): void => {
    const draftEvent: FactualChangeEventAdmin = createEvent('Draft', 4);
    const refresh = new Subject<PagedResult<FactualChangeEventAdmin>>();
    port.search
      .mockReturnValueOnce(of(createPage(draftEvent)))
      .mockReturnValueOnce(refresh);
    port.verify.mockReturnValue(of(undefined));
    facade.load({ page: 1, size: 20, status: 'Draft' });

    facade.changeStatus(draftEvent, 'verify');

    expect(facade.actionEventId()).toBeNull();
    expect(facade.loading()).toBe(true);
    facade.changeStatus(draftEvent, 'verify');
    expect(port.verify).toHaveBeenCalledTimes(1);
    refresh.next(createPage(createEvent('Verified', 5)));
    refresh.complete();
    expect(facade.loading()).toBe(false);
  });

  it('keeps actions locked until the conflict refresh completes', (): void => {
    const draftEvent: FactualChangeEventAdmin = createEvent('Draft', 4);
    const refresh = new Subject<PagedResult<FactualChangeEventAdmin>>();
    port.search
      .mockReturnValueOnce(of(createPage(draftEvent)))
      .mockReturnValueOnce(refresh);
    port.verify.mockReturnValue(throwError(
      (): HttpErrorResponse => new HttpErrorResponse({ status: 409 }),
    ));
    facade.load({ page: 1, size: 20, status: 'Draft' });

    facade.changeStatus(draftEvent, 'verify');

    expect(facade.actionEventId()).toBeNull();
    expect(facade.loading()).toBe(true);
    refresh.next(createPage(createEvent('Verified', 5)));
    refresh.complete();
    expect(facade.loading()).toBe(false);
  });

  it('does not let an older filter search overwrite a conflict refresh', (): void => {
    const draftEvent: FactualChangeEventAdmin = createEvent('Draft', 4);
    const mutation = new Subject<void>();
    const pendingFilter = new Subject<PagedResult<FactualChangeEventAdmin>>();
    const conflictRefresh = new Subject<PagedResult<FactualChangeEventAdmin>>();
    port.search
      .mockReturnValueOnce(of(createPage(draftEvent)))
      .mockReturnValueOnce(pendingFilter)
      .mockReturnValueOnce(conflictRefresh);
    port.verify.mockReturnValue(mutation);
    facade.load({ page: 1, size: 20, status: 'Draft' });
    facade.changeStatus(draftEvent, 'verify');
    facade.load({ page: 1, size: 20, status: 'Verified' });

    mutation.error(new HttpErrorResponse({ status: 409 }));
    conflictRefresh.next(createPage(createEvent('Verified', 5)));
    conflictRefresh.complete();
    pendingFilter.next(createPage(draftEvent));
    pendingFilter.complete();

    expect(port.search).toHaveBeenLastCalledWith({ page: 1, size: 20, status: 'Verified' });
    expect(facade.events()[0]?.status).toBe('Verified');
  });

  it('removes the stale actionable card when the conflict refresh fails', (): void => {
    const draftEvent: FactualChangeEventAdmin = createEvent('Draft', 4);
    port.search
      .mockReturnValueOnce(of(createPage(draftEvent)))
      .mockReturnValueOnce(throwError((): HttpErrorResponse => new HttpErrorResponse({ status: 503 })));
    port.verify.mockReturnValue(throwError((): HttpErrorResponse => new HttpErrorResponse({ status: 409 })));
    facade.load({ page: 1, size: 20, status: 'Draft' });

    facade.changeStatus(draftEvent, 'verify');

    expect(facade.events()).toEqual([]);
    expect(facade.pagination()).toBeNull();
    expect(facade.loadError()).toBe(true);
    expect(facade.actionError()).toBe('failure');
  });
});

function createPage(event: FactualChangeEventAdmin): PagedResult<FactualChangeEventAdmin> {
  return {
    items: [event],
    pagination: { totalItems: 1, totalPages: 1, currentPage: 1, itemsPerPage: 20 },
  };
}

function createEvent(status: FactualChangeEventAdmin['status'], version: number): FactualChangeEventAdmin {
  return {
    eventId: 'event-1',
    type: 'OpeningCalendarChanged',
    definitionVersion: 1,
    target: { type: 'Park', name: 'Parc exemple', parentParkName: null },
    previousValue: null,
    newValue: { kind: 'Text', canonicalValue: 'Nouveau calendrier', unitCode: null },
    source: {
      type: 'OfficialWebsite',
      publisherName: 'Parc exemple',
      title: 'Calendrier officiel',
      url: 'https://example.com/calendar',
      publishedAtUtc: '2026-09-15T08:00:00Z',
    },
    confidence: 'High',
    occurredAtUtc: '2026-09-15T09:00:00Z',
    revision: 1,
    status,
    createdAtUtc: '2026-09-15T10:00:00Z',
    updatedAtUtc: '2026-09-15T10:00:00Z',
    verifiedAtUtc: status === 'Verified' ? '2026-09-15T10:30:00Z' : null,
    publishedAtUtc: null,
    version,
    canBeDistributed: false,
  };
}
