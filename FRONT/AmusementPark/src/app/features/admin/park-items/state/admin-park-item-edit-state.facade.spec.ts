import { TestBed } from '@angular/core/testing';
import { Observable, of } from 'rxjs';

import { Park } from '@app/models/parks/park';
import { ParkItem } from '@app/models/parks/park-item';
import { ParkItemSiblingNavigation } from '@app/models/parks/park-item-sibling-navigation';
import { ParksApiResponse } from '@app/models/parks/parks_api_response';
import {
  ADMIN_PARK_ITEM_EDIT_STATE_PARK_ITEMS_API_SERVICE_PORT,
  AdminParkItemEditStateParkItemsApiServicePort,
  ADMIN_PARK_ITEM_EDIT_STATE_PARKS_API_SERVICE_PORT,
  AdminParkItemEditStateParksApiServicePort
} from './admin-park-item-edit-state-data.ports';
import { AdminParkItemEditStateFacade } from './admin-park-item-edit-state.facade';
import { FakeParkItemsPort } from './test-helpers/admin-park-item-edit-state.facade/fake-park-items-port';
import { FakeParksPort } from './test-helpers/admin-park-item-edit-state.facade/fake-parks-port';

describe('AdminParkItemEditStateFacade', () => {
  let facade: AdminParkItemEditStateFacade;
  let parkItemsPort: FakeParkItemsPort;
  let parksPort: FakeParksPort;

  beforeEach(() => {
    parkItemsPort = new FakeParkItemsPort();
    parksPort = new FakeParksPort();

    TestBed.configureTestingModule({
      providers: [
        AdminParkItemEditStateFacade,
        { provide: ADMIN_PARK_ITEM_EDIT_STATE_PARK_ITEMS_API_SERVICE_PORT, useValue: parkItemsPort },
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

  it('loads the current park option without waiting for the full park list', async () => {
    await facade.ensureParkOption('park-2');

    expect(parksPort.getByIdCalls).toEqual(['park-2']);
    expect(facade.parkOptions().map((option: { id: string }) => option.id)).toContain('park-2');

    await facade.loadParkOptions();

    expect(parksPort.calls).toBe(1);
  });

  it('loads full park options one page at a time', async () => {
    parksPort.rowsByPage.set(1, [
      { id: 'park-1', name: 'Walibi', city: 'Wavre', countryCode: 'BE', latitude: 50.7, longitude: 4.6, descriptions: [] } as Park
    ]);
    parksPort.rowsByPage.set(2, [
      { id: 'park-2', name: 'Phantasialand', city: 'Bruhl', countryCode: 'DE', latitude: 50.8, longitude: 6.8, descriptions: [] } as Park
    ]);
    parksPort.totalItems = 2;
    parksPort.totalPages = 2;

    await facade.loadParkOptions();

    expect(parksPort.pageCalls).toEqual([
      { page: 1, size: 100 },
      { page: 2, size: 100 }
    ]);
    expect(facade.parkOptions().map((option: { id: string }) => option.id).sort()).toEqual(['park-1', 'park-2']);
  });

  it('loads sequential navigation from the shared lightweight endpoint', async () => {
    await facade.loadSequentialNavigation('park-1', 'item-2', true);

    expect(parkItemsPort.siblingCalls).toEqual(['item-2']);
    expect(facade.sequentialNavigationState()).toEqual({
      isLoading: false,
      currentItemId: 'item-2',
      currentPosition: 2,
      remainingItems: 1,
      totalItems: 3,
      previousItemId: 'item-1',
      nextItemId: 'item-3'
    });
  });

  it('normalizes park and item ids before loading sequential navigation', async () => {
    parkItemsPort.siblingResponse = {
      parkId: 'park-1',
      currentItemId: 'item-2',
      currentPosition: 2,
      totalItems: 2,
      remainingItems: 0,
      previous: { id: 'item-1', name: 'One' },
      next: null
    };

    await facade.loadSequentialNavigation(' park-1 ', ' item-2 ', true);

    expect(parkItemsPort.siblingCalls).toEqual(['item-2']);
    expect(facade.sequentialNavigationState().currentPosition).toBe(2);
    expect(facade.sequentialNavigationState().previousItemId).toBe('item-1');
  });
});
