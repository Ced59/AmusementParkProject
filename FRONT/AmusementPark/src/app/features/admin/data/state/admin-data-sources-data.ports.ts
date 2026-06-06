import { inject, InjectionToken } from '@angular/core';
import { DataSourcesApiService } from '@data-access/admin/data-sources-api.service';

export interface AdminDataSourcesDataSourcesApiServicePort extends Pick<DataSourcesApiService, 'listSources'> {
}

export const ADMIN_DATA_SOURCES_DATA_SOURCES_API_SERVICE_PORT = new InjectionToken<AdminDataSourcesDataSourcesApiServicePort>('ADMIN_DATA_SOURCES_DATA_SOURCES_API_SERVICE_PORT', {
  providedIn: 'root',
  factory: () => inject(DataSourcesApiService)
});
