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

@Component({
  selector: 'app-park-list-map',
  templateUrl: './park-list-map.component.html',
  styleUrls: ['./park-list-map.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LeafletMapComponent, TranslateModule, UiChipComponent, UiMapShellComponent, UiMapSlotComponent]
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
  @Output() parkSelected: EventEmitter<string | null> = new EventEmitter<string | null>();
  @Output() regionFilterChanged: EventEmitter<ParkRegionFilter | null> = new EventEmitter<ParkRegionFilter | null>();

  readonly regionFilters: ParkRegionFilterOption[] = [
    { value: null, labelKey: 'parks.map.regionFilters.all' },
    { value: 'europe', labelKey: 'parks.map.regionFilters.europe' },
    { value: 'north-america', labelKey: 'parks.map.regionFilters.northAmerica' },
    { value: 'south-america', labelKey: 'parks.map.regionFilters.southAmerica' },
    { value: 'orient', labelKey: 'parks.map.regionFilters.orient' },
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
      iconKind: 'park',
      details: this.buildMarkerDetails(point)
    }, {
      directions: {
        latitude: point.latitude,
        longitude: point.longitude,
        label: point.name
      },
      directionsLabel: navigateLabel,
      parkDetail: {
        language: this.translateService.currentLang,
        parkId: point.id,
        parkName: point.name
      },
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
}

