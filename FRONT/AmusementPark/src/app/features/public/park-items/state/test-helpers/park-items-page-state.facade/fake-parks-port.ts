import { Observable, of } from 'rxjs';

import { Park } from '@app/models/parks/park';

import { ParkExplorer } from '@app/models/parks/park-explorer';

import { ParkMapItems } from '@app/models/parks/park-map-items';

import { ClosedEntityFilter } from '@app/models/shared/closed-entity-filter';

import { AnonymousHttpOptions } from '@core/http/auth/anonymous-http-options';

import { ParkItemsPageStateParksApiServicePort } from '../../park-items-page-state-data.ports';

interface ClosedFilterHttpOptions extends AnonymousHttpOptions {
  closedFilter?: ClosedEntityFilter;
}

function createPark(status: Park['status']): Park {
  return {
    id: 'park-1',
    name: 'Six Flags New Orleans',
    status,
    countryCode: 'US',
    latitude: 30.0,
    longitude: -89.9,
    isVisible: true
  };
}

function createExplorer(): ParkExplorer {
  return {
    parkId: 'park-1',
    hasZones: false,
    overview: {
      id: null,
      name: 'overview',
      names: [],
      slug: null,
      isVirtual: true,
      totalItems: 0,
      countsByCategory: [],
      countsByType: []
    },
    zones: [],
    unassigned: null
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

export class FakeParksPort implements ParkItemsPageStateParksApiServicePort {
  public parkResponse$: Observable<Park> = of(createPark('Operating'));
  public explorerResponse$: Observable<ParkExplorer> = of(createExplorer());
  public mapItemsResponse$: Observable<ParkMapItems> = of(createMapItems(createPark('Operating')));
  public readonly parkCalls: string[] = [];
  public readonly explorerCalls: Array<{ parkId: string; closedFilter?: ClosedEntityFilter }> = [];
  public readonly mapItemsCalls: Array<{ parkId: string; closedFilter?: ClosedEntityFilter }> = [];

  getParkById(id: string): Observable<Park> {
    this.parkCalls.push(id);
    return this.parkResponse$;
  }

  getParkExplorer(parkId: string, options?: ClosedFilterHttpOptions): Observable<ParkExplorer> {
    this.explorerCalls.push({ parkId, closedFilter: options?.closedFilter });
    return this.explorerResponse$;
  }

  getParkMapItems(id: string, options?: ClosedFilterHttpOptions): Observable<ParkMapItems> {
    this.mapItemsCalls.push({ parkId: id, closedFilter: options?.closedFilter });
    return this.mapItemsResponse$;
  }
}
