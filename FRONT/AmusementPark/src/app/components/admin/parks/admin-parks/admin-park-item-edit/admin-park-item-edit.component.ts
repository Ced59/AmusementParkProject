import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, Signal, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormArray, FormBuilder, FormGroup } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

import { ToastMessageService } from '@app/services/messages/toast-message.service';
import { EntitySelectOption } from '@app/models/shared/entity-select-option';
import { ParkItem } from '@app/models/parks/park-item';
import { ParkItemCategory } from '@app/models/parks/park-item-category';
import { AttractionAccessConditionType } from '@app/models/parks/attraction-access-condition-type';
import { AttractionAccessConditionTypeDefinition } from '@app/models/parks/attraction-access-condition-type-definition';
import { AttractionAccessConditionTypesApiService } from '@app/data-access/park-items/attraction-access-condition-types-api.service';
import { resolveLocalizedValue } from '@shared/utils/localization';
import { AttractionWaterExposureLevel } from '@app/models/parks/attraction-water-exposure-level';
import { OwnedImageItem } from '@shared/models/images/owned-image-item.model';
import {
  ATTRACTION_ACCESS_CONDITION_PRESET_OPTIONS,
  ATTRACTION_ACCESS_CONDITION_UNIT_OPTIONS,
  ATTRACTION_WATER_EXPOSURE_LEVEL_OPTIONS,
  TranslationOption
} from '@shared/utils/display/display-options';
import {
  AdminParkItemAccessConditionTypeOption,
  addAdminParkItemAccessCondition,
  moveAdminParkItemAccessConditionDown,
  moveAdminParkItemAccessConditionUp,
  removeAdminParkItemAccessCondition,
  updateAdminParkItemAccessConditionType
} from '@features/admin/park-items/mappers/admin-park-item-access-condition-form.utils';
import {
  applyAdminParkItemCategorySelection,
  getAdminParkItemCategoryOptions
} from '@features/admin/park-items/mappers/admin-park-item-type-options';
import {
  buildAdminParkItemEditSnapshot,
  createAdminParkItemEditForm,
  getAdminParkItemFirstInvalidTabIndex,
  mapAdminParkItemEditFormToParkItem,
  patchAdminParkItemEditForm
} from '@features/admin/park-items/mappers/admin-park-item-edit-form.mapper';
import {
  ATTRACTION_LOCATION_OPTIONS,
  AdminParkItemCategoryOption,
  AdminParkItemTypeOption,
  AttractionLocationKey,
  SaveMode,
  SaveScope
} from '@features/admin/park-items/models/admin-park-item-edit.model';
import { AdminParkItemManufacturersStateFacade } from '@features/admin/park-items/state/admin-park-item-manufacturers-state.facade';
import { AdminParkItemZonesStateFacade } from '@features/admin/park-items/state/admin-park-item-zones-state.facade';
import { AdminParkItemLocationStateFacade } from '@features/admin/park-items/state/admin-park-item-location-state.facade';
import { AdminParkItemPhotosStateFacade } from '@features/admin/park-items/state/admin-park-item-photos-state.facade';
import { AdminParkItemEditStateFacade } from '@features/admin/park-items/state/admin-park-item-edit-state.facade';
import { AdminParkItemEditFormComponent } from './admin-park-item-edit-form.component';

@Component({
  selector: 'app-admin-park-item-edit',
  templateUrl: './admin-park-item-edit.component.html',
  styleUrls: ['./admin-park-item-edit.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [
    AdminParkItemManufacturersStateFacade,
    AdminParkItemZonesStateFacade,
    AdminParkItemLocationStateFacade,
    AdminParkItemPhotosStateFacade,
    AdminParkItemEditStateFacade
  ],
  imports: [AdminParkItemEditFormComponent, TranslateModule]
})
export class AdminParkItemEditComponent implements OnInit {
  public readonly form: FormGroup;
  public readonly categoryOptions: AdminParkItemCategoryOption[] = getAdminParkItemCategoryOptions();
  public readonly accessConditionPresetOptions = signal<AdminParkItemAccessConditionTypeOption[]>(this.buildFallbackAccessConditionTypeOptions());
  public readonly waterExposureLevelOptions: Array<TranslationOption<AttractionWaterExposureLevel>> = [...ATTRACTION_WATER_EXPOSURE_LEVEL_OPTIONS];
  public readonly accessConditionUnitOptions = [...ATTRACTION_ACCESS_CONDITION_UNIT_OPTIONS];
  public readonly attractionLocationOptions = [...ATTRACTION_LOCATION_OPTIONS];
  public readonly allowMultiplePhotoUpload: boolean = true;
  public readonly activeTabIndex = signal(0);
  public readonly currentLang = signal('en');
  public readonly parkId = signal('');
  public readonly itemId = signal<string | null>(null);
  public readonly hasPendingChanges = signal(false);
  public readonly selectedAccessConditionPreset = signal<string>('pregnancy-restriction');
  public readonly filteredTypeOptions = signal<AdminParkItemTypeOption[]>([]);
  public readonly isEditMode = computed(() => !!this.itemId());
  public readonly isAttractionCategory = computed(() => this.form.get('category')?.value === 'Attraction');

