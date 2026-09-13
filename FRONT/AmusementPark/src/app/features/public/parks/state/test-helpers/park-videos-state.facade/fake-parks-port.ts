import { Observable, of } from 'rxjs';

import { Park } from '@app/models/parks/park';

import { ParkDetailSummary } from '@app/models/parks/park-detail-summary';

import { ParkVideosParksPort } from '../../park-videos-data.ports';

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

function createSummary(): ParkDetailSummary {
  return {
    park: createPark(),
    mainImage: null,
    references: {},
    stats: {
      totalItems: 0,
      zoneCount: 0,
      attractionCount: 0,
      restaurantCount: 0,
      showCount: 0,
      shopCount: 0,
      hotelCount: 0,
      countsByCategory: {},
    },
  };
}

export class FakeParksPort implements ParkVideosParksPort {
  public response$: Observable<ParkDetailSummary> = of(createSummary());
  public readonly calls: string[] = [];

  getParkDetailSummary(id: string): Observable<ParkDetailSummary> {
    this.calls.push(id);
    return this.response$;
  }
}
