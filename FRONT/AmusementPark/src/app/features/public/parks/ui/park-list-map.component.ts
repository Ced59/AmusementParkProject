import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output, Signal, computed, signal } from '@angular/core';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { LeafletMapComponent } from '@app/components/shared/leaflet-map/leaflet-map.component';
import { MapMarker } from '@app/models/map/map-marker';
import { ScreenState } from '@shared/models/contracts/screen-state.model';
import { ParkRegionFilter, ParkRegionFilterOption } from '@shared/models/geo/world-region-filter.model';
import { UiChipComponent } from '@ui/primitives';
import { UiMapShellComponent, UiMapSlotComponent } from '@ui/maps';
import { ParkMapPointViewModel } from '../models/park-map-point-view.model';
import { MapDirectionsUrlService } from '@shared/services/maps/map-directions-url.service';

@Component({
  selector: 'app-park-list-map',
  templateUrl: './park-list-map.component.html',
  styleUrls: ['./park-list-map.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LeafletMapComponent, TranslateModule, UiChipComponent, UiMapShellComponent, UiMapSlotComponent]
})
export class ParkListMapComponent {
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
    return this.mapPoints().map((point: ParkMapPointViewModel) => ({
      id: point.id,
      lat: point.latitude,
      lng: point.longitude,
      title: point.name,
      subtitle: point.locationLine ?? point.countryName ?? point.countryCode ?? null,
      details: this.buildMarkerDetails(point),
      actionUrl: this.mapDirectionsUrlService.buildDirectionsUrl({
        latitude: point.latitude,
        longitude: point.longitude,
        label: point.name
      }),
      actionLabel: this.translateService.instant('parks.map.navigate'),
    }));
  });

  constructor(
    private readonly translateService: TranslateService,
    private readonly mapDirectionsUrlService: MapDirectionsUrlService
  ) {
  }

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

