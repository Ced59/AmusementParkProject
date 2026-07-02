import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { FormGroup, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { MapMarker } from '@app/models/map/map-marker';
import { ParkItemCategory } from '@app/models/parks/park-item-category';
import { EntitySelectOption } from '@app/models/shared/entity-select-option';
import { ParkItemType } from '@app/models/parks/park-item-type';
import { InputText } from '@shared/ui/primitives/inputtext';
import { Select } from '@shared/ui/primitives/select';
import { Card } from '@shared/ui/primitives/card';
import { LeafletMapComponent } from '@shared/components/leaflet-map/leaflet-map.component';
import { ButtonDirective } from '@shared/ui/primitives/button';
import { LocalizedRichTextEditorComponent } from '@shared/components/localized-rich-text-editor/localized-rich-text-editor.component';
import { ToggleSwitch } from '@shared/ui/primitives/toggleswitch';
import { TranslateModule } from '@ngx-translate/core';

interface Option<T> {
  labelKey: string;
  value: T;
}

@Component({
    selector: 'app-admin-park-item-general-tab',
    templateUrl: './admin-park-item-general-tab.component.html',
    styleUrls: ['./admin-park-item-general-tab.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [FormsModule, ReactiveFormsModule, InputText, Select, Card, LeafletMapComponent, ButtonDirective, LocalizedRichTextEditorComponent, ToggleSwitch, TranslateModule]
})
export class AdminParkItemGeneralTabComponent {
  @Input({ required: true }) form!: FormGroup;
  @Input() categoryOptions: Option<ParkItemCategory>[] = [];
  @Input() filteredTypeOptions: Option<ParkItemType>[] = [];
  @Input() parkOptions: EntitySelectOption[] = [];
  @Input() parkOptionsLoading: boolean = false;
  @Input() zones: { id: string; label: string }[] = [];
  @Input() generalMapCenter: [number, number] = [48.8566, 2.3522];
  @Input() generalMapZoom: number = 18;
  @Input() generalMapMarkers: MapMarker[] = [];
  @Input() canUseParkLocation: boolean = false;
  @Input() isSaving: boolean = false;

  @Output() generalMapPositionChange: EventEmitter<{ lat: number; lng: number }> = new EventEmitter<{ lat: number; lng: number }>();
  @Output() resetGeneralLocationToPark: EventEmitter<void> = new EventEmitter<void>();
  @Output() parkOptionsRequested: EventEmitter<void> = new EventEmitter<void>();
  @Output() parkSelectionChange: EventEmitter<string> = new EventEmitter<string>();
  @Output() saveSection: EventEmitter<void> = new EventEmitter<void>();

  onParkSelectionChanged(value: unknown): void {
    if (typeof value !== 'string') {
      return;
    }

    this.parkSelectionChange.emit(value);
  }
}
