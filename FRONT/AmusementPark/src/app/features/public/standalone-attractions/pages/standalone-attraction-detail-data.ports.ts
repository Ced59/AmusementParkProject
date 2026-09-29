import { inject, InjectionToken } from '@angular/core';
import { ManufacturersApiService } from '@data-access/manufacturers/manufacturers-api.service';

export type StandaloneAttractionDetailManufacturersPort = Pick<
  ManufacturersApiService,
  'getAttractionManufacturerById'
>;

export const STANDALONE_ATTRACTION_DETAIL_MANUFACTURERS_PORT = new InjectionToken<StandaloneAttractionDetailManufacturersPort>(
  'STANDALONE_ATTRACTION_DETAIL_MANUFACTURERS_PORT',
  {
    providedIn: 'root',
    factory: () => inject(ManufacturersApiService)
  }
);
