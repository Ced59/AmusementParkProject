import { Component, EventEmitter, Input, Output } from '@angular/core';
import { AbstractControl, FormGroup } from '@angular/forms';
import { MapMarker } from '../../../../../../../models/map/map-marker';

@Component({
    selector: 'app-admin-park-location-tab',
    templateUrl: './admin-park-location-tab.component.html',
    styleUrls: ['./admin-park-location-tab.component.scss'],
    standalone: false
})
export class AdminParkLocationTabComponent {
  @Input({ required: true }) form!: FormGroup;
  @Input() mapCenter: [number, number] = [48.8566, 2.3522];
  @Input() mapZoom: number = 16;
  @Input() mapMarkers: MapMarker[] = [];
  @Input() isSaving: boolean = false;

  @Output() mapPositionChange: EventEmitter<{ lat: number; lng: number }> = new EventEmitter<{ lat: number; lng: number }>();
  @Output() saveSection: EventEmitter<void> = new EventEmitter<void>();

  get latitudeControl(): AbstractControl | null {
    return this.form.get('latitude');
  }

  get longitudeControl(): AbstractControl | null {
    return this.form.get('longitude');
  }
}
