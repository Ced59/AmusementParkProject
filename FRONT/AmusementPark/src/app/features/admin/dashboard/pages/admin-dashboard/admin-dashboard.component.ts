import { ChangeDetectionStrategy, Component } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

interface AdminDashboardShortcut {
  readonly icon: string;
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
      icon: '👤',
      titleKey: 'admin.users.title',
      descriptionKey: 'admin.dashboard.shortcuts.users',
      segment: 'users'
    },
    {
      icon: '🗺️',
      titleKey: 'admin.parks.title',
      descriptionKey: 'admin.dashboard.shortcuts.parks',
      segment: 'parks'
    },
    {
      icon: '🎢',
      titleKey: 'admin.parkItems.title',
      descriptionKey: 'admin.dashboard.shortcuts.parkItems',
      segment: 'items'
    },
    {
      icon: '🏢',
      titleKey: 'admin.operators.title',
      descriptionKey: 'admin.dashboard.shortcuts.operators',
      segment: 'operators'
    },
    {
      icon: '🧬',
      titleKey: 'admin.parkFounders.title',
      descriptionKey: 'admin.dashboard.shortcuts.founders',
      segment: 'founders'
    },
    {
      icon: '🔧',
      titleKey: 'admin.manufacturers.title',
      descriptionKey: 'admin.dashboard.shortcuts.manufacturers',
      segment: 'manufacturers'
    },
    {
      icon: '🖼️',
      titleKey: 'admin.images.title',
      descriptionKey: 'admin.dashboard.shortcuts.images',
      segment: 'images'
    },
    {
      icon: '⚙️',
      titleKey: 'admin.dataSources.title',
      descriptionKey: 'admin.dashboard.shortcuts.data',
      segment: 'data'
    },
    {
      icon: '🧩',
      titleKey: 'admin.parkGraphUpserts.title',
      descriptionKey: 'admin.dashboard.shortcuts.parkGraphUpserts',
      segment: 'park-graph-upserts'
    },
    {
      icon: '🌍',
      titleKey: 'admin.localizedContent.navTitle',
      descriptionKey: 'admin.dashboard.shortcuts.localizedContent',
      segment: 'localized-content'
    },
    {
      icon: '🛡️',
      titleKey: 'admin.auditLogs.title',
      descriptionKey: 'admin.dashboard.shortcuts.auditLogs',
      segment: 'audit-logs'
    },
    {
      icon: '🔎',
      titleKey: 'admin.seoSitemaps.navTitle',
      descriptionKey: 'admin.dashboard.shortcuts.seoSitemaps',
      segment: 'seo-sitemaps'
    }
  ];

  constructor(private readonly router: Router) {
  }

  protected buildAdminRoute(segment: string): string[] {
    const lang: string = this.router.url.split('/')[1] || 'en';
    return ['/', lang, 'admin', segment];
  }
}
