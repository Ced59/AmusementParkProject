import { Observable, of } from 'rxjs';

import { ParkZone } from '@app/models/parks/park-zone';

import { ParkItemsPageStateParkZonesApiServicePort } from '../../park-items-page-state-data.ports';

export class FakeZonesPort implements ParkItemsPageStateParkZonesApiServicePort {
  getParkZonesByParkId(): Observable<ParkZone[]> {
    return of([]);
  }
}
