import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output, Signal, computed, signal } from '@angular/core';
import { NgFor, NgIf } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

import { LeafletStaticMapComponent } from '@shared/components/leaflet-static-map/leaflet-static-map.component';
import { ImageDisplayComponent } from '@shared/components/image-display/image-display.component';
import { PageStateComponent } from '@shared/components/page-state/page-state.component';
import { ScreenState } from '@shared/models/contracts/screen-state.model';
import { MapDirectionsUrlService } from '@shared/services/maps/map-directions-url.service';
import { resolveLocationMarkerIconKind } from '@shared/utils/maps/map-marker-icon-kind.resolver';
import { MapMarker } from '@app/models/map/map-marker';
import { ParkItemDetailRowViewModel, ParkItemDetailViewModel } from '../models/park-item-detail-view.model';
import { UiItemCardComponent } from '@ui/cards';
import { UiMapShellComponent, UiMapSlotComponent } from '@ui/maps';
import { SafeRichHtmlPipe } from '@shared/pipes';
import { UiButtonDirective, UiChipComponent, UiSectionHeaderComponent, UiSurfaceDirective } from '@ui/primitives';
import { PublicSharePanelComponent } from '@ui/sharing/public-share-panel/public-share-panel.component';
import { RatingStarsComponent } from '@features/public/ratings/ui/rating-stars.component';
import { AdminContextualBlockDirective } from '@features/admin/contextual-editing/ui/admin-contextual-block/admin-contextual-block.directive';
import { AdminContextualBlockInstance } from '@features/admin/contextual-editing/models/admin-contextual-block.model';
import { AdminContextualBlockRegistryService } from '@features/admin/contextual-editing/services/admin-contextual-block-registry.service';

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
    LeafletStaticMapComponent,
    UiButtonDirective,
    UiChipComponent,
    UiItemCardComponent,
    UiMapShellComponent,
    UiMapSlotComponent,
    SafeRichHtmlPipe,
    UiSectionHeaderComponent,
    UiSurfaceDirective,
    PublicSharePanelComponent,
    RatingStarsComponent,
    AdminContextualBlockDirective
  ]
})
export class ParkItemDetailViewComponent {
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
  @Input() heroImageResponsiveWidths: readonly number[] = [320, 480, 640, 800, 960, 1280];
  @Input() heroImageSizes: string = '(max-width: 900px) 100vw, 900px';
  @Input() heroImageSrcWidth: number | null = 960;
  @Input() currentLang: string = 'en';

  @Output() backToItemsClicked: EventEmitter<void> = new EventEmitter<void>();

  constructor(
    private readonly mapDirectionsUrlService: MapDirectionsUrlService,
    private readonly translateService: TranslateService,
    private readonly contextualBlockRegistry: AdminContextualBlockRegistryService
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

  protected getDescriptionContextualBlock(currentDetail: ParkItemDetailViewModel): AdminContextualBlockInstance | null {
    return this.contextualBlockRegistry.createParkItemBlock(
      'parkItem.description',
      currentDetail.id,
      currentDetail.parkId,
      currentDetail.name,
      this.currentLang
    );
  }

  protected getLocationContextualBlock(currentDetail: ParkItemDetailViewModel): AdminContextualBlockInstance | null {
    return this.contextualBlockRegistry.createParkItemBlock(
      'parkItem.location',
      currentDetail.id,
      currentDetail.parkId,
      currentDetail.name,
      this.currentLang,
      currentDetail.mapCenter
    );
  }

  protected getSpecGroupContextualBlock(
    group: { titleKey: string },
    currentDetail: ParkItemDetailViewModel
  ): AdminContextualBlockInstance | null {
    return group.titleKey === 'parkItems.detail.locationTitle'
      ? this.getLocationContextualBlock(currentDetail)
      : null;
  }
}

