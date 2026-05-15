import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output, Signal } from '@angular/core';
import { NgIf } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { SafeExternalUrlPipe } from '@shared/pipes';

import { PageStateComponent } from '@app/components/shared/page-state/page-state.component';
import { ScreenState } from '@shared/models/contracts/screen-state.model';
import { ParkCardModel } from '@shared/models/parks/park-card.model';
import { ParkContentSummaryViewModel } from '../models/park-content-summary.model';
import { ParkDetailViewModel } from '../models/park-detail-view.model';
import { ParkContentSummaryComponent } from './park-content-summary.component';
import { ParkHeroSectionComponent } from './park-hero-section.component';
import { ParkLocationSectionComponent } from './park-location-section.component';
import { ParkNearbySectionComponent } from './park-nearby-section.component';
import { ParkPracticalInfoSectionComponent } from './park-practical-info-section.component';
import { UiButtonDirective, UiChipComponent, UiKickerComponent, UiSurfaceDirective } from '@ui/primitives';

@Component({
  selector: 'app-park-detail-view',
  templateUrl: './park-detail-view.component.html',
  styleUrls: ['./park-detail-view.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [PageStateComponent, NgIf, ParkHeroSectionComponent, ParkPracticalInfoSectionComponent, ParkLocationSectionComponent, ParkNearbySectionComponent, ParkContentSummaryComponent, TranslateModule, SafeExternalUrlPipe, UiButtonDirective, UiChipComponent, UiKickerComponent, UiSurfaceDirective]
})
export class ParkDetailViewComponent {
  @Input() state!: Signal<ScreenState<unknown, string>>;
  @Input() park!: Signal<ParkDetailViewModel | null>;
  @Input() summary!: Signal<ParkContentSummaryViewModel | null>;
  @Input() nearbyParks!: Signal<ParkCardModel[]>;
  @Input() nearbyState!: Signal<import('@shared/models/contracts/screen-state.model').ScreenStateKind>;
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
