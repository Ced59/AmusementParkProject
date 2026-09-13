import { TestBed } from '@angular/core/testing';
import { Observable, of } from 'rxjs';

import { AttractionManufacturer } from '@app/models/parks/attraction-manufacturer';
import {
  ADMIN_PARK_ITEM_MANUFACTURERS_STATE_MANUFACTURERS_API_SERVICE_PORT,
  AdminParkItemManufacturersStateManufacturersApiServicePort
} from './admin-park-item-manufacturers-state-data.ports';
import { AdminParkItemManufacturersStateFacade } from './admin-park-item-manufacturers-state.facade';
import { FakeManufacturersPort } from './test-helpers/admin-park-item-manufacturers-state.facade/fake-manufacturers-port';

describe('AdminParkItemManufacturersStateFacade', () => {
  let facade: AdminParkItemManufacturersStateFacade;
  let port: FakeManufacturersPort;

  beforeEach(() => {
    port = new FakeManufacturersPort();

    TestBed.configureTestingModule({
      providers: [
        AdminParkItemManufacturersStateFacade,
        { provide: ADMIN_PARK_ITEM_MANUFACTURERS_STATE_MANUFACTURERS_API_SERVICE_PORT, useValue: port }
      ]
    });

    facade = TestBed.inject(AdminParkItemManufacturersStateFacade);
    facade.invalidateCache();
  });

  it('reuses cached manufacturer options on repeated loads', () => {
    facade.load();
    facade.load();

    expect(port.calls).toBe(1);
    expect(port.includeHiddenValues).toEqual([true]);
    expect(facade.manufacturerOptions()).toEqual([
      { id: 'manufacturer-1', label: 'Mack Rides' }
    ]);
  });

  it('reloads manufacturers after cache invalidation', () => {
    facade.load();
    facade.invalidateCache();
    facade.load();

    expect(port.calls).toBe(2);
  });
});
