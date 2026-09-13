import { Observable, of } from 'rxjs';

import { Park } from '@app/models/parks/park';

import { ParkDetailSummary } from '@app/models/parks/park-detail-summary';

import { ParkMapItems } from '@app/models/parks/park-map-items';

import { ClosedEntityFilter } from '@app/models/shared/closed-entity-filter';

import { AnonymousHttpOptions } from '@core/http/auth/anonymous-http-options';

import { SKIP_AUTHORIZATION_HEADER } from '@core/http/auth/auth-request-policy';

import { ParkMapHttpOptions, ParkMapParksPort } from '../../park-map-data.ports';

function createPark(status: Park['status'] = 'Operating'): Park {
  return {
    id: 'park-1',
    name: 'Walibi Belgium',
    status,
    countryCode: 'BE',
    latitude: 50.7,
    longitude: 4.5,
    isVisible: true
  };
}

function createSummary(status: Park['status'], totalItems: number): ParkDetailSummary {
  return {
    park: createPark(status),
    mainImage: null,
    references: {
      founderName: null,
      operatorName: null
    },
    stats: {
      totalItems,
      zoneCount: totalItems > 0 ? 1 : 0,
      attractionCount: totalItems,
      restaurantCount: 0,
      showCount: 0,
      shopCount: 0,
      hotelCount: 0,
      countsByCategory: {
        Attraction: totalItems
      }
    }
  };
}

function createMapItems(park: Park): ParkMapItems {
  return {
    park,
    zones: [],
    items: [],
    unlocatedItems: []
  };
}

export class FakeParksPort implements ParkMapParksPort {
  public summaryResponse$: Observable<ParkDetailSummary> = of(createSummary('Operating', 1));
  public mapItemsResponse$: Observable<ParkMapItems> = of(createMapItems(createPark('Operating')));
  public summaryResponses$: Observable<ParkDetailSummary>[] = [];
  public mapItemsResponses$: Observable<ParkMapItems>[] = [];
  public readonly summaryCalls: Array<{ id: string; closedFilter?: ClosedEntityFilter }> = [];
  public readonly mapItemsCalls: Array<{ id: string; closedFilter?: ClosedEntityFilter }> = [];
  public readonly officialMapFileCalls: Array<{ url: string; skipsAuthorization: boolean }> = [];

  getParkDetailSummary(id: string, options?: ParkMapHttpOptions): Observable<ParkDetailSummary> {
    this.summaryCalls.push({ id, closedFilter: options?.closedFilter });
    return this.summaryResponses$.shift() ?? this.summaryResponse$;
  }

  getParkMapItems(id: string, options?: ParkMapHttpOptions): Observable<ParkMapItems> {
    this.mapItemsCalls.push({ id, closedFilter: options?.closedFilter });
    return this.mapItemsResponses$.shift() ?? this.mapItemsResponse$;
  }

  getParkOfficialMapFile(url: string, options?: AnonymousHttpOptions): Observable<Blob> {
    this.officialMapFileCalls.push({
      url,
      skipsAuthorization: options?.context.get(SKIP_AUTHORIZATION_HEADER) ?? false
    });
    return of(new Blob(['map'], { type: 'application/pdf' }));
  }
}
