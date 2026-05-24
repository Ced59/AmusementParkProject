import { ChangeDetectionStrategy, Component } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

@Component({
  selector: 'app-admin-app-layout',
  templateUrl: './admin-app-layout.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, RouterLinkActive, RouterOutlet, TranslateModule]
})
export class AdminAppLayoutComponent {
  constructor(private readonly router: Router) {
  }

  protected get currentLang(): string {
    return this.router.url.split('/')[1] || 'en';
  }
}
