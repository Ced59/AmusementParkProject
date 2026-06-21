import { inject, InjectionToken } from '@angular/core';

import { ContextualBlocksApiService } from '@data-access/admin/contextual-blocks-api.service';

export interface AdminContextualBlockFormDataPort extends Pick<ContextualBlocksApiService, 'getBlockExportDocument' | 'applyBlock'> {
}

export const ADMIN_CONTEXTUAL_BLOCK_FORM_DATA_PORT = new InjectionToken<AdminContextualBlockFormDataPort>('ADMIN_CONTEXTUAL_BLOCK_FORM_DATA_PORT', {
  providedIn: 'root',
  factory: () => inject(ContextualBlocksApiService)
});
