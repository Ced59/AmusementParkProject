import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { AbstractControl, FormBuilder, FormGroup, Validators, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateService, TranslateModule } from '@ngx-translate/core';
import { firstValueFrom, Subscription } from 'rxjs';
import { PaginatorState } from 'primeng/paginator';

import { CountriesApiService } from '@data-access/countries/countries-api.service';
import { ImagesApiService } from '@data-access/images/images-api.service';
import { ParkFoundersApiService } from '@data-access/parks/park-founders-api.service';
import { ParkOperatorsApiService } from '@data-access/parks/park-operators-api.service';
import { ParksApiService } from '@data-access/parks/parks-api.service';
import { ToastMessageService } from '../../../../../services/messages/toast-message.service';
import { CountryDto } from '../../../../../models/countries/country-dto';
import { UploadedImage } from '../../../../../models/images/uploaded-image';
import { ImageCategory } from '../../../../../models/images/image-category';
import { ImageDto } from '../../../../../models/images/image-dto';
import { ImageOwnerType } from '../../../../../models/images/image-owner-type';
import { ImageTagDto } from '../../../../../models/images/image-tag-dto';
import { MapMarker } from '../../../../../models/map/map-marker';
import { ParkFounder } from '../../../../../models/parks/park-founder';
import { ParkOperator } from '../../../../../models/parks/park-operator';
import { Park } from '../../../../../models/parks/park';
import { ParkType } from '../../../../../models/parks/park-type';
import { EntitySelectOption } from '../../../../../models/shared/entity-select-option';
import { LocalizedItem } from '../../../../../models/shared/localized-item';
import { Bind } from 'primeng/bind';
import { Tag } from 'primeng/tag';
import { EditorSaveToolbarComponent } from '../../../../shared/editor-save-toolbar/editor-save-toolbar.component';
import { Card } from 'primeng/card';
import { Tabs, TabList, Tab, TabPanels, TabPanel } from 'primeng/tabs';
import { Ripple } from 'primeng/ripple';
import { AdminParkGeneralTabComponent } from './tabs/admin-park-general-tab/admin-park-general-tab.component';
import { AdminParkLocationTabComponent } from './tabs/admin-park-location-tab/admin-park-location-tab.component';
import { AdminParkDescriptionsTabComponent } from './tabs/admin-park-descriptions-tab/admin-park-descriptions-tab.component';
import { AdminParkLogosTabComponent } from './tabs/admin-park-logos-tab/admin-park-logos-tab.component';
import { OwnedImageItem } from '@shared/models/images/owned-image-item.model';
import { mapImageDtoToOwnedImageItem } from '@shared/utils/images/owned-image-item.mapper';

interface ParkTypeOption {
  labelKey: string;
  value: ParkType;
}

type SaveMode = 'stay' | 'back';
type SaveScope = 'section' | 'all';