  private readonly destroyRef: DestroyRef = inject(DestroyRef);
  private lastSavedSnapshot: string = '';
  private isInitializing: boolean = false;

  constructor(
    private readonly formBuilder: FormBuilder,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly translate: TranslateService,
    private readonly toastMessageService: ToastMessageService,
    private readonly accessConditionTypesApiService: AttractionAccessConditionTypesApiService,
    private readonly manufacturersStateFacade: AdminParkItemManufacturersStateFacade,
    private readonly zonesStateFacade: AdminParkItemZonesStateFacade,
    private readonly locationStateFacade: AdminParkItemLocationStateFacade,
    private readonly photosStateFacade: AdminParkItemPhotosStateFacade,
    private readonly editStateFacade: AdminParkItemEditStateFacade
  ) {
    this.form = createAdminParkItemEditForm(this.formBuilder, '');
  }

  public get isSaving(): Signal<boolean> {
    return this.editStateFacade.isSaving;
  }

  public get manufacturersState(): AdminParkItemManufacturersStateFacade {
    return this.manufacturersStateFacade;
  }

  public get zonesState(): AdminParkItemZonesStateFacade {
    return this.zonesStateFacade;
  }

  public get locationState(): AdminParkItemLocationStateFacade {
    return this.locationStateFacade;
  }

  public get photosState(): AdminParkItemPhotosStateFacade {
    return this.photosStateFacade;
  }

  public get parkOptions(): Signal<EntitySelectOption[]> {
    return this.editStateFacade.parkOptions;
  }

  public get parkOptionsLoading(): Signal<boolean> {
    return this.editStateFacade.parkOptionsLoading;
  }

  get isFormDirty(): boolean {
    return this.hasPendingChanges();
  }

  get manufacturerCreateLinkQueryParams(): Record<string, string | number> {
    return {
      returnUrl: this.router.url.split('?')[0],
      returnTab: this.activeTabIndex()
    };
  }

  ngOnInit(): void {
    this.initializeContextFromRoute();
    this.form.patchValue({ parkId: this.parkId() }, { emitEvent: false });
    this.filteredTypeOptions.set(
      applyAdminParkItemCategorySelection(this.form, this.form.get('category')?.value as ParkItemCategory)
    );

    this.isInitializing = true;
    this.locationStateFacade.bindForm(this.form);
    this.photosStateFacade.setCurrentLanguage(this.currentLang());
    this.photosStateFacade.reset();
    this.manufacturersStateFacade.load();
    this.loadAccessConditionTypeOptions();
    this.zonesStateFacade.load(this.parkId(), this.currentLang());
    void this.editStateFacade.loadParkOptions();
    this.setupFormSync();

    void this.initializeEditorAsync();
  }

  onSubmit(): void {
    void this.persistItemAsync('stay', 'all');
  }

  saveSection(): void {
    void this.persistItemAsync('stay', 'section');
  }

  saveAll(): void {
    void this.persistItemAsync('stay', 'all');
  }

  saveAndClose(): void {
    void this.persistItemAsync('back', 'all');
  }

  goBack(): void {
    this.router.navigate(['/', this.currentLang(), 'admin', 'parks', 'edit', this.parkId(), 'items']);
  }

  onTabChange(index: number | string | undefined): void {
    const normalizedIndex: number =
      typeof index === 'string'
        ? Number(index)
        : typeof index === 'number'
          ? index
          : 0;

    this.activeTabIndex.set(Number.isFinite(normalizedIndex) ? normalizedIndex : 0);

    if (this.activeTabIndex() === 0 || this.activeTabIndex() === 3) {
      setTimeout((): void => {
        this.locationStateFacade.refreshFromForm();
      }, 80);
    }
  }

  onGeneralMapPositionChange(position: { lat: number; lng: number }): void {
    this.locationStateFacade.updateGeneralPosition(position);
  }

  resetGeneralLocationToPark(): void {
    this.locationStateFacade.resetGeneralLocationToPark();
    this.updatePendingChanges();
  }

