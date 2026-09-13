import { Signal, signal } from '@angular/core';

import { PublicParkNavigationTreeViewModel } from '@features/public/navigation/models/public-park-navigation-tree.model';

export class PublicParkNavigationTreeFacadeStub {
  private readonly treeSignal = signal<PublicParkNavigationTreeViewModel>({
    isAvailable: false,
    isLoading: false,
    items: [],
  });

  readonly tree: Signal<PublicParkNavigationTreeViewModel> =
    this.treeSignal.asReadonly();
}
