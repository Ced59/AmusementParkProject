import { inject, InjectionToken } from '@angular/core';
import { ImagesApiService } from '@data-access/images/images-api.service';

export interface AdminParkLogosStateImagesApiServicePort extends Pick<ImagesApiService, 'createAdminImageTag' | 'deleteImage' | 'getAdminImageTags' | 'getImages' | 'importRemoteImage' | 'linkImage' | 'setCurrentImage' | 'updateAdminImage' | 'uploadImage'> {
}

export const ADMIN_PARK_LOGOS_STATE_IMAGES_API_SERVICE_PORT = new InjectionToken<AdminParkLogosStateImagesApiServicePort>('ADMIN_PARK_LOGOS_STATE_IMAGES_API_SERVICE_PORT', {
  providedIn: 'root',
  factory: () => inject(ImagesApiService)
});
