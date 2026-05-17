import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output, Signal } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { LeafletMapComponent } from '@app/components/shared/leaflet-map/leaflet-map.component';
import { MapMarker } from '@app/models/map/map-marker';
import { ScreenState } from '@shared/models/contracts/screen-state.model';
import { UiChipComponent } from '@ui/primitives';
import { UiMapShellComponent, UiMapSlotComponent } from '@ui/maps';
import { ParkMapPointViewModel } from '../models/park-map-point-view.model';

@Component({
  selector: 'app-park-list-map',
  templateUrl: './park-list-map.component.html',
  styleUrls: ['./park-list-map.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LeafletMapComponent, TranslateModule, UiChipComponent, UiMapShellComponent, UiMapSlotComponent]
})
export class ParkListMapComponent {
  @Input() mapState!: Signal<ScreenState<ParkMapPointViewModel[], string>>;
  @Input() mapPoints!: Signal<ParkMapPointViewModel[]>;
  @Input() selectedParkId!: Signal<string | null>;
  @Output() parkSelected: EventEmitter<string | null> = new EventEmitter<string | null>();

  get hasMapPoints(): boolean {
    return this.mapPoints().length > 0;
  }

  get mapCenter(): [number, number] {
    const points: ParkMapPointViewModel[] = this.mapPoints();

    if (points.length === 0) {
      return [46.8, 2.2];
    }

    const totalLatitude: number = points.reduce((sum: number, point: ParkMapPointViewModel) => sum + point.latitude, 0);
    const totalLongitude: number = points.reduce((sum: number, point: ParkMapPointViewModel) => sum + point.longitude, 0);

    return [totalLatitude / points.length, totalLongitude / points.length];
  }

  get mapMarkers(): MapMarker[] {
    return this.mapPoints().map((point: ParkMapPointViewModel) => ({
      id: point.id,
      lat: point.latitude,
      lng: point.longitude,
      title: point.name,
      subtitle: point.locationLine ?? point.countryCode ?? null,
      details: this.buildMarkerDetails(point),
    }));
  }

  onMarkerClick(marker: MapMarker): void {
    this.parkSelected.emit(marker.id);
  }

  private buildMarkerDetails(point: ParkMapPointViewModel): string[] {
    const details: string[] = [];

    if (point.addressLine && point.addressLine !== point.locationLine) {
      details.push(point.addressLine);
    }

    details.push(point.coordinatesLine);
    return details;
  }
}
