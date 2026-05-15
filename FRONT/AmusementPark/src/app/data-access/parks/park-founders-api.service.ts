import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { ParkFounder } from '@app/models/parks/park-founder';
import { PagedCollectionResponse, unwrapCollection } from '../shared/api-helpers';
import { PARK_FOUNDERS_API_ENDPOINTS } from './park-founders-api-endpoints';

@Injectable({
  providedIn: 'root'
})
export class ParkFoundersApiService {
  private readonly jsonHttpOptions = {
    headers: new HttpHeaders({
      'Content-Type': 'application/json'
    })
  };

  constructor(private readonly http: HttpClient) {
  }

  getParkFounders(): Observable<ParkFounder[]> {
    const url: string = `${environment.apiBaseUrl}${PARK_FOUNDERS_API_ENDPOINTS.getParkFounders}`;
    return this.http.get<ParkFounder[] | PagedCollectionResponse<ParkFounder>>(url).pipe(
      map((response: ParkFounder[] | PagedCollectionResponse<ParkFounder>) => unwrapCollection<ParkFounder>(response))
    );
  }

  getParkFounderById(id: string): Observable<ParkFounder> {
    const url: string = `${environment.apiBaseUrl}${PARK_FOUNDERS_API_ENDPOINTS.getParkFounderById(id)}`;
    return this.http.get<ParkFounder>(url);
  }

  createParkFounder(founder: ParkFounder): Observable<ParkFounder> {
    const url: string = `${environment.apiBaseUrl}${PARK_FOUNDERS_API_ENDPOINTS.createParkFounder}`;
    return this.http.post<ParkFounder>(url, JSON.stringify(founder), this.jsonHttpOptions);
  }

  updateParkFounder(id: string, founder: ParkFounder): Observable<ParkFounder> {
    const url: string = `${environment.apiBaseUrl}${PARK_FOUNDERS_API_ENDPOINTS.updateParkFounder(id)}`;
    return this.http.put<ParkFounder>(url, JSON.stringify(founder), this.jsonHttpOptions);
  }
}
