import { Observable, of } from 'rxjs';

import { AttractionManufacturer } from '@app/models/parks/attraction-manufacturer';

import { AdminParkItemManufacturersStateManufacturersApiServicePort } from '../../admin-park-item-manufacturers-state-data.ports';

export class FakeManufacturersPort implements AdminParkItemManufacturersStateManufacturersApiServicePort {
  public calls: number = 0;
  public includeHiddenValues: boolean[] = [];

  getAttractionManufacturers(includeHidden: boolean = false): Observable<AttractionManufacturer[]> {
    this.calls += 1;
    this.includeHiddenValues.push(includeHidden);
    return of([
      {
        id: 'manufacturer-1',
        name: 'Mack Rides',
        aliases: [],
        descriptions: []
      } as AttractionManufacturer
    ]);
  }
}
