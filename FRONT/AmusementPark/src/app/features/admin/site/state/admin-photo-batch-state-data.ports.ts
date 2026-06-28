import { inject, InjectionToken } from '@angular/core';

import { ImagesApiService } from '@data-access/images/images-api.service';
import { ParkItemsApiService } from '@data-access/park-items/park-items-api.service';
import { ParksApiService } from '@data-access/parks/parks-api.service';

export interface AdminPhotoBatchImagesPort extends Pick<ImagesApiService,
  'createAdminImageTag' |
  'getAdminImageTags' |
  'getAdminImages' |
  'getParkItemImagesByPark' |
  'linkImage' |
  'updateAdminImage' |
  'uploadImage'> {
}

export const ADMIN_PHOTO_BATCH_IMAGES_PORT = new InjectionToken<AdminPhotoBatchImagesPort>(
  'ADMIN_PHOTO_BATCH_IMAGES_PORT',
  {
    providedIn: 'root',
    factory: () => inject(ImagesApiService)
  }
);

export interface AdminPhotoBatchParksPort extends Pick<ParksApiService, 'getParksPaginated' | 'searchParks'> {
}

export const ADMIN_PHOTO_BATCH_PARKS_PORT = new InjectionToken<AdminPhotoBatchParksPort>(
  'ADMIN_PHOTO_BATCH_PARKS_PORT',
  {
    providedIn: 'root',
    factory: () => inject(ParksApiService)
  }
);

export interface AdminPhotoBatchParkItemsPort extends Pick<ParkItemsApiService, 'getParkItemsPaginated'> {
}

export const ADMIN_PHOTO_BATCH_PARK_ITEMS_PORT = new InjectionToken<AdminPhotoBatchParkItemsPort>(
  'ADMIN_PHOTO_BATCH_PARK_ITEMS_PORT',
  {
    providedIn: 'root',
    factory: () => inject(ParkItemsApiService)
  }
);
