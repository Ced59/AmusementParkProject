import { Component, EventEmitter, Input, Output } from '@angular/core';
import { AbstractControl, FormGroup, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { MapMarker } from '../../../../../../../models/map/map-marker';
import { LeafletMapComponent } from '../../../../../../shared/leaflet-map/leaflet-map.component';
import { Bind } from 'primeng/bind';
import { InputText } from 'primeng/inputtext';
import { ButtonDirective } from 'primeng/button';
import { TranslateModule } from '@ngx-translate/core';

@Component({
    selector: 'app-admin-park-location-tab',
    templateUrl: './admin-park-location-tab.component.html',
    styleUrls: ['./admin-park-location-tab.component.scss'],
    imports: [FormsModule, ReactiveFormsModule, LeafletMapComponent, Bind, InputText, ButtonDirective, TranslateModule]
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
