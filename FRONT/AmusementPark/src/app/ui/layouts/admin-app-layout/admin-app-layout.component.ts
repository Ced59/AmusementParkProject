import { ChangeDetectionStrategy, Component, ViewEncapsulation } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { ADMIN_NAVIGATION_ITEMS, AdminNavigationItem } from '@shared/models/admin/admin-navigation.models';

@Component({
  selector: 'app-admin-app-layout',
  templateUrl: './admin-app-layout.component.html',
  styleUrls: ['./admin-app-layout.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  encapsulation: ViewEncapsulation.None,
  imports: [RouterLink, RouterLinkActive, RouterOutlet, TranslateModule]
})
export class AdminAppLayoutComponent {
  protected readonly navigationItems: readonly AdminNavigationItem[] = ADMIN_NAVIGATION_ITEMS;

  constructor(private readonly router: Router) {
  }

  protected get currentLang(): string {
    return this.router.url.split('/')[1] || 'en';
  }

  protected buildAdminRoute(segments: readonly string[]): string[] {
    return ['/', this.currentLang, 'admin', ...segments];
  }
}
