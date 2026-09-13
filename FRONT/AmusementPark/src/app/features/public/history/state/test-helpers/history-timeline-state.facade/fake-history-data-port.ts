import { Observable, of } from 'rxjs';

import { HistoryArticle, HistoryTimeline } from '@app/models/history/history.models';

import { AnonymousHttpOptions } from '@core/http/auth/anonymous-http-options';

import { HistoryDataPort } from '../../history-data.ports';

function createTimeline(
  entityType: 'Park' | 'ParkItem' | 'StandaloneAttraction' = 'Park',
): HistoryTimeline {
  return {
    entityType,
    park:
      entityType === 'Park'
        ? {
            id: 'park-1',
            name: 'Mirapolis',
            countryCode: 'FR',
            latitude: 49.054,
            longitude: 2.0,
            isVisible: true,
          }
        : null,
    parkItem: null,
    standaloneAttraction:
      entityType === 'StandaloneAttraction'
        ? {
            id: 'standalone-1',
            name: 'Pendolino',
            type: 'RollerCoaster',
            latitude: 46.561236,
            longitude: 13.253481,
            isVisible: true,
            adminReviewStatus: 'Validated',
          }
        : null,
    includedParkItems: [],
    events: [
      {
        event: {
          id: 'auto-event-1',
          key: 'auto-event-1',
          entityType,
          ownerId: entityType === 'Park' ? 'park-1' : entityType === 'ParkItem' ? 'item-1' : 'standalone-1',
          parkId: entityType === 'Park' || entityType === 'ParkItem' ? 'park-1' : null,
          parkItemId: entityType === 'ParkItem' ? 'item-1' : null,
          contextParkId: entityType === 'ParkItem' ? 'park-1' : null,
          year: 1988,
          month: null,
          day: null,
          datePrecision: 'Year',
          eventType: 'Opening',
          isMajor: false,
          isVisible: true,
          slug: null,
          titles: [],
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
        },
        contextPark: null,
        parkItem: null,
        mainImage: null,
      },
    ],
  };
}

export class FakeHistoryDataPort implements HistoryDataPort {
  public parkTimelineResponses$: Observable<HistoryTimeline>[] = [
    of(createTimeline()),
  ];
  public readonly parkTimelineCalls: {
    parkId: string;
    includeParkItems?: boolean;
    parkItemIds?: readonly string[];
    page?: number;
  }[] = [];
  public readonly standaloneTimelineCalls: { standaloneAttractionId: string; page?: number }[] = [];

  getParkTimeline(
    parkId: string,
    includeParkItems?: boolean,
    parkItemIds?: readonly string[],
    _options?: AnonymousHttpOptions,
    page?: number,
  ): Observable<HistoryTimeline> {
    this.parkTimelineCalls.push({
      parkId,
      includeParkItems,
      parkItemIds,
      page,
    });
    return this.parkTimelineResponses$.shift() ?? of(createTimeline());
  }

  getParkItemTimeline(
    _parkItemId: string,
    _options?: AnonymousHttpOptions,
  ): Observable<HistoryTimeline> {
    return of(createTimeline('ParkItem'));
  }

  getStandaloneAttractionTimeline(
    standaloneAttractionId: string,
    _options?: AnonymousHttpOptions,
    page?: number,
  ): Observable<HistoryTimeline> {
    this.standaloneTimelineCalls.push({ standaloneAttractionId, page });
    return of(createTimeline('StandaloneAttraction'));
  }

  getArticle(
    _eventId: string,
    _options?: AnonymousHttpOptions,
  ): Observable<HistoryArticle> {
    throw new Error('Not implemented in this spec');
  }
}
