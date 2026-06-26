import { ChangeDetectionStrategy, Component, ViewEncapsulation } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'app-admin-app-layout',
  templateUrl: './admin-app-layout.component.html',
  styleUrls: ['./admin-app-layout.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  encapsulation: ViewEncapsulation.None,
  imports: [RouterLink, RouterLinkActive, RouterOutlet, TranslateModule]
})
export class AdminAppLayoutComponent {
  constructor(private readonly router: Router) {
  }

  protected get currentLang(): string {
    return this.router.url.split('/')[1] || 'en';
  }

  protected get fieldModeNavLabel(): string {
    return this.currentLang === 'fr' ? 'Mode terrain' : 'Field mode';
  }

  protected get technicalStatsNavLabel(): string {
    return this.currentLang === 'fr' ? 'Stats techniques' : 'Technical stats';
  }
}
