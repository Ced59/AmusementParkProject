import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { SafeExternalUrlPipe, SafeRichHtmlPipe } from '@shared/pipes';

import {
  PublicContextualBlockMarker,
  PublicContextualBlockType
} from '@features/public/contextual-editing/models/public-contextual-block-marker.model';
import { PublicContextualBlockDirective } from '@features/public/contextual-editing/ui/public-contextual-block.directive';
import { ImageDisplayComponent } from '@shared/components/image-display/image-display.component';
import { PageStateComponent } from '@shared/components/page-state/page-state.component';
import { ScreenState } from '@shared/models/contracts/screen-state.model';
import { ParkCardModel } from '@shared/models/parks/park-card.model';
import { ParkOpeningHoursCalendar } from '@app/models/parks/park-opening-hours';
import { ParkWeatherForecast } from '@app/models/parks/park-weather';
import { UiButtonDirective, UiChipComponent, UiKickerComponent, UiStatCardComponent } from '@ui/primitives';
import { ParkContentSummaryViewModel } from '../models/park-content-summary.model';
import { ParkDetailViewModel } from '../models/park-detail-view.model';
import { ParkContentSummaryComponent } from './park-content-summary.component';
import { ParkLocationSectionComponent } from './park-location-section.component';
import { ParkNearbySectionComponent } from './park-nearby-section.component';
import { ParkOpeningHoursCardComponent } from './park-opening-hours-card.component';
import { ParkWeatherCardComponent } from './park-weather-card.component';
import { PublicSharePanelComponent } from '@ui/sharing/public-share-panel/public-share-panel.component';
import { RatingStarsComponent } from '@features/public/ratings/ui/rating-stars.component';

@Component({
  selector: 'app-park-detail-view',
  templateUrl: './park-detail-view.component.html',
  styleUrls: ['./park-detail-view.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    PageStateComponent,
    ImageDisplayComponent,
    ParkContentSummaryComponent,
    ParkLocationSectionComponent,
    ParkNearbySectionComponent,
    ParkOpeningHoursCardComponent,
    ParkWeatherCardComponent,
    TranslateModule,
    SafeExternalUrlPipe,
    SafeRichHtmlPipe,
    RouterLink,
    UiButtonDirective,
    UiChipComponent,
    UiKickerComponent,
    UiStatCardComponent,
    PublicSharePanelComponent,
    RatingStarsComponent,
    PublicContextualBlockDirective
  ]
})
export class ParkDetailViewComponent {
  @Input() state: ScreenState<unknown, string> | null = null;
  @Input() park: ParkDetailViewModel | null = null;
  @Input() summary: ParkContentSummaryViewModel | null = null;
  @Input() weather: ParkWeatherForecast | null = null;
  @Input() weatherState: ScreenState<unknown, string> | null = null;
  @Input() openingHours: ParkOpeningHoursCalendar | null = null;
  @Input() openingHoursState: ScreenState<unknown, string> | null = null;
  @Input() nearbyParks: ParkCardModel[] = [];
  @Input() nearbyState: ScreenState<unknown, string> | null = null;
  @Input() currentLang: string = 'en';
  @Input() heroImageResponsiveWidths: readonly number[] = [320, 480, 640, 800, 960, 1280];
  @Input() heroImageSizes: string = '(max-width: 900px) 100vw, 900px';
  @Input() heroImageSrcWidth: number | null = 960;
  @Output() backClicked: EventEmitter<void> = new EventEmitter<void>();
  @Output() exploreClicked: EventEmitter<void> = new EventEmitter<void>();

  goBack(): void {
    this.backClicked.emit();
  }

  goToExplore(): void {
    this.exploreClicked.emit();
  }

  protected getContextualBlock(type: PublicContextualBlockType, currentPark: ParkDetailViewModel): PublicContextualBlockMarker {
    return {
      type,
      parkId: currentPark.id,
      contextLabel: currentPark.name,
      languageCode: this.currentLang,
      locationFallbackCenter: this.resolveLocationFallbackCenter(currentPark)
    };
  }

  private resolveLocationFallbackCenter(currentPark: ParkDetailViewModel): readonly [number, number] | null {
    return this.isValidCoordinatePair(currentPark.latitude, currentPark.longitude)
      ? [currentPark.latitude as number, currentPark.longitude as number]
      : null;
  }

  private isValidCoordinatePair(latitude: number | null, longitude: number | null): boolean {
    return latitude !== null &&
      longitude !== null &&
      Number.isFinite(latitude) &&
      Number.isFinite(longitude) &&
      latitude >= -90 &&
      latitude <= 90 &&
      longitude >= -180 &&
      longitude <= 180;
  }
}
