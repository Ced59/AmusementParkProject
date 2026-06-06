import { inject, InjectionToken } from '@angular/core';
import { HomeApiService } from '@data-access/home/home-api.service';
import { ParksApiService } from '@data-access/parks/parks-api.service';
import { SearchApiService } from '@data-access/search/search-api.service';

export interface HomeStateSearchApiServicePort extends Pick<SearchApiService, 'getSearch'> {
}

export const HOME_STATE_SEARCH_API_SERVICE_PORT = new InjectionToken<HomeStateSearchApiServicePort>('HOME_STATE_SEARCH_API_SERVICE_PORT', {
  providedIn: 'root',
  factory: () => inject(SearchApiService)
});

export interface HomeStateParksApiServicePort extends Pick<ParksApiService, 'getRandomVisibleParks'> {
}

export const HOME_STATE_PARKS_API_SERVICE_PORT = new InjectionToken<HomeStateParksApiServicePort>('HOME_STATE_PARKS_API_SERVICE_PORT', {
  providedIn: 'root',
  factory: () => inject(ParksApiService)
});

export interface HomeStateHomeApiServicePort extends Pick<HomeApiService, 'getFeaturedParks' | 'getHomeStats'> {
}

export const HOME_STATE_HOME_API_SERVICE_PORT = new InjectionToken<HomeStateHomeApiServicePort>('HOME_STATE_HOME_API_SERVICE_PORT', {
  providedIn: 'root',
  factory: () => inject(HomeApiService)
});
