import { TestBed } from '@angular/core/testing';
import { Observable, of } from 'rxjs';

import { Park } from '@app/models/parks/park';
import { ParkItem } from '@app/models/parks/park-item';
import { ParksApiResponse } from '@app/models/parks/parks_api_response';
import {
  ADMIN_PARK_ITEM_EDIT_STATE_PARK_ITEMS_API_SERVICE_PORT,
  AdminParkItemEditStateParkItemsApiServicePort,
  ADMIN_PARK_ITEM_EDIT_STATE_PARKS_API_SERVICE_PORT,
  AdminParkItemEditStateParksApiServicePort
} from './admin-park-item-edit-state-data.ports';
import { AdminParkItemEditStateFacade } from './admin-park-item-edit-state.facade';

class FakeParkItemsPort implements AdminParkItemEditStateParkItemsApiServicePort {
  createParkItem(item: ParkItem): Observable<ParkItem> {
    return of(item);
  }

  getParkItemById(itemId: string): Observable<ParkItem> {
    return of({ id: itemId } as ParkItem);
  }

  updateParkItem(_itemId: string, item: ParkItem): Observable<ParkItem> {
    return of(item);
  }
}

class FakeParksPort implements AdminParkItemEditStateParksApiServicePort {
  public calls: number = 0;

  getParksPaginated(): Observable<ParksApiResponse> {
    this.calls += 1;
    return of({
      data: [
        {
          id: 'park-1',
          name: 'Walibi',
          city: 'Wavre',
          countryCode: 'BE',
          latitude: 50.7,
          longitude: 4.6,
          descriptions: []
        } as Park
      ],
      pagination: {
        currentPage: 1,
        itemsPerPage: 100,
        totalItems: 1,
        totalPages: 1
      }
    });
  }
}

describe('AdminParkItemEditStateFacade', () => {
  let facade: AdminParkItemEditStateFacade;
  let parksPort: FakeParksPort;

  beforeEach(() => {
    parksPort = new FakeParksPort();

    TestBed.configureTestingModule({
      providers: [
        AdminParkItemEditStateFacade,
        { provide: ADMIN_PARK_ITEM_EDIT_STATE_PARK_ITEMS_API_SERVICE_PORT, useClass: FakeParkItemsPort },
        { provide: ADMIN_PARK_ITEM_EDIT_STATE_PARKS_API_SERVICE_PORT, useValue: parksPort }
      ]
    });

    facade = TestBed.inject(AdminParkItemEditStateFacade);
    facade.invalidateParkOptionsCache();
  });

  it('reuses cached park options after the first load', async () => {
    await facade.loadParkOptions();
    await facade.loadParkOptions();

    expect(parksPort.calls).toBe(1);
    expect(facade.parkOptions()[0].id).toBe('park-1');
    expect(facade.parkOptions()[0].label).toContain('Walibi');
    expect(facade.parkOptions()[0].label).toContain('Wavre');
  });
});
