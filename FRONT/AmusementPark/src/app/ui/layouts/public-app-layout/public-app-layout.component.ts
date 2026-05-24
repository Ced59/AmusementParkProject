import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { PublicParkNavigationTreeFacade } from '@features/public/navigation/state/public-park-navigation-tree.facade';
import { PublicFooterComponent } from '@ui/layouts/public-footer/public-footer.component';
import { PublicHeaderComponent } from '@ui/layouts/public-header/public-header.component';
import { PublicMobileBottomNavComponent } from '@ui/layouts/public-mobile-bottom-nav/public-mobile-bottom-nav.component';
import { PublicParkNavigationTrailComponent } from '@ui/layouts/public-park-navigation-trail/public-park-navigation-trail.component';

@Component({
  selector: 'app-public-app-layout',
  templateUrl: './public-app-layout.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [PublicParkNavigationTreeFacade],
  imports: [
    PublicFooterComponent,
    PublicHeaderComponent,
    PublicMobileBottomNavComponent,
    PublicParkNavigationTrailComponent,
    RouterOutlet
  ]
})
export class PublicAppLayoutComponent implements OnInit {
  constructor(private readonly publicParkNavigationTreeFacade: PublicParkNavigationTreeFacade) {
  }

  ngOnInit(): void {
    this.publicParkNavigationTreeFacade.initialize();
  }
}