  onParkSelectionChange(parkId: string): void {
    if (!parkId || parkId === this.parkId()) {
      return;
    }

    this.form.get('zoneId')?.setValue(null, { emitEvent: false });
    this.zonesStateFacade.load(parkId, this.currentLang());
    void this.locationStateFacade.loadParkLocationDefaultAsync(parkId, false);
    this.updatePendingChanges();
  }

  selectLocationEditor(locationKey: AttractionLocationKey): void {
    this.locationStateFacade.selectLocationEditor(locationKey);
    setTimeout((): void => {
      this.locationStateFacade.refreshFromForm();
    }, 80);
  }

  onSpecificLocationMapPositionChange(position: { lat: number; lng: number }): void {
    this.locationStateFacade.updateSpecificLocation(position);
  }

  clearLocationPoint(locationKey: AttractionLocationKey): void {
    this.locationStateFacade.clearLocationPoint(locationKey);
  }

  clearSelectedLocationPoint(): void {
    this.locationStateFacade.clearSelectedLocationPoint();
  }

  useGeneralLocationForSelectedPoint(): void {
    this.locationStateFacade.useGeneralLocationForSelectedPoint();
  }

  addAccessCondition(typeKey: string): void {
    addAdminParkItemAccessCondition(this.formBuilder, this.accessConditions, typeKey, this.accessConditionPresetOptions());
    this.activeTabIndex.set(this.isAttractionCategory() ? 2 : 0);
  }

  removeAccessCondition(index: number): void {
    removeAdminParkItemAccessCondition(this.accessConditions, index);
  }

  moveAccessConditionUp(index: number): void {
    moveAdminParkItemAccessConditionUp(this.accessConditions, index);
  }

  moveAccessConditionDown(index: number): void {
    moveAdminParkItemAccessConditionDown(this.accessConditions, index);
  }

  onAccessConditionTypeChanged(index: number): void {
    updateAdminParkItemAccessConditionType(this.accessConditions, index, this.accessConditionPresetOptions());
  }

  onCreateAccessConditionType(request: { key: string; fr: string; en: string }): void {
    this.accessConditionTypesApiService.upsert({
      key: request.key,
      legacyType: 'Custom',
      isActive: true,
      labels: [
        { languageCode: 'fr', value: request.fr },
        { languageCode: 'en', value: request.en }
      ],
      descriptions: [],
      sortOrder: 1000
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (): void => {
          this.loadAccessConditionTypeOptions();
          this.toastMessageService.add(
            'success',
            this.translate.instant('admin.parks.items.accessConditionTypesAdmin.createdSummary'),
            this.translate.instant('admin.parks.items.accessConditionTypesAdmin.createdDetail')
          );
        },
        error: (error: unknown): void => {
          console.error('Error creating access condition type', error);
          this.toastMessageService.add(
            'error',
            this.translate.instant('admin.parks.items.accessConditionTypesAdmin.errorSummary'),
            this.translate.instant('admin.parks.items.accessConditionTypesAdmin.errorDetail')
          );
        }
      });
  }

  onPhotoFileSelected(event: Event): void {
    this.photosStateFacade.selectPhotoFiles(event);
  }

  async onUploadPhoto(): Promise<void> {
    const itemId: string | null = this.itemId();

    if (!itemId || !this.isAttractionCategory()) {
      return;
    }

    await this.photosStateFacade.uploadSelectedPhotos(
      itemId,
      this.form.get('name')?.value ?? ''
    );
  }

  onSetCurrentPhoto(photo: OwnedImageItem): void {
    this.photosStateFacade.setCurrentPhoto(photo);
  }

  onDeletePhoto(photo: OwnedImageItem): void {
    if (!confirm(this.translate.instant('admin.parks.items.photos.deleteConfirm'))) {
      return;
    }

    this.photosStateFacade.deletePhoto(photo);
  }

  onPhotosPageChange(event: { page?: number; rows?: number }): void {
    this.photosStateFacade.onPhotosPageChange(event);
  }

  reloadCurrentItem(): void {
    const itemId: string | null = this.itemId();
    if (!itemId) {
      return;
    }

    void this.loadItemAsync(itemId);
    this.loadAccessConditionTypeOptions();
  }

