import { Observable, of } from 'rxjs';

import { ParksApiResponse } from '@app/models/parks/parks_api_response';

import { PassportVisitQuickCreateParksPort } from '../../passport-visit-quick-create-state-data.ports';

export class FakeParksApi implements PassportVisitQuickCreateParksPort {
  searchParks(_query: string, _page: number, _size: number, _visibleOnly: boolean): Observable<ParksApiResponse> {
    return of({ data: [], pagination: { currentPage: 1, itemsPerPage: 8, totalItems: 0, totalPages: 0 } });
  }
}
