import { InjectionToken, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { AdminTechnicalStatsApiService } from '@data-access/admin/admin-technical-stats-api.service';
import { TechnicalStatsSnapshot } from '@app/models/admin/technical-stats/technical-stats.models';

export interface AdminTechnicalStatsDataPort {
  getStats(): Observable<TechnicalStatsSnapshot>;
}

export const ADMIN_TECHNICAL_STATS_DATA_PORT = new InjectionToken<AdminTechnicalStatsDataPort>('ADMIN_TECHNICAL_STATS_DATA_PORT', {
  providedIn: 'root',
  factory: () => inject(AdminTechnicalStatsApiService)
});
