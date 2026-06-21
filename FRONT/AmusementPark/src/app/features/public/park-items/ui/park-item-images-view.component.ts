import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { AdminContextualBlockDirective } from '@features/admin/contextual-editing/ui/admin-contextual-block/admin-contextual-block.directive';
import { AdminContextualBlockInstance } from '@features/admin/contextual-editing/models/admin-contextual-block.model';
import { AdminContextualBlockRegistryService } from '@features/admin/contextual-editing/services/admin-contextual-block-registry.service';
import { Park } from '@app/models/parks/park';
import { ParkItem } from '@app/models/parks/park-item';
import { PageStateComponent } from '@shared/components/page-state/page-state.component';
import { ScreenState } from '@shared/models/contracts/screen-state.model';
import { UiPhotoCarouselCategoryOption, UiPhotoCarouselComponent, UiPhotoCarouselImage } from '@ui/media';
import { UiButtonDirective, UiChipComponent, UiKickerComponent, UiSurfaceDirective } from '@ui/primitives';

@Component({
  selector: 'app-park-item-images-view',
  templateUrl: './park-item-images-view.component.html',
  styleUrls: ['./park-item-images-view.component.scss'],
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
    AdminContextualBlockDirective
  ]
})
export class ParkItemImagesViewComponent {
  @Input() state: ScreenState<unknown, string> | null = null;
  @Input() item: ParkItem | null = null;
  @Input() park: Park | null = null;
  @Input() photos: UiPhotoCarouselImage[] = [];
  @Input() categories: UiPhotoCarouselCategoryOption[] = [];
  @Input() totalImages: number = 0;
  @Input() canLoadMore: boolean = false;
  @Input() loadingMore: boolean = false;
  @Input() language: string = 'en';
  @Input() detailLink: string[] | null = null;
  @Input() itemsLink: string[] | null = null;
  @Input() parkLink: string[] | null = null;

  @Output() loadMoreClicked: EventEmitter<void> = new EventEmitter<void>();

  constructor(private readonly contextualBlockRegistry: AdminContextualBlockRegistryService) {
  }

  loadMore(): void {
    this.loadMoreClicked.emit();
  }

  protected getImagesContextualBlock(currentItem: ParkItem): AdminContextualBlockInstance | null {
    return this.contextualBlockRegistry.createParkItemBlock(
      'parkItem.images',
      currentItem.id,
      currentItem.parkId,
      currentItem.name,
      this.language
    );
  }
}
