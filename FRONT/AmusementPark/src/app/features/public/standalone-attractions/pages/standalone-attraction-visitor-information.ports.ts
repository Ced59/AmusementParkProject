import { InjectionToken, inject } from '@angular/core';

import { StandaloneAttractionsApiService } from '@data-access/standalone-attractions/standalone-attractions-api.service';

export type StandaloneAttractionVisitorInformationPort = Pick<
  StandaloneAttractionsApiService,
  'getOpeningHours' | 'getPricing' | 'getWeather'
>;

export const STANDALONE_ATTRACTION_VISITOR_INFORMATION_PORT =
  new InjectionToken<StandaloneAttractionVisitorInformationPort>(
    'STANDALONE_ATTRACTION_VISITOR_INFORMATION_PORT',
    {
      providedIn: 'root',
      factory: () => inject(StandaloneAttractionsApiService)
    }
  );
