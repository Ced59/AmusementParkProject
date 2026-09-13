import { signal } from '@angular/core';

import { PublicVideoFilterState } from '../../../models/public-video-view.model';

export class FakeParkVideosStateFacade {
  readonly state = signal({ kind: 'loading' }).asReadonly();
  readonly park = signal(null).asReadonly();
  readonly parkImageId = signal(null).asReadonly();
  readonly videoCards = signal([]).asReadonly();
  readonly totalVideos = signal(0).asReadonly();
  readonly parkTabVideoCount = signal(0).asReadonly();
  readonly itemTabVideoCount = signal(0).asReadonly();
  readonly showItemTab = signal(false).asReadonly();
  readonly activeTab = signal('park').asReadonly();
  readonly canLoadMore = signal(false).asReadonly();
  readonly loadingMore = signal(false).asReadonly();
  readonly itemVideosLoading = signal(false).asReadonly();
  readonly filters = signal<PublicVideoFilterState>({
    type: null,
    tagId: null,
    creatorName: '',
  }).asReadonly();
  readonly typeOptions = signal([]).asReadonly();
  readonly tagOptions = signal([]).asReadonly();
  readonly languages: string[] = [];
  readonly loads: Array<{
    parkId: string;
    filters: PublicVideoFilterState;
  }> = [];
  readonly selectedTabs: string[] = [];

  setCurrentLanguage(language: string): void {
    this.languages.push(language);
  }

  loadParkVideos(parkId: string, filters: PublicVideoFilterState): void {
    this.loads.push({ parkId, filters });
  }

  selectTab(tab: string): void {
    this.selectedTabs.push(tab);
  }

  loadNextPage(): void {}
}
