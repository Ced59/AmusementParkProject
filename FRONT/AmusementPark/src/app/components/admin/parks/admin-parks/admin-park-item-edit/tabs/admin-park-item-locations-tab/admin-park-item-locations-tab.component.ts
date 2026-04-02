import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormGroup } from '@angular/forms';
import { MapMarker } from '../../../../../../../models/map/map-marker';
import { AttractionLocationPoint } from '../../../../../../../models/parks/attraction-location-point';

export type AttractionLocationKey = 'entrance' | 'exit' | 'fastPassEntrance' | 'reducedMobilityEntrance';

interface AttractionLocationOption {
  key: AttractionLocationKey;
  labelKey: string;
}

@Component({
    selector: 'app-admin-park-item-locations-tab',
    templateUrl: './admin-park-item-locations-tab.component.html',
    styleUrls: ['./admin-park-item-locations-tab.component.scss'],
    standalone: false
})
export class AdminParkItemLocationsTabComponent {
  @Input({ required: true }) formGroup!: FormGroup;
  @Input() selectedLocationKey: AttractionLocationKey = 'entrance';
  @Input() attractionLocationOptions: AttractionLocationOption[] = [];
  @Input() locationMapCenter: [number, number] = [48.8566, 2.3522];
  @Input() locationMapZoom: number = 19;
  @Input() locationMapMarkers: MapMarker[] = [];
  @Input() isSaving: boolean = false;

  @Output() selectedLocationKeyChange: EventEmitter<AttractionLocationKey> = new EventEmitter<AttractionLocationKey>();
  @Output() specificLocationMapPositionChange: EventEmitter<{ lat: number; lng: number }> = new EventEmitter<{ lat: number; lng: number }>();
  @Output() clearLocationPoint: EventEmitter<AttractionLocationKey> = new EventEmitter<AttractionLocationKey>();
  @Output() useGeneralLocation: EventEmitter<void> = new EventEmitter<void>();
  @Output() clearSelectedLocation: EventEmitter<void> = new EventEmitter<void>();
  @Output() saveSection: EventEmitter<void> = new EventEmitter<void>();

  getDefinedLocationCount(): number {
    return this.attractionLocationOptions.filter((option: AttractionLocationOption) => this.hasLocationPoint(option.key)).length;
  }

  hasLocationPoint(locationKey: AttractionLocationKey): boolean {
    return this.getLocationPoint(locationKey) !== null;
  }

  getLocationCoordinatesLabel(locationKey: AttractionLocationKey): string {
    const point: AttractionLocationPoint | null = this.getLocationPoint(locationKey);

    if (!point || point.latitude === null || point.longitude === null || point.latitude === undefined || point.longitude === undefined) {
      return '—';
    }

    return `${point.latitude.toFixed(6)}, ${point.longitude.toFixed(6)}`;
  }

  getSelectedLocationLabelKey(): string {
    return this.attractionLocationOptions.find((option: AttractionLocationOption) => option.key === this.selectedLocationKey)?.labelKey
      ?? this.attractionLocationOptions[0]?.labelKey
      ?? 'admin.parks.items.locationFields.entrance';
  }

  private getLocationPoint(locationKey: AttractionLocationKey): AttractionLocationPoint | null {
    const group: FormGroup | null = this.formGroup.get(locationKey) as FormGroup | null;

    if (!group) {
      return null;
    }

    const latitude: number | null = this.toNullableNumber(group.get('latitude')?.value);
    const longitude: number | null = this.toNullableNumber(group.get('longitude')?.value);

    if (latitude === null || longitude === null) {
      return null;
    }

    return {
      latitude,
      longitude
    };
  }

  private toNullableNumber(value: unknown): number | null {
    if (value === null || value === undefined || value === '') {
      return null;
    }

    const parsed: number = Number(value);
    return Number.isFinite(parsed) ? parsed : null;
  }
}
