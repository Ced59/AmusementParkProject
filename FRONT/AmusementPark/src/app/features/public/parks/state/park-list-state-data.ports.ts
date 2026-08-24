import { inject, InjectionToken } from '@angular/core';
import { ParksApiService } from '@data-access/parks/parks-api.service';
import { SearchApiService } from '@data-access/search/search-api.service';
import { StandaloneAttractionsApiService } from '@data-access/standalone-attractions/standalone-attractions-api.service';

export interface ParkListStateParksApiServicePort extends Pick<ParksApiService, 'getParkById' | 'getParksPaginated' | 'getVisibleParkMapPoints' | 'searchParks'> {
}

export const PARK_LIST_STATE_PARKS_API_SERVICE_PORT = new InjectionToken<ParkListStateParksApiServicePort>('PARK_LIST_STATE_PARKS_API_SERVICE_PORT', {
  providedIn: 'root',
  factory: () => inject(ParksApiService)
});

export interface ParkListStateSearchApiServicePort extends Pick<SearchApiService, 'getSearch'> {
}

export const PARK_LIST_STATE_SEARCH_API_SERVICE_PORT = new InjectionToken<ParkListStateSearchApiServicePort>('PARK_LIST_STATE_SEARCH_API_SERVICE_PORT', {
  providedIn: 'root',
  factory: () => inject(SearchApiService)
});

export interface ParkListStateStandaloneAttractionsApiServicePort extends Pick<StandaloneAttractionsApiService, 'getVisibleMapPoints'> {
}

export const PARK_LIST_STATE_STANDALONE_ATTRACTIONS_API_SERVICE_PORT = new InjectionToken<ParkListStateStandaloneAttractionsApiServicePort>('PARK_LIST_STATE_STANDALONE_ATTRACTIONS_API_SERVICE_PORT', {
  providedIn: 'root',
  factory: () => inject(StandaloneAttractionsApiService)
});
