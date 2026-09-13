import { inject, InjectionToken } from '@angular/core';

import { ShareModerationApiService } from '@data-access/sharing/share-moderation-api.service';

export interface AdminShareModerationStatePort extends Pick<ShareModerationApiService, 'review' | 'search'> {}

export const ADMIN_SHARE_MODERATION_STATE_PORT = new InjectionToken<AdminShareModerationStatePort>(
  'ADMIN_SHARE_MODERATION_STATE_PORT',
  {
    providedIn: 'root',
    factory: (): AdminShareModerationStatePort => inject(ShareModerationApiService),
  },
);
