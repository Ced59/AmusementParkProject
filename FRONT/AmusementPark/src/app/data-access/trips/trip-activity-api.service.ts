import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { TripActivityPage } from '@app/models/trips/trip-activity.models';
import { environment } from '../../../environments/environment';
import { TRIP_API_ENDPOINTS } from './trip-api-endpoints';

@Injectable({ providedIn: 'root' })
export class TripActivityApiService {
  constructor(private readonly http: HttpClient) {
  }

  get(tripPlanId: string, beforeSequence: number | null = null): Observable<TripActivityPage> {
    const params: HttpParams = beforeSequence === null
      ? new HttpParams()
      : new HttpParams().set('beforeSequence', beforeSequence);
    return this.http.get<TripActivityPage>(
      `${environment.apiBaseUrl}${TRIP_API_ENDPOINTS.activity(tripPlanId)}`,
      { params, transferCache: false }
    );
  }
}
