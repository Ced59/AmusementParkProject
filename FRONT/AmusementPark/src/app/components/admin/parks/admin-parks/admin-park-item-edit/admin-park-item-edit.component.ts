import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { FormArray, FormBuilder, FormGroup, Validators, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateService, TranslateModule } from '@ngx-translate/core';
import { ToastMessageService } from '../../../../../services/messages/toast-message.service';
import { firstValueFrom, Subscription } from 'rxjs';
import { PaginatorState } from 'primeng/paginator';
import { LANGUAGES } from '../../../../../commons/languages';
import { resolveLocalizedValue } from '../../../../../commons/localized-item.utils';
import { ImageCategory } from '../../../../../models/images/image-category';
import { ImageDto } from '../../../../../models/images/image-dto';
import { ImageOwnerType } from '../../../../../models/images/image-owner-type';
import { UploadedImage } from '../../../../../models/images/uploaded-image';
import { MapMarker } from '../../../../../models/map/map-marker';
import { AttractionAccessCondition } from '../../../../../models/parks/attraction-access-condition';
import { AttractionAccessConditionType } from '../../../../../models/parks/attraction-access-condition-type';
import { AttractionAccessConditionUnit } from '../../../../../models/parks/attraction-access-condition-unit';
import { AttractionDetails } from '../../../../../models/parks/attraction-details';
import { AttractionLocationPoint } from '../../../../../models/parks/attraction-location-point';
import { AttractionLocations } from '../../../../../models/parks/attraction-locations';
import { AttractionManufacturer } from '../../../../../models/parks/attraction-manufacturer';
import { AttractionWaterExposureLevel } from '../../../../../models/parks/attraction-water-exposure-level';
import { Park } from '../../../../../models/parks/park';
import { ParkItem } from '../../../../../models/parks/park-item';
import { ParkItemCategory } from '../../../../../models/parks/park-item-category';
import { ParkItemType } from '../../../../../models/parks/park-item-type';
import { ParkZone } from '../../../../../models/parks/park-zone';
import { EntitySelectOption } from '../../../../../models/shared/entity-select-option';
import { LocalizedItem } from '../../../../../models/shared/localized-item';
import { ApiService } from '../../../../../services/api.service';
import { Bind } from 'primeng/bind';
import { Card } from 'primeng/card';
import { EditorSaveToolbarComponent } from '../../../../shared/editor-save-toolbar/editor-save-toolbar.component';
import { Tabs, TabList, Tab, TabPanels, TabPanel } from 'primeng/tabs';
import { Ripple } from 'primeng/ripple';
import { AdminParkItemGeneralTabComponent } from './tabs/admin-park-item-general-tab/admin-park-item-general-tab.component';
import { AdminParkItemDetailsTabComponent } from './tabs/admin-park-item-details-tab/admin-park-item-details-tab.component';
import { AdminParkItemAccessConditionsTabComponent } from './tabs/admin-park-item-access-conditions-tab/admin-park-item-access-conditions-tab.component';
import { AdminParkItemLocationsTabComponent } from './tabs/admin-park-item-locations-tab/admin-park-item-locations-tab.component';
import { AdminParkItemPhotosTabComponent } from './tabs/admin-park-item-photos-tab/admin-park-item-photos-tab.component';

interface Option<T> {
  labelKey: string;
  value: T;
}

interface AttractionPhotoItem {
  id: string;
  imageUrl: string;
  description?: string;
  isCurrent: boolean;
  createdAt: string;
}

type AttractionLocationKey = 'entrance' | 'exit' | 'fastPassEntrance' | 'reducedMobilityEntrance';

interface AttractionLocationOption {
  key: AttractionLocationKey;
  labelKey: string;
}

type SaveMode = 'stay' | 'back';
type SaveScope = 'section' | 'all';

