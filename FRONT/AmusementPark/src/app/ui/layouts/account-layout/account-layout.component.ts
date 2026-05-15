import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { PublicFooterComponent } from '@ui/layouts/public-footer/public-footer.component';
import { PublicHeaderComponent } from '@ui/layouts/public-header/public-header.component';
import { PublicMobileBottomNavComponent } from '@ui/layouts/public-mobile-bottom-nav/public-mobile-bottom-nav.component';
import { PublicParkNavigationTreeFacade } from '@features/public/navigation/state/public-park-navigation-tree.facade';

@Component({
  selector: 'app-account-layout',
  templateUrl: './account-layout.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [PublicParkNavigationTreeFacade],
  imports: [PublicFooterComponent, PublicHeaderComponent, PublicMobileBottomNavComponent, RouterOutlet]
})
export class AccountLayoutComponent {
}
