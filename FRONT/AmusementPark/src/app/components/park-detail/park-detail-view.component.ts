import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output, Signal } from '@angular/core';
import { NgIf } from '@angular/common';
import { Bind } from 'primeng/bind';
import { ButtonDirective } from 'primeng/button';
import { TranslateModule } from '@ngx-translate/core';
import { Park } from '@app/models/parks/park';
import { ParkExplorer } from '@app/models/parks/park-explorer';
import { ScreenState } from '@shared/models/contracts/screen-state.model';
import { PageStateComponent } from '../shared/page-state/page-state.component';
import { ParkHeroSectionComponent } from '../public/park-hero-section/park-hero-section.component';
import { ParkPracticalInfoSectionComponent } from '../public/park-practical-info-section/park-practical-info-section.component';
import { ParkLocationSectionComponent } from '../public/park-location-section/park-location-section.component';
import { ParkNearbySectionComponent } from '../public/park-nearby-section/park-nearby-section.component';
import { ParkContentSummaryComponent } from '../public/park-content-summary/park-content-summary.component';

@Component({
  selector: 'app-park-detail-view',
  templateUrl: './park-detail-view.component.html',
  styleUrls: ['./park-detail.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [PageStateComponent, NgIf, Bind, ButtonDirective, ParkHeroSectionComponent, ParkPracticalInfoSectionComponent, ParkLocationSectionComponent, ParkNearbySectionComponent, ParkContentSummaryComponent, TranslateModule]
})
export class ParkDetailViewComponent {
  @Input() state!: Signal<ScreenState<unknown, string>>;
  @Input() park!: Signal<Park | null>;
  @Input() explorer!: Signal<ParkExplorer | null>;
  @Input() nearbyParks!: Signal<Park[]>;
  @Input() nearbyState!: Signal<import('@shared/models/contracts/screen-state.model').ScreenStateKind>;
  @Input() currentLang!: Signal<string>;
  @Input() hasPracticalInfoFn: (park: Park | null) => boolean = () => false;
  @Input() hasLocationInfoFn: (park: Park | null) => boolean = () => false;

  @Output() backClicked: EventEmitter<void> = new EventEmitter<void>();
  @Output() exploreClicked: EventEmitter<void> = new EventEmitter<void>();

  goBack(): void {
    this.backClicked.emit();
  }

  goToExplore(): void {
    this.exploreClicked.emit();
  }

  hasPracticalInfo(park: Park | null): boolean {
    return this.hasPracticalInfoFn(park);
  }

  hasLocationInfo(park: Park | null): boolean {
    return this.hasLocationInfoFn(park);
  }
}
