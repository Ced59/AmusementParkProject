import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormGroup, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { MapMarker } from '@app/models/map/map-marker';
import { ParkItemCategory } from '@app/models/parks/park-item-category';
import { ParkItemType } from '@app/models/parks/park-item-type';
import { Bind } from 'primeng/bind';
import { InputText } from 'primeng/inputtext';
import { Select } from 'primeng/select';
import { Card } from 'primeng/card';
import { LeafletMapComponent } from '@app/components/shared/leaflet-map/leaflet-map.component';
import { ButtonDirective } from 'primeng/button';
import { LocalizedRichTextEditorComponent } from '@app/components/shared/localized-rich-text-editor/localized-rich-text-editor.component';
import { ToggleSwitch } from 'primeng/toggleswitch';
import { TranslateModule } from '@ngx-translate/core';

interface Option<T> {
  labelKey: string;
  value: T;
}

@Component({
    selector: 'app-admin-park-item-general-tab',
    templateUrl: './admin-park-item-general-tab.component.html',
    styleUrls: ['./admin-park-item-general-tab.component.scss'],
    imports: [FormsModule, ReactiveFormsModule, Bind, InputText, Select, Card, LeafletMapComponent, ButtonDirective, LocalizedRichTextEditorComponent, ToggleSwitch, TranslateModule]
})
export class AdminParkItemGeneralTabComponent {
  @Input({ required: true }) form!: FormGroup;
  @Input() categoryOptions: Option<ParkItemCategory>[] = [];
  @Input() filteredTypeOptions: Option<ParkItemType>[] = [];
  @Input() zones: { id: string; label: string }[] = [];
  @Input() generalMapCenter: [number, number] = [48.8566, 2.3522];
  @Input() generalMapZoom: number = 18;
  @Input() generalMapMarkers: MapMarker[] = [];
  @Input() canUseParkLocation: boolean = false;
  @Input() isSaving: boolean = false;

  @Output() generalMapPositionChange: EventEmitter<{ lat: number; lng: number }> = new EventEmitter<{ lat: number; lng: number }>();
  @Output() resetGeneralLocationToPark: EventEmitter<void> = new EventEmitter<void>();
  @Output() saveSection: EventEmitter<void> = new EventEmitter<void>();
}
