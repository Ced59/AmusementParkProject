import { inject, InjectionToken } from '@angular/core';

import { HomeApiService } from '@data-access/home/home-api.service';

export interface AboutStateHomeStatsPort extends Pick<HomeApiService, 'getHomeStats'> {
}

export const ABOUT_STATE_HOME_STATS_PORT = new InjectionToken<AboutStateHomeStatsPort>('ABOUT_STATE_HOME_STATS_PORT', {
  providedIn: 'root',
  factory: () => inject(HomeApiService)
});
