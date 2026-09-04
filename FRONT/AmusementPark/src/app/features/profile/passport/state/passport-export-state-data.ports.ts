import { inject, InjectionToken } from '@angular/core';
import { PassportExportsApiService } from '@data-access/passport/passport-exports-api.service';

export interface PassportExportApiPort extends Pick<PassportExportsApiService, 'requestExport' | 'getExport' | 'downloadExport'> {
}

export const PASSPORT_EXPORT_API_PORT = new InjectionToken<PassportExportApiPort>(
  'PASSPORT_EXPORT_API_PORT',
  {
    providedIn: 'root',
    factory: () => inject(PassportExportsApiService)
  }
);
