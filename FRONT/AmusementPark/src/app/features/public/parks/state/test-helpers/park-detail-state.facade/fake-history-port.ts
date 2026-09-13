import { Observable, of } from 'rxjs';

import { HistoryTimeline } from '@app/models/history/history.models';

import { Park } from '@app/models/parks/park';

import { AnonymousHttpOptions } from '@core/http/auth/anonymous-http-options';

import { ParkDetailHistoryPort } from '../../park-detail-data.ports';

function createPark(
  hasLogo: boolean = true,
  status: Park['status'] = 'Operating',
): Park {
  return {
    id: 'park-1',
    name: 'Bellewaerde',
    status,
    countryCode: 'BE',
    latitude: 50.845,
    longitude: 2.945,
    isVisible: true,
    founderId: 'founder-1',
    operatorId: 'operator-1',
    currentLogoImageId: hasLogo ? 'logo-1' : null,
    descriptions: [{ languageCode: 'en', value: '<p>Belgian park.</p>' }],
  };
}

function createHistoryTimeline(totalEvents: number): HistoryTimeline {
  return {
    entityType: 'Park',
    park: createPark(),
    parkItem: null,
    includedParkItems: [],
    events:
      totalEvents > 0
        ? [
            {
              event: {
                id: 'history-event-1',
                key: 'opening',
                entityType: 'Park',
                ownerId: 'park-1',
                parkId: 'park-1',
                parkItemId: null,
                contextParkId: null,
                year: 1954,
                month: null,
                day: null,
                datePrecision: 'Year',
                eventType: 'Opening',
                isMajor: false,
                isVisible: true,
                slug: 'opening',
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
          ]
        : [],
  };
}

export class FakeHistoryPort implements ParkDetailHistoryPort {
  public timelineResponse$: Observable<HistoryTimeline> = of(
    createHistoryTimeline(0),
  );
  public timelineResponses$: Observable<HistoryTimeline>[] = [];
  public readonly calls: {
    parkId: string;
    includeParkItems?: boolean;
    parkItemIds?: readonly string[];
  }[] = [];
  public readonly options: Array<AnonymousHttpOptions | undefined> = [];

  getParkTimeline(
    parkId: string,
    includeParkItems: boolean = false,
    parkItemIds: readonly string[] = [],
    options?: AnonymousHttpOptions,
  ): Observable<HistoryTimeline> {
    this.calls.push({ parkId, includeParkItems, parkItemIds });
    this.options.push(options);
    const queuedResponse$: Observable<HistoryTimeline> | undefined =
      this.timelineResponses$.shift();
    if (queuedResponse$) {
      return queuedResponse$;
    }

    return this.timelineResponse$;
  }
}
