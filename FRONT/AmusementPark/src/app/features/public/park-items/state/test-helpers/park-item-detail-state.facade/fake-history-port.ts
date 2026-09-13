import { Observable, of } from 'rxjs';

import { HistoryTimeline } from '@app/models/history/history.models';

import { Park } from '@app/models/parks/park';

import { ParkItem } from '@app/models/parks/park-item';

import { ParkItemDetailHistoryPort } from '../../park-item-detail-data.ports';

function createPark(): Park {
  return {
    id: 'park-1',
    name: 'Phantasialand',
    countryCode: 'DE',
    latitude: 50.8,
    longitude: 6.8,
    isVisible: true,
    descriptions: [],
  };
}

function createParkItem(overrides: Partial<ParkItem> = {}): ParkItem {
  return {
    id: 'item-1',
    parkId: 'park-1',
    zoneId: 'zone-1',
    name: 'Taron',
    category: 'Attraction',
    type: 'RollerCoaster',
    latitude: 50.8,
    longitude: 6.8,
    isVisible: true,
    attractionDetails: {
      manufacturerId: 'manufacturer-1',
      manufacturerName: null,
      model: 'Launch Coaster',
      restraintType: 'Lap bar',
    },
    ...overrides,
  } as ParkItem;
}

function createHistoryTimeline(totalEvents: number): HistoryTimeline {
  return {
    entityType: 'ParkItem',
    park: createPark(),
    parkItem: createParkItem(),
    includedParkItems: [],
    events:
      totalEvents > 0
        ? [
            {
              event: {
                id: 'history-event-1',
                key: 'item-opening',
                entityType: 'ParkItem',
                ownerId: 'item-1',
                parkId: 'park-1',
                parkItemId: 'item-1',
                contextParkId: 'park-1',
                year: 2016,
                month: null,
                day: null,
                datePrecision: 'Year',
                eventType: 'Opening',
                isMajor: false,
                isVisible: true,
                slug: 'item-opening',
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
                relatedParkIds: ['park-1'],
                relatedParkItemIds: [],
                sources: [],
                article: null,
                createdAtUtc: '2026-01-01T00:00:00Z',
                updatedAtUtc: '2026-01-01T00:00:00Z',
              },
              contextPark: createPark(),
              parkItem: createParkItem(),
              mainImage: null,
            },
          ]
        : [],
  };
}

export class FakeHistoryPort implements ParkItemDetailHistoryPort {
  public timelineResponse$: Observable<HistoryTimeline> = of(
    createHistoryTimeline(0),
  );
  public readonly calls: string[] = [];

  getParkItemTimeline(parkItemId: string): Observable<HistoryTimeline> {
    this.calls.push(parkItemId);
    return this.timelineResponse$;
  }
}
