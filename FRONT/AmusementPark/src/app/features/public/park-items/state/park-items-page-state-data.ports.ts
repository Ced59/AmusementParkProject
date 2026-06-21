import { inject, InjectionToken } from '@angular/core';
import { ImagesApiService } from '@data-access/images/images-api.service';
import { ManufacturersApiService } from '@data-access/manufacturers/manufacturers-api.service';
import { ParkItemsApiService } from '@data-access/park-items/park-items-api.service';
import { ParkZonesApiService } from '@data-access/parks/park-zones-api.service';
import { ParksApiService } from '@data-access/parks/parks-api.service';

export interface ParkItemsPageStateImagesApiServicePort extends Pick<ImagesApiService, 'getImages' | 'buildImageUrl' | 'buildImageSrcSet'> {
}

export const PARK_ITEMS_PAGE_STATE_IMAGES_API_SERVICE_PORT = new InjectionToken<ParkItemsPageStateImagesApiServicePort>('PARK_ITEMS_PAGE_STATE_IMAGES_API_SERVICE_PORT', {
  providedIn: 'root',
  factory: () => inject(ImagesApiService)
});

export interface ParkItemsPageStateManufacturersApiServicePort extends Pick<ManufacturersApiService, 'getAttractionManufacturers'> {
}

export const PARK_ITEMS_PAGE_STATE_MANUFACTURERS_API_SERVICE_PORT = new InjectionToken<ParkItemsPageStateManufacturersApiServicePort>('PARK_ITEMS_PAGE_STATE_MANUFACTURERS_API_SERVICE_PORT', {
  providedIn: 'root',
  factory: () => inject(ManufacturersApiService)
});

export interface ParkItemsPageStateParkItemsApiServicePort extends Pick<ParkItemsApiService, 'getParkItemsByParkIdPage'> {
}

export const PARK_ITEMS_PAGE_STATE_PARK_ITEMS_API_SERVICE_PORT = new InjectionToken<ParkItemsPageStateParkItemsApiServicePort>('PARK_ITEMS_PAGE_STATE_PARK_ITEMS_API_SERVICE_PORT', {
  providedIn: 'root',
  factory: () => inject(ParkItemsApiService)
});

export interface ParkItemsPageStateParksApiServicePort extends Pick<ParksApiService, 'getParkById' | 'getParkExplorer' | 'getParkMapItems'> {
}

export const PARK_ITEMS_PAGE_STATE_PARKS_API_SERVICE_PORT = new InjectionToken<ParkItemsPageStateParksApiServicePort>('PARK_ITEMS_PAGE_STATE_PARKS_API_SERVICE_PORT', {
  providedIn: 'root',
  factory: () => inject(ParksApiService)
});

export interface ParkItemsPageStateParkZonesApiServicePort extends Pick<ParkZonesApiService, 'getParkZonesByParkId'> {
}

export const PARK_ITEMS_PAGE_STATE_PARK_ZONES_API_SERVICE_PORT = new InjectionToken<ParkItemsPageStateParkZonesApiServicePort>('PARK_ITEMS_PAGE_STATE_PARK_ZONES_API_SERVICE_PORT', {
  providedIn: 'root',
  factory: () => inject(ParkZonesApiService)
});
