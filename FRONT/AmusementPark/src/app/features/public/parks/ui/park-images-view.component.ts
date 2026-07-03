import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { Park } from '@app/models/parks/park';
import { PublicContextualBlockMarker } from '@features/public/contextual-editing/models/public-contextual-block-marker.model';
import { PublicContextualBlockDirective } from '@features/public/contextual-editing/ui/public-contextual-block.directive';
import { PageStateComponent } from '@shared/components/page-state/page-state.component';
import { ScreenState } from '@shared/models/contracts/screen-state.model';
import { UiButtonDirective, UiChipComponent, UiKickerComponent, UiSurfaceDirective } from '@ui/primitives';
import { UiPhotoCarouselCategoryOption, UiPhotoCarouselComponent, UiPhotoCarouselImage } from '@ui/media';
import { ParkImagesGalleryTab } from '../models/park-images-view.model';

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
    UiSurfaceDirective,
    UiPhotoCarouselComponent,
    PublicContextualBlockDirective
  ]
})
export class ParkImagesViewComponent {
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

  protected getImagesContextualBlock(currentPark: Park): PublicContextualBlockMarker {
    return {
      type: 'park.images',
      parkId: currentPark.id,
      contextLabel: currentPark.name,
      languageCode: this.language
    };
  }
}
