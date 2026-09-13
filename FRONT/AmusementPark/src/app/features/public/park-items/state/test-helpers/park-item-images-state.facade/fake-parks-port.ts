import { Observable, of } from 'rxjs';

import { Park } from '@app/models/parks/park';

import { ParkItemImagesParksPort } from '../../park-item-images-data.ports';

function createPark(): Park {
  return {
    id: 'park-1',
    name: 'Phantasialand',
    countryCode: 'DE',
    latitude: 50.8,
    longitude: 6.8,
    isVisible: true,
    descriptions: [],
  };
}

export class FakeParksPort implements ParkItemImagesParksPort {
  public response$: Observable<Park> = of(createPark());
  public readonly calls: string[] = [];

  getParkById(id: string): Observable<Park> {
    this.calls.push(id);
    return this.response$;
  }
}
