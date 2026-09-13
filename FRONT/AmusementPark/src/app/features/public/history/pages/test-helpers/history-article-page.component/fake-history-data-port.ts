import { Observable, of } from 'rxjs';

import { HistoryArticle, HistoryTimeline } from '@app/models/history/history.models';

import { AnonymousHttpOptions } from '@core/http/auth/anonymous-http-options';

import { HistoryDataPort } from '../../../state/history-data.ports';

function createHistoryArticle(title: string): HistoryArticle {
  return {
    event: {
      id: 'event-1',
      key: 'event-1',
      entityType: 'ParkItem',
      ownerId: 'item-1',
      parkId: 'park-1',
      parkItemId: 'item-1',
      contextParkId: 'park-1',
      year: 2026,
      month: 7,
      day: 4,
      datePrecision: 'Day',
      eventType: 'Incident',
      isMajor: true,
      isVisible: true,
      slug: 'nitro-incident',
      titles: [],
      summaries: [],
      mainImageId: 'image-1',
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
      article: {
        slug: 'nitro-incident',
        titles: [{ languageCode: 'fr', value: title }],
        subtitles: [],
        summaries: [
          { languageCode: 'fr', value: 'Un résumé public de l’incident.' },
        ],
        mainImageId: 'image-1',
        blocks: [
          {
            id: 'paragraph-1',
            type: 'Paragraph',
            sortOrder: 1,
            headingLevel: null,
            texts: [
              {
                languageCode: 'fr',
                value: 'Le contenu de l’article est rendu dès le SSR.',
              },
            ],
            imageId: null,
            imageIds: [],
            captions: [],
          },
        ],
        sources: [],
        isPublished: true,
      },
      createdAtUtc: '2026-07-04T00:00:00Z',
      updatedAtUtc: '2026-07-04T00:00:00Z',
    },
    contextPark: {
      id: 'park-1',
      name: 'Dennlys Parc',
      countryCode: 'FR',
      latitude: 50.58,
      longitude: 2.17,
      isVisible: true,
    },
    park: null,
    parkItem: {
      id: 'item-1',
      parkId: 'park-1',
      name: 'Le Nitro',
      category: 'Attraction',
      type: 'RollerCoaster',
      latitude: null,
      longitude: null,
      isVisible: true,
      mainImageId: null,
    },
    mainImage: null,
  };
}

export class FakeHistoryDataPort implements HistoryDataPort {
  public articleCallCount: number = 0;

  getParkTimeline(
    _parkId: string,
    _includeParkItems?: boolean,
    _parkItemIds?: readonly string[],
    _options?: AnonymousHttpOptions,
  ): Observable<HistoryTimeline> {
    return of({} as HistoryTimeline);
  }

  getParkItemTimeline(
    _parkItemId: string,
    _options?: AnonymousHttpOptions,
  ): Observable<HistoryTimeline> {
    return of({} as HistoryTimeline);
  }

  getStandaloneAttractionTimeline(
    _standaloneAttractionId: string,
    _options?: AnonymousHttpOptions,
  ): Observable<HistoryTimeline> {
    return of({} as HistoryTimeline);
  }

  getArticle(
    _eventId: string,
    _options?: AnonymousHttpOptions,
  ): Observable<HistoryArticle> {
    this.articleCallCount += 1;
    return of(createHistoryArticle('Fallback Article'));
  }
}
