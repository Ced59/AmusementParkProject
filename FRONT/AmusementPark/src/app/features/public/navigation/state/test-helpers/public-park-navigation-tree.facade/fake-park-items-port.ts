import { Observable } from 'rxjs';

import { ParkItem } from '@app/models/parks/park-item';

import { PublicParkNavigationTreeParkItemsApiServicePort } from '../../public-park-navigation-tree-data.ports';

export class FakeParkItemsPort implements PublicParkNavigationTreeParkItemsApiServicePort {
  getParkItemById(): Observable<ParkItem> {
    throw new Error('Park item should not be loaded for park detail navigation.');
  }
}
