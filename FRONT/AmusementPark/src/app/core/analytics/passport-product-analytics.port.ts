import { InjectionToken, inject } from '@angular/core';

import { PassportProductEvent } from './passport-product-event.model';
import { MatomoPassportProductAnalyticsService } from './matomo-passport-product-analytics.service';

export interface PassportProductAnalyticsPort {
  track(event: PassportProductEvent): void;
}

export const PASSPORT_PRODUCT_ANALYTICS_PORT = new InjectionToken<PassportProductAnalyticsPort>(
  'PASSPORT_PRODUCT_ANALYTICS_PORT',
  {
    providedIn: 'root',
    factory: () => inject(MatomoPassportProductAnalyticsService)
  }
);
