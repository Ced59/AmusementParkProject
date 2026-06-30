import { TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';

import { AdminHistoryEventListQuery } from '@data-access/history/history-api-endpoints';
import { HistoryEvent, HistoryEventWriteModel } from '@app/models/history/history.models';
import { PagedResult } from '@shared/models/contracts';
import { createPagedResult } from '@shared/utils/mapping';
import { ADMIN_HISTORY_DATA_PORT, AdminHistoryDataPort } from './admin-history-data.ports';
import { AdminHistoryStateFacade } from './admin-history-state.facade';

class FakeAdminHistoryPort implements AdminHistoryDataPort {
  public response$: Observable<PagedResult<HistoryEvent>> = of(createPagedResult([createHistoryEvent('event-1')]));
  public readonly queries: AdminHistoryEventListQuery[] = [];
  public readonly createdRequests: HistoryEventWriteModel[] = [];
  public readonly updatedRequests: { eventId: string; request: HistoryEventWriteModel }[] = [];
  public readonly deletedEventIds: string[] = [];

  getAdminEvents(query: AdminHistoryEventListQuery): Observable<PagedResult<HistoryEvent>> {
    this.queries.push(query);
    return this.response$;
  }

  createAdminEvent(request: HistoryEventWriteModel): Observable<HistoryEvent> {
    this.createdRequests.push(request);
    return of(createHistoryEvent('created-event'));
  }

  updateAdminEvent(eventId: string, request: HistoryEventWriteModel): Observable<HistoryEvent> {
    this.updatedRequests.push({ eventId, request });
    return of(createHistoryEvent(eventId));
  }

  deleteAdminEvent(eventId: string): Observable<boolean> {
    this.deletedEventIds.push(eventId);
    return of(true);
  }
}

describe('AdminHistoryStateFacade', () => {
  let facade: AdminHistoryStateFacade;
  let port: FakeAdminHistoryPort;

  beforeEach(() => {
    port = new FakeAdminHistoryPort();

    TestBed.configureTestingModule({
      providers: [
        AdminHistoryStateFacade,
        { provide: ADMIN_HISTORY_DATA_PORT, useValue: port }
      ]
    });

    facade = TestBed.inject(AdminHistoryStateFacade);
  });

  it('loads admin history events with filters and pagination', () => {
    facade.load({
      page: 2,
      size: 25,
      entityType: 'Park',
      ownerId: 'park-1',
      search: 'opening',
      includeHidden: false
    });

    expect(port.queries).toEqual([{
      page: 2,
      size: 25,
      entityType: 'Park',
      ownerId: 'park-1',
      search: 'opening',
      includeHidden: false
    }]);
    expect(facade.events().map((event: HistoryEvent) => event.id)).toEqual(['event-1']);
    expect(facade.totalRecords()).toBe(1);
    expect(facade.errorKey()).toBeNull();
  });

  it('exposes a localized error key when the list fails to load', () => {
    port.response$ = throwError(() => new Error('network'));

    facade.load();

    expect(facade.events()).toEqual([]);
    expect(facade.totalRecords()).toBe(0);
    expect(facade.loading()).toBeFalse();
    expect(facade.errorKey()).toBe('admin.history.errors.loadFailed');
  });

  it('creates an event and reloads the last query', () => {
    const request: HistoryEventWriteModel = createWriteRequest('created-key');
    facade.load({ page: 3, size: 10, includeHidden: true });
    port.queries.length = 0;

    facade.save(null, request).subscribe();

    expect(port.createdRequests).toEqual([request]);
    expect(port.updatedRequests).toEqual([]);
    expect(port.queries).toEqual([{ page: 3, size: 10, entityType: null, ownerId: null, search: null, includeHidden: true }]);
    expect(facade.saving()).toBeFalse();
  });

  it('updates an event when an id is provided', () => {
    const request: HistoryEventWriteModel = createWriteRequest('updated-key');

    facade.save('event-1', request).subscribe();

    expect(port.createdRequests).toEqual([]);
    expect(port.updatedRequests).toEqual([{ eventId: 'event-1', request }]);
    expect(facade.saving()).toBeFalse();
  });

  it('deletes an event and reloads the current list', () => {
    facade.load({ page: 2, size: 50, entityType: 'ParkItem', includeHidden: false });
    port.queries.length = 0;

    facade.delete('event-1').subscribe();

    expect(port.deletedEventIds).toEqual(['event-1']);
    expect(port.queries).toEqual([{ page: 2, size: 50, entityType: 'ParkItem', ownerId: null, search: null, includeHidden: false }]);
    expect(facade.deleting()).toBeFalse();
  });
});

function createHistoryEvent(id: string): HistoryEvent {
  return {
    id,
    key: id,
    entityType: 'Park',
    ownerId: 'park-1',
    parkId: 'park-1',
    parkItemId: null,
    contextParkId: null,
    year: 1987,
    month: null,
    day: null,
    datePrecision: 'Year',
    eventType: 'Opening',
    isMajor: true,
    isVisible: true,
    slug: id,
    titles: [{ languageCode: 'fr', value: id }],
    summaries: [],
    mainImageId: null,
    previousName: null,
    newName: null,
    previousLogoImageId: null,
    newLogoImageId: null,
    previousOperatorId: null,
    newOperatorId: null,
    locationLabel: null,
    relatedParkIds: [],
    relatedParkItemIds: [],
    sources: [],
    article: null,
    createdAtUtc: '2026-01-01T00:00:00Z',
    updatedAtUtc: '2026-01-01T00:00:00Z'
  };
}

function createWriteRequest(key: string): HistoryEventWriteModel {
  return {
    key,
    entityType: 'Park',
    ownerId: 'park-1',
    parkId: 'park-1',
    parkItemId: null,
    contextParkId: null,
    year: 1987,
    month: null,
    day: null,
    datePrecision: 'Year',
    eventType: 'Opening',
    isMajor: false,
    isVisible: true,
    slug: null,
    titles: [{ languageCode: 'fr', value: key }],
    summaries: [],
    mainImageId: null,
    previousName: null,
    newName: null,
    previousLogoImageId: null,
    newLogoImageId: null,
    previousOperatorId: null,
    newOperatorId: null,
    locationLabel: null,
    relatedParkIds: [],
    relatedParkItemIds: [],
    sources: [],
    article: null
  };
}
