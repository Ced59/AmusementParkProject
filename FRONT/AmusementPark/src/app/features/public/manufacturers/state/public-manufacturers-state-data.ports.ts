import { inject, InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';

import { AttractionManufacturer } from '@app/models/parks/attraction-manufacturer';
import { ManufacturersApiService } from '@data-access/manufacturers/manufacturers-api.service';
import { PagedResult } from '@shared/models/contracts';

export interface PublicManufacturersPort {
  getAttractionManufacturersPage(page?: number, size?: number, search?: string | null): Observable<PagedResult<AttractionManufacturer>>;
  getAllAttractionManufacturers(): Observable<AttractionManufacturer[]>;
}

export const PUBLIC_MANUFACTURERS_PORT = new InjectionToken<PublicManufacturersPort>('PUBLIC_MANUFACTURERS_PORT', {
  providedIn: 'root',
  factory: () => inject(ManufacturersApiService)
});
