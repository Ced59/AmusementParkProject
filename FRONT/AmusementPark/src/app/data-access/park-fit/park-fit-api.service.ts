import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { ParkFitSearchRequest, ParkFitSearchResponse } from '@app/models/park-fit/park-fit-search.models';
import { anonymousHttpOptions } from '@core/http/auth/anonymous-http-options';
import { environment } from '../../../environments/environment';
import { PARK_FIT_API_ENDPOINTS } from './park-fit-api-endpoints';

@Injectable({ providedIn: 'root' })
export class ParkFitApiService {
  constructor(private readonly http: HttpClient) {
  }

  search(request: ParkFitSearchRequest): Observable<ParkFitSearchResponse> {
    const url: string = `${environment.apiBaseUrl}${PARK_FIT_API_ENDPOINTS.search}`;
    return this.http.post<ParkFitSearchResponse>(url, request, {
      ...anonymousHttpOptions(),
      transferCache: false
    });
  }
}
