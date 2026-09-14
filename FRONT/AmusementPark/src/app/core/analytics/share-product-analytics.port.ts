import { InjectionToken, inject } from '@angular/core';

import { MatomoShareProductAnalyticsService } from './matomo-share-product-analytics.service';
import { ShareProductEvent } from './share-product-event.model';

export interface ShareProductAnalyticsPort {
  track(event: ShareProductEvent): void;
}

export const SHARE_PRODUCT_ANALYTICS_PORT = new InjectionToken<ShareProductAnalyticsPort>(
  'SHARE_PRODUCT_ANALYTICS_PORT',
  {
    providedIn: 'root',
    factory: () => inject(MatomoShareProductAnalyticsService)
  }
);
