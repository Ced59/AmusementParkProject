import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output, Signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { SafeExternalUrlPipe, SafeRichHtmlPipe } from '@shared/pipes';

import { ImageDisplayComponent } from '@app/components/shared/image-display/image-display.component';
import { PageStateComponent } from '@app/components/shared/page-state/page-state.component';
import { ScreenState, ScreenStateKind } from '@shared/models/contracts/screen-state.model';
import { ParkCardModel } from '@shared/models/parks/park-card.model';
import { UiButtonDirective, UiChipComponent, UiKickerComponent, UiStatCardComponent } from '@ui/primitives';
import { ParkContentSummaryViewModel } from '../models/park-content-summary.model';
import { ParkDetailViewModel } from '../models/park-detail-view.model';
import { ParkItemsMapViewModel } from '../models/park-items-map-view.model';
import { ParkZoneDetailViewModel } from '../models/park-zone-detail-view.model';
import { ParkContentSummaryComponent } from './park-content-summary.component';
import { ParkItemsMapSectionComponent } from './park-items-map-section.component';
import { ParkLocationSectionComponent } from './park-location-section.component';
import { ParkNearbySectionComponent } from './park-nearby-section.component';
import { ParkZonesSectionComponent } from './park-zones-section.component';

@Component({
  selector: 'app-park-detail-view',
  templateUrl: './park-detail-view.component.html',
  styleUrls: ['./park-detail-view.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    PageStateComponent,
    ImageDisplayComponent,
    ParkContentSummaryComponent,
    ParkItemsMapSectionComponent,
    ParkLocationSectionComponent,
    ParkNearbySectionComponent,
    ParkZonesSectionComponent,
    TranslateModule,
    SafeExternalUrlPipe,
    SafeRichHtmlPipe,
    RouterLink,
    UiButtonDirective,
    UiChipComponent,
    UiKickerComponent,
    UiStatCardComponent
  ]
})
export class ParkDetailViewComponent {
  @Input() state!: Signal<ScreenState<unknown, string>>;
  @Input() park!: Signal<ParkDetailViewModel | null>;
  @Input() summary!: Signal<ParkContentSummaryViewModel | null>;
  @Input() itemsMap!: Signal<ParkItemsMapViewModel | null>;
  @Input() zones!: Signal<ParkZoneDetailViewModel[]>;
  @Input() nearbyParks!: Signal<ParkCardModel[]>;
  @Input() nearbyState!: Signal<ScreenStateKind>;
  @Input() currentLang!: Signal<string>;

  @Output() backClicked: EventEmitter<void> = new EventEmitter<void>();
  @Output() exploreClicked: EventEmitter<void> = new EventEmitter<void>();

  goBack(): void {
    this.backClicked.emit();
  }

  goToExplore(): void {
    this.exploreClicked.emit();
  }
}
