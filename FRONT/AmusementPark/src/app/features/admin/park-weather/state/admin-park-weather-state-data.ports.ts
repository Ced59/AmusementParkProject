import { InjectionToken, inject } from '@angular/core';

import { AdminParkWeatherApiService } from '@data-access/admin/admin-park-weather-api.service';

export interface AdminParkWeatherStatePort extends Pick<AdminParkWeatherApiService, 'getLatestRun' | 'getRunItems' | 'startManualRefresh' | 'retryFailedRun' | 'refreshPark'> {
}

export const ADMIN_PARK_WEATHER_STATE_PORT = new InjectionToken<AdminParkWeatherStatePort>('ADMIN_PARK_WEATHER_STATE_PORT', {
  providedIn: 'root',
  factory: () => inject(AdminParkWeatherApiService)
});
