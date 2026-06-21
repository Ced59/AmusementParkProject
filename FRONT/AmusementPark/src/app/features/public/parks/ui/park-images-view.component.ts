import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { AdminContextualBlockDirective } from '@features/admin/contextual-editing/ui/admin-contextual-block/admin-contextual-block.directive';
import { AdminContextualBlockInstance } from '@features/admin/contextual-editing/models/admin-contextual-block.model';
import { AdminContextualBlockRegistryService } from '@features/admin/contextual-editing/services/admin-contextual-block-registry.service';
import { Park } from '@app/models/parks/park';
import { PageStateComponent } from '@shared/components/page-state/page-state.component';
import { ScreenState } from '@shared/models/contracts/screen-state.model';
import { UiButtonDirective, UiChipComponent, UiKickerComponent, UiSurfaceDirective } from '@ui/primitives';
import { UiPhotoCarouselCategoryOption, UiPhotoCarouselComponent, UiPhotoCarouselImage } from '@ui/media';

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
    AdminContextualBlockDirective
  ]
})
export class ParkImagesViewComponent {
  @Input() state: ScreenState<unknown, string> | null = null;
  @Input() park: Park | null = null;
  @Input() photos: UiPhotoCarouselImage[] = [];
  @Input() categories: UiPhotoCarouselCategoryOption[] = [];
  @Input() totalImages: number = 0;
  @Input() canLoadMore: boolean = false;
  @Input() loadingMore: boolean = false;
  @Input() language: string = 'en';
  @Input() detailLink: string[] | null = null;
  @Input() itemsLink: string[] | null = null;

  @Output() loadMoreClicked: EventEmitter<void> = new EventEmitter<void>();

  constructor(private readonly contextualBlockRegistry: AdminContextualBlockRegistryService) {
  }

  loadMore(): void {
    this.loadMoreClicked.emit();
  }

  protected getImagesContextualBlock(currentPark: Park): AdminContextualBlockInstance | null {
    return this.contextualBlockRegistry.createParkBlock(
      'park.images',
      currentPark.id,
      currentPark.name,
      this.language
    );
  }
}
