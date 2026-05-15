import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { PublicFooterComponent } from '@ui/layouts/public-footer/public-footer.component';
import { PublicHeaderComponent } from '@ui/layouts/public-header/public-header.component';
import { PublicMobileBottomNavComponent } from '@ui/layouts/public-mobile-bottom-nav/public-mobile-bottom-nav.component';

@Component({
  selector: 'app-public-app-layout',
  templateUrl: './public-app-layout.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [PublicFooterComponent, PublicHeaderComponent, PublicMobileBottomNavComponent, RouterOutlet]
})
export class PublicAppLayoutComponent {
}
