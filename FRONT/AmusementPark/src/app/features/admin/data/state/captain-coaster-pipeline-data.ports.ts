import { inject, InjectionToken } from '@angular/core';
import { DataSourcesApiService } from '@data-access/admin/data-sources-api.service';

export interface CaptainCoasterPipelineDataSourcesApiServicePort extends Pick<DataSourcesApiService, 'getLatestSession' | 'getSessionById' | 'getSettings' | 'getStatus' | 'startImport' | 'updateSettings'> {
}

export const CAPTAIN_COASTER_PIPELINE_DATA_SOURCES_API_SERVICE_PORT = new InjectionToken<CaptainCoasterPipelineDataSourcesApiServicePort>('CAPTAIN_COASTER_PIPELINE_DATA_SOURCES_API_SERVICE_PORT', {
  providedIn: 'root',
  factory: () => inject(DataSourcesApiService)
});
