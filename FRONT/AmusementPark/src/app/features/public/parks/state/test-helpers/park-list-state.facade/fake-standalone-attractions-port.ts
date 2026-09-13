import { Observable, of } from 'rxjs';

import { ParkRegionFilter } from '@shared/models/geo/world-region-filter.model';

import { ParkListStateStandaloneAttractionsApiServicePort } from '../../park-list-state-data.ports';

import { StandaloneAttractionMapPoint } from '@app/models/standalone-attractions/standalone-attraction-map-point';

function createStandaloneMapPoint(): StandaloneAttractionMapPoint {
  return {
    id: 'standalone-1',
    name: 'Pendolino',
    countryCode: 'AT',
    type: 'RollerCoaster',
    subtype: 'Mountain Coaster',
    status: 'Operating',
    city: 'Nassfeld',
    street: null,
    postalCode: null,
    latitude: 46.56,
    longitude: 13.25
  };
}

export class FakeStandaloneAttractionsPort implements ParkListStateStandaloneAttractionsApiServicePort {
  public response$: Observable<StandaloneAttractionMapPoint[]> = of([createStandaloneMapPoint()]);
  public readonly calls: Array<{ query: string; region: ParkRegionFilter | null }> = [];

  getVisibleMapPoints(query: string = '', region: ParkRegionFilter | null = null): Observable<StandaloneAttractionMapPoint[]> {
    this.calls.push({ query, region });
    return this.response$;
  }
}
