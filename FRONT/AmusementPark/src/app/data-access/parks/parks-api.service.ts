import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { ParkExplorer } from '@app/models/parks/park-explorer';
import { Park } from '@app/models/parks/park';
import { ParksApiResponse } from '@app/models/parks/parks_api_response';
import { PARKS_API_ENDPOINTS } from './parks-api-endpoints';

@Injectable({
  providedIn: 'root'
})
export class ParksApiService {
  private readonly jsonHttpOptions = {
    headers: new HttpHeaders({
      'Content-Type': 'application/json'
    })
  };

  constructor(private readonly http: HttpClient) {
  }

  getParksPaginated(page: number, size: number): Observable<ParksApiResponse> {
    const url: string = `${environment.apiBaseUrl}${PARKS_API_ENDPOINTS.getParksPaginated(page, size)}`;
    return this.http.get<ParksApiResponse>(url);
  }

  getParkById(id: string): Observable<Park> {
    const url: string = `${environment.apiBaseUrl}${PARKS_API_ENDPOINTS.getParkById(id)}`;
    return this.http.get<Park>(url);
  }

  searchParks(name: string, page: number, size: number): Observable<ParksApiResponse> {
    const url: string = `${environment.apiBaseUrl}${PARKS_API_ENDPOINTS.searchParks(name, page, size)}`;
    return this.http.get<ParksApiResponse>(url);
  }

  getParksByLocation(latitude: number, longitude: number, radius: number): Observable<Park[]> {
    const url: string = `${environment.apiBaseUrl}${PARKS_API_ENDPOINTS.getParksByLocation(latitude, longitude, radius)}`;
    return this.http.get<Park[]>(url);
  }

  updateParkVisibility(parkId: string, isVisible: boolean): Observable<Park> {
    const url: string = `${environment.apiBaseUrl}${PARKS_API_ENDPOINTS.updateParkVisibility(parkId)}`;
    return this.http.patch<Park>(url, { isVisible });
  }

  createPark(park: Park): Observable<Park> {
    const url: string = `${environment.apiBaseUrl}${PARKS_API_ENDPOINTS.createPark}`;
    return this.http.post<Park>(url, JSON.stringify(park), this.jsonHttpOptions);
  }

  updatePark(id: string, park: Park): Observable<Park> {
    const url: string = `${environment.apiBaseUrl}${PARKS_API_ENDPOINTS.updatePark(id)}`;
    return this.http.put<Park>(url, JSON.stringify(park), this.jsonHttpOptions);
  }

  getParkExplorer(parkId: string): Observable<ParkExplorer> {
    const url: string = `${environment.apiBaseUrl}${PARKS_API_ENDPOINTS.getParkExplorer(parkId)}`;
    return this.http.get<ParkExplorer>(url);
  }
}
