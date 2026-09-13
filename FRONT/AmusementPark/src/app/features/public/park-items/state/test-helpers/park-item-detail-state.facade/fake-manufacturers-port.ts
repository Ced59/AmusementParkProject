import { Observable, of } from 'rxjs';

import { ParkItemDetailManufacturersPort } from '../../park-item-detail-data.ports';

export class FakeManufacturersPort implements ParkItemDetailManufacturersPort {
  public response$: Observable<{
    name?: string | null;
  }> = of({ name: 'Intamin' });
  public readonly calls: string[] = [];

  getAttractionManufacturerById(id: string): Observable<{
    name?: string | null;
  }> {
    this.calls.push(id);
    return this.response$;
  }
}
