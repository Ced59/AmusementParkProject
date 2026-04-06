import { Component } from '@angular/core';
import { Bind } from 'primeng/bind';
import { ButtonDirective } from 'primeng/button';
import { Router, RouterLinkActive, RouterLink, RouterOutlet } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

@Component({
    selector: 'app-admin-dashboard',
    templateUrl: './admin-dashboard.component.html',
    styleUrl: './admin-dashboard.component.scss',
    imports: [Bind, ButtonDirective, RouterLinkActive, RouterLink, RouterOutlet, TranslateModule]
})
export class AdminDashboardComponent {
  constructor(private readonly router: Router) {
  }

  buildAdminRoute(segment: string): string[] {
    const lang: string = this.router.url.split('/')[1] || 'en';
    return ['/', lang, 'admin', segment];
  }
}
