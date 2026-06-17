import { InjectionToken, inject } from '@angular/core';

import { VideosApiService } from '@data-access/videos/videos-api.service';

export interface AdminVideoCreateVideosApiServicePort extends Pick<VideosApiService,
  | 'getVideoTags'
  | 'resolveVideoMetadata'
  | 'createVideo'
> {
}

export const ADMIN_VIDEO_CREATE_VIDEOS_API_SERVICE_PORT = new InjectionToken<AdminVideoCreateVideosApiServicePort>(
  'AdminVideoCreateVideosApiServicePort',
  {
    providedIn: 'root',
    factory: () => inject(VideosApiService),
  }
);
