import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output, Signal, computed, inject, signal } from '@angular/core';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { LeafletMapComponent } from '@shared/components/leaflet-map/leaflet-map.component';
import { MapMarker } from '@app/models/map/map-marker';
import { ScreenState } from '@shared/models/contracts/screen-state.model';
import { ParkRegionFilter, ParkRegionFilterOption } from '@shared/models/geo/world-region-filter.model';
import { UiChipComponent } from '@ui/primitives';
import { UiMapShellComponent, UiMapSlotComponent } from '@ui/maps';
import { ParkMapPointViewModel } from '../models/park-map-point-view.model';
import { MapMarkerPopupActionService } from '@shared/services/maps/map-marker-popup-action.service';
import { LocalizedPluralPipe } from '@shared/pipes';
import { buildPublicStandaloneAttractionRouteCommands } from '@shared/utils/routing/public-detail-route.helpers';

@Component({
  selector: 'app-park-list-map',
  templateUrl: './park-list-map.component.html',
  styleUrls: ['./park-list-map.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LeafletMapComponent, TranslateModule, UiChipComponent, UiMapShellComponent, UiMapSlotComponent, LocalizedPluralPipe]
})
export class ParkListMapComponent {
  private readonly translateService: TranslateService = inject(TranslateService);
  private readonly mapMarkerPopupActionService: MapMarkerPopupActionService = inject(MapMarkerPopupActionService);
  private readonly emptyMapState = signal<ScreenState<ParkMapPointViewModel[], string>>({ kind: 'ready', data: [] });
  private readonly emptyMapPoints = signal<ParkMapPointViewModel[]>([]);
  private readonly emptySelectedParkId = signal<string | null>(null);
  private readonly emptySelectedRegion = signal<ParkRegionFilter | null>(null);

  @Input() mapState: Signal<ScreenState<ParkMapPointViewModel[], string>> = this.emptyMapState.asReadonly();
  @Input() mapPoints: Signal<ParkMapPointViewModel[]> = this.emptyMapPoints.asReadonly();
  @Input() selectedParkId: Signal<string | null> = this.emptySelectedParkId.asReadonly();
  @Input() selectedRegion: Signal<ParkRegionFilter | null> = this.emptySelectedRegion.asReadonly();
  @Input() titleKey: string = 'parks.map.title';
  @Input() subtitleKey: string = 'parks.map.subtitle';
  @Input() mapCountPluralKey: string = 'publicCounts.parkOnMap';
  @Output() parkSelected: EventEmitter<string | null> = new EventEmitter<string | null>();
  @Output() regionFilterChanged: EventEmitter<ParkRegionFilter | null> = new EventEmitter<ParkRegionFilter | null>();

  readonly regionFilters: ParkRegionFilterOption[] = [
    { value: null, labelKey: 'parks.map.regionFilters.all' },
    { value: 'europe', labelKey: 'parks.map.regionFilters.europe' },
    { value: 'north-america', labelKey: 'parks.map.regionFilters.northAmerica' },
    { value: 'south-america', labelKey: 'parks.map.regionFilters.southAmerica' },
    { value: 'asia', labelKey: 'parks.map.regionFilters.asia' },
    { value: 'middle-east', labelKey: 'parks.map.regionFilters.middleEast' },
    { value: 'oceania', labelKey: 'parks.map.regionFilters.oceania' },
    { value: 'africa', labelKey: 'parks.map.regionFilters.africa' },
  ];

  readonly hasMapPoints = computed<boolean>(() => this.mapPoints().length > 0);

  readonly mapCenter = computed<[number, number]>(() => {
    const points: ParkMapPointViewModel[] = this.mapPoints();

    if (points.length === 0) {
      return [46.8, 2.2];
    }

    const totalLatitude: number = points.reduce((sum: number, point: ParkMapPointViewModel) => sum + point.latitude, 0);
    const totalLongitude: number = points.reduce((sum: number, point: ParkMapPointViewModel) => sum + point.longitude, 0);

    return [totalLatitude / points.length, totalLongitude / points.length];
  });

  readonly mapMarkers = computed<MapMarker[]>(() => {
    const navigateLabel: string = this.translateService.instant('parks.map.navigate');
    const openDetailLabel: string = this.translateService.instant('parks.map.openDetail');

    return this.mapPoints().map((point: ParkMapPointViewModel) => this.mapMarkerPopupActionService.enrich({
      id: point.id,
      lat: point.latitude,
      lng: point.longitude,
      title: point.name,
      subtitle: point.locationLine ?? point.countryName ?? point.countryCode ?? null,
      iconKind: point.kind === 'park' ? 'park' : 'rollerCoaster',
      detailActionRouteCommands: point.kind === 'standaloneAttraction'
        ? buildPublicStandaloneAttractionRouteCommands({
          language: this.translateService.currentLang,
          attractionId: point.id,
          attractionName: point.name
        })
        : null,
      details: this.buildMarkerDetails(point)
    }, {
      directions: this.isOperating(point) ? {
        latitude: point.latitude,
        longitude: point.longitude,
        label: point.name
      } : null,
      directionsLabel: this.isOperating(point) ? navigateLabel : null,
      parkDetail: point.kind === 'park' ? {
        language: this.translateService.currentLang,
        parkId: point.id,
        parkName: point.name
      } : null,
      detailLabel: openDetailLabel
    }));
  });


  onRegionFilterClick(region: ParkRegionFilter | null): void {
    this.regionFilterChanged.emit(region);
  }

  isRegionSelected(region: ParkRegionFilter | null): boolean {
    return this.selectedRegion() === region;
  }

  onMarkerClick(marker: MapMarker): void {
    this.parkSelected.emit(marker.id);
  }

  private buildMarkerDetails(point: ParkMapPointViewModel): string[] {
    const details: string[] = [];

    if (point.addressLine && point.addressLine !== point.locationLine) {
      details.push(point.addressLine);
    }

    return details;
  }

  private isOperating(point: ParkMapPointViewModel): boolean {
    const status: string = point.status?.trim().toLowerCase().replace(/[\s_-]+/g, '') ?? '';
    return status === 'operating' || status === 'open' || status === 'opened';
  }
}
