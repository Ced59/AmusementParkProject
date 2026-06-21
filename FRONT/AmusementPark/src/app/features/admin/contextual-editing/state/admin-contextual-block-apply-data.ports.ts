import { inject, InjectionToken } from '@angular/core';

import { ContextualBlocksApiService } from '@data-access/admin/contextual-blocks-api.service';

export interface AdminContextualBlockApplyDataPort extends Pick<ContextualBlocksApiService, 'applyBlock'> {
}

export const ADMIN_CONTEXTUAL_BLOCK_APPLY_DATA_PORT = new InjectionToken<AdminContextualBlockApplyDataPort>('ADMIN_CONTEXTUAL_BLOCK_APPLY_DATA_PORT', {
  providedIn: 'root',
  factory: () => inject(ContextualBlocksApiService)
});
