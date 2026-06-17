import { InjectionToken, inject } from '@angular/core';

import { VideosApiService } from '@data-access/videos/videos-api.service';

export interface AdminVideosStateVideosApiServicePort extends Pick<VideosApiService,
  | 'getVideosPage'
  | 'getVideoTags'
  | 'resolveVideoMetadata'
  | 'createVideo'
  | 'updateVideo'
  | 'deleteVideo'
  | 'createVideoTag'
  | 'updateVideoTag'
> {
}

export const ADMIN_VIDEOS_STATE_VIDEOS_API_SERVICE_PORT = new InjectionToken<AdminVideosStateVideosApiServicePort>(
  'AdminVideosStateVideosApiServicePort',
  {
    providedIn: 'root',
    factory: () => inject(VideosApiService),
  }
);
