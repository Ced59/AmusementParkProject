import { inject, InjectionToken } from '@angular/core';
import { ImagesApiService } from '@data-access/images/images-api.service';

export interface AdminParkPhotosStateImagesApiServicePort extends Pick<ImagesApiService, 'createAdminImageTag' | 'deleteImage' | 'getAdminImageTags' | 'getImages' | 'importRemoteImage' | 'linkImage' | 'setCurrentImage' | 'updateAdminImage' | 'uploadImage'> {
}

export const ADMIN_PARK_PHOTOS_STATE_IMAGES_API_SERVICE_PORT = new InjectionToken<AdminParkPhotosStateImagesApiServicePort>('ADMIN_PARK_PHOTOS_STATE_IMAGES_API_SERVICE_PORT', {
  providedIn: 'root',
  factory: () => inject(ImagesApiService)
});
