import { inject, InjectionToken } from '@angular/core';
import { AdminSeoSitemapsApiService } from '@app/data-access/admin/admin-seo-sitemaps-api.service';

export interface AdminSeoSitemapsStatePort extends Pick<AdminSeoSitemapsApiService, 'generate' | 'getHistory' | 'getOverview' | 'updateSettings'> {
}

export const ADMIN_SEO_SITEMAPS_STATE__PORT = new InjectionToken<AdminSeoSitemapsStatePort>('ADMIN_SEO_SITEMAPS_STATE__PORT', {
  providedIn: 'root',
  factory: () => inject(AdminSeoSitemapsApiService)
});
