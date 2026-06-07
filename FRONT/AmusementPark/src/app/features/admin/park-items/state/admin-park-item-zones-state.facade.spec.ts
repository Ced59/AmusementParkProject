import { TestBed } from '@angular/core/testing';
import { Observable, of } from 'rxjs';

import { ParkZone } from '@app/models/parks/park-zone';
import {
  ADMIN_PARK_ITEM_ZONES_STATE_PARK_ZONES_API_SERVICE_PORT,
  AdminParkItemZonesStateParkZonesApiServicePort
} from './admin-park-item-zones-state-data.ports';
import { AdminParkItemZonesStateFacade } from './admin-park-item-zones-state.facade';

class FakeZonesPort implements AdminParkItemZonesStateParkZonesApiServicePort {
  public calls: string[] = [];

  getParkZonesByParkId(parkId: string): Observable<ParkZone[]> {
    this.calls.push(parkId);
    return of([
      {
        id: 'zone-1',
        parkId,
        name: 'Frontier',
        names: [{ languageCode: 'en', value: 'Frontier' }],
        descriptions: []
      } as ParkZone
    ]);
  }
}

describe('AdminParkItemZonesStateFacade', () => {
  let facade: AdminParkItemZonesStateFacade;
  let port: FakeZonesPort;

  beforeEach(() => {
    port = new FakeZonesPort();

    TestBed.configureTestingModule({
      providers: [
        AdminParkItemZonesStateFacade,
        { provide: ADMIN_PARK_ITEM_ZONES_STATE_PARK_ZONES_API_SERVICE_PORT, useValue: port }
      ]
    });

    facade = TestBed.inject(AdminParkItemZonesStateFacade);
    facade.invalidateCache();
  });

  it('caches zone options by park and language', () => {
    facade.load('park-1', 'en');
    facade.load('park-1', 'en');
    facade.load('park-1', 'fr');

    expect(port.calls).toEqual(['park-1', 'park-1']);
    expect(facade.zones()).toEqual([
      { id: 'zone-1', label: 'Frontier' }
    ]);
  });

  it('can invalidate one park cache', () => {
    facade.load('park-1', 'en');
    facade.invalidateCache('park-1');
    facade.load('park-1', 'en');

    expect(port.calls).toEqual(['park-1', 'park-1']);
  });
});
