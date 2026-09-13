import { Observable, of } from 'rxjs';

import { ParkItem } from '@app/models/parks/park-item';

import { ParkItemSiblingNavigation } from '@app/models/parks/park-item-sibling-navigation';

import { ParkItemDetailItemsPort } from '../../park-item-detail-data.ports';

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

function createSiblingNavigation(): ParkItemSiblingNavigation {
  return {
    parkId: 'park-1',
    currentItemId: 'item-1',
    currentPosition: 1,
    totalItems: 2,
    remainingItems: 1,
    previous: null,
    next: { id: 'item-2', name: 'Raik' },
  };
}

export class FakeItemsPort implements ParkItemDetailItemsPort {
  public itemResponse$: Observable<ParkItem> = of(createParkItem());
  public relatedResponse$: Observable<ParkItem[]> = of([]);
  public siblingResponse$: Observable<ParkItemSiblingNavigation> = of(
    createSiblingNavigation(),
  );
  public readonly itemCalls: string[] = [];
  public readonly relatedCalls: string[] = [];
  public readonly siblingCalls: string[] = [];

  getParkItemById(id: string): Observable<ParkItem> {
    this.itemCalls.push(id);
    return this.itemResponse$;
  }

  getParkItemSiblingNavigation(
    itemId: string,
  ): Observable<ParkItemSiblingNavigation> {
    this.siblingCalls.push(itemId);
    return this.siblingResponse$;
  }

  getRelatedParkItems(
    itemId: string,
    limit: number = 3,
  ): Observable<ParkItem[]> {
    this.relatedCalls.push(`${itemId}:${limit}`);
    return this.relatedResponse$;
  }
}
