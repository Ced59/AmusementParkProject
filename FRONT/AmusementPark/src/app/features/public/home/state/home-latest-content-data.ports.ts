import { inject, InjectionToken } from '@angular/core';

import { HomeApiService } from '@data-access/home/home-api.service';

export interface HomeLatestContentDataPort extends Pick<HomeApiService, 'getLatestParks' | 'getLatestArticles'> {
}

export const HOME_LATEST_CONTENT_DATA_PORT = new InjectionToken<HomeLatestContentDataPort>('HOME_LATEST_CONTENT_DATA_PORT', {
  providedIn: 'root',
  factory: () => inject(HomeApiService)
});
