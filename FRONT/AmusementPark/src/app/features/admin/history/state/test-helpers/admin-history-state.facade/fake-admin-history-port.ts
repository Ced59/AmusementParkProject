import { Observable, of } from 'rxjs';

import { AdminHistoryEventListQuery } from '@data-access/history/history-api-endpoints';

import { HistoryEvent, HistoryEventWriteModel } from '@app/models/history/history.models';

import { PagedResult } from '@shared/models/contracts';

import { createPagedResult } from '@shared/utils/mapping';

import { AdminHistoryDataPort } from '../../admin-history-data.ports';

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
    updatedAtUtc: '2026-01-01T00:00:00Z',
  };
}

export class FakeAdminHistoryPort implements AdminHistoryDataPort {
  public response$: Observable<PagedResult<HistoryEvent>> = of(
    createPagedResult([createHistoryEvent('event-1')]),
  );
  public readonly queries: AdminHistoryEventListQuery[] = [];
  public readonly createdRequests: HistoryEventWriteModel[] = [];
  public readonly updatedRequests: {
    eventId: string;
    request: HistoryEventWriteModel;
  }[] = [];
  public readonly deletedEventIds: string[] = [];

  getAdminEvents(
    query: AdminHistoryEventListQuery,
  ): Observable<PagedResult<HistoryEvent>> {
    this.queries.push(query);
    return this.response$;
  }

  createAdminEvent(request: HistoryEventWriteModel): Observable<HistoryEvent> {
    this.createdRequests.push(request);
    return of(createHistoryEvent('created-event'));
  }

  updateAdminEvent(
    eventId: string,
    request: HistoryEventWriteModel,
  ): Observable<HistoryEvent> {
    this.updatedRequests.push({ eventId, request });
    return of(createHistoryEvent(eventId));
  }

  deleteAdminEvent(eventId: string): Observable<boolean> {
    this.deletedEventIds.push(eventId);
    return of(true);
  }
}
