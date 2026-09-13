import { Observable, of } from 'rxjs';

import { ParkItem } from '@app/models/parks/park-item';

import { ParkItemSiblingNavigation } from '@app/models/parks/park-item-sibling-navigation';

import { AdminParkItemEditStateParkItemsApiServicePort } from '../../admin-park-item-edit-state-data.ports';

export class FakeParkItemsPort implements AdminParkItemEditStateParkItemsApiServicePort {
  public readonly siblingCalls: string[] = [];
  public siblingResponse: ParkItemSiblingNavigation = {
    parkId: 'park-1',
    currentItemId: 'item-2',
    currentPosition: 2,
    totalItems: 3,
    remainingItems: 1,
    previous: { id: 'item-1', name: 'One' },
    next: { id: 'item-3', name: 'Three' }
  };

  createParkItem(item: ParkItem): Observable<ParkItem> {
    return of(item);
  }

  getParkItemById(itemId: string): Observable<ParkItem> {
    return of({ id: itemId } as ParkItem);
  }

  getParkItemSiblingNavigation(itemId: string): Observable<ParkItemSiblingNavigation> {
    this.siblingCalls.push(itemId);
    return of(this.siblingResponse);
  }

  updateParkItem(_itemId: string, item: ParkItem): Observable<ParkItem> {
    return of(item);
  }
}
