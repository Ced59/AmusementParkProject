import { inject, InjectionToken } from '@angular/core';

import type { ConsentedProductAnalyticsEvent } from './product-analytics-event.model';
import { MatomoProductAnalyticsService } from './matomo-product-analytics.service';

export interface ProductAnalyticsPort {
  track(event: ConsentedProductAnalyticsEvent): void;
}

export const PRODUCT_ANALYTICS_PORT = new InjectionToken<ProductAnalyticsPort>(
  'PRODUCT_ANALYTICS_PORT',
  {
    providedIn: 'root',
    factory: () => inject(MatomoProductAnalyticsService)
  }
);
