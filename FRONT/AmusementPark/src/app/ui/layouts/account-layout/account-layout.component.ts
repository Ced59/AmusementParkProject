import { ChangeDetectionStrategy, Component } from '@angular/core';
import { ActivatedRoute, RouterOutlet } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { PublicFooterComponent } from '@ui/layouts/public-footer/public-footer.component';
import { PublicHeaderComponent } from '@ui/layouts/public-header/public-header.component';
import { PublicMobileBottomNavComponent } from '@ui/layouts/public-mobile-bottom-nav/public-mobile-bottom-nav.component';
import { UiChipComponent, UiKickerComponent } from '@ui/primitives';
import { PublicParkNavigationTreeFacade } from '@features/public/navigation/state/public-park-navigation-tree.facade';
import { PublicParkNavigationTreeState } from '@features/public/navigation/state/public-park-navigation-tree.state';

@Component({
  selector: 'app-account-layout',
  templateUrl: './account-layout.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [PublicParkNavigationTreeFacade, PublicParkNavigationTreeState],
  imports: [
    PublicFooterComponent,
    PublicHeaderComponent,
    PublicMobileBottomNavComponent,
    RouterOutlet,
    TranslateModule,
    UiChipComponent,
    UiKickerComponent
  ]
})
export class AccountLayoutComponent {
  protected wideLayout: boolean;

  constructor(private readonly activatedRoute: ActivatedRoute) {
    this.wideLayout = this.hasWideLayoutRoute();
  }

  protected syncLayoutMode(): void {
    this.wideLayout = this.hasWideLayoutRoute();
  }

  private hasWideLayoutRoute(): boolean {
    return this.activatedRoute.firstChild?.snapshot.data['accountLayout'] === 'wide';
  }
}
