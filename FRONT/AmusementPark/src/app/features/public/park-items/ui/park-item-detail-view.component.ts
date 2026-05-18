import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output, Signal } from '@angular/core';
import { NgFor, NgIf } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { LeafletMapComponent } from '@app/components/shared/leaflet-map/leaflet-map.component';
import { PageStateComponent } from '@app/components/shared/page-state/page-state.component';
import { ScreenState } from '@shared/models/contracts/screen-state.model';
import { ParkItemDetailViewModel } from '../models/park-item-detail-view.model';
import { UiItemCardComponent } from '@ui/cards';
import { UiMapShellComponent, UiMapSlotComponent } from '@ui/maps';
import { UiButtonDirective, UiChipComponent, UiSectionHeaderComponent, UiStatCardComponent, UiSurfaceDirective } from '@ui/primitives';

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
    TranslateModule,
    LeafletMapComponent,
    UiButtonDirective,
    UiChipComponent,
    UiItemCardComponent,
    UiMapShellComponent,
    UiMapSlotComponent,
    UiSectionHeaderComponent,
    UiStatCardComponent,
    UiSurfaceDirective
  ]
})
export class ParkItemDetailViewComponent {
  @Input({ required: true }) state!: Signal<ScreenState<unknown, string>>;
  @Input({ required: true }) detail!: Signal<ParkItemDetailViewModel | null>;

  @Output() backToItemsClicked: EventEmitter<void> = new EventEmitter<void>();

  goBackToItems(): void {
    this.backToItemsClicked.emit();
  }
}
