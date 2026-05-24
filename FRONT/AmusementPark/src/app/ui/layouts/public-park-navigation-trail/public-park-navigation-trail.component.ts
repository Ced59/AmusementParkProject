import { ChangeDetectionStrategy, Component, Signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { PublicParkNavigationTreeViewModel } from '@features/public/navigation/models/public-park-navigation-tree.model';
import { PublicParkNavigationTreeFacade } from '@features/public/navigation/state/public-park-navigation-tree.facade';

@Component({
  selector: 'app-public-park-navigation-trail',
  templateUrl: './public-park-navigation-trail.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, TranslateModule]
})
export class PublicParkNavigationTrailComponent {
  protected readonly tree: Signal<PublicParkNavigationTreeViewModel> = this.publicParkNavigationTreeFacade.tree;

  constructor(private readonly publicParkNavigationTreeFacade: PublicParkNavigationTreeFacade) {
  }
}
