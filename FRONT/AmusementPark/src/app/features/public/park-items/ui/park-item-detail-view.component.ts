import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output, Signal } from '@angular/core';
import { NgFor, NgIf } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { LeafletMapComponent } from '@app/components/shared/leaflet-map/leaflet-map.component';
import { ImageDisplayComponent } from '@app/components/shared/image-display/image-display.component';
import { PageStateComponent } from '@app/components/shared/page-state/page-state.component';
import { ScreenState } from '@shared/models/contracts/screen-state.model';
import { ParkItemDetailViewModel } from '../models/park-item-detail-view.model';
import { UiItemCardComponent } from '@ui/cards';
import { UiMapShellComponent, UiMapSlotComponent } from '@ui/maps';
import { UiPhotoCarouselComponent } from '@ui/media';
import { UiButtonDirective, UiChipComponent, UiSectionHeaderComponent, UiSurfaceDirective } from '@ui/primitives';

@Component({
  selector: 'app-park-item-detail-view',
  templateUrl: './park-item-detail-view.component.html',
  styleUrls: ['./park-item-detail-view.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    NgIf,
    NgFor,
    RouterLink,
    PageStateComponent,
    ImageDisplayComponent,
    TranslateModule,
    LeafletMapComponent,
    UiButtonDirective,
    UiChipComponent,
    UiItemCardComponent,
    UiMapShellComponent,
    UiMapSlotComponent,
    UiPhotoCarouselComponent,
    UiSectionHeaderComponent,
    UiSurfaceDirective
  ]
})
export class ParkItemDetailViewComponent {
  readonly photoDisplayLimits: number[] = [4, 8, 12, 0];

  @Input({ required: true }) state!: Signal<ScreenState<unknown, string>>;
  @Input({ required: true }) detail!: Signal<ParkItemDetailViewModel | null>;

  @Output() backToItemsClicked: EventEmitter<void> = new EventEmitter<void>();

  goBackToItems(): void {
    this.backToItemsClicked.emit();
  }
}
