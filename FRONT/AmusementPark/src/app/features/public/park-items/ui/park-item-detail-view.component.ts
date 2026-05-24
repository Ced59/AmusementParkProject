import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output, Signal, computed, signal } from '@angular/core';
import { NgFor, NgIf } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

import { LeafletMapComponent } from '@app/components/shared/leaflet-map/leaflet-map.component';
import { ImageDisplayComponent } from '@app/components/shared/image-display/image-display.component';
import { PageStateComponent } from '@app/components/shared/page-state/page-state.component';
import { ScreenState } from '@shared/models/contracts/screen-state.model';
import { MapDirectionsUrlService } from '@shared/services/maps/map-directions-url.service';
import { resolveLocationMarkerIconKind } from '@shared/utils/maps/map-marker-icon-kind.resolver';
import { MapMarker } from '@app/models/map/map-marker';
import { ParkItemDetailRowViewModel, ParkItemDetailViewModel } from '../models/park-item-detail-view.model';
import { UiItemCardComponent } from '@ui/cards';
import { UiMapShellComponent, UiMapSlotComponent } from '@ui/maps';
import { UiPhotoCarouselComponent } from '@ui/media';
import { SafeRichHtmlPipe } from '@shared/pipes';
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
    SafeRichHtmlPipe,
    UiSectionHeaderComponent,
    UiSurfaceDirective
  ]
})
export class ParkItemDetailViewComponent {
  readonly photoDisplayLimits: number[] = [4, 8, 12, 0];
  readonly selectedLocationPointId = signal<string | null>(null);

  readonly locationMapMarkers = computed<MapMarker[]>(() => {
    const currentDetail: ParkItemDetailViewModel | null = this.detail();

    if (!currentDetail) {
      return [];
    }

    return currentDetail.locationPoints.map((point) => {
      const pointLabel: string = this.translateService.instant(point.labelKey);

      return {
        id: point.id,
        lat: point.latitude,
        lng: point.longitude,
        title: pointLabel,
        subtitle: currentDetail.name,
        iconKind: resolveLocationMarkerIconKind(point.id),
        details: [],
        actionUrl: this.mapDirectionsUrlService.buildDirectionsUrl({
          latitude: point.latitude,
          longitude: point.longitude,
          label: `${currentDetail.name} - ${pointLabel}`
        }),
        actionLabel: this.translateService.instant('parks.map.navigate')
      };
    });
  });

  @Input({ required: true }) state!: Signal<ScreenState<unknown, string>>;
  @Input({ required: true }) detail!: Signal<ParkItemDetailViewModel | null>;

  @Output() backToItemsClicked: EventEmitter<void> = new EventEmitter<void>();

  constructor(
    private readonly mapDirectionsUrlService: MapDirectionsUrlService,
    private readonly translateService: TranslateService
  ) {
  }


  protected getSpotlightValue(row: ParkItemDetailRowViewModel): string {
    if (row.valueKey) {
      return this.translateService.instant(row.valueKey);
    }

    return row.value;
  }

  protected isSpotlightTextualValue(row: ParkItemDetailRowViewModel): boolean {
    if (row.isTextualValue === true) {
      return true;
    }

    if (row.labelKey === 'parkItems.fields.status') {
      return true;
    }

    const value: string = this.getSpotlightValue(row);
    return value.length > 7 && /[A-Za-zÀ-ÿ]/.test(value);
  }

  protected isSpotlightLongTextValue(row: ParkItemDetailRowViewModel): boolean {
    const value: string = this.getSpotlightValue(row);
    return this.isSpotlightTextualValue(row) && value.length > 12;
  }

  goBackToItems(): void {
    this.backToItemsClicked.emit();
  }

  selectLocationPoint(pointId: string): void {
    if (this.selectedLocationPointId() === pointId) {
      this.selectedLocationPointId.set(null);
      window.setTimeout((): void => this.selectedLocationPointId.set(pointId), 0);
      return;
    }

    this.selectedLocationPointId.set(pointId);
  }

  onLocationMarkerClick(marker: MapMarker): void {
    this.selectedLocationPointId.set(marker.id);
  }
}

