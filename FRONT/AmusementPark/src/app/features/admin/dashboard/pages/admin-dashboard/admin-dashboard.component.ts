import { ChangeDetectionStrategy, Component } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { ADMIN_NAVIGATION_ITEMS, AdminNavigationItem } from '@shared/models/admin/admin-navigation.models';

@Component({
  selector: 'app-admin-dashboard',
  templateUrl: './admin-dashboard.component.html',
  styleUrl: './admin-dashboard.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, TranslateModule]
})
export class AdminDashboardComponent {
  protected readonly shortcuts: readonly AdminNavigationItem[] = ADMIN_NAVIGATION_ITEMS;

  constructor(private readonly router: Router) {
  }

  protected buildAdminRoute(segments: readonly string[]): string[] {
    const lang: string = this.router.url.split('/')[1] || 'en';
    return ['/', lang, 'admin', ...segments];
  }
}
