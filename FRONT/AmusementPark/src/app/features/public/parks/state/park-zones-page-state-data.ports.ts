import { inject, InjectionToken } from '@angular/core';
import { ParkItemsApiService } from '@data-access/park-items/park-items-api.service';
import { ParkZonesApiService } from '@data-access/parks/park-zones-api.service';
import { ParksApiService } from '@data-access/parks/parks-api.service';

export interface ParkZonesPageStateParksApiServicePort extends Pick<ParksApiService, 'getParkById' | 'getParkExplorer'> {
}

export const PARK_ZONES_PAGE_STATE_PARKS_API_SERVICE_PORT = new InjectionToken<ParkZonesPageStateParksApiServicePort>('PARK_ZONES_PAGE_STATE_PARKS_API_SERVICE_PORT', {
  providedIn: 'root',
  factory: () => inject(ParksApiService)
});

export interface ParkZonesPageStateParkZonesApiServicePort extends Pick<ParkZonesApiService, 'getParkZonesByParkId'> {
}

export const PARK_ZONES_PAGE_STATE_PARK_ZONES_API_SERVICE_PORT = new InjectionToken<ParkZonesPageStateParkZonesApiServicePort>('PARK_ZONES_PAGE_STATE_PARK_ZONES_API_SERVICE_PORT', {
  providedIn: 'root',
  factory: () => inject(ParkZonesApiService)
});

export interface ParkZonesPageStateParkItemsApiServicePort extends Pick<ParkItemsApiService, 'getParkItemsByParkId'> {
}

export const PARK_ZONES_PAGE_STATE_PARK_ITEMS_API_SERVICE_PORT = new InjectionToken<ParkZonesPageStateParkItemsApiServicePort>('PARK_ZONES_PAGE_STATE_PARK_ITEMS_API_SERVICE_PORT', {
  providedIn: 'root',
  factory: () => inject(ParkItemsApiService)
});
