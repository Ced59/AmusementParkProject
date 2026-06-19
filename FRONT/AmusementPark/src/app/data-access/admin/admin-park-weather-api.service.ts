import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { ParkWeatherRun, ParkWeatherRunItem } from '@app/models/admin/park-weather/park-weather-admin.models';

@Injectable({
  providedIn: 'root'
})
export class AdminParkWeatherApiService {
  private readonly baseUrl: string = `${environment.apiBaseUrl}admin/park-weather`;

  constructor(private readonly http: HttpClient) {
  }

  getLatestRun(): Observable<ParkWeatherRun | null> {
    return this.http.get<ParkWeatherRun | null>(`${this.baseUrl}/runs/latest`);
  }

  getRun(runId: string): Observable<ParkWeatherRun> {
    return this.http.get<ParkWeatherRun>(`${this.baseUrl}/runs/${encodeURIComponent(runId)}`);
  }

  getRunItems(runId: string, status: string | null = null): Observable<ParkWeatherRunItem[]> {
    let params: HttpParams = new HttpParams();
    if (status) {
      params = params.set('status', status);
    }

    return this.http.get<ParkWeatherRunItem[]>(`${this.baseUrl}/runs/${encodeURIComponent(runId)}/items`, { params });
  }

  startManualRefresh(): Observable<ParkWeatherRun> {
    return this.http.post<ParkWeatherRun>(`${this.baseUrl}/refresh`, {});
  }

  retryFailedRun(runId: string): Observable<ParkWeatherRun> {
    return this.http.post<ParkWeatherRun>(`${this.baseUrl}/runs/${encodeURIComponent(runId)}/retry-failed`, {});
  }

  refreshPark(parkId: string): Observable<ParkWeatherRun> {
    return this.http.post<ParkWeatherRun>(`${this.baseUrl}/parks/${encodeURIComponent(parkId)}/refresh`, {});
  }
}
