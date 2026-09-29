import { inject, InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';

import { ManufacturersApiService } from '@data-access/manufacturers/manufacturers-api.service';

export interface StandaloneAttractionDetailManufacturersPort {
  getAttractionManufacturerById(id: string): Observable<{ name?: string | null }>;
}

export const STANDALONE_ATTRACTION_DETAIL_MANUFACTURERS_PORT = new InjectionToken<StandaloneAttractionDetailManufacturersPort>(
  'STANDALONE_ATTRACTION_DETAIL_MANUFACTURERS_PORT',
  {
    providedIn: 'root',
    factory: () => inject(ManufacturersApiService)
  }
);
