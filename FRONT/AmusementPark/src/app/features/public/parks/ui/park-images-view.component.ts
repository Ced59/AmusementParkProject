import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { Park } from '@app/models/parks/park';
import { PublicContextualBlockMarker } from '@features/public/contextual-editing/models/public-contextual-block-marker.model';
import { PublicContextualBlockDirective } from '@features/public/contextual-editing/ui/public-contextual-block.directive';
import { PageStateComponent } from '@shared/components/page-state/page-state.component';
import { ScreenState } from '@shared/models/contracts/screen-state.model';
import { UiButtonDirective, UiChipComponent, UiKickerComponent, UiSegmentedTab, UiSegmentedTabsComponent, UiSurfaceDirective } from '@ui/primitives';
import { UiPhotoCarouselCategoryOption, UiPhotoCarouselComponent, UiPhotoCarouselImage } from '@ui/media';
import { PublicSharePanelComponent } from '@ui/sharing/public-share-panel/public-share-panel.component';
import { ParkImagesGalleryTab } from '../models/park-images-view.model';
import { ParkLifecycleNoticeComponent } from './park-lifecycle-notice.component';

@Component({
  selector: 'app-park-images-view',
  templateUrl: './park-images-view.component.html',
  styleUrls: ['./park-images-view.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    PageStateComponent,
    RouterLink,
    TranslateModule,
    UiButtonDirective,
    UiChipComponent,
    UiKickerComponent,
    UiSegmentedTabsComponent,
    UiSurfaceDirective,
    UiPhotoCarouselComponent,
    PublicSharePanelComponent,
    PublicContextualBlockDirective,
    ParkLifecycleNoticeComponent
  ]
})
export class ParkImagesViewComponent {
  protected readonly imageTabs: readonly UiSegmentedTab[] = [
    { id: 'park', labelKey: 'parks.imagesPage.tabs.park' },
    { id: 'items', labelKey: 'parks.imagesPage.tabs.items' }
  ];
  @Input() state: ScreenState<unknown, string> | null = null;
  @Input() park: Park | null = null;
  @Input() photos: UiPhotoCarouselImage[] = [];
  @Input() categories: UiPhotoCarouselCategoryOption[] = [];
  @Input() activeTab: ParkImagesGalleryTab = 'park';
  @Input() parkTabImageCount: number = 0;
  @Input() itemTabImageCount: number = 0;
  @Input() showItemTab: boolean = false;
  @Input() totalImages: number = 0;
  @Input() canLoadMore: boolean = false;
  @Input() loadingMore: boolean = false;
  @Input() itemImagesLoading: boolean = false;
  @Input() language: string = 'en';
  @Input() detailLink: string[] | null = null;
  @Input() itemsLink: string[] | null = null;

  @Output() tabSelected: EventEmitter<ParkImagesGalleryTab> = new EventEmitter<ParkImagesGalleryTab>();
  @Output() loadMoreClicked: EventEmitter<void> = new EventEmitter<void>();

  loadMore(): void {
    this.loadMoreClicked.emit();
  }

  selectTab(tab: ParkImagesGalleryTab): void {
    this.tabSelected.emit(tab);
  }

  selectTabById(tabId: string): void {
    if (tabId === 'park' || tabId === 'items') {
      this.selectTab(tabId);
    }
  }

  protected tabsWithCounts(): readonly UiSegmentedTab[] {
    return [
      { ...this.imageTabs[0], count: this.parkTabImageCount },
      { ...this.imageTabs[1], count: this.itemTabImageCount }
    ];
  }

  protected getImagesContextualBlock(currentPark: Park): PublicContextualBlockMarker {
    return {
      type: 'park.images',
      parkId: currentPark.id,
      contextLabel: currentPark.name,
      languageCode: this.language
    };
  }
}
