import { Observable, of } from 'rxjs';

import { AttractionManufacturer } from '@app/models/parks/attraction-manufacturer';

import { ParkItemsPageStateManufacturersApiServicePort } from '../../park-items-page-state-data.ports';

export class FakeManufacturersPort implements ParkItemsPageStateManufacturersApiServicePort {
  getAttractionManufacturers(): Observable<AttractionManufacturer[]> {
    return of([]);
  }
}
