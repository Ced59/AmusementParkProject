import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { AbstractControl, FormGroup, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { MapMarker } from '@app/models/map/map-marker';
import { LeafletMapComponent } from '@shared/components/leaflet-map/leaflet-map.component';
import { InputText } from '@shared/primeless/inputtext';
import { ButtonDirective } from '@shared/primeless/button';
import { TranslateModule } from '@ngx-translate/core';

@Component({
    selector: 'app-admin-park-location-tab',
    templateUrl: './admin-park-location-tab.component.html',
    styleUrls: ['./admin-park-location-tab.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [FormsModule, ReactiveFormsModule, LeafletMapComponent, InputText, ButtonDirective, TranslateModule]
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
