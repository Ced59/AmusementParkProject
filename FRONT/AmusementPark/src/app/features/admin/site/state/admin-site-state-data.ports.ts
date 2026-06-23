import { inject, InjectionToken } from '@angular/core';
import { ImagesApiService } from '@data-access/images/images-api.service';

export interface AdminSiteStateImagesApiServicePort extends Pick<ImagesApiService,
  | 'applyWatermark'
  | 'createAdminImageTag'
  | 'deleteImage'
  | 'getAdminImageTags'
  | 'getAdminImages'
  | 'updateAdminImage'
  | 'updateAdminImagesBulkMetadata'
> {
}

export const ADMIN_SITE_STATE_IMAGES_API_SERVICE_PORT = new InjectionToken<AdminSiteStateImagesApiServicePort>('ADMIN_SITE_STATE_IMAGES_API_SERVICE_PORT', {
  providedIn: 'root',
  factory: () => inject(ImagesApiService)
});
