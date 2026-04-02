import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormGroup } from '@angular/forms';
import { MapMarker } from '../../../../../../../models/map/map-marker';
import { ParkItemCategory } from '../../../../../../../models/parks/park-item-category';
import { ParkItemType } from '../../../../../../../models/parks/park-item-type';

interface Option<T> {
  labelKey: string;
  value: T;
}

@Component({
    selector: 'app-admin-park-item-general-tab',
    templateUrl: './admin-park-item-general-tab.component.html',
    styleUrls: ['./admin-park-item-general-tab.component.scss'],
    standalone: false
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
