import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { ParkZone } from '@app/models/parks/park-zone';
import { PagedCollectionResponse, unwrapCollection } from '../shared/api-helpers';
import { PARK_ZONES_API_ENDPOINTS } from './park-zones-api-endpoints';

@Injectable({
  providedIn: 'root'
})
export class ParkZonesApiService {
  constructor(private readonly http: HttpClient) {
  }

  getParkZonesByParkId(parkId: string): Observable<ParkZone[]> {
    const url: string = `${environment.apiBaseUrl}${PARK_ZONES_API_ENDPOINTS.getParkZonesByParkId(parkId)}`;
    return this.http.get<ParkZone[] | PagedCollectionResponse<ParkZone>>(url).pipe(
      map((response: ParkZone[] | PagedCollectionResponse<ParkZone>) => unwrapCollection<ParkZone>(response))
    );
  }

  getParkZoneById(id: string): Observable<ParkZone> {
    const url: string = `${environment.apiBaseUrl}${PARK_ZONES_API_ENDPOINTS.getParkZoneById(id)}`;
    return this.http.get<ParkZone>(url);
  }

  createParkZone(zone: ParkZone): Observable<ParkZone> {
    const url: string = `${environment.apiBaseUrl}${PARK_ZONES_API_ENDPOINTS.createParkZone}`;
    return this.http.post<ParkZone>(url, zone);
  }

  updateParkZone(id: string, zone: ParkZone): Observable<ParkZone> {
    const url: string = `${environment.apiBaseUrl}${PARK_ZONES_API_ENDPOINTS.updateParkZone(id)}`;
    return this.http.put<ParkZone>(url, zone);
  }

  deleteParkZone(id: string): Observable<boolean> {
    const url: string = `${environment.apiBaseUrl}${PARK_ZONES_API_ENDPOINTS.deleteParkZone(id)}`;
    return this.http.delete<boolean>(url);
  }
}
