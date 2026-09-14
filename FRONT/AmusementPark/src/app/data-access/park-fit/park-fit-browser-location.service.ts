import { Injectable, PLATFORM_ID, inject } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { Observable } from 'rxjs';

import { ParkFitSearchOrigin } from '@features/public/park-fit/models/park-fit-search-form.models';

@Injectable({ providedIn: 'root' })
export class ParkFitBrowserLocationService {
  private readonly platformId: object = inject(PLATFORM_ID);

  requestCurrentPosition(): Observable<ParkFitSearchOrigin> {
    return new Observable<ParkFitSearchOrigin>((subscriber) => {
      if (!isPlatformBrowser(this.platformId) || !navigator.geolocation) {
        subscriber.error('unsupported');
        return;
      }

      navigator.geolocation.getCurrentPosition(
        (position: GeolocationPosition): void => {
          subscriber.next({
            latitude: minimizeCoordinate(position.coords.latitude),
            longitude: minimizeCoordinate(position.coords.longitude)
          });
          subscriber.complete();
        },
        (error: GeolocationPositionError): void => subscriber.error(resolveErrorCode(error)),
        {
          enableHighAccuracy: false,
          timeout: 10000,
          maximumAge: 0
        }
      );
    });
  }
}

function minimizeCoordinate(value: number): number {
  return Math.round(value * 10000) / 10000;
}

function resolveErrorCode(error: GeolocationPositionError): string {
  if (error.code === error.PERMISSION_DENIED) {
    return 'denied';
  }

  if (error.code === error.TIMEOUT) {
    return 'timeout';
  }

  return 'unavailable';
}
