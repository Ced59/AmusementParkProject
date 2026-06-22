import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output, Signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { AdminContextualBlockDirective } from '@features/admin/contextual-editing/ui/admin-contextual-block/admin-contextual-block.directive';
import { AdminContextualBlockInstance } from '@features/admin/contextual-editing/models/admin-contextual-block.model';
import { AdminContextualBlockRegistryService } from '@features/admin/contextual-editing/services/admin-contextual-block-registry.service';
import { PageStateComponent } from '@shared/components/page-state/page-state.component';
import { PaginationComponent } from '@shared/components/pagination/pagination.component';
import { ScreenState } from '@shared/models/contracts/screen-state.model';
import { SafeRichHtmlPipe } from '@shared/pipes';
import { UiPhotoCarouselComponent } from '@ui/media';
import { UiButtonDirective, UiChipComponent, UiKickerComponent } from '@ui/primitives';
import { ParkReferenceDetailViewModel } from '../models/park-reference-detail-view.model';

@Component({
  selector: 'app-park-reference-detail-view',
  templateUrl: './park-reference-detail-view.component.html',
  styleUrls: ['./park-reference-detail-view.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    PageStateComponent,
    PaginationComponent,
    RouterLink,
    TranslateModule,
    SafeRichHtmlPipe,
    UiButtonDirective,
    UiChipComponent,
    UiKickerComponent,
    UiPhotoCarouselComponent,
    AdminContextualBlockDirective
  ]
})
export class ParkReferenceDetailViewComponent {
  @Input() state!: Signal<ScreenState<unknown, string>>;
  @Input() reference!: Signal<ParkReferenceDetailViewModel | null>;
  @Input() attractionsLoading!: Signal<boolean>;
  @Input() currentLang: string = 'en';
  @Input() backLabelKey: string = 'parks.reference.backToParks';

  @Output() backClicked: EventEmitter<void> = new EventEmitter<void>();
  @Output() attractionsPageChanged: EventEmitter<{ page?: number; rows?: number }> = new EventEmitter<{ page?: number; rows?: number }>();

  constructor(private readonly contextualBlockRegistry: AdminContextualBlockRegistryService) {
  }

  goBack(): void {
    this.backClicked.emit();
  }

  onAttractionsPageChanged(event: { page?: number; rows?: number }): void {
    this.attractionsPageChanged.emit(event);
  }

  protected getManufacturerContextualBlock(currentReference: ParkReferenceDetailViewModel): AdminContextualBlockInstance | null {
    if (currentReference.kind !== 'manufacturer') {
      return null;
    }

    return this.contextualBlockRegistry.createManufacturerBlock(
      currentReference.id,
      currentReference.name,
      this.currentLang,
      currentReference.adminParkGraphUpsertJson,
      currentReference.adminParkGraphUpsertFileName
    );
  }
}