  get parkItemJsonImportExample(): string {
    const payload: unknown = {
      name: this.form.get('name')?.value || 'Nom de l’élément',
      category: this.form.get('category')?.value || 'Attraction',
      type: this.form.get('type')?.value || 'WaterRide',
      subtype: this.form.get('subtype')?.value || null,
      isVisible: this.form.get('isVisible')?.value ?? true,
      descriptions: [
        { languageCode: 'fr', value: '<p>Description en français.</p>' },
        { languageCode: 'en', value: '<p>English description.</p>' }
      ],
      attractionDetails: {
        model: this.form.get(['attractionDetails', 'model'])?.value || null,
        openingDateText: null,
        durationInSeconds: null,
        heightInMeters: null,
        lengthInMeters: null,
        speedInKmH: null,
        waterExposureLevel: 'Splash'
      },
      accessConditions: [
        {
          typeKey: 'min-height',
          value: 105,
          unit: 'Centimeter',
          displayOrder: 1,
          label: { fr: '105 cm', en: '105 cm' }
        }
      ]
    };

    return JSON.stringify(payload, null, 2);
  }

  getEditorStatusLabel(): string {
    if (this.isSaving()) {
      return this.translate.instant('admin.parks.items.editorStatus.saving');
    }

    if (this.isFormDirty) {
      return this.translate.instant('admin.parks.items.editorStatus.unsavedChanges');
    }

    return this.translate.instant('admin.parks.items.editorStatus.upToDate');
  }

  private get accessConditions(): FormArray {
    return this.form.get(['attractionDetails', 'accessConditions']) as FormArray;
  }

  private initializeContextFromRoute(): void {
    const languageCode: string =
      this.route.root.firstChild?.snapshot.params['lang']
      ?? this.route.snapshot.params['lang']
      ?? 'en';

    this.currentLang.set(languageCode);
    this.parkId.set(this.route.snapshot.paramMap.get('idPark') ?? '');
    this.itemId.set(this.route.snapshot.paramMap.get('idItem'));

    const requestedTabIndex: number = Number(
      this.route.snapshot.queryParamMap.get('returnTab')
      ?? this.route.snapshot.queryParamMap.get('tab')
      ?? 0
    );

    this.activeTabIndex.set(
      Number.isFinite(requestedTabIndex) && requestedTabIndex >= 0
        ? requestedTabIndex
        : 0
    );
  }

  private async initializeEditorAsync(): Promise<void> {
    const itemId: string | null = this.itemId();

    if (itemId) {
      const wasLoaded: boolean = await this.loadItemAsync(itemId);

      if (wasLoaded && this.isAttractionCategory()) {
        this.photosStateFacade.loadPhotos(itemId);
      }

      return;
    }

    this.applySelectionOverridesFromQueryParams();
    await this.locationStateFacade.loadParkLocationDefaultAsync(this.parkId(), true);
    this.finalizeLoadedFormState();
  }

  private async loadItemAsync(itemId: string): Promise<boolean> {
    try {
      const item: ParkItem = await this.editStateFacade.loadItem(itemId);

      patchAdminParkItemEditForm(this.formBuilder, this.form, item);
      this.applySelectionOverridesFromQueryParams();
      this.filteredTypeOptions.set(applyAdminParkItemCategorySelection(this.form, item.category));
      this.locationStateFacade.markGeneralLocationAsManuallyChanged();
      await this.locationStateFacade.loadParkLocationDefaultAsync(this.parkId(), false);
      this.finalizeLoadedFormState();
      return true;
    } catch (error: unknown) {
      console.error('Error loading park item', error);
      this.goBack();
      return false;
    }
  }

  private applySelectionOverridesFromQueryParams(): void {
    const manufacturerId: string | null = this.route.snapshot.queryParamMap.get('manufacturerId');

    if (!manufacturerId) {
      return;
    }

    this.form.get(['attractionDetails', 'manufacturerId'])?.setValue(manufacturerId, { emitEvent: false });
  }

