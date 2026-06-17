import { ChangeDetectionStrategy, Component } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

interface AdminDashboardShortcut {
  readonly iconClass: string;
  readonly titleKey: string;
  readonly descriptionKey: string;
  readonly segment: string;
}

@Component({
  selector: 'app-admin-dashboard',
  templateUrl: './admin-dashboard.component.html',
  styleUrl: './admin-dashboard.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, TranslateModule]
})
export class AdminDashboardComponent {
  protected readonly shortcuts: readonly AdminDashboardShortcut[] = [
    {
      iconClass: 'pi pi-users',
      titleKey: 'admin.users.title',
      descriptionKey: 'admin.dashboard.shortcuts.users',
      segment: 'users'
    },
    {
      iconClass: 'pi pi-map',
      titleKey: 'admin.parks.title',
      descriptionKey: 'admin.dashboard.shortcuts.parks',
      segment: 'parks'
    },
    {
      iconClass: 'pi pi-ticket',
      titleKey: 'admin.parkItems.title',
      descriptionKey: 'admin.dashboard.shortcuts.parkItems',
      segment: 'items'
    },
    {
      iconClass: 'pi pi-building',
      titleKey: 'admin.operators.title',
      descriptionKey: 'admin.dashboard.shortcuts.operators',
      segment: 'operators'
    },
    {
      iconClass: 'pi pi-sparkles',
      titleKey: 'admin.parkFounders.title',
      descriptionKey: 'admin.dashboard.shortcuts.founders',
      segment: 'founders'
    },
    {
      iconClass: 'pi pi-wrench',
      titleKey: 'admin.manufacturers.title',
      descriptionKey: 'admin.dashboard.shortcuts.manufacturers',
      segment: 'manufacturers'
    },
    {
      iconClass: 'pi pi-image',
      titleKey: 'admin.images.title',
      descriptionKey: 'admin.dashboard.shortcuts.images',
      segment: 'images'
    },
    {
      iconClass: 'pi pi-cog',
      titleKey: 'admin.dataSources.title',
      descriptionKey: 'admin.dashboard.shortcuts.data',
      segment: 'data'
    },
    {
      iconClass: 'pi pi-sitemap',
      titleKey: 'admin.parkGraphUpserts.title',
      descriptionKey: 'admin.dashboard.shortcuts.parkGraphUpserts',
      segment: 'park-graph-upserts'
    },
    {
      iconClass: 'pi pi-shield',
      titleKey: 'admin.auditLogs.title',
      descriptionKey: 'admin.dashboard.shortcuts.auditLogs',
      segment: 'audit-logs'
    },
    {
      iconClass: 'pi pi-search',
      titleKey: 'admin.seoSitemaps.navTitle',
      descriptionKey: 'admin.dashboard.shortcuts.seoSitemaps',
      segment: 'seo-sitemaps'
    },
    {
      iconClass: 'pi pi-inbox',
      titleKey: 'admin.contactGrievances.navTitle',
      descriptionKey: 'admin.dashboard.shortcuts.contactGrievances',
      segment: 'contact-grievances'
    }
  ];

  constructor(private readonly router: Router) {
  }

  protected buildAdminRoute(segment: string): string[] {
    const lang: string = this.router.url.split('/')[1] || 'en';
    return ['/', lang, 'admin', segment];
  }
}
