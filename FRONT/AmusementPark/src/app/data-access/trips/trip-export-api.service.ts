import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { TripExport } from '@app/models/trips/trip-export.models';
import { environment } from '../../../environments/environment';
import { TRIP_API_ENDPOINTS } from './trip-api-endpoints';

@Injectable({ providedIn: 'root' })
export class TripExportApiService {
  constructor(private readonly http: HttpClient) {
  }

  get(tripPlanId: string, exportRequestId: string): Observable<TripExport> {
    return this.http.get<TripExport>(
      `${environment.apiBaseUrl}${TRIP_API_ENDPOINTS.export(tripPlanId)}`,
      {
        headers: new HttpHeaders().set('Idempotency-Key', exportRequestId),
        transferCache: false
      }
    );
  }
}
