import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { ParkItemAdminRow } from '@app/models/parks/park-item-admin-row';
import { ParkItem } from '@app/models/parks/park-item';
import { ApiResponse } from '@app/models/shared/api_reponse';
import {
  normalizeParkItem,
  normalizeParkItemAdminRows,
  PagedCollectionResponse,
  unwrapCollection
} from '../shared/api-helpers';
import { PARK_ITEMS_API_ENDPOINTS } from './park-items-api-endpoints';

@Injectable({
  providedIn: 'root'
})
export class ParkItemsApiService {
  constructor(private readonly http: HttpClient) {
  }

  getParkItemsByParkId(parkId: string): Observable<ParkItem[]> {
    const url: string = `${environment.apiBaseUrl}${PARK_ITEMS_API_ENDPOINTS.getParkItemsByParkId(parkId)}`;
    return this.http.get<ParkItem[] | PagedCollectionResponse<ParkItem>>(url).pipe(
      map((response: ParkItem[] | PagedCollectionResponse<ParkItem>) => unwrapCollection<ParkItem>(response).map((item: ParkItem) => normalizeParkItem(item)))
    );
  }

  getParkItemsPaginated(
    page: number,
    size: number,
    parkId?: string | null,
    search?: string | null
  ): Observable<ApiResponse<ParkItemAdminRow>> {
    const url: string = `${environment.apiBaseUrl}${PARK_ITEMS_API_ENDPOINTS.getParkItemsPaginated(page, size, parkId, search)}`;
    return this.http.get<ApiResponse<ParkItemAdminRow>>(url).pipe(
      map((response: ApiResponse<ParkItemAdminRow>) => ({
        ...response,
        data: normalizeParkItemAdminRows(response.data)
      }))
    );
  }

  getParkItemById(id: string): Observable<ParkItem> {
    const url: string = `${environment.apiBaseUrl}${PARK_ITEMS_API_ENDPOINTS.getParkItemById(id)}`;
    return this.http.get<ParkItem>(url).pipe(
      map((item: ParkItem) => normalizeParkItem(item))
    );
  }

  createParkItem(item: ParkItem): Observable<ParkItem> {
    const url: string = `${environment.apiBaseUrl}${PARK_ITEMS_API_ENDPOINTS.createParkItem}`;
    return this.http.post<ParkItem>(url, item);
  }

  updateParkItem(id: string, item: ParkItem): Observable<ParkItem> {
    const url: string = `${environment.apiBaseUrl}${PARK_ITEMS_API_ENDPOINTS.updateParkItem(id)}`;
    return this.http.put<ParkItem>(url, item);
  }

  deleteParkItem(id: string): Observable<boolean> {
    const url: string = `${environment.apiBaseUrl}${PARK_ITEMS_API_ENDPOINTS.deleteParkItem(id)}`;
    return this.http.delete<boolean>(url);
  }
}
