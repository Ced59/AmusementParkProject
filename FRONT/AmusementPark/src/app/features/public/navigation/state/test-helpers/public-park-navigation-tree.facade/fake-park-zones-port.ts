import { Observable } from 'rxjs';

import { ParkZone } from '@app/models/parks/park-zone';

import { PublicParkNavigationTreeParkZonesApiServicePort } from '../../public-park-navigation-tree-data.ports';

export class FakeParkZonesPort implements PublicParkNavigationTreeParkZonesApiServicePort {
  getParkZoneById(): Observable<ParkZone> {
    throw new Error('Park zone should not be loaded for park detail navigation.');
  }
}
