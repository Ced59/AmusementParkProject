import { inject, InjectionToken } from '@angular/core';

import { ContextualBlocksApiService } from '@data-access/admin/contextual-blocks-api.service';

export interface AdminContextualBlockPreviewDataPort extends Pick<ContextualBlocksApiService, 'previewBlock'> {
}

export const ADMIN_CONTEXTUAL_BLOCK_PREVIEW_DATA_PORT = new InjectionToken<AdminContextualBlockPreviewDataPort>('ADMIN_CONTEXTUAL_BLOCK_PREVIEW_DATA_PORT', {
  providedIn: 'root',
  factory: () => inject(ContextualBlocksApiService)
});