@Component({
    selector: 'app-admin-park-edit',
    templateUrl: './admin-park-edit.component.html',
    styleUrls: ['./admin-park-edit.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [Bind, Tag, EditorSaveToolbarComponent, Card, FormsModule, ReactiveFormsModule, Tabs, TabList, Ripple, Tab, TabPanels, TabPanel, AdminParkGeneralTabComponent, AdminParkLocationTabComponent, AdminParkDescriptionsTabComponent, AdminParkLogosTabComponent, TranslateModule]
})
export class AdminParkEditComponent implements OnInit, OnDestroy {
  form!: FormGroup;

  isEditMode: boolean = false;
  parkId: string | null = null;
  currentLang: string = 'en';
  activeTabIndex: number = 0;

  countryOptions: { code: string; label: string }[] = [];
  countriesLoading: boolean = false;

  founderOptions: EntitySelectOption[] = [];
  operatorOptions: EntitySelectOption[] = [];
  foundersLoading: boolean = false;
  operatorsLoading: boolean = false;

  parkTypeOptions: ParkTypeOption[] = [
    { labelKey: 'admin.parks.types.themePark', value: 'ThemePark' },
    { labelKey: 'admin.parks.types.waterPark', value: 'WaterPark' },
    { labelKey: 'admin.parks.types.zoo', value: 'Zoo' },
    { labelKey: 'admin.parks.types.animalPark', value: 'AnimalPark' },
    { labelKey: 'admin.parks.types.amusementPark', value: 'AmusementPark' },
    { labelKey: 'admin.parks.types.resort', value: 'Resort' }
  ];

  mapCenter: [number, number] = [48.8566, 2.3522];
  mapZoom: number = 16;
  mapMarkers: MapMarker[] = [];

  parkLogos: OwnedImageItem[] = [];
  currentLogo: OwnedImageItem | null = null;
  logosLoading: boolean = false;
  logosUploading: boolean = false;
  logosPage: number = 0;
  logosPageSize: number = 8;

  selectedLogoFiles: File[] = [];
  allowMultipleLogoUpload: boolean = true;
  newLogoDescription: string = '';

  isSaving: boolean = false;
  hasPendingChanges: boolean = false;
  private lastSavedSnapshot: string = '';
  private isInitializing: boolean = false;
  private readonly subscriptions: Subscription = new Subscription();

  constructor(
    private readonly fb: FormBuilder,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    protected readonly imagesApiService: ImagesApiService,
    private readonly countriesApiService: CountriesApiService,
    private readonly parkFoundersApiService: ParkFoundersApiService,
    private readonly parkOperatorsApiService: ParkOperatorsApiService,
    private readonly parksApiService: ParksApiService,
    private readonly translate: TranslateService,
    private readonly toastMessageService: ToastMessageService,
    private readonly cdr: ChangeDetectorRef
  ) {
  }

  get selectedLogoCount(): number {
    return this.selectedLogoFiles.length;
  }

  get isFormDirty(): boolean {
    return this.hasPendingChanges;
  }

  get nameControl(): AbstractControl | null {
    return this.form.get('name');
  }

  get isVisibleControl(): AbstractControl | null {
    return this.form.get('isVisible');
  }

  ngOnInit(): void {
    this.currentLang =
      this.route.root.firstChild?.snapshot.params['lang'] ??
      this.route.snapshot.params['lang'] ??
      'en';

    this.parkId = this.route.snapshot.paramMap.get('idPark');
    this.isEditMode = !!this.parkId;
    this.isInitializing = true;

    const requestedTabIndex: number = Number(this.route.snapshot.queryParamMap.get('returnTab') ?? this.route.snapshot.queryParamMap.get('tab') ?? 0);
    this.activeTabIndex = Number.isFinite(requestedTabIndex) && requestedTabIndex >= 0
      ? requestedTabIndex
      : 0;

    this.buildForm();
    this.loadCountries();
    this.loadFounders();
    this.loadOperators();
    this.setupFormSync();

    if (this.isEditMode && this.parkId) {
      this.loadPark(this.parkId);
      this.loadLogos(this.parkId);
      return;
    }

    this.applySelectionOverridesFromQueryParams();
    this.updateMarkerFromForm();
    this.finalizeLoadedFormState();
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  onSubmit(): void {
    this.saveAll();
  }

  saveSection(): void {
    this.persistPark('stay', 'section');
  }

  saveAll(): void {
    this.persistPark('stay', 'all');
  }

  saveAndClose(): void {
    this.persistPark('back', 'all');
  }

  goBack(): void {
    this.navigateToList();
  }

  onTabChange(index: number | string | undefined): void {
    const normalizedIndex: number =
      typeof index === 'string'
        ? Number(index)
        : typeof index === 'number'
          ? index
          : 0;

    this.activeTabIndex = Number.isFinite(normalizedIndex) ? normalizedIndex : 0;

    if (this.activeTabIndex === 1) {
      window.setTimeout((): void => {
        this.updateMarkerFromForm();
      }, 80);
    }
  }

  onMapPositionChange(position: { lat: number; lng: number }): void {
    this.form.patchValue(
      {
        latitude: position.lat,
        longitude: position.lng
      },
      { emitEvent: true }
    );
  }

  onLogoFileSelected(event: Event): void {
    const input: HTMLInputElement = event.target as HTMLInputElement;

    if (!input.files || input.files.length === 0) {
      this.selectedLogoFiles = [];
      return;
    }

    this.selectedLogoFiles = Array.from(input.files);
  }

  async onUploadLogo(): Promise<void> {
    if (!this.parkId || this.selectedLogoFiles.length === 0 || this.logosUploading) {
      return;
    }

    this.logosUploading = true;
    const files: File[] = [...this.selectedLogoFiles];
    let uploadedCount: number = 0;

    try {
      for (let index: number = 0; index < files.length; index++) {
        await this.uploadLogoAsync(files[index], true);
        uploadedCount++;
      }

      this.selectedLogoFiles = [];
      this.newLogoDescription = '';
      this.toastMessageService.add(
        'success',
        this.translate.instant('admin.parks.saveMessages.successSummary'),
        this.translate.instant('admin.parks.logos.uploadSuccess', { count: uploadedCount })
      );
    } catch (error: unknown) {
      console.error('Error uploading logo images', error);
      this.toastMessageService.add(
        'error',
        this.translate.instant('admin.parks.saveMessages.errorSummary'),
        this.translate.instant('admin.parks.logos.uploadError', { count: uploadedCount })
      );
    } finally {
      this.logosUploading = false;
      this.cdr.markForCheck();
    }
  }

  onSetCurrentLogo(logo: OwnedImageItem): void {
    if (!this.parkId || logo.isCurrent) {
      return;
    }

    this.imagesApiService.setCurrentImage(logo.id).subscribe({
      next: (image: ImageDto) => {
        const updated: OwnedImageItem = this.toOwnedImageItem(image);

        this.parkLogos = this.parkLogos.map((item: OwnedImageItem) => ({
          ...item,
          isCurrent: item.id === updated.id
        }));

        this.currentLogo = updated;
        this.toastMessageService.add(
          'success',
          this.translate.instant('admin.parks.saveMessages.successSummary'),
          this.translate.instant('admin.parks.logos.currentSetSuccess')
        );
        this.cdr.markForCheck();
      },
      error: (error: unknown) => {
        console.error('Error setting current park logo', error);
      }
    });
  }

  onDeleteLogo(logo: OwnedImageItem): void {
    if (!confirm(this.translate.instant('admin.parks.logos.deleteConfirm'))) {
      return;
    }

    this.imagesApiService.deleteImage(logo.id).subscribe({
      next: () => {
        this.parkLogos = this.parkLogos.filter((item: OwnedImageItem) => item.id !== logo.id);
        this.currentLogo = this.parkLogos.find((item: OwnedImageItem) => item.isCurrent) ?? null;
        this.toastMessageService.add(
          'success',
          this.translate.instant('admin.parks.saveMessages.successSummary'),
          this.translate.instant('admin.parks.logos.deleteSuccess')
        );
        this.cdr.markForCheck();
      },
      error: (error: unknown) => {
        console.error('Error deleting park logo', error);
      }
    });
  }

  onLogosPageChange(event: PaginatorState): void {
    this.logosPage = event.page ?? 0;
    this.logosPageSize = event.rows ?? this.logosPageSize;
  }

  get pagedLogos(): OwnedImageItem[] {
    const start: number = this.logosPage * this.logosPageSize;
    return this.parkLogos.slice(start, start + this.logosPageSize);
  }

  get founderCreateLinkQueryParams(): Record<string, string | number> {
    return {
      returnUrl: this.router.url.split('?')[0],
      returnTab: this.activeTabIndex
    };
  }

  get operatorCreateLinkQueryParams(): Record<string, string | number> {
    return {
      returnUrl: this.router.url.split('?')[0],
      returnTab: this.activeTabIndex
    };
  }

  getEditorStatusLabel(): string {
    if (this.isSaving) {
      return this.translate.instant('admin.parks.editorStatus.saving');
    }

    if (this.isFormDirty) {
      return this.translate.instant('admin.parks.editorStatus.unsavedChanges');
    }

    return this.translate.instant('admin.parks.editorStatus.upToDate');
  }

  private buildForm(): void {
    const initial: Park = {
      id: undefined,
      name: '',
      countryCode: '',
      type: null,
      founderId: null,
      operatorId: null,
      latitude: 48.8566,
      longitude: 2.3522,
      isVisible: true
    };

    this.form = this.fb.group({
      id: [initial.id],
      name: [initial.name, Validators.required],
      countryCode: [initial.countryCode],
      type: [initial.type],
      founderId: [initial.founderId],
      operatorId: [initial.operatorId],
      latitude: [initial.latitude, Validators.required],
      longitude: [initial.longitude, Validators.required],
      isVisible: [initial.isVisible],
      descriptions: [initial.descriptions ?? []],
      websiteUrl: [initial.webSiteUrl ?? ''],
      street: [initial.street ?? ''],
      city: [initial.city ?? ''],
      postalCode: [initial.postalCode ?? '']
    });

    this.mapCenter = [initial.latitude, initial.longitude];
  }

  private loadCountries(): void {
    this.countriesLoading = true;
    this.form.get('countryCode')?.disable({ emitEvent: false });

    this.countriesApiService.getCountries(this.currentLang).subscribe({
      next: (countries: CountryDto[]) => {
        this.countryOptions = countries.map((country: CountryDto) => ({
          code: country.isoCode,
          label: country.name
        }));

        this.countriesLoading = false;
        this.form.get('countryCode')?.enable({ emitEvent: false });
        this.cdr.markForCheck();
      },
      error: (error: unknown) => {
        console.error('Error loading countries', error);
        this.countriesLoading = false;
        this.form.get('countryCode')?.enable({ emitEvent: false });
        this.cdr.markForCheck();
      }
    });
  }

  private loadFounders(): void {
    this.foundersLoading = true;

    this.parkFoundersApiService.getParkFounders().subscribe({
      next: (founders: ParkFounder[]) => {
        this.founderOptions = founders.map((founder: ParkFounder) => ({
          id: founder.id ?? '',
          label: founder.name
        }));
        this.foundersLoading = false;
        this.cdr.markForCheck();
      },
      error: (error: unknown) => {
        console.error('Error loading founders', error);
        this.foundersLoading = false;
        this.cdr.markForCheck();
      }
    });
  }

  private loadOperators(): void {
    this.operatorsLoading = true;

    this.parkOperatorsApiService.getParkOperators().subscribe({
      next: (operators: ParkOperator[]) => {
        this.operatorOptions = operators.map((parkOperator: ParkOperator) => ({
          id: parkOperator.id ?? '',
          label: parkOperator.name
        }));
        this.operatorsLoading = false;
        this.cdr.markForCheck();
      },
      error: (error: unknown) => {
        console.error('Error loading operators', error);
        this.operatorsLoading = false;
        this.cdr.markForCheck();
      }
    });
  }

  private loadPark(id: string): void {
    this.parksApiService.getParkById(id).subscribe({
      next: (park: Park) => {
        this.form.patchValue({
          id: park.id,
          name: park.name ?? '',
          countryCode: park.countryCode ?? '',
          type: park.type ?? null,
          founderId: park.founderId ?? null,
          operatorId: park.operatorId ?? null,
          latitude: park.latitude,
          longitude: park.longitude,
          isVisible: park.isVisible ?? true,
          descriptions: park.descriptions ?? [],
          websiteUrl: park.webSiteUrl ?? '',
          street: park.street ?? '',
          city: park.city ?? '',
          postalCode: park.postalCode ?? ''
        }, { emitEvent: false });

        this.applySelectionOverridesFromQueryParams();
        this.mapCenter = [park.latitude, park.longitude];
        this.updateMarkerFromForm();
        this.finalizeLoadedFormState();
        this.cdr.markForCheck();
      },
      error: (error: unknown) => {
        console.error('Error loading park', error);
        this.navigateToList();
      }
    });
  }

  private applySelectionOverridesFromQueryParams(): void {
    const founderId: string | null = this.route.snapshot.queryParamMap.get('founderId');
    const operatorId: string | null = this.route.snapshot.queryParamMap.get('operatorId');

    if (founderId) {
      this.form.patchValue({ founderId }, { emitEvent: false });
    }

    if (operatorId) {
      this.form.patchValue({ operatorId }, { emitEvent: false });
    }
  }

  private setupFormSync(): void {
    const latitudeSubscription: Subscription | undefined = this.form.get('latitude')?.valueChanges.subscribe(() => {
      this.updateMarkerFromForm();
    });

    const longitudeSubscription: Subscription | undefined = this.form.get('longitude')?.valueChanges.subscribe(() => {
      this.updateMarkerFromForm();
    });

    const formSubscription: Subscription = this.form.valueChanges.subscribe(() => {
      if (this.isInitializing) {
        return;
      }

      this.updatePendingChanges();
    });

    if (latitudeSubscription) {
      this.subscriptions.add(latitudeSubscription);
    }

    if (longitudeSubscription) {
      this.subscriptions.add(longitudeSubscription);
    }

    this.subscriptions.add(formSubscription);
  }

  private updateMarkerFromForm(): void {
    const lat: number = Number(this.form.get('latitude')?.value);
    const lng: number = Number(this.form.get('longitude')?.value);

    if (Number.isNaN(lat) || Number.isNaN(lng)) {
      return;
    }

    this.mapMarkers = [
      {
        id: 'park-marker',
        lat,
        lng,
        draggable: true
      }
    ];

    this.mapCenter = [lat, lng];
  }

  private buildPayload(): Park {
    const raw = this.form.value;

    return {
      id: raw.id,
      name: raw.name,
      countryCode: raw.countryCode || undefined,
      type: raw.type || null,
      founderId: raw.founderId || null,
      operatorId: raw.operatorId || null,
      latitude: Number(raw.latitude),
      longitude: Number(raw.longitude),
      isVisible: raw.isVisible,
      descriptions: (raw.descriptions ?? []) as LocalizedItem<string>[],
      webSiteUrl: raw.websiteUrl || undefined,
      street: raw.street || undefined,
      city: raw.city || undefined,
      postalCode: raw.postalCode || undefined
    } as Park;
  }

  private persistPark(mode: SaveMode, scope: SaveScope): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.focusFirstInvalidTab();
      return;
    }

    if (this.isSaving) {
      return;
    }

    const payload: Park = this.buildPayload();
    this.isSaving = true;

    if (this.isEditMode && this.parkId) {
      this.parksApiService.updatePark(this.parkId, payload).subscribe({
        next: (updated: Park) => {
          this.isSaving = false;
          this.afterSuccessfulSave(updated, mode, scope);
          this.cdr.markForCheck();
        },
        error: (error: unknown) => {
          console.error('Error updating park', error);
          this.isSaving = false;
          this.showSaveErrorMessage();
          this.cdr.markForCheck();
        }
      });
      return;
    }

    this.parksApiService.createPark(payload).subscribe({
      next: (created: Park) => {
        this.isSaving = false;
        this.afterSuccessfulSave(created, mode, scope);
      },
      error: (error: unknown) => {
        console.error('Error creating park', error);
        this.isSaving = false;
        this.showSaveErrorMessage();
      }
    });
  }

  private afterSuccessfulSave(savedPark: Park, mode: SaveMode, scope: SaveScope): void {
    const wasEditMode: boolean = !!this.parkId;

    this.captureCurrentSnapshot();
    this.showSaveSuccessMessage(scope, wasEditMode);

    if (!this.parkId && savedPark.id) {
      this.parkId = savedPark.id;
      this.isEditMode = true;

      if (mode === 'back') {
        this.goBack();
        return;
      }

      this.router.navigate(
        ['/', this.currentLang, 'admin', 'parks', 'edit', savedPark.id],
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
      ? 'admin.parks.saveMessages.sectionSaved'
      : (hasIdentifier ? 'admin.parks.saveMessages.parkSaved' : 'admin.parks.saveMessages.parkCreated');

    this.toastMessageService.add(
      'success',
      this.translate.instant('admin.parks.saveMessages.successSummary'),
      this.translate.instant(detailKey)
    );
  }

  private showSaveErrorMessage(): void {
    this.toastMessageService.add(
      'error',
      this.translate.instant('admin.parks.saveMessages.errorSummary'),
      this.translate.instant('admin.parks.saveMessages.errorDetail')
    );
  }

  private focusFirstInvalidTab(): void {
    if (this.form.get('name')?.invalid) {
      this.activeTabIndex = 0;
      return;
    }

    if (this.form.get('latitude')?.invalid || this.form.get('longitude')?.invalid) {
      this.activeTabIndex = 1;
    }
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

  private navigateToList(): void {
    this.router.navigate(['/', this.currentLang, 'admin', 'parks']);
  }

  private loadLogos(parkId: string): void {
    this.logosLoading = true;

    this.imagesApiService.getImages(ImageOwnerType.PARK, parkId, ImageCategory.PARK_LOGO).subscribe({
      next: (images: ImageDto[]) => {
        this.parkLogos = images.map((image: ImageDto) => this.toOwnedImageItem(image));
        this.currentLogo = this.parkLogos.find((item: OwnedImageItem) => item.isCurrent) ?? null;
        this.logosLoading = false;
        this.cdr.markForCheck();
      },
      error: (error: unknown) => {
        console.error('Error loading logos', error);
        this.logosLoading = false;
        this.cdr.markForCheck();
      }
    });
  }

  private async uploadLogoAsync(file: File, setAsCurrent: boolean): Promise<void> {
    const uploaded: UploadedImage = await firstValueFrom(
      this.imagesApiService.uploadImage(
        file,
        ImageCategory.PARK_LOGO,
        false,
        `${this.form.get('name')?.value || ''}`
      )
    );

    const linkedImage: ImageDto = await firstValueFrom(
      this.imagesApiService.linkImage({
        imageId: uploaded.id,
        ownerType: ImageOwnerType.PARK,
        ownerId: this.parkId as string,
        description: this.newLogoDescription || undefined,
        setAsCurrent
      })
    );

    const taggedImage: ImageDto = await this.tryApplyLogoTagAsync(linkedImage);
    const item: OwnedImageItem = this.toOwnedImageItem(taggedImage);

    this.parkLogos = this.parkLogos.map((logo: OwnedImageItem) => ({
      ...logo,
      isCurrent: logo.id === item.id
    }));

    const existingIndex: number = this.parkLogos.findIndex((logo: OwnedImageItem) => logo.id === item.id);

    if (existingIndex >= 0) {
      this.parkLogos[existingIndex] = item;
    } else {
      this.parkLogos.unshift(item);
    }

    this.currentLogo = item;
    this.cdr.markForCheck();
  }

  private async tryApplyLogoTagAsync(image: ImageDto): Promise<ImageDto> {
    try {
      let logoTag: ImageTagDto | undefined = (await firstValueFrom(this.imagesApiService.getAdminImageTags()))
        .find((tag: ImageTagDto) => tag.slug.trim().toLowerCase() === 'logo');

      if (!logoTag) {
        logoTag = await firstValueFrom(this.imagesApiService.createAdminImageTag({
          slug: 'logo',
          labels: [
            { languageCode: 'fr', value: 'Logo' },
            { languageCode: 'en', value: 'Logo' }
          ],
          descriptions: []
        }));
      }

      if (image.tagIds.includes(logoTag.id)) {
        return image;
      }

      return await firstValueFrom(this.imagesApiService.updateAdminImage(image.id, {
        description: image.description,
        geoLocation: image.geoLocation ?? null,
        altTexts: image.altTexts ?? [],
        captions: image.captions ?? [],
        credits: image.credits ?? [],
        tagIds: [...image.tagIds, logoTag.id],
        isPublished: image.isPublished
      }));
    } catch (error: unknown) {
      console.warn('Unable to apply logo tag to image.', error);
      return image;
    }
  }

  private toOwnedImageItem(image: ImageDto): OwnedImageItem {
    return mapImageDtoToOwnedImageItem(image, this.currentLang);
  }
}
