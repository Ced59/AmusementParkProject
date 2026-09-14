import { isPlatformBrowser } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Inject, Injectable, PLATFORM_ID } from '@angular/core';

import { ParkFitPilotObservation } from '@app/models/park-fit/park-fit-pilot-observation.model';
import { anonymousHttpOptions } from '@core/http/auth/anonymous-http-options';
import { environment } from '../../../environments/environment';
import { PARK_FIT_API_ENDPOINTS } from './park-fit-api-endpoints';

@Injectable({ providedIn: 'root' })
export class ParkFitPilotTelemetryApiService {
  private readonly isBrowser: boolean;

  constructor(
    private readonly http: HttpClient,
    @Inject(PLATFORM_ID) platformId: object
  ) {
    this.isBrowser = isPlatformBrowser(platformId);
  }

  track(observation: ParkFitPilotObservation): void {
    if (!this.isBrowser) {
      return;
    }

    const url: string = `${environment.apiBaseUrl}${PARK_FIT_API_ENDPOINTS.pilotEvents}`;
    this.http.post<void>(url, observation, {
      ...anonymousHttpOptions(),
      transferCache: false
    }).subscribe({
      error: (): void => undefined
    });
  }
}
