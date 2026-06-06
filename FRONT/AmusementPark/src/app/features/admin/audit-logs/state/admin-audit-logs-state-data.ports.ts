import { inject, InjectionToken } from '@angular/core';
import { AdminAuditLogsApiService } from '@data-access/admin/admin-audit-logs-api.service';

export interface AdminAuditLogsStatePort extends Pick<AdminAuditLogsApiService, 'search'> {
}

export const ADMIN_AUDIT_LOGS_STATE__PORT = new InjectionToken<AdminAuditLogsStatePort>('ADMIN_AUDIT_LOGS_STATE__PORT', {
  providedIn: 'root',
  factory: () => inject(AdminAuditLogsApiService)
});
