import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormGroup, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { PaginatorState } from 'primeng/paginator';

import { Bind } from 'primeng/bind';
import { Card } from 'primeng/card';
import { Ripple } from 'primeng/ripple';
import { Tab, TabList, TabPanel, TabPanels, Tabs } from 'primeng/tabs';
import { EditorSaveToolbarComponent } from '../../../../shared/editor-save-toolbar/editor-save-toolbar.component';
import { EntitySelectOption } from '@app/models/shared/entity-select-option';
import { AttractionAccessConditionType } from '@app/models/parks/attraction-access-condition-type';
import { AttractionAccessConditionUnit } from '@app/models/parks/attraction-access-condition-unit';
import { AttractionWaterExposureLevel } from '@app/models/parks/attraction-water-exposure-level';
import { OwnedImageItem } from '@shared/models/images/owned-image-item.model';
import { MapMarker } from '@app/models/map/map-marker';
import {
  AdminParkItemCategoryOption,
  AdminParkItemTypeOption,
  AttractionLocationKey,
  AttractionLocationOption,
  AdminParkItemPhotoCategoryOption
} from '@features/admin/park-items/models/admin-park-item-edit.model';
import { AdminParkItemGeneralTabComponent } from './tabs/admin-park-item-general-tab/admin-park-item-general-tab.component';
import { AdminParkItemDetailsTabComponent } from './tabs/admin-park-item-details-tab/admin-park-item-details-tab.component';
import { AdminParkItemAccessConditionsTabComponent } from './tabs/admin-park-item-access-conditions-tab/admin-park-item-access-conditions-tab.component';
import { AdminParkItemLocationsTabComponent } from './tabs/admin-park-item-locations-tab/admin-park-item-locations-tab.component';
import { AdminParkItemPhotosTabComponent } from './tabs/admin-park-item-photos-tab/admin-park-item-photos-tab.component';

@Component({
  selector: 'app-admin-park-item-edit-form',
  templateUrl: './admin-park-item-edit-form.component.html',
  styleUrls: ['./admin-park-item-edit.component.scss'],
  imports: [
    Bind,
    Card,
    EditorSaveToolbarComponent,
    FormsModule,
    ReactiveFormsModule,
    Tabs,
    TabList,
    Ripple,
    Tab,
    TabPanels,
    TabPanel,
    AdminParkItemGeneralTabComponent,
    AdminParkItemDetailsTabComponent,
    AdminParkItemAccessConditionsTabComponent,
    AdminParkItemLocationsTabComponent,
    AdminParkItemPhotosTabComponent,
    TranslateModule
  ]
})
export class AdminParkItemEditFormComponent {
  @Input({ required: true }) form!: FormGroup;
  @Input() activeTabIndex: number = 0;
  @Input() isEditMode: boolean = false;
  @Input() isAttractionCategory: boolean = true;
  @Input() currentLang: string = 'en';
  @Input() statusLabel: string = '';
  @Input() isDirty: boolean = false;
  @Input() isSaving: boolean = false;
  @Input() categoryOptions: AdminParkItemCategoryOption[] = [];
  @Input() filteredTypeOptions: AdminParkItemTypeOption[] = [];
  @Input() parkOptions: EntitySelectOption[] = [];
  @Input() parkOptionsLoading: boolean = false;
  @Input() zones: Array<{ id: string; label: string }> = [];
  @Input() manufacturerOptions: EntitySelectOption[] = [];
  @Input() manufacturersLoading: boolean = false;
  @Input() manufacturerAddLink: unknown[] | string | null = null;
  @Input() manufacturerAddQueryParams: Record<string, string | number | boolean | null | undefined> | null = null;
  @Input() waterExposureLevelOptions: Array<{ labelKey: string; value: AttractionWaterExposureLevel }> = [];
  @Input() accessConditionPresetOptions: Array<{ labelKey: string; value: AttractionAccessConditionType }> = [];
  @Input() accessConditionUnitOptions: Array<{ labelKey: string; value: AttractionAccessConditionUnit }> = [];
  @Input() selectedAccessConditionPreset: AttractionAccessConditionType = 'MinHeight';
  @Input() attractionLocationOptions: AttractionLocationOption[] = [];
  @Input() selectedLocationKey: AttractionLocationKey = 'entrance';
  @Input() generalMapCenter: [number, number] = [48.8566, 2.3522];
  @Input() generalMapZoom: number = 18;
  @Input() generalMapMarkers: MapMarker[] = [];
  @Input() canUseParkLocation: boolean = false;
  @Input() locationMapCenter: [number, number] = [48.8566, 2.3522];
  @Input() locationMapZoom: number = 19;
  @Input() locationMapMarkers: MapMarker[] = [];
  @Input() currentPhoto: OwnedImageItem | null = null;
  @Input() allowMultiplePhotoUpload: boolean = true;
  @Input() selectedPhotoCount: number = 0;
  @Input() newPhotoDescription: string = '';
  @Input() selectedPhotoCategorySlug: string = 'park-item-gallery';
  @Input() photoCategoryOptions: AdminParkItemPhotoCategoryOption[] = [];
  @Input() photosUploading: boolean = false;
  @Input() photosLoading: boolean = false;
  @Input() attractionPhotos: OwnedImageItem[] = [];
  @Input() pagedPhotos: OwnedImageItem[] = [];
  @Input() photosPageSize: number = 8;

