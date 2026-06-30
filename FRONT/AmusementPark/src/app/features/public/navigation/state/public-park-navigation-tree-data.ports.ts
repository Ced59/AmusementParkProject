import { inject, InjectionToken } from '@angular/core';
import { ParkItemsApiService } from '@data-access/park-items/park-items-api.service';
import { ParkZonesApiService } from '@data-access/parks/park-zones-api.service';
import { ParksApiService } from '@data-access/parks/parks-api.service';
import { HistoryApiService } from '@data-access/history/history-api.service';
import { VideosApiService } from '@data-access/videos/videos-api.service';
import { AnonymousHttpOptions } from '@core/http/auth/anonymous-http-options';

export interface PublicParkNavigationTreeParkItemsApiServicePort {
  getParkItemById(id: string, options?: AnonymousHttpOptions): ReturnType<ParkItemsApiService['getParkItemById']>;
}

export const PUBLIC_PARK_NAVIGATION_TREE_PARK_ITEMS_API_SERVICE_PORT = new InjectionToken<PublicParkNavigationTreeParkItemsApiServicePort>('PUBLIC_PARK_NAVIGATION_TREE_PARK_ITEMS_API_SERVICE_PORT', {
  providedIn: 'root',
  factory: () => inject(ParkItemsApiService)
});

export interface PublicParkNavigationTreeParkZonesApiServicePort {
  getParkZoneById(id: string, options?: AnonymousHttpOptions): ReturnType<ParkZonesApiService['getParkZoneById']>;
}

export const PUBLIC_PARK_NAVIGATION_TREE_PARK_ZONES_API_SERVICE_PORT = new InjectionToken<PublicParkNavigationTreeParkZonesApiServicePort>('PUBLIC_PARK_NAVIGATION_TREE_PARK_ZONES_API_SERVICE_PORT', {
  providedIn: 'root',
  factory: () => inject(ParkZonesApiService)
});

export interface PublicParkNavigationTreeParksApiServicePort {
  getParkDetailSummary(id: string, options?: AnonymousHttpOptions): ReturnType<ParksApiService['getParkDetailSummary']>;
}

export const PUBLIC_PARK_NAVIGATION_TREE_PARKS_API_SERVICE_PORT = new InjectionToken<PublicParkNavigationTreeParksApiServicePort>('PUBLIC_PARK_NAVIGATION_TREE_PARKS_API_SERVICE_PORT', {
  providedIn: 'root',
  factory: () => inject(ParksApiService)
});

export interface PublicParkNavigationTreeVideosApiServicePort {
  getVideoById(id: string, options?: AnonymousHttpOptions, languageCode?: string | null): ReturnType<VideosApiService['getVideoById']>;
}

export const PUBLIC_PARK_NAVIGATION_TREE_VIDEOS_API_SERVICE_PORT = new InjectionToken<PublicParkNavigationTreeVideosApiServicePort>('PUBLIC_PARK_NAVIGATION_TREE_VIDEOS_API_SERVICE_PORT', {
  providedIn: 'root',
  factory: () => inject(VideosApiService)
});

export interface PublicParkNavigationTreeHistoryApiServicePort {
  getArticle(id: string, options?: AnonymousHttpOptions): ReturnType<HistoryApiService['getArticle']>;
}

export const PUBLIC_PARK_NAVIGATION_TREE_HISTORY_API_SERVICE_PORT = new InjectionToken<PublicParkNavigationTreeHistoryApiServicePort>('PUBLIC_PARK_NAVIGATION_TREE_HISTORY_API_SERVICE_PORT', {
  providedIn: 'root',
  factory: () => inject(HistoryApiService)
});
