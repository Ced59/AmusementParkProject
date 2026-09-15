import { inject, InjectionToken } from '@angular/core';

import { AdminFactualEventsApiService } from '@data-access/admin/admin-factual-events-api.service';

export interface AdminFactualEventsStatePort extends Pick<AdminFactualEventsApiService, 'publish' | 'search' | 'verify'> {}

export const ADMIN_FACTUAL_EVENTS_STATE_PORT = new InjectionToken<AdminFactualEventsStatePort>(
  'ADMIN_FACTUAL_EVENTS_STATE_PORT',
  {
    providedIn: 'root',
    factory: (): AdminFactualEventsStatePort => inject(AdminFactualEventsApiService),
  },
);
