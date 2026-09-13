import { Observable, of } from 'rxjs';

import { ParkItem } from '@app/models/parks/park-item';

import { ParkItemImagesItemsPort } from '../../park-item-images-data.ports';

function createParkItem(overrides: Partial<ParkItem> = {}): ParkItem {
  return {
    id: 'item-1',
    parkId: 'park-1',
    zoneId: null,
    name: 'Taron',
    category: 'Attraction',
    type: 'RollerCoaster',
    latitude: 50.8,
    longitude: 6.8,
    isVisible: true,
    ...overrides,
  } as ParkItem;
}

export class FakeItemsPort implements ParkItemImagesItemsPort {
  public response$: Observable<ParkItem> = of(createParkItem());
  public readonly calls: string[] = [];

  getParkItemById(id: string): Observable<ParkItem> {
    this.calls.push(id);
    return this.response$;
  }
}
