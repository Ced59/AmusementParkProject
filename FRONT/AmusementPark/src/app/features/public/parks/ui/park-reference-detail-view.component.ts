import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output, Signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { PublicContextualBlockMarker } from '@features/public/contextual-editing/models/public-contextual-block-marker.model';
import { PublicContextualBlockDirective } from '@features/public/contextual-editing/ui/public-contextual-block.directive';
import { ImageDisplayComponent } from '@shared/components/image-display/image-display.component';
import { PageStateComponent } from '@shared/components/page-state/page-state.component';
import { PaginationComponent } from '@shared/components/pagination/pagination.component';
import { ScreenState } from '@shared/models/contracts/screen-state.model';
import { SafeRichHtmlPipe } from '@shared/pipes';
import { UiPhotoCarouselComponent } from '@ui/media';
import { UiButtonDirective, UiChipComponent, UiKickerComponent } from '@ui/primitives';
import { PublicSharePanelComponent } from '@ui/sharing/public-share-panel/public-share-panel.component';
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
    ImageDisplayComponent,
    UiButtonDirective,
    UiChipComponent,
    UiKickerComponent,
    UiPhotoCarouselComponent,
    PublicSharePanelComponent,
    PublicContextualBlockDirective
  ]
})
export class ParkReferenceDetailViewComponent {
  protected readonly heroLogoResponsiveWidths: readonly number[] = [96, 160, 240];

  @Input() state!: Signal<ScreenState<unknown, string>>;
  @Input() reference!: Signal<ParkReferenceDetailViewModel | null>;
  @Input() attractionsLoading!: Signal<boolean>;
  @Input() currentLang: string = 'en';
  @Input() backLabelKey: string = 'parks.reference.backToParks';

  @Output() backClicked: EventEmitter<void> = new EventEmitter<void>();
  @Output() attractionsPageChanged: EventEmitter<{ page?: number; rows?: number }> = new EventEmitter<{ page?: number; rows?: number }>();

  goBack(): void {
    this.backClicked.emit();
  }

  onAttractionsPageChanged(event: { page?: number; rows?: number }): void {
    this.attractionsPageChanged.emit(event);
  }

  protected getManufacturerContextualBlock(currentReference: ParkReferenceDetailViewModel): PublicContextualBlockMarker | null {
    if (currentReference.kind !== 'manufacturer') {
      return null;
    }

    return {
      type: 'reference.manufacturer',
      manufacturerId: currentReference.id,
      contextLabel: currentReference.name,
      languageCode: this.currentLang,
      parkGraphUpsertDraftJson: currentReference.adminParkGraphUpsertJson,
      parkGraphUpsertFileName: currentReference.adminParkGraphUpsertFileName
    };
  }
}