  private loadAccessConditionTypeOptions(): void {
    this.accessConditionTypesApiService.getAll()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (definitions: AttractionAccessConditionTypeDefinition[]): void => {
          const options: AdminParkItemAccessConditionTypeOption[] = definitions
            .filter((definition: AttractionAccessConditionTypeDefinition) => definition.isActive)
            .map((definition: AttractionAccessConditionTypeDefinition) => ({
              labelKey: resolveLocalizedValue(definition.labels ?? [], this.currentLang()) || definition.key,
              value: definition.key,
              legacyType: definition.legacyType ?? 'Custom',
              labels: definition.labels ?? []
            }));

          if (options.length > 0) {
            this.accessConditionPresetOptions.set(options);
          }
        },
        error: (): void => {
          this.accessConditionPresetOptions.set(this.buildFallbackAccessConditionTypeOptions());
        }
      });
  }

  private buildFallbackAccessConditionTypeOptions(): AdminParkItemAccessConditionTypeOption[] {
    return ATTRACTION_ACCESS_CONDITION_PRESET_OPTIONS.map((option: TranslationOption<AttractionAccessConditionType>) => ({
      labelKey: option.labelKey,
      value: this.toAccessConditionTypeKey(option.value),
      legacyType: option.value
    }));
  }

  private toAccessConditionTypeKey(type: AttractionAccessConditionType): string {
    switch (type) {
      case 'MinHeight': return 'min-height';
      case 'MinHeightAccompanied': return 'min-height-accompanied';
      case 'MaxHeight': return 'max-height';
      case 'MinAge': return 'min-age';
      case 'MinAgeAccompanied': return 'min-age-accompanied';
      case 'PregnancyRestriction': return 'pregnancy-restriction';
      case 'HeartRestriction': return 'heart-restriction';
      case 'BackNeckRestriction': return 'back-neck-restriction';
      case 'WheelchairTransferRequired': return 'wheelchair-transfer-required';
      case 'AccessPassRequired': return 'access-pass-required';
      default: return 'custom';
    }
  }

  private setupFormSync(): void {
    this.form.get('category')?.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((categoryValue: unknown): void => {
        const category: ParkItemCategory = categoryValue as ParkItemCategory;
        this.filteredTypeOptions.set(applyAdminParkItemCategorySelection(this.form, category));

        if (categoryValue !== 'Attraction') {
          this.activeTabIndex.set(0);
        }
      });

    this.form.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((): void => {
        if (this.isInitializing) {
          return;
        }

        this.updatePendingChanges();
      });
  }

  private async persistItemAsync(mode: SaveMode, scope: SaveScope): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.focusFirstInvalidTab();
      return;
    }

    if (this.isSaving()) {
      return;
    }

    const payload: ParkItem = mapAdminParkItemEditFormToParkItem(this.form);
    const wasEditMode: boolean = this.isEditMode();

    try {
      const savedItem: ParkItem = await this.editStateFacade.saveItem(this.itemId(), payload);
      this.afterSuccessfulSave(savedItem, mode, scope, wasEditMode);
    } catch (error: unknown) {
      console.error('Error saving park item', error);
      this.showSaveErrorMessage();
    }
  }

  private afterSuccessfulSave(savedItem: ParkItem, mode: SaveMode, scope: SaveScope, wasEditMode: boolean): void {
    const savedParkId: string = savedItem.parkId?.trim() || this.form.get('parkId')?.value || this.parkId();
    const previousRouteParkId: string = this.parkId();
    const hasParkChanged: boolean = !!savedParkId && savedParkId !== previousRouteParkId;

    if (hasParkChanged) {
      this.parkId.set(savedParkId);
      this.zonesStateFacade.load(savedParkId, this.currentLang());
      void this.locationStateFacade.loadParkLocationDefaultAsync(savedParkId, false);
    }

    this.captureCurrentSnapshot();
    this.showSaveSuccessMessage(scope, wasEditMode);

    if ((!this.itemId() || hasParkChanged) && savedItem.id) {
      this.itemId.set(savedItem.id);

      if (mode === 'back') {
        this.goBack();
        return;
      }

      this.router.navigate(
        ['/', this.currentLang(), 'admin', 'parks', 'edit', savedParkId, 'items', savedItem.id],
        {
          replaceUrl: true,
          queryParams: { tab: this.activeTabIndex() }
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
      this.translate.instant('admin.parks.items.saveMessages.successSummary'),
      this.translate.instant(detailKey)
    );
  }

  private showSaveErrorMessage(): void {
    this.toastMessageService.add(
      'error',
      this.translate.instant('admin.parks.items.saveMessages.errorSummary'),
      this.translate.instant('admin.parks.items.saveMessages.errorDetail')
    );
  }

  private focusFirstInvalidTab(): void {
    this.activeTabIndex.set(getAdminParkItemFirstInvalidTabIndex(this.form));
  }

  private finalizeLoadedFormState(): void {
    this.isInitializing = false;
    this.captureCurrentSnapshot();
  }

  private captureCurrentSnapshot(): void {
    this.lastSavedSnapshot = buildAdminParkItemEditSnapshot(this.form);
    this.hasPendingChanges.set(false);
    this.form.markAsPristine();
    this.form.markAsUntouched();
  }

  private updatePendingChanges(): void {
    this.hasPendingChanges.set(buildAdminParkItemEditSnapshot(this.form) !== this.lastSavedSnapshot);
  }
}
