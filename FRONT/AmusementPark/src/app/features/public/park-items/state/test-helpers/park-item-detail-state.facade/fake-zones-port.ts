import { Observable, of } from 'rxjs';

import { ParkItemDetailZonesPort } from '../../park-item-detail-data.ports';

export class FakeZonesPort implements ParkItemDetailZonesPort {
  public response$: Observable<{
    name?: string | null;
  }> = of({ name: 'Mexico' });
  public readonly calls: string[] = [];

  getParkZoneById(id: string): Observable<{
    name?: string | null;
  }> {
    this.calls.push(id);
    return this.response$;
  }
}
