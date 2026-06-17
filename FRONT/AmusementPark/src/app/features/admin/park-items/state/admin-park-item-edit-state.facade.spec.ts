import { TestBed } from '@angular/core/testing';
import { Observable, of } from 'rxjs';

import { Park } from '@app/models/parks/park';
import { ParkItem } from '@app/models/parks/park-item';
import { ParkItemAdminRow } from '@app/models/parks/park-item-admin-row';
import { ParksApiResponse } from '@app/models/parks/parks_api_response';
import { ApiResponse } from '@app/models/shared/api_reponse';
import {
  ADMIN_PARK_ITEM_EDIT_STATE_PARK_ITEMS_API_SERVICE_PORT,
  AdminParkItemEditStateParkItemsApiServicePort,
  ADMIN_PARK_ITEM_EDIT_STATE_PARKS_API_SERVICE_PORT,
  AdminParkItemEditStateParksApiServicePort
} from './admin-park-item-edit-state-data.ports';
import { AdminParkItemEditStateFacade } from './admin-park-item-edit-state.facade';

class FakeParkItemsPort implements AdminParkItemEditStateParkItemsApiServicePort {
  public readonly pageCalls: Array<{ page: number; size: number; parkId: string | null | undefined }> = [];
  public readonly rowsByPage: Map<number, ParkItemAdminRow[]> = new Map<number, ParkItemAdminRow[]>();
  public totalPages: number = 0;
  public totalItems: number = 0;

  createParkItem(item: ParkItem): Observable<ParkItem> {
    return of(item);
  }

  getParkItemById(itemId: string): Observable<ParkItem> {
    return of({ id: itemId } as ParkItem);
  }

  getParkItemsPaginated(page: number = 1, size: number = 100, parkId?: string | null): Observable<ApiResponse<ParkItemAdminRow>> {
    this.pageCalls.push({ page, size, parkId });

    return of({
      data: this.rowsByPage.get(page) ?? [],
      pagination: {
        currentPage: page,
        itemsPerPage: size,
        totalItems: this.totalItems,
        totalPages: this.totalPages
      }
    });
  }

  updateParkItem(_itemId: string, item: ParkItem): Observable<ParkItem> {
    return of(item);
  }
}

class FakeParksPort implements AdminParkItemEditStateParksApiServicePort {
  public calls: number = 0;
  public readonly pageCalls: Array<{ page: number; size: number }> = [];
  public getByIdCalls: string[] = [];
  public readonly rowsByPage: Map<number, Park[]> = new Map<number, Park[]>();
  public totalItems: number = 1;
  public totalPages: number = 1;

  getParkById(parkId: string): Observable<Park> {
    this.getByIdCalls.push(parkId);
    return of({
      id: parkId,
      name: 'Phantasialand',
      city: 'Bruhl',
      countryCode: 'DE',
      latitude: 50.8,
      longitude: 6.8,
      descriptions: []
    } as Park);
  }

  getParksPaginated(page: number = 1, size: number = 100): Observable<ParksApiResponse> {
    this.calls += 1;
    this.pageCalls.push({ page, size });
    return of({
      data: this.rowsByPage.get(page) ?? [
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
        currentPage: page,
        itemsPerPage: size,
        totalItems: this.totalItems,
        totalPages: this.totalPages
      }
    });
  }
}

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

  it('loads sequential navigation with the backend-supported page size', async () => {
    parkItemsPort.rowsByPage.set(1, [
      { id: 'item-1', parkId: 'park-1', parkName: 'Park', name: 'One', category: 'Attraction', type: 'Attraction', isVisible: false, adminReviewStatus: 'ToReview' },
      { id: 'item-2', parkId: 'park-1', parkName: 'Park', name: 'Two', category: 'Attraction', type: 'Attraction', isVisible: false, adminReviewStatus: 'ToReview' }
    ]);
    parkItemsPort.rowsByPage.set(2, [
      { id: 'item-3', parkId: 'park-1', parkName: 'Park', name: 'Three', category: 'Attraction', type: 'Attraction', isVisible: false, adminReviewStatus: 'ToReview' }
    ]);
    parkItemsPort.totalItems = 3;
    parkItemsPort.totalPages = 2;

    await facade.loadSequentialNavigation('park-1', 'item-2', true);

    expect(parkItemsPort.pageCalls.map((call: { page: number; size: number }) => ({ page: call.page, size: call.size }))).toEqual([
      { page: 1, size: 100 },
      { page: 2, size: 100 }
    ]);
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
    parkItemsPort.rowsByPage.set(1, [
      { id: 'item-1', parkId: 'park-1', parkName: 'Park', name: 'One', category: 'Attraction', type: 'Attraction', isVisible: true, adminReviewStatus: 'Validated' },
      { id: 'item-2 ', parkId: 'park-1', parkName: 'Park', name: 'Two', category: 'Attraction', type: 'Attraction', isVisible: true, adminReviewStatus: 'Validated' }
    ]);
    parkItemsPort.totalItems = 2;
    parkItemsPort.totalPages = 1;

    await facade.loadSequentialNavigation(' park-1 ', ' item-2 ', true);

    expect(parkItemsPort.pageCalls[0].parkId).toBe('park-1');
    expect(facade.sequentialNavigationState().currentPosition).toBe(2);
    expect(facade.sequentialNavigationState().previousItemId).toBe('item-1');
  });
});
