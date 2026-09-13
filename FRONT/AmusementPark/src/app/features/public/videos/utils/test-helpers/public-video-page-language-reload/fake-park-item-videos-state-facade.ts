import { signal } from '@angular/core';

import { PublicVideoFilterState } from '../../../models/public-video-view.model';

export class FakeParkItemVideosStateFacade {
  readonly state = signal({ kind: 'loading' }).asReadonly();
  readonly item = signal(null).asReadonly();
  readonly park = signal(null).asReadonly();
  readonly itemImageId = signal(null).asReadonly();
  readonly parkImageId = signal(null).asReadonly();
  readonly videoCards = signal([]).asReadonly();
  readonly totalVideos = signal(0).asReadonly();
  readonly canLoadMore = signal(false).asReadonly();
  readonly loadingMore = signal(false).asReadonly();
  readonly filters = signal<PublicVideoFilterState>({
    type: null,
    tagId: null,
    creatorName: '',
  }).asReadonly();
  readonly typeOptions = signal([]).asReadonly();
  readonly tagOptions = signal([]).asReadonly();
  readonly languages: string[] = [];
  readonly loads: Array<{
    itemId: string;
    filters: PublicVideoFilterState;
  }> = [];

  setCurrentLanguage(language: string): void {
    this.languages.push(language);
  }

  loadItemVideos(itemId: string, filters: PublicVideoFilterState): void {
    this.loads.push({ itemId, filters });
  }

  loadNextPage(): void {}
}
