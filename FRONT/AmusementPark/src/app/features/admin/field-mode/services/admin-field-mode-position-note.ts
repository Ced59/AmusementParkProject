import { Injectable } from '@angular/core';

import { AdminFieldModeGeolocationPort } from '../state/admin-field-mode-data.ports';

@Injectable({
  providedIn: 'root'
})
export class AdminFieldModePositionService implements AdminFieldModeGeolocationPort {
  getCurrentPosition(options?: PositionOptions): Promise<GeolocationPosition> {
    const nav: Navigator | undefined = typeof globalThis !== 'undefined'
      ? globalThis.navigator
      : undefined;
    const provider: Geolocation | undefined = nav?.geolocation;

    if (!provider) {
      return Promise.reject(new Error('Position is not available in this browser.'));
    }

    return new Promise((resolve: PositionCallback, reject: PositionErrorCallback): void => {
      provider.getCurrentPosition(resolve, reject, options);
    });
  }
}
