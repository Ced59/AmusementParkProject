import { inject, InjectionToken } from '@angular/core';

import { ContextualBlocksApiService } from '@data-access/admin/contextual-blocks-api.service';

export interface AdminContextualBlockExportDataPort extends Pick<ContextualBlocksApiService, 'downloadBlockExport'> {
}

export const ADMIN_CONTEXTUAL_BLOCK_EXPORT_DATA_PORT = new InjectionToken<AdminContextualBlockExportDataPort>('ADMIN_CONTEXTUAL_BLOCK_EXPORT_DATA_PORT', {
  providedIn: 'root',
  factory: () => inject(ContextualBlocksApiService)
});