@Component({
    selector: 'app-admin-park-item-edit',
    templateUrl: './admin-park-item-edit.component.html',
    styleUrls: ['./admin-park-item-edit.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [Bind, Card, EditorSaveToolbarComponent, FormsModule, ReactiveFormsModule, Tabs, TabList, Ripple, Tab, TabPanels, TabPanel, AdminParkItemGeneralTabComponent, AdminParkItemDetailsTabComponent, AdminParkItemAccessConditionsTabComponent, AdminParkItemLocationsTabComponent, AdminParkItemPhotosTabComponent, TranslateModule]
})
export class AdminParkItemEditComponent implements OnInit, OnDestroy {
  form!: FormGroup;
  parkId: string = '';
  itemId: string | null = null;
  currentLang: string = 'en';
  zones: { id: string; label: string }[] = [];
  filteredTypeOptions: Option<ParkItemType>[] = [];
  manufacturerOptions: EntitySelectOption[] = [];
  manufacturersLoading: boolean = false;

  activeTabIndex: number = 0;

  generalMapCenter: [number, number] = [48.8566, 2.3522];
  generalMapZoom: number = 18;
  generalMapMarkers: MapMarker[] = [];

  selectedLocationKey: AttractionLocationKey = 'entrance';
  locationMapCenter: [number, number] = [48.8566, 2.3522];
  locationMapZoom: number = 19;
  locationMapMarkers: MapMarker[] = [];

  selectedPhotoFiles: File[] = [];
  allowMultiplePhotoUpload: boolean = true;
  newPhotoDescription: string = '';
  photosUploading: boolean = false;
  photosLoading: boolean = false;
  attractionPhotos: AttractionPhotoItem[] = [];
  currentPhoto: AttractionPhotoItem | null = null;
  photosPage: number = 0;
  photosPageSize: number = 8;

  selectedAccessConditionPreset: AttractionAccessConditionType = 'MinHeight';

  isSaving: boolean = false;
  hasPendingChanges: boolean = false;
  private lastSavedSnapshot: string = '';
  private isInitializing: boolean = false;
  parkLocationDefault: AttractionLocationPoint | null = null;
  private generalLocationManuallyChanged: boolean = false;
  private isApplyingGeneralLocationProgrammatically: boolean = false;

  private readonly subscriptions: Subscription = new Subscription();

  readonly attractionLocationOptions: AttractionLocationOption[] = [
    { key: 'entrance', labelKey: 'admin.parks.items.locationFields.entrance' },
    { key: 'exit', labelKey: 'admin.parks.items.locationFields.exit' },
    { key: 'fastPassEntrance', labelKey: 'admin.parks.items.locationFields.fastPassEntrance' },
    { key: 'reducedMobilityEntrance', labelKey: 'admin.parks.items.locationFields.reducedMobilityEntrance' }
  ];

  readonly categoryOptions: Option<ParkItemCategory>[] = [
    { labelKey: 'parkExplorer.categories.attraction', value: 'Attraction' },
    { labelKey: 'parkExplorer.categories.restaurant', value: 'Restaurant' },
    { labelKey: 'parkExplorer.categories.hotel', value: 'Hotel' },
    { labelKey: 'parkExplorer.categories.animal', value: 'Animal' },
    { labelKey: 'parkExplorer.categories.show', value: 'Show' },
    { labelKey: 'parkExplorer.categories.shop', value: 'Shop' },
    { labelKey: 'parkExplorer.categories.service', value: 'Service' },
    { labelKey: 'parkExplorer.categories.transport', value: 'Transport' },
    { labelKey: 'parkExplorer.categories.other', value: 'Other' }
  ];

  readonly attractionTypeOptions: Option<ParkItemType>[] = [
    { labelKey: 'parkExplorer.types.attraction', value: 'Attraction' },
    { labelKey: 'parkExplorer.types.rollerCoaster', value: 'RollerCoaster' },
    { labelKey: 'parkExplorer.types.waterRide', value: 'WaterRide' },
    { labelKey: 'parkExplorer.types.flatRide', value: 'FlatRide' },
    { labelKey: 'parkExplorer.types.darkRide', value: 'DarkRide' },
    { labelKey: 'parkExplorer.types.familyRide', value: 'FamilyRide' },
    { labelKey: 'parkExplorer.types.thrillRide', value: 'ThrillRide' },
    { labelKey: 'parkExplorer.types.transportRide', value: 'TransportRide' },
    { labelKey: 'parkExplorer.types.walkThrough', value: 'WalkThrough' },
    { labelKey: 'parkExplorer.types.playground', value: 'Playground' },
    { labelKey: 'parkExplorer.types.interactiveExperience', value: 'InteractiveExperience' },
    { labelKey: 'parkExplorer.types.observationRide', value: 'ObservationRide' },
    { labelKey: 'parkExplorer.types.other', value: 'Other' }
  ];

  readonly nonAttractionTypeOptionsByCategory: Record<Exclude<ParkItemCategory, 'Attraction'>, Option<ParkItemType>[]> = {
    Restaurant: [
      { labelKey: 'parkExplorer.types.restaurant', value: 'Restaurant' },
      { labelKey: 'parkExplorer.types.snack', value: 'Snack' }
    ],
    Hotel: [
      { labelKey: 'parkExplorer.types.hotel', value: 'Hotel' }
    ],
    Animal: [
      { labelKey: 'parkExplorer.types.animalExhibit', value: 'AnimalExhibit' }
    ],
    Show: [
      { labelKey: 'parkExplorer.types.show', value: 'Show' }
    ],
    Shop: [
      { labelKey: 'parkExplorer.types.shop', value: 'Shop' }
    ],
    Service: [
      { labelKey: 'parkExplorer.types.service', value: 'Service' }
    ],
    Transport: [
      { labelKey: 'parkExplorer.types.transport', value: 'Transport' }
    ],
    Other: [
      { labelKey: 'parkExplorer.types.other', value: 'Other' }
    ]
  };

  readonly accessConditionPresetOptions: Option<AttractionAccessConditionType>[] = [
    { labelKey: 'admin.parks.items.accessConditionTypes.minHeight', value: 'MinHeight' },
    { labelKey: 'admin.parks.items.accessConditionTypes.minHeightAccompanied', value: 'MinHeightAccompanied' },
    { labelKey: 'admin.parks.items.accessConditionTypes.maxHeight', value: 'MaxHeight' },
    { labelKey: 'admin.parks.items.accessConditionTypes.minAge', value: 'MinAge' },
    { labelKey: 'admin.parks.items.accessConditionTypes.minAgeAccompanied', value: 'MinAgeAccompanied' },
    { labelKey: 'admin.parks.items.accessConditionTypes.pregnancyRestriction', value: 'PregnancyRestriction' },
    { labelKey: 'admin.parks.items.accessConditionTypes.heartRestriction', value: 'HeartRestriction' },
    { labelKey: 'admin.parks.items.accessConditionTypes.backNeckRestriction', value: 'BackNeckRestriction' },
    { labelKey: 'admin.parks.items.accessConditionTypes.wheelchairTransferRequired', value: 'WheelchairTransferRequired' },
    { labelKey: 'admin.parks.items.accessConditionTypes.accessPassRequired', value: 'AccessPassRequired' },
    { labelKey: 'admin.parks.items.accessConditionTypes.custom', value: 'Custom' }
  ];

  readonly waterExposureLevelOptions: Option<AttractionWaterExposureLevel>[] = [
    { labelKey: 'admin.parks.items.waterExposureLevels.none', value: 'None' },
    { labelKey: 'admin.parks.items.waterExposureLevels.splash', value: 'Splash' },
    { labelKey: 'admin.parks.items.waterExposureLevels.moderate', value: 'Moderate' },
    { labelKey: 'admin.parks.items.waterExposureLevels.soaking', value: 'Soaking' },
    { labelKey: 'admin.parks.items.waterExposureLevels.extremeSoaking', value: 'ExtremeSoaking' }
  ];

  readonly accessConditionUnitOptions: Option<AttractionAccessConditionUnit>[] = [
    { labelKey: 'admin.parks.items.accessConditionUnits.centimeter', value: 'Centimeter' },
    { labelKey: 'admin.parks.items.accessConditionUnits.year', value: 'Year' }
  ];

  readonly accessConditionDefaultLabels: Record<AttractionAccessConditionType, Record<string, string>> = {
    MinHeight: {
      en: 'Minimum height',
      fr: 'Taille minimale',
      es: 'Altura mínima',
      de: 'Mindestgröße',
      it: 'Altezza minima',
      pl: 'Minimalny wzrost',
      nl: 'Minimumlengte',
      pt: 'Altura mínima'
    },
    MinHeightAccompanied: {
      en: 'Minimum height with accompaniment',
      fr: 'Taille minimale accompagné',
      es: 'Altura mínima acompañado',
      de: 'Mindestgröße in Begleitung',
      it: 'Altezza minima con accompagnatore',
      pl: 'Minimalny wzrost z opiekunem',
      nl: 'Minimumlengte met begeleiding',
      pt: 'Altura mínima acompanhado'
    },
    MaxHeight: {
      en: 'Maximum height',
      fr: 'Taille maximale',
      es: 'Altura máxima',
      de: 'Maximalgröße',
      it: 'Altezza massima',
      pl: 'Maksymalny wzrost',
      nl: 'Maximumlengte',
      pt: 'Altura máxima'
    },
    MinAge: {
      en: 'Minimum age',
      fr: 'Âge minimum',
      es: 'Edad mínima',
      de: 'Mindestalter',
      it: 'Età minima',
      pl: 'Minimalny wiek',
      nl: 'Minimumleeftijd',
      pt: 'Idade mínima'
    },
    MinAgeAccompanied: {
      en: 'Minimum age with accompaniment',
      fr: 'Âge minimum accompagné',
      es: 'Edad mínima acompañado',
      de: 'Mindestalter in Begleitung',
      it: 'Età minima con accompagnatore',
      pl: 'Minimalny wiek z opiekunem',
      nl: 'Minimumleeftijd met begeleiding',
      pt: 'Idade mínima acompanhado'
    },
    PregnancyRestriction: {
      en: 'Pregnancy restriction',
      fr: 'Restriction grossesse',
      es: 'Restricción embarazo',
      de: 'Einschränkung Schwangerschaft',
      it: 'Restrizione gravidanza',
      pl: 'Ograniczenie ciąży',
      nl: 'Zwangerschapsbeperking',
      pt: 'Restrição gravidez'
    },
    HeartRestriction: {
      en: 'Cardiac restriction',
      fr: 'Restriction cardiaque',
      es: 'Restricción cardíaca',
      de: 'Herzbeschränkung',
      it: 'Restrizione cardiaca',
      pl: 'Ograniczenie kardiologiczne',
      nl: 'Hartbeperking',
      pt: 'Restrição cardíaca'
    },
    BackNeckRestriction: {
      en: 'Back or neck restriction',
      fr: 'Restriction dos ou nuque',
      es: 'Restricción espalda o cuello',
      de: 'Einschränkung Rücken oder Nacken',
      it: 'Restrizione schiena o collo',
      pl: 'Ograniczenie pleców lub szyi',
      nl: 'Rug- of nekbeperking',
      pt: 'Restrição costas ou pescoço'
    },
    WheelchairTransferRequired: {
      en: 'Wheelchair transfer required',
      fr: 'Transfert fauteuil requis',
      es: 'Transferencia desde silla requerida',
      de: 'Rollstuhltransfer erforderlich',
      it: 'Trasferimento da sedia richiesto',
      pl: 'Wymagany transfer z wózka',
      nl: 'Transfer uit rolstoel vereist',
      pt: 'Transferência da cadeira obrigatória'
    },
    AccessPassRequired: {
      en: 'Access pass required',
      fr: 'Access pass requis',
      es: 'Access pass requerido',
      de: 'Access Pass erforderlich',
      it: 'Access pass richiesto',
      pl: 'Wymagany access pass',
      nl: 'Access pass vereist',
      pt: 'Access pass obrigatório'
    },
    Custom: {
      en: '',
      fr: '',
      es: '',
      de: '',
      it: '',
      pl: '',
      nl: '',
      pt: ''
    }
  };

  get isEditMode(): boolean {
    return !!this.itemId;
  }

  get isAttractionCategory(): boolean {
    return this.form?.get('category')?.value === 'Attraction';
  }

  get pagedPhotos(): AttractionPhotoItem[] {
    const start: number = this.photosPage * this.photosPageSize;
    return this.attractionPhotos.slice(start, start + this.photosPageSize);
  }

  get attractionDetailsGroup(): FormGroup {
    return this.form.get('attractionDetails') as FormGroup;
  }

  get attractionLocationsGroup(): FormGroup {
    return this.form.get('attractionLocations') as FormGroup;
  }

  get accessConditions(): FormArray {
    return this.form.get(['attractionDetails', 'accessConditions']) as FormArray;
  }

  get manufacturerCreateLinkQueryParams(): Record<string, string | number> {
    return {
      returnUrl: this.router.url.split('?')[0],
      returnTab: this.activeTabIndex
    };
  }

  get isFormDirty(): boolean {
    return this.hasPendingChanges;
  }

  get selectedPhotoCount(): number {
    return this.selectedPhotoFiles.length;
  }

  constructor(
    private readonly fb: FormBuilder,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly apiService: ApiService,
    private readonly translateService: TranslateService,
    private readonly toastMessageService: ToastMessageService,
    private readonly cdr: ChangeDetectorRef
  ) {
  }

  ngOnInit(): void {
    this.currentLang = this.route.root.firstChild?.snapshot.params['lang'] ?? 'en';
    this.parkId = this.route.snapshot.paramMap.get('idPark') ?? '';
    this.itemId = this.route.snapshot.paramMap.get('idItem');
    this.isInitializing = true;

    const requestedTabIndex: number = Number(this.route.snapshot.queryParamMap.get('returnTab') ?? this.route.snapshot.queryParamMap.get('tab') ?? 0);
    this.activeTabIndex = Number.isFinite(requestedTabIndex) && requestedTabIndex >= 0
      ? requestedTabIndex
      : 0;

    this.form = this.fb.group({
      parkId: [this.parkId, Validators.required],
      zoneId: [null],
      name: ['', Validators.required],
      category: ['Attraction', Validators.required],
      type: ['Attraction', Validators.required],
      subtype: [''],
      latitude: [48.8566, Validators.required],
      longitude: [2.3522, Validators.required],
      descriptions: [[]],
      attractionDetails: this.fb.group({
        manufacturerId: [null],
        model: [''],
        openingDate: [''],
        closingDate: [''],
        durationInSeconds: [null],
        capacityPerHour: [null],
        heightInMeters: [null],
        lengthInMeters: [null],
        speedInKmH: [null],
        dropInMeters: [null],
        inversionCount: [null],
        trainCount: [null],
        carsPerTrain: [null],
        ridersPerVehicle: [null],
        hasSingleRider: [false],
        hasFastPass: [false],
        isAccessibleForReducedMobility: [false],
        isIndoor: [false],
        waterExposureLevel: [null],
        accessConditions: this.fb.array([])
      }),
      attractionLocations: this.fb.group({
        entrance: this.createLocationGroup(),
        exit: this.createLocationGroup(),
        fastPassEntrance: this.createLocationGroup(),
        reducedMobilityEntrance: this.createLocationGroup()
      }),
      isVisible: [true]
    });

    this.applyCategorySelection(this.form.get('category')?.value as ParkItemCategory);
    this.setupFormSync();
    this.loadManufacturers();

    this.apiService.getParkZonesByParkId(this.parkId).subscribe((zones: ParkZone[]) => {
      this.zones = zones
        .filter((zone: ParkZone) => !!zone.id)
        .map((zone: ParkZone) => ({
          id: zone.id!,
          label: resolveLocalizedValue(zone.names, this.currentLang) ?? zone.name ?? zone.id!
        }));
      this.cdr.markForCheck();
    });

    this.loadParkLocationDefault();

    if (this.itemId) {
      this.apiService.getParkItemById(this.itemId).subscribe((item: ParkItem) => {
        this.form.patchValue({
          parkId: item.parkId,
          zoneId: item.zoneId ?? null,
          name: item.name,
          category: item.category,
          type: item.type,
          subtype: item.subtype ?? '',
          latitude: item.latitude,
          longitude: item.longitude,
          descriptions: item.descriptions ?? [],
          isVisible: item.isVisible ?? true
        }, { emitEvent: false });

        this.patchAttractionDetails(item.attractionDetails ?? null);
        this.patchAttractionLocations(item.attractionLocations ?? null);
        this.applyManufacturerSelectionOverride();
        this.applyCategorySelection(item.category);
        this.generalLocationManuallyChanged = true;
        this.refreshAllMapStates();
        this.finalizeLoadedFormState();
        this.applyManufacturerSelectionOverride();

        if (item.category === 'Attraction') {
          this.loadAttractionPhotos();
        }
        this.cdr.markForCheck();
      });

      return;
    }

    this.refreshAllMapStates();
    this.finalizeLoadedFormState();
    this.applyManufacturerSelectionOverride();
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  onSubmit(): void {
    this.saveAll();
  }

  saveSection(): void {
    this.persistItem('stay', 'section');
  }

  saveAll(): void {
    this.persistItem('stay', 'all');
  }

  saveAndClose(): void {
    this.persistItem('back', 'all');
  }

  onGeneralMapPositionChange(position: { lat: number; lng: number }): void {
    this.generalLocationManuallyChanged = true;
    this.form.patchValue({
      latitude: position.lat,
      longitude: position.lng
    });
  }

  resetGeneralLocationToPark(): void {
    const parkLocation: { latitude: number; longitude: number } | null = this.getResolvedParkLocationDefault();

    if (!parkLocation) {
      return;
    }

    this.generalLocationManuallyChanged = false;
    this.applyGeneralLocation(parkLocation.latitude, parkLocation.longitude);
  }

  selectLocationEditor(locationKey: AttractionLocationKey): void {
    this.selectedLocationKey = locationKey;
    this.updateLocationMapState();

    window.setTimeout((): void => {
      this.updateLocationMapState();
    }, 80);
  }

  onTabChange(index: number | string | undefined): void {
    const normalizedIndex: number =
      typeof index === 'string'
        ? Number(index)
        : typeof index === 'number'
          ? index
          : 0;

    this.activeTabIndex = Number.isFinite(normalizedIndex) ? normalizedIndex : 0;

    if (this.activeTabIndex === 0 || this.activeTabIndex === 3) {
      window.setTimeout((): void => {
        this.refreshAllMapStates();
      }, 80);
    }
  }

  onSpecificLocationMapPositionChange(position: { lat: number; lng: number }): void {
    const group: FormGroup = this.getLocationGroup(this.selectedLocationKey);
    group.patchValue({
      latitude: position.lat,
      longitude: position.lng
    });
  }

  clearSelectedLocationPoint(): void {
    this.clearLocationPoint(this.selectedLocationKey);
  }

  clearLocationPoint(locationKey: AttractionLocationKey): void {
    const group: FormGroup = this.getLocationGroup(locationKey);
    group.patchValue({
      latitude: null,
      longitude: null
    });
  }

  useGeneralLocationForSelectedPoint(): void {
    const latitude: number = this.toRequiredNumber(this.form.get('latitude')?.value);
    const longitude: number = this.toRequiredNumber(this.form.get('longitude')?.value);

    this.getLocationGroup(this.selectedLocationKey).patchValue({
      latitude,
      longitude
    });
  }

  hasLocationPoint(locationKey: AttractionLocationKey): boolean {
    return this.getLocationPoint(locationKey) !== null;
  }

  getLocationCoordinatesLabel(locationKey: AttractionLocationKey): string {
    const point: AttractionLocationPoint | null = this.getLocationPoint(locationKey);

    if (!point || point.latitude === null || point.longitude === null || point.latitude === undefined || point.longitude === undefined) {
      return this.translateService.instant('admin.parks.items.locationNotDefined');
    }

    return `${point.latitude.toFixed(6)}, ${point.longitude.toFixed(6)}`;
  }

  getDefinedLocationCount(): number {
    return this.attractionLocationOptions.filter((option: AttractionLocationOption) => this.hasLocationPoint(option.key)).length;
  }

  getLocationLabelKey(locationKey: AttractionLocationKey): string {
    return this.attractionLocationOptions.find((option: AttractionLocationOption) => option.key === locationKey)?.labelKey
      ?? 'admin.parks.items.locationFields.entrance';
  }

  getSelectedLocationLabelKey(): string {
    return this.getLocationLabelKey(this.selectedLocationKey);
  }

  addAccessCondition(type: AttractionAccessConditionType = this.selectedAccessConditionPreset): void {
    const condition: AttractionAccessCondition = this.buildDefaultAccessCondition(type);
    this.accessConditions.push(this.createAccessConditionGroup(condition));
    this.syncAccessConditionDisplayOrders();
    this.activeTabIndex = this.isAttractionCategory ? 2 : 0;
  }

  removeAccessCondition(index: number): void {
    this.accessConditions.removeAt(index);
    this.syncAccessConditionDisplayOrders();
  }

  moveAccessConditionUp(index: number): void {
    if (index <= 0) {
      return;
    }

    const control = this.accessConditions.at(index);
    this.accessConditions.removeAt(index);
    this.accessConditions.insert(index - 1, control);
    this.syncAccessConditionDisplayOrders();
  }

  moveAccessConditionDown(index: number): void {
    if (index >= this.accessConditions.length - 1) {
      return;
    }

    const control = this.accessConditions.at(index);
    this.accessConditions.removeAt(index);
    this.accessConditions.insert(index + 1, control);
    this.syncAccessConditionDisplayOrders();
  }

  onAccessConditionTypeChanged(index: number): void {
    const group: FormGroup = this.getAccessConditionGroup(index);
    const type: AttractionAccessConditionType = group.get('type')?.value as AttractionAccessConditionType;
    const defaultCondition: AttractionAccessCondition = this.buildDefaultAccessCondition(type);
    const currentLabel: LocalizedItem<string>[] | null = this.toLocalizedItems(group.get('label')?.value);
    const shouldReplaceLabel: boolean = !this.hasLocalizedValues(currentLabel);

    group.patchValue({
      isCustom: type === 'Custom',
      unit: defaultCondition.unit ?? null,
      requiresAccompaniment: defaultCondition.requiresAccompaniment ?? false,
      minimumCompanionAge: defaultCondition.minimumCompanionAge ?? null
    });

    if (shouldReplaceLabel) {
      group.get('label')?.setValue(defaultCondition.label ?? []);
    }
  }

  getAccessConditionGroup(index: number): FormGroup {
    return this.accessConditions.at(index) as FormGroup;
  }

  getAccessConditionTitle(index: number): string {
    const group: FormGroup = this.getAccessConditionGroup(index);
    const label: LocalizedItem<string>[] | null = this.toLocalizedItems(group.get('label')?.value);
    const resolvedLabel: string | undefined = resolveLocalizedValue(label ?? [], this.currentLang);

    if (resolvedLabel && resolvedLabel.trim().length > 0) {
      return resolvedLabel;
    }

    const type: AttractionAccessConditionType = group.get('type')?.value as AttractionAccessConditionType;
    return this.translateService.instant(this.getAccessConditionLabelKey(type));
  }

  onPhotoFileSelected(event: Event): void {
    const input: HTMLInputElement = event.target as HTMLInputElement;

    if (!input.files || input.files.length === 0) {
      this.selectedPhotoFiles = [];
      return;
    }

    this.selectedPhotoFiles = Array.from(input.files);
  }

  async onUploadPhoto(): Promise<void> {
    if (!this.itemId || this.selectedPhotoFiles.length === 0 || !this.isAttractionCategory || this.photosUploading) {
      return;
    }

    this.photosUploading = true;

    const files: File[] = [...this.selectedPhotoFiles];
    const hadNoPhotoInitially: boolean = this.attractionPhotos.length === 0;
    let uploadedCount: number = 0;

    try {
      for (let index: number = 0; index < files.length; index++) {
        const shouldSetCurrent: boolean = hadNoPhotoInitially && index === 0;
        await this.uploadAttractionPhotoAsync(files[index], shouldSetCurrent);
        uploadedCount++;
      }

      this.selectedPhotoFiles = [];
      this.newPhotoDescription = '';
      this.showUploadSuccessMessage(
        this.translateService.instant('admin.parks.items.photos.uploadSuccess', { count: uploadedCount })
      );
    } catch (error: unknown) {
      console.error('Error uploading attraction images', error);
      this.showUploadErrorMessage(
        this.translateService.instant('admin.parks.items.photos.uploadError', { count: uploadedCount })
      );
    } finally {
      this.photosUploading = false;
      this.cdr.markForCheck();
    }
  }

  private async uploadAttractionPhotoAsync(file: File, setAsCurrent: boolean): Promise<void> {
    const uploaded: UploadedImage = await firstValueFrom(
      this.apiService.uploadImage(
        file,
        ImageCategory.ATTRACTION,
        false,
        `${this.form.get('name')?.value || ''}`
      )
    );

    const image: ImageDto = await firstValueFrom(
      this.apiService.linkImage({
        imageId: uploaded.id,
        ownerType: ImageOwnerType.ATTRACTION,
        ownerId: this.itemId as string,
        description: this.newPhotoDescription || undefined,
        setAsCurrent
      })
    );

    this.upsertAttractionPhoto(this.toAttractionPhotoItem(image));
  }

  private upsertAttractionPhoto(item: AttractionPhotoItem): void {
    if (item.isCurrent) {
      this.attractionPhotos = this.attractionPhotos.map((photo: AttractionPhotoItem) => ({
        ...photo,
        isCurrent: photo.id === item.id
      }));
    }

    const existingIndex: number = this.attractionPhotos.findIndex((photo: AttractionPhotoItem) => photo.id === item.id);

    if (existingIndex >= 0) {
      this.attractionPhotos[existingIndex] = item;
    } else {
      this.attractionPhotos.unshift(item);
    }

    this.currentPhoto = this.attractionPhotos.find((photo: AttractionPhotoItem) => photo.isCurrent) ?? item;
    this.cdr.markForCheck();
  }

  onSetCurrentPhoto(photo: AttractionPhotoItem): void {
    if (!this.itemId || photo.isCurrent) {
      return;
    }

    this.apiService.setCurrentImage(photo.id).subscribe({
      next: (image: ImageDto) => {
        const updated: AttractionPhotoItem = this.toAttractionPhotoItem(image);

        this.attractionPhotos = this.attractionPhotos.map((item: AttractionPhotoItem) => ({
          ...item,
          isCurrent: item.id === updated.id
        }));

        this.currentPhoto = updated;
        this.showUploadSuccessMessage(this.translateService.instant('admin.parks.items.photos.currentSetSuccess'));
        this.cdr.markForCheck();
      },
      error: (error: unknown) => {
        console.error('Error setting current attraction image', error);
      }
    });
  }

  onDeletePhoto(photo: AttractionPhotoItem): void {
    if (!confirm(this.translateService.instant('admin.parks.items.photos.deleteConfirm'))) {
      return;
    }

    this.apiService.deleteImage(photo.id).subscribe({
      next: () => {
        this.attractionPhotos = this.attractionPhotos.filter((item: AttractionPhotoItem) => item.id !== photo.id);
        this.currentPhoto = this.attractionPhotos.find((item: AttractionPhotoItem) => item.isCurrent) ?? null;
        this.showUploadSuccessMessage(this.translateService.instant('admin.parks.items.photos.deleteSuccess'));
        this.cdr.markForCheck();
      },
      error: (error: unknown) => {
        console.error('Error deleting attraction image', error);
      }
    });
  }

  onPhotosPageChange(event: PaginatorState): void {
    this.photosPage = event.page ?? 0;
    this.photosPageSize = event.rows ?? this.photosPageSize;
  }

  goBack(): void {
    this.router.navigate(['/', this.currentLang, 'admin', 'parks', 'edit', this.parkId, 'items']);
  }

  getEditorStatusLabel(): string {
    if (this.isSaving) {
      return this.translateService.instant('admin.parks.items.editorStatus.saving');
    }

    if (this.isFormDirty) {
      return this.translateService.instant('admin.parks.items.editorStatus.unsavedChanges');
    }

    return this.translateService.instant('admin.parks.items.editorStatus.upToDate');
  }

  private setupFormSync(): void {
    const categorySubscription: Subscription = this.form.get('category')!.valueChanges.subscribe((categoryValue: unknown) => {
      this.applyCategorySelection(categoryValue as ParkItemCategory);

      if (categoryValue !== 'Attraction') {
        this.activeTabIndex = 0;
      }
    });

    const generalLatitudeSubscription: Subscription = this.form.get('latitude')!.valueChanges.subscribe(() => {
      if (!this.isApplyingGeneralLocationProgrammatically) {
        this.generalLocationManuallyChanged = true;
      }

      this.updateGeneralMapState();
      this.updateLocationMapState();
    });

    const generalLongitudeSubscription: Subscription = this.form.get('longitude')!.valueChanges.subscribe(() => {
      if (!this.isApplyingGeneralLocationProgrammatically) {
        this.generalLocationManuallyChanged = true;
      }

      this.updateGeneralMapState();
      this.updateLocationMapState();
    });

    const locationSubscription: Subscription = this.form.get('attractionLocations')!.valueChanges.subscribe(() => {
      this.updateLocationMapState();
    });

    const formSubscription: Subscription = this.form.valueChanges.subscribe(() => {
      if (this.isInitializing) {
        return;
      }

      this.updatePendingChanges();
    });

    this.subscriptions.add(categorySubscription);
    this.subscriptions.add(generalLatitudeSubscription);
    this.subscriptions.add(generalLongitudeSubscription);
    this.subscriptions.add(locationSubscription);
    this.subscriptions.add(formSubscription);
  }

  private loadParkLocationDefault(): void {
    if (!this.parkId) {
      return;
    }

    this.apiService.getParkById(this.parkId).subscribe({
      next: (park: Park) => {
        this.parkLocationDefault = {
          latitude: park.latitude,
          longitude: park.longitude
        };

        if (!this.generalLocationManuallyChanged) {
          this.applyGeneralLocation(park.latitude, park.longitude);
          this.finalizeLoadedFormState();
          this.cdr.markForCheck();
          return;
        }

        this.updatePendingChanges();
        this.cdr.markForCheck();
      },
      error: (error: unknown) => {
        console.error('Error loading default park location for item editor', error);
      }
    });
  }

  private refreshAllMapStates(): void {
    this.updateGeneralMapState();
    this.updateLocationMapState();
  }

  private getResolvedParkLocationDefault(): { latitude: number; longitude: number } | null {
    const latitude: number | null | undefined = this.parkLocationDefault?.latitude;
    const longitude: number | null | undefined = this.parkLocationDefault?.longitude;

    if (typeof latitude !== 'number' || !Number.isFinite(latitude) || typeof longitude !== 'number' || !Number.isFinite(longitude)) {
      return null;
    }

    return {
      latitude,
      longitude
    };
  }

  private updateGeneralMapState(): void {
    const latitude: number = this.toRequiredNumber(this.form.get('latitude')?.value);
    const longitude: number = this.toRequiredNumber(this.form.get('longitude')?.value);
    const hasValidCoordinates: boolean = Number.isFinite(latitude) && Number.isFinite(longitude) && !(latitude === 0 && longitude === 0);
    const parkLocation: { latitude: number; longitude: number } | null = this.getResolvedParkLocationDefault();

    if (!hasValidCoordinates && parkLocation) {
      this.generalMapCenter = [parkLocation.latitude, parkLocation.longitude];
      this.generalMapMarkers = [
        {
          id: 'general-location-default',
          lat: parkLocation.latitude,
          lng: parkLocation.longitude
        }
      ];
      return;
    }

    this.generalMapCenter = [latitude, longitude];
    this.generalMapMarkers = [
      {
        id: 'general-location',
        lat: latitude,
        lng: longitude
      }
    ];
  }

  private updateLocationMapState(): void {
    const point: AttractionLocationPoint | null = this.getLocationPoint(this.selectedLocationKey);

    if (point && point.latitude !== null && point.longitude !== null && point.latitude !== undefined && point.longitude !== undefined) {
      this.locationMapCenter = [point.latitude, point.longitude];
      this.locationMapMarkers = [
        {
          id: this.selectedLocationKey,
          lat: point.latitude,
          lng: point.longitude
        }
      ];
      return;
    }

    const latitude: number = this.toRequiredNumber(this.form.get('latitude')?.value);
    const longitude: number = this.toRequiredNumber(this.form.get('longitude')?.value);
    const hasGeneralCoordinates: boolean = Number.isFinite(latitude) && Number.isFinite(longitude) && !(latitude === 0 && longitude === 0);
    const parkLocation: { latitude: number; longitude: number } | null = this.getResolvedParkLocationDefault();

    if (!hasGeneralCoordinates && parkLocation) {
      this.locationMapCenter = [parkLocation.latitude, parkLocation.longitude];
      this.locationMapMarkers = [];
      return;
    }

    this.locationMapCenter = [latitude, longitude];
    this.locationMapMarkers = [];
  }


  private persistItem(mode: SaveMode, scope: SaveScope): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.activeTabIndex = 0;
      return;
    }

    if (this.isSaving) {
      return;
    }

    const payload: ParkItem = this.buildPayload();
    this.isSaving = true;

    if (this.itemId) {
      this.apiService.updateParkItem(this.itemId, payload).subscribe({
        next: (updated: ParkItem) => {
          this.isSaving = false;
          this.afterSuccessfulSave(updated, mode, scope);
          this.cdr.markForCheck();
        },
        error: (error: unknown) => {
          console.error('Error updating park item', error);
          this.isSaving = false;
          this.showSaveErrorMessage();
          this.cdr.markForCheck();
        }
      });
      return;
    }

    this.apiService.createParkItem(payload).subscribe({
      next: (created: ParkItem) => {
        this.isSaving = false;
        this.afterSuccessfulSave(created, mode, scope);
      },
      error: (error: unknown) => {
        console.error('Error creating park item', error);
        this.isSaving = false;
        this.showSaveErrorMessage();
      }
    });
  }

  private afterSuccessfulSave(savedItem: ParkItem, mode: SaveMode, scope: SaveScope): void {
    const wasEditMode: boolean = !!this.itemId;

    this.captureCurrentSnapshot();
    this.showSaveSuccessMessage(scope, wasEditMode);

    if (!this.itemId && savedItem.id) {
      this.itemId = savedItem.id;

      if (mode === 'back') {
        this.goBack();
        return;
      }

      this.router.navigate(
        ['/', this.currentLang, 'admin', 'parks', 'edit', this.parkId, 'items', savedItem.id],
        {
          replaceUrl: true,
          queryParams: { tab: this.activeTabIndex }
        }
      );
      return;
    }

    if (mode === 'back') {
      this.goBack();
    }
  }

  private showSaveSuccessMessage(scope: SaveScope, hasIdentifier: boolean): void {
    const detailKey: string = scope === 'section'
      ? 'admin.parks.items.saveMessages.sectionSaved'
      : (hasIdentifier ? 'admin.parks.items.saveMessages.itemSaved' : 'admin.parks.items.saveMessages.itemCreated');

    this.toastMessageService.add(
      'success',
      this.translateService.instant('admin.parks.items.saveMessages.successSummary'),
      this.translateService.instant(detailKey)
    );
  }

  private showSaveErrorMessage(): void {
    this.toastMessageService.add(
      'error',
      this.translateService.instant('admin.parks.items.saveMessages.errorSummary'),
      this.translateService.instant('admin.parks.items.saveMessages.errorDetail')
    );
  }

  private finalizeLoadedFormState(): void {
    this.isInitializing = false;
    this.captureCurrentSnapshot();
  }

  private captureCurrentSnapshot(): void {
    this.lastSavedSnapshot = this.buildComparableSnapshot();
    this.hasPendingChanges = false;
    this.form.markAsPristine();
    this.form.markAsUntouched();
  }

  private updatePendingChanges(): void {
    if (this.isInitializing) {
      return;
    }

    this.hasPendingChanges = this.buildComparableSnapshot() !== this.lastSavedSnapshot;
  }

  private buildComparableSnapshot(): string {
    return JSON.stringify(this.buildPayload());
  }

  private showUploadSuccessMessage(detail: string): void {
    this.toastMessageService.add(
      'success',
      this.translateService.instant('admin.parks.items.saveMessages.successSummary'),
      detail
    );
  }

  private showUploadErrorMessage(detail: string): void {
    this.toastMessageService.add(
      'error',
      this.translateService.instant('admin.parks.items.saveMessages.errorSummary'),
      detail
    );
  }

  private applyGeneralLocation(latitude: number, longitude: number): void {
    this.isApplyingGeneralLocationProgrammatically = true;
    this.form.patchValue({
      latitude,
      longitude
    }, { emitEvent: false });
    this.isApplyingGeneralLocationProgrammatically = false;
    this.refreshAllMapStates();
    this.updatePendingChanges();
  }

  private getLocationGroup(locationKey: AttractionLocationKey): FormGroup {
    return this.form.get(['attractionLocations', locationKey]) as FormGroup;
  }

  private getLocationPoint(locationKey: AttractionLocationKey): AttractionLocationPoint | null {
    const groupValue: any = this.getLocationGroup(locationKey).getRawValue();
    return this.buildLocationPoint(groupValue);
  }

  private createLocationGroup(): FormGroup {
    return this.fb.group({
      latitude: [null],
      longitude: [null]
    });
  }

  private createAccessConditionGroup(condition?: AttractionAccessCondition): FormGroup {
    return this.fb.group({
      type: [condition?.type ?? 'Custom', Validators.required],
      isCustom: [condition?.isCustom ?? (condition?.type === 'Custom')],
      value: [condition?.value ?? null],
      unit: [condition?.unit ?? null],
      requiresAccompaniment: [condition?.requiresAccompaniment ?? false],
      minimumCompanionAge: [condition?.minimumCompanionAge ?? null],
      label: [condition?.label ?? []],
      description: [condition?.description ?? []],
      displayOrder: [condition?.displayOrder ?? null]
    });
  }

  private applyCategorySelection(category: ParkItemCategory): void {
    this.filteredTypeOptions = this.getTypeOptionsForCategory(category);

    const currentType: ParkItemType | null = this.form.get('type')?.value as ParkItemType | null;
    const allowedTypes: ParkItemType[] = this.filteredTypeOptions.map((option: Option<ParkItemType>) => option.value);

    if (!currentType || !allowedTypes.includes(currentType)) {
      this.form.get('type')?.setValue(allowedTypes[0]);
    }
  }

  private getTypeOptionsForCategory(category: ParkItemCategory): Option<ParkItemType>[] {
    if (category === 'Attraction') {
      return this.attractionTypeOptions;
    }

    return this.nonAttractionTypeOptionsByCategory[category as Exclude<ParkItemCategory, 'Attraction'>]
      ?? [{ labelKey: 'parkExplorer.types.other', value: 'Other' }];
  }

  private patchAttractionDetails(details: AttractionDetails | null): void {
    this.form.get('attractionDetails')?.patchValue({
      manufacturerId: details?.manufacturerId ?? null,
      model: details?.model ?? '',
      openingDate: this.normalizeDateForInput(details?.openingDate),
      closingDate: this.normalizeDateForInput(details?.closingDate),
      durationInSeconds: details?.durationInSeconds ?? null,
      capacityPerHour: details?.capacityPerHour ?? null,
      heightInMeters: details?.heightInMeters ?? null,
      lengthInMeters: details?.lengthInMeters ?? null,
      speedInKmH: details?.speedInKmH ?? null,
      dropInMeters: details?.dropInMeters ?? null,
      inversionCount: details?.inversionCount ?? null,
      trainCount: details?.trainCount ?? null,
      carsPerTrain: details?.carsPerTrain ?? null,
      ridersPerVehicle: details?.ridersPerVehicle ?? null,
      hasSingleRider: details?.hasSingleRider ?? false,
      hasFastPass: details?.hasFastPass ?? false,
      isAccessibleForReducedMobility: details?.isAccessibleForReducedMobility ?? false,
      isIndoor: details?.isIndoor ?? false,
      waterExposureLevel: details?.waterExposureLevel ?? null
    }, { emitEvent: false });

    this.setAccessConditions(details?.accessConditions ?? null);
  }

  private setAccessConditions(conditions: AttractionAccessCondition[] | null): void {
    while (this.accessConditions.length > 0) {
      this.accessConditions.removeAt(0);
    }

    for (const condition of conditions ?? []) {
      this.accessConditions.push(this.createAccessConditionGroup(condition));
    }

    this.syncAccessConditionDisplayOrders();
  }

  private syncAccessConditionDisplayOrders(): void {
    for (let index: number = 0; index < this.accessConditions.length; index++) {
      this.getAccessConditionGroup(index).get('displayOrder')?.setValue(index + 1, { emitEvent: false });
    }
  }

  private patchAttractionLocations(locations: AttractionLocations | null): void {
    this.patchLocation('entrance', locations?.entrance ?? null);
    this.patchLocation('exit', locations?.exit ?? null);
    this.patchLocation('fastPassEntrance', locations?.fastPassEntrance ?? null);
    this.patchLocation('reducedMobilityEntrance', locations?.reducedMobilityEntrance ?? null);
  }

  private patchLocation(controlName: AttractionLocationKey, point: AttractionLocationPoint | null): void {
    this.form.get(['attractionLocations', controlName])?.patchValue({
      latitude: point?.latitude ?? null,
      longitude: point?.longitude ?? null
    }, { emitEvent: false });
  }

  private buildPayload(): ParkItem {
    const raw: any = this.form.getRawValue();
    const category: ParkItemCategory = raw.category as ParkItemCategory;

    return {
      parkId: raw.parkId,
      zoneId: raw.zoneId || null,
      name: raw.name,
      category,
      type: raw.type,
      subtype: raw.subtype || null,
      latitude: this.toRequiredNumber(raw.latitude),
      longitude: this.toRequiredNumber(raw.longitude),
      descriptions: raw.descriptions ?? [],
      attractionDetails: category === 'Attraction' ? this.buildAttractionDetails(raw.attractionDetails) : null,
      attractionLocations: category === 'Attraction' ? this.buildAttractionLocations(raw.attractionLocations) : null,
      isVisible: !!raw.isVisible
    };
  }

  private buildAttractionDetails(raw: any): AttractionDetails | null {
    const details: AttractionDetails = {
      manufacturerId: this.toNullableText(raw?.manufacturerId),
      model: this.toNullableText(raw?.model),
      openingDate: this.toNullableDateText(raw?.openingDate),
      closingDate: this.toNullableDateText(raw?.closingDate),
      durationInSeconds: this.toNullableInteger(raw?.durationInSeconds),
      capacityPerHour: this.toNullableInteger(raw?.capacityPerHour),
      heightInMeters: this.toNullableNumber(raw?.heightInMeters),
      lengthInMeters: this.toNullableNumber(raw?.lengthInMeters),
      speedInKmH: this.toNullableNumber(raw?.speedInKmH),
      dropInMeters: this.toNullableNumber(raw?.dropInMeters),
      inversionCount: this.toNullableInteger(raw?.inversionCount),
      trainCount: this.toNullableInteger(raw?.trainCount),
      carsPerTrain: this.toNullableInteger(raw?.carsPerTrain),
      ridersPerVehicle: this.toNullableInteger(raw?.ridersPerVehicle),
      hasSingleRider: raw?.hasSingleRider ?? false,
      hasFastPass: raw?.hasFastPass ?? false,
      isAccessibleForReducedMobility: raw?.isAccessibleForReducedMobility ?? false,
      isIndoor: raw?.isIndoor ?? false,
      waterExposureLevel: this.toNullableWaterExposureLevel(raw?.waterExposureLevel),
      accessConditions: this.buildAttractionAccessConditions(raw?.accessConditions)
    };

    return this.hasAtLeastOneAttractionDetail(details) ? details : null;
  }

  private buildAttractionAccessConditions(raw: any[] | null | undefined): AttractionAccessCondition[] | null {
    if (!raw || raw.length === 0) {
      return null;
    }

    const conditions: AttractionAccessCondition[] = raw
      .map((item: any, index: number) => this.buildAttractionAccessCondition(item, index))
      .filter((item: AttractionAccessCondition | null): item is AttractionAccessCondition => item !== null);

    return conditions.length > 0 ? conditions : null;
  }

  private buildAttractionAccessCondition(raw: any, index: number): AttractionAccessCondition | null {
    const type: AttractionAccessConditionType = (raw?.type as AttractionAccessConditionType) ?? 'Custom';
    const label: LocalizedItem<string>[] | null = this.toLocalizedItems(raw?.label);
    const description: LocalizedItem<string>[] | null = this.toLocalizedItems(raw?.description);
    const condition: AttractionAccessCondition = {
      type,
      isCustom: type === 'Custom',
      value: this.toNullableNumber(raw?.value),
      unit: this.toNullableUnit(raw?.unit),
      requiresAccompaniment: !!raw?.requiresAccompaniment,
      minimumCompanionAge: this.toNullableInteger(raw?.minimumCompanionAge),
      label,
      description,
      displayOrder: index + 1
    };

    if (!this.hasAtLeastOneAccessConditionValue(condition)) {
      return null;
    }

    return condition;
  }

  private buildAttractionLocations(raw: any): AttractionLocations | null {
    const locations: AttractionLocations = {
      entrance: this.buildLocationPoint(raw?.entrance),
      exit: this.buildLocationPoint(raw?.exit),
      fastPassEntrance: this.buildLocationPoint(raw?.fastPassEntrance),
      reducedMobilityEntrance: this.buildLocationPoint(raw?.reducedMobilityEntrance)
    };

    if (!locations.entrance && !locations.exit && !locations.fastPassEntrance && !locations.reducedMobilityEntrance) {
      return null;
    }

    return locations;
  }

  private buildLocationPoint(raw: any): AttractionLocationPoint | null {
    const latitude: number | null = this.toNullableNumber(raw?.latitude);
    const longitude: number | null = this.toNullableNumber(raw?.longitude);

    if (latitude === null || longitude === null) {
      return null;
    }

    return {
      latitude,
      longitude
    };
  }

  private hasAtLeastOneAttractionDetail(details: AttractionDetails): boolean {
    return Object.values(details).some((value: string | number | boolean | AttractionAccessCondition[] | null | undefined) => {
      if (typeof value === 'boolean') {
        return value === true;
      }

      if (Array.isArray(value)) {
        return value.length > 0;
      }

      return value !== null && value !== undefined && value !== '';
    });
  }

  private loadManufacturers(): void {
    this.manufacturersLoading = true;

    this.apiService.getAttractionManufacturers().subscribe({
      next: (manufacturers: AttractionManufacturer[]) => {
        this.manufacturerOptions = manufacturers
          .filter((manufacturer: AttractionManufacturer) => !!manufacturer.id)
          .map((manufacturer: AttractionManufacturer) => ({
            id: manufacturer.id ?? '',
            label: manufacturer.name
          }));
        this.manufacturersLoading = false;
        this.cdr.markForCheck();
      },
      error: (error: unknown) => {
        console.error('Error loading manufacturers', error);
        this.manufacturersLoading = false;
        this.cdr.markForCheck();
      }
    });
  }

  private applyManufacturerSelectionOverride(): void {
    const manufacturerId: string | null = this.route.snapshot.queryParamMap.get('manufacturerId');

    if (!manufacturerId) {
      return;
    }

    this.form.get(['attractionDetails', 'manufacturerId'])?.setValue(manufacturerId);
    this.updatePendingChanges();
  }

  private hasAtLeastOneAccessConditionValue(condition: AttractionAccessCondition): boolean {
    if (condition.type !== 'Custom') {
      return true;
    }

    return condition.value !== null
      || condition.unit !== null
      || condition.requiresAccompaniment === true
      || condition.minimumCompanionAge !== null
      || this.hasLocalizedValues(condition.label)
      || this.hasLocalizedValues(condition.description);
  }

  private hasLocalizedValues(items: LocalizedItem<string>[] | null | undefined): boolean {
    return (items ?? []).some((item: LocalizedItem<string>) => !!item.value && item.value.trim().length > 0);
  }

  private buildDefaultAccessCondition(type: AttractionAccessConditionType): AttractionAccessCondition {
    return {
      type,
      isCustom: type === 'Custom',
      value: null,
      unit: this.getDefaultUnit(type),
      requiresAccompaniment: type === 'MinHeightAccompanied' || type === 'MinAgeAccompanied',
      minimumCompanionAge: null,
      label: this.buildDefaultLocalizedLabel(type),
      description: [],
      displayOrder: this.accessConditions.length + 1
    };
  }

  private buildDefaultLocalizedLabel(type: AttractionAccessConditionType): LocalizedItem<string>[] {
    return LANGUAGES
      .map((language) => ({
        languageCode: language.value,
        value: this.accessConditionDefaultLabels[type][language.value] ?? ''
      }))
      .filter((item: LocalizedItem<string>) => item.value.trim().length > 0);
  }

  private getDefaultUnit(type: AttractionAccessConditionType): AttractionAccessConditionUnit | null {
    switch (type) {
      case 'MinHeight':
      case 'MinHeightAccompanied':
      case 'MaxHeight':
        return 'Centimeter';
      case 'MinAge':
      case 'MinAgeAccompanied':
        return 'Year';
      default:
        return null;
    }
  }

  private getAccessConditionLabelKey(type: AttractionAccessConditionType): string {
    switch (type) {
      case 'MinHeight':
        return 'admin.parks.items.accessConditionTypes.minHeight';
      case 'MinHeightAccompanied':
        return 'admin.parks.items.accessConditionTypes.minHeightAccompanied';
      case 'MaxHeight':
        return 'admin.parks.items.accessConditionTypes.maxHeight';
      case 'MinAge':
        return 'admin.parks.items.accessConditionTypes.minAge';
      case 'MinAgeAccompanied':
        return 'admin.parks.items.accessConditionTypes.minAgeAccompanied';
      case 'PregnancyRestriction':
        return 'admin.parks.items.accessConditionTypes.pregnancyRestriction';
      case 'HeartRestriction':
        return 'admin.parks.items.accessConditionTypes.heartRestriction';
      case 'BackNeckRestriction':
        return 'admin.parks.items.accessConditionTypes.backNeckRestriction';
      case 'WheelchairTransferRequired':
        return 'admin.parks.items.accessConditionTypes.wheelchairTransferRequired';
      case 'AccessPassRequired':
        return 'admin.parks.items.accessConditionTypes.accessPassRequired';
      default:
        return 'admin.parks.items.accessConditionTypes.custom';
    }
  }

  private loadAttractionPhotos(): void {
    if (!this.itemId) {
      return;
    }

    this.photosLoading = true;

    this.apiService.getImages(ImageOwnerType.ATTRACTION, this.itemId, ImageCategory.ATTRACTION).subscribe({
      next: (images: ImageDto[]) => {
        this.attractionPhotos = images.map((image: ImageDto) => this.toAttractionPhotoItem(image));
        this.currentPhoto = this.attractionPhotos.find((item: AttractionPhotoItem) => item.isCurrent) ?? null;
        this.photosLoading = false;
        this.cdr.markForCheck();
      },
      error: (error: unknown) => {
        console.error('Error loading attraction photos', error);
        this.photosLoading = false;
        this.cdr.markForCheck();
      }
    });
  }

  private toAttractionPhotoItem(image: ImageDto): AttractionPhotoItem {
    return {
      id: image.id,
      imageUrl: this.apiService.buildImageUrl(image.id),
      description: image.description,
      isCurrent: image.isCurrent,
      createdAt: image.createdAt
    };
  }

  private toLocalizedItems(value: unknown): LocalizedItem<string>[] | null {
    if (!Array.isArray(value)) {
      return null;
    }

    const normalized: LocalizedItem<string>[] = value
      .filter((item: LocalizedItem<string>) => !!item && typeof item.languageCode === 'string')
      .map((item: LocalizedItem<string>) => ({
        languageCode: item.languageCode.trim().toLowerCase(),
        value: String(item.value ?? '').trim()
      }))
      .filter((item: LocalizedItem<string>) => item.languageCode.length > 0 && item.value.length > 0);

    return normalized.length > 0 ? normalized : null;
  }

  private normalizeDateForInput(value: unknown): string {
    const normalized: string = String(value ?? '').trim();

    if (normalized.length === 0) {
      return '';
    }

    const isoDateMatch: RegExpMatchArray | null = normalized.match(/^(\d{4}-\d{2}-\d{2})/);
    if (isoDateMatch) {
      return isoDateMatch[1];
    }

    const parsedDate: Date = new Date(normalized);
    if (Number.isNaN(parsedDate.getTime())) {
      return '';
    }

    const year: string = String(parsedDate.getUTCFullYear()).padStart(4, '0');
    const month: string = String(parsedDate.getUTCMonth() + 1).padStart(2, '0');
    const day: string = String(parsedDate.getUTCDate()).padStart(2, '0');

    return `${year}-${month}-${day}`;
  }

  private toNullableText(value: unknown): string | null {
    const normalized: string = String(value ?? '').trim();
    return normalized.length > 0 ? normalized : null;
  }

  private toNullableDateText(value: unknown): string | null {
    const normalized: string = this.normalizeDateForInput(value);
    return normalized.length > 0 ? normalized : null;
  }

  private toNullableInteger(value: unknown): number | null {
    if (value === null || value === undefined || value === '') {
      return null;
    }

    const parsed: number = Number(value);
    return Number.isFinite(parsed) ? Math.trunc(parsed) : null;
  }

  private toNullableNumber(value: unknown): number | null {
    if (value === null || value === undefined || value === '') {
      return null;
    }

    const parsed: number = Number(value);
    return Number.isFinite(parsed) ? parsed : null;
  }

  private toNullableUnit(value: unknown): AttractionAccessConditionUnit | null {
    return value === 'Centimeter' || value === 'Year'
      ? value
      : null;
  }

  private toNullableWaterExposureLevel(value: unknown): AttractionWaterExposureLevel | null {
    return value === 'None' || value === 'Splash' || value === 'Moderate' || value === 'Soaking' || value === 'ExtremeSoaking'
      ? value
      : null;
  }

  private toRequiredNumber(value: unknown): number {
    const parsed: number = Number(value);
    return Number.isFinite(parsed) ? parsed : 0;
  }
}
