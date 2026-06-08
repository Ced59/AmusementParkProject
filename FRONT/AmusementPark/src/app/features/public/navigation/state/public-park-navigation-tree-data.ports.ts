import { inject, InjectionToken } from '@angular/core';
import { ParkItemsApiService } from '@data-access/park-items/park-items-api.service';
import { ParkZonesApiService } from '@data-access/parks/park-zones-api.service';
import { ParksApiService } from '@data-access/parks/parks-api.service';
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
  getParkById(id: string, options?: AnonymousHttpOptions): ReturnType<ParksApiService['getParkById']>;
}

export const PUBLIC_PARK_NAVIGATION_TREE_PARKS_API_SERVICE_PORT = new InjectionToken<PublicParkNavigationTreeParksApiServicePort>('PUBLIC_PARK_NAVIGATION_TREE_PARKS_API_SERVICE_PORT', {
  providedIn: 'root',
  factory: () => inject(ParksApiService)
});
