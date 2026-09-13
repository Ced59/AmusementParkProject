import { Observable, of } from 'rxjs';

import { Park } from '@app/models/parks/park';

import { ParksApiResponse } from '@app/models/parks/parks_api_response';

import { AdminParkItemEditStateParksApiServicePort } from '../../admin-park-item-edit-state-data.ports';

export class FakeParksPort implements AdminParkItemEditStateParksApiServicePort {
  public calls: number = 0;
  public readonly pageCalls: Array<{ page: number; size: number }> = [];
  public getByIdCalls: string[] = [];
  public readonly rowsByPage: Map<number, Park[]> = new Map<number, Park[]>();
  public totalItems: number = 1;
  public totalPages: number = 1;

  getParkById(parkId: string): Observable<Park> {
    this.getByIdCalls.push(parkId);
    return of({
      id: parkId,
      name: 'Phantasialand',
      city: 'Bruhl',
      countryCode: 'DE',
      latitude: 50.8,
      longitude: 6.8,
      descriptions: []
    } as Park);
  }

  getParksPaginated(page: number = 1, size: number = 100): Observable<ParksApiResponse> {
    this.calls += 1;
    this.pageCalls.push({ page, size });
    return of({
      data: this.rowsByPage.get(page) ?? [
        {
          id: 'park-1',
          name: 'Walibi',
          city: 'Wavre',
          countryCode: 'BE',
          latitude: 50.7,
          longitude: 4.6,
          descriptions: []
        } as Park
      ],
      pagination: {
        currentPage: page,
        itemsPerPage: size,
        totalItems: this.totalItems,
        totalPages: this.totalPages
      }
    });
  }
}
