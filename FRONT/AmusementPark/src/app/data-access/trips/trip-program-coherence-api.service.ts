import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { TripProgramCoherence } from '@app/models/trips/trip.models';
import { environment } from '../../../environments/environment';
import { TRIP_API_ENDPOINTS } from './trip-api-endpoints';

@Injectable({ providedIn: 'root' })
export class TripProgramCoherenceApiService {
  constructor(private readonly http: HttpClient) {
  }

  get(tripPlanId: string): Observable<TripProgramCoherence> {
    return this.http.get<TripProgramCoherence>(
      `${environment.apiBaseUrl}${TRIP_API_ENDPOINTS.coherence(tripPlanId)}`,
      { transferCache: false }
    );
  }
}
