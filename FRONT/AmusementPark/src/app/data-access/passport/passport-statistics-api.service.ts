import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  PassportGlobalStatistics,
  PassportItemStatistics,
  PassportParkStatistics,
  PassportYearStatistics
} from '@app/models/passport/passport-statistics.models';
import { environment } from '../../../environments/environment';
import { PASSPORT_STATISTICS_API_ENDPOINTS } from './passport-statistics-api-endpoints';

@Injectable({ providedIn: 'root' })
export class PassportStatisticsApiService {
  constructor(private readonly http: HttpClient) {
  }

  getGlobalStatistics(year: number | null, parkId: string | null): Observable<PassportGlobalStatistics> {
    const url: string = `${environment.apiBaseUrl}${PASSPORT_STATISTICS_API_ENDPOINTS.global}`;
    const params: Record<string, string> = {};
    if (year !== null) {
      params['year'] = String(year);
    }
    if (parkId) {
      params['parkId'] = parkId;
    }
    return this.http.get<PassportGlobalStatistics>(url, { params, transferCache: false });
  }

  getItemStatistics(parkItemId: string): Observable<PassportItemStatistics> {
    const url: string = `${environment.apiBaseUrl}${PASSPORT_STATISTICS_API_ENDPOINTS.item(parkItemId)}`;
    return this.http.get<PassportItemStatistics>(url, { transferCache: false });
  }

  getParkStatistics(parkId: string): Observable<PassportParkStatistics> {
    const url: string = `${environment.apiBaseUrl}${PASSPORT_STATISTICS_API_ENDPOINTS.park(parkId)}`;
    return this.http.get<PassportParkStatistics>(url, { transferCache: false });
  }

  getYearStatistics(year: number): Observable<PassportYearStatistics> {
    const url: string = `${environment.apiBaseUrl}${PASSPORT_STATISTICS_API_ENDPOINTS.year(year)}`;
    return this.http.get<PassportYearStatistics>(url, { transferCache: false });
  }
}
