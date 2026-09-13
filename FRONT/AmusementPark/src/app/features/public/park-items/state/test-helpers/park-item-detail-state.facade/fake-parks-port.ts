import { Observable, of } from 'rxjs';

import { Park } from '@app/models/parks/park';

import { ParkItemDetailParksPort } from '../../park-item-detail-data.ports';

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

export class FakeParksPort implements ParkItemDetailParksPort {
  public parkResponse$: Observable<Park> = of(createPark());
  public readonly calls: string[] = [];

  getParkById(id: string): Observable<Park> {
    this.calls.push(id);
    return this.parkResponse$;
  }
}