  @Output() submitForm: EventEmitter<void> = new EventEmitter<void>();
  @Output() back: EventEmitter<void> = new EventEmitter<void>();
  @Output() saveAll: EventEmitter<void> = new EventEmitter<void>();
  @Output() saveAndClose: EventEmitter<void> = new EventEmitter<void>();
  @Output() tabChanged: EventEmitter<number | string | undefined> = new EventEmitter<number | string | undefined>();
  @Output() generalMapPositionChange: EventEmitter<{ lat: number; lng: number }> = new EventEmitter<{ lat: number; lng: number }>();
  @Output() resetGeneralLocationToPark: EventEmitter<void> = new EventEmitter<void>();
  @Output() parkSelectionChange: EventEmitter<string> = new EventEmitter<string>();
  @Output() saveSection: EventEmitter<void> = new EventEmitter<void>();
  @Output() selectedAccessConditionPresetChange: EventEmitter<AttractionAccessConditionType> = new EventEmitter<AttractionAccessConditionType>();
  @Output() addAccessCondition: EventEmitter<AttractionAccessConditionType> = new EventEmitter<AttractionAccessConditionType>();
  @Output() removeAccessCondition: EventEmitter<number> = new EventEmitter<number>();
  @Output() moveAccessConditionUp: EventEmitter<number> = new EventEmitter<number>();
  @Output() moveAccessConditionDown: EventEmitter<number> = new EventEmitter<number>();
  @Output() accessConditionTypeChanged: EventEmitter<number> = new EventEmitter<number>();
  @Output() selectedLocationKeyChange: EventEmitter<AttractionLocationKey> = new EventEmitter<AttractionLocationKey>();
  @Output() specificLocationMapPositionChange: EventEmitter<{ lat: number; lng: number }> = new EventEmitter<{ lat: number; lng: number }>();
  @Output() clearLocationPoint: EventEmitter<AttractionLocationKey> = new EventEmitter<AttractionLocationKey>();
  @Output() useGeneralLocation: EventEmitter<void> = new EventEmitter<void>();
  @Output() clearSelectedLocation: EventEmitter<void> = new EventEmitter<void>();
  @Output() photoFileSelected: EventEmitter<Event> = new EventEmitter<Event>();
  @Output() newPhotoDescriptionChange: EventEmitter<string> = new EventEmitter<string>();
  @Output() selectedPhotoCategorySlugChange: EventEmitter<string> = new EventEmitter<string>();
  @Output() uploadPhoto: EventEmitter<void> = new EventEmitter<void>();
  @Output() setCurrentPhoto: EventEmitter<OwnedImageItem> = new EventEmitter<OwnedImageItem>();
  @Output() deletePhoto: EventEmitter<OwnedImageItem> = new EventEmitter<OwnedImageItem>();
  @Output() photosPageChange: EventEmitter<PaginatorState> = new EventEmitter<PaginatorState>();

  get attractionDetailsGroup(): FormGroup {
    return this.form.get('attractionDetails') as FormGroup;
  }

  get attractionLocationsGroup(): FormGroup {
    return this.form.get('attractionLocations') as FormGroup;
  }
}
