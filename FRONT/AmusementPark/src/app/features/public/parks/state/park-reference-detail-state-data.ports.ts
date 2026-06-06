import { inject, InjectionToken } from '@angular/core';
import { ImagesApiService } from '@data-access/images/images-api.service';
import { ManufacturersApiService } from '@data-access/manufacturers/manufacturers-api.service';
import { ParkFoundersApiService } from '@data-access/parks/park-founders-api.service';
import { ParkItemsApiService } from '@data-access/park-items/park-items-api.service';
import { ParkOperatorsApiService } from '@data-access/parks/park-operators-api.service';

export interface ParkReferenceDetailStateImagesApiServicePort extends Pick<ImagesApiService, 'getImages'> {
}

export const PARK_REFERENCE_DETAIL_STATE_IMAGES_API_SERVICE_PORT = new InjectionToken<ParkReferenceDetailStateImagesApiServicePort>('PARK_REFERENCE_DETAIL_STATE_IMAGES_API_SERVICE_PORT', {
  providedIn: 'root',
  factory: () => inject(ImagesApiService)
});

export interface ParkReferenceDetailStateManufacturersApiServicePort extends Pick<ManufacturersApiService, 'getAttractionManufacturerById'> {
}

export const PARK_REFERENCE_DETAIL_STATE_MANUFACTURERS_API_SERVICE_PORT = new InjectionToken<ParkReferenceDetailStateManufacturersApiServicePort>('PARK_REFERENCE_DETAIL_STATE_MANUFACTURERS_API_SERVICE_PORT', {
  providedIn: 'root',
  factory: () => inject(ManufacturersApiService)
});

export interface ParkReferenceDetailStateParkItemsApiServicePort extends Pick<ParkItemsApiService, 'getParkItemsPaginated'> {
}

export const PARK_REFERENCE_DETAIL_STATE_PARK_ITEMS_API_SERVICE_PORT = new InjectionToken<ParkReferenceDetailStateParkItemsApiServicePort>('PARK_REFERENCE_DETAIL_STATE_PARK_ITEMS_API_SERVICE_PORT', {
  providedIn: 'root',
  factory: () => inject(ParkItemsApiService)
});

export interface ParkReferenceDetailStateParkFoundersApiServicePort extends Pick<ParkFoundersApiService, 'getParkFounderById'> {
}

export const PARK_REFERENCE_DETAIL_STATE_PARK_FOUNDERS_API_SERVICE_PORT = new InjectionToken<ParkReferenceDetailStateParkFoundersApiServicePort>('PARK_REFERENCE_DETAIL_STATE_PARK_FOUNDERS_API_SERVICE_PORT', {
  providedIn: 'root',
  factory: () => inject(ParkFoundersApiService)
});

export interface ParkReferenceDetailStateParkOperatorsApiServicePort extends Pick<ParkOperatorsApiService, 'getParkOperatorById'> {
}

export const PARK_REFERENCE_DETAIL_STATE_PARK_OPERATORS_API_SERVICE_PORT = new InjectionToken<ParkReferenceDetailStateParkOperatorsApiServicePort>('PARK_REFERENCE_DETAIL_STATE_PARK_OPERATORS_API_SERVICE_PORT', {
  providedIn: 'root',
  factory: () => inject(ParkOperatorsApiService)
});
