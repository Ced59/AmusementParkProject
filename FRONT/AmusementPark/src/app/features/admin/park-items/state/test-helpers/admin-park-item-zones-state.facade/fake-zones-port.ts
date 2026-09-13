import { Observable, of } from 'rxjs';

import { ParkZone } from '@app/models/parks/park-zone';

import { AdminParkItemZonesStateParkZonesApiServicePort } from '../../admin-park-item-zones-state-data.ports';

export class FakeZonesPort implements AdminParkItemZonesStateParkZonesApiServicePort {
  public calls: string[] = [];

  getParkZonesByParkId(parkId: string): Observable<ParkZone[]> {
    this.calls.push(parkId);
    return of([
      {
        id: 'zone-1',
        parkId,
        name: 'Frontier',
        names: [{ languageCode: 'en', value: 'Frontier' }],
        descriptions: [],
      } as ParkZone,
    ]);
  }
}
