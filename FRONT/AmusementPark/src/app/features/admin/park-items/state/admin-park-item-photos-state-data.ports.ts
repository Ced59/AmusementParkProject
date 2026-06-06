import { inject, InjectionToken } from '@angular/core';
import { ImagesApiService } from '@data-access/images/images-api.service';

export interface AdminParkItemPhotosStateImagesApiServicePort extends Pick<ImagesApiService, 'createAdminImageTag' | 'deleteImage' | 'getAdminImageTags' | 'getImages' | 'linkImage' | 'setCurrentImage' | 'updateAdminImage' | 'uploadImage'> {
}

export const ADMIN_PARK_ITEM_PHOTOS_STATE_IMAGES_API_SERVICE_PORT = new InjectionToken<AdminParkItemPhotosStateImagesApiServicePort>('ADMIN_PARK_ITEM_PHOTOS_STATE_IMAGES_API_SERVICE_PORT', {
  providedIn: 'root',
  factory: () => inject(ImagesApiService)
});
