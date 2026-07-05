import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output, Signal, inject } from '@angular/core';
import { NgFor, NgIf } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

import { EmptyStateComponent } from '@shared/components/empty-state/empty-state.component';
import { LeafletMapComponent } from '@shared/components/leaflet-map/leaflet-map.component';
import { MapMarker } from '@app/models/map/map-marker';
import { PageStateComponent } from '@shared/components/page-state/page-state.component';
import { SafeRichHtmlPipe } from '@shared/pipes';
import { ScreenState } from '@shared/models/contracts/screen-state.model';
import { MapMarkerPopupActionService } from '@shared/services/maps/map-marker-popup-action.service';
import { UiMapSlotComponent } from '@ui/maps';
import { UiButtonDirective, UiChipComponent, UiKickerComponent, UiStatCardComponent, UiSurfaceDirective } from '@ui/primitives';
import { PublicSharePanelComponent } from '@ui/sharing/public-share-panel/public-share-panel.component';
import { ParkItemCardComponent } from '../../park-items/ui/park-item-card.component';
import { ParkZonePageViewModel } from '../models/park-zone-page.model';

@Component({
  selector: 'app-park-zone-view',
  templateUrl: './park-zone-view.component.html',
  styleUrls: ['./park-zone-view.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    NgFor,
    NgIf,
    RouterLink,
    TranslateModule,
    EmptyStateComponent,
    LeafletMapComponent,
    PageStateComponent,
    ParkItemCardComponent,
    SafeRichHtmlPipe,
    UiButtonDirective,
    UiChipComponent,
    UiKickerComponent,
    UiMapSlotComponent,
    PublicSharePanelComponent,
    UiStatCardComponent,
    UiSurfaceDirective
  ]
})
export class ParkZoneViewComponent {
  @Input({ required: true }) state!: Signal<ScreenState<unknown, string>>;
  @Input({ required: true }) page!: Signal<ParkZonePageViewModel | null>;
  @Output() backClicked: EventEmitter<void> = new EventEmitter<void>();

  private readonly mapMarkerPopupActionService: MapMarkerPopupActionService = inject(MapMarkerPopupActionService);
  private readonly translateService: TranslateService = inject(TranslateService);

  protected get mapMarkers(): MapMarker[] {
    const currentPage: ParkZonePageViewModel | null = this.page();

    if (!currentPage) {
      return [];
    }

    const navigateLabel: string = this.translateService.instant('parks.map.navigate');
    const openDetailLabel: string = this.translateService.instant('parks.map.openDetail');

    return currentPage.map.markers.map((marker: MapMarker) => this.mapMarkerPopupActionService.enrich(marker, {
      directions: {
        latitude: marker.lat,
        longitude: marker.lng,
        label: marker.title
      },
      directionsLabel: navigateLabel,
      detailLabel: openDetailLabel
    }));
  }

  onBackClicked(): void {
    this.backClicked.emit();
  }
}
