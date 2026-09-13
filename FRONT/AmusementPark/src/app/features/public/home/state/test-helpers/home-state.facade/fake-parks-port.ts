import { Observable, of } from 'rxjs';

import { Park } from '@app/models/parks/park';

import { HomeStateParksApiServicePort } from '../../home-state-data.ports';

function createPark(id: string): Park {
  return {
    id,
    name: id,
    countryCode: 'FR',
    latitude: 48.8,
    longitude: 2.3,
    isVisible: true,
    descriptions: [{ languageCode: 'en', value: '<p>Park description.</p>' }],
  };
}

export class FakeParksPort implements HomeStateParksApiServicePort {
  public response$: Observable<Park[]> = of([createPark('park-1')]);
  public readonly calls: number[] = [];

  getRandomVisibleParks(limit: number): Observable<Park[]> {
    this.calls.push(limit);
    return this.response$;
  }
}
