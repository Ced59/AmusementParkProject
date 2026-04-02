import { Component, OnDestroy, OnInit } from '@angular/core';
import { AbstractControl, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { firstValueFrom, Subscription } from 'rxjs';
import { PaginatorState } from 'primeng/paginator';

import { ApiService } from '../../../../../services/api.service';
import { ToastMessageService } from '../../../../../services/messages/toast-message.service';
import { CountryDto } from '../../../../../models/countries/country-dto';
import { UploadedImage } from '../../../../../models/images/uploaded-image';
import { ImageCategory } from '../../../../../models/images/image-category';
import { ImageDto } from '../../../../../models/images/image-dto';
import { ImageOwnerType } from '../../../../../models/images/image-owner-type';
import { MapMarker } from '../../../../../models/map/map-marker';
import { ParkFounder } from '../../../../../models/parks/park-founder';
import { ParkOperator } from '../../../../../models/parks/park-operator';
import { Park } from '../../../../../models/parks/park';
import { ParkType } from '../../../../../models/parks/park-type';
import { EntitySelectOption } from '../../../../../models/shared/entity-select-option';
import { LocalizedItem } from '../../../../../models/shared/localized-item';

interface ParkLogoItem {
  id: string;
  imageUrl: string;
  description?: string;
  isCurrent: boolean;
  createdAt: string;
}

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
    standalone: false
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

  parkLogos: ParkLogoItem[] = [];
  currentLogo: ParkLogoItem | null = null;
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
    protected readonly apiService: ApiService,
    private readonly translate: TranslateService,
    private readonly toastMessageService: ToastMessageService
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
    }
  }

  onSetCurrentLogo(logo: ParkLogoItem): void {
    if (!this.parkId || logo.isCurrent) {
      return;
    }

    this.apiService.setCurrentImage(logo.id).subscribe({
      next: (image: ImageDto) => {
        const updated: ParkLogoItem = this.toParkLogoItem(image);

        this.parkLogos = this.parkLogos.map((item: ParkLogoItem) => ({
          ...item,
          isCurrent: item.id === updated.id
        }));

        this.currentLogo = updated;
        this.toastMessageService.add(
          'success',
          this.translate.instant('admin.parks.saveMessages.successSummary'),
          this.translate.instant('admin.parks.logos.currentSetSuccess')
        );
      },
      error: (error: unknown) => {
        console.error('Error setting current park logo', error);
      }
    });
  }

  onDeleteLogo(logo: ParkLogoItem): void {
    if (!confirm(this.translate.instant('admin.parks.logos.deleteConfirm'))) {
      return;
    }

    this.apiService.deleteImage(logo.id).subscribe({
      next: () => {
        this.parkLogos = this.parkLogos.filter((item: ParkLogoItem) => item.id !== logo.id);
        this.currentLogo = this.parkLogos.find((item: ParkLogoItem) => item.isCurrent) ?? null;
        this.toastMessageService.add(
          'success',
          this.translate.instant('admin.parks.saveMessages.successSummary'),
          this.translate.instant('admin.parks.logos.deleteSuccess')
        );
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

  get pagedLogos(): ParkLogoItem[] {
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

    this.apiService.getCountries(this.currentLang).subscribe({
      next: (countries: CountryDto[]) => {
        this.countryOptions = countries.map((country: CountryDto) => ({
          code: country.isoCode,
          label: country.name
        }));

        this.countriesLoading = false;
        this.form.get('countryCode')?.enable({ emitEvent: false });
      },
      error: (error: unknown) => {
        console.error('Error loading countries', error);
        this.countriesLoading = false;
        this.form.get('countryCode')?.enable({ emitEvent: false });
      }
    });
  }

  private loadFounders(): void {
    this.foundersLoading = true;

    this.apiService.getParkFounders().subscribe({
      next: (founders: ParkFounder[]) => {
        this.founderOptions = founders.map((founder: ParkFounder) => ({
          id: founder.id ?? '',
          label: founder.name
        }));
        this.foundersLoading = false;
      },
      error: (error: unknown) => {
        console.error('Error loading founders', error);
        this.foundersLoading = false;
      }
    });
  }

  private loadOperators(): void {
    this.operatorsLoading = true;

    this.apiService.getParkOperators().subscribe({
      next: (operators: ParkOperator[]) => {
        this.operatorOptions = operators.map((parkOperator: ParkOperator) => ({
          id: parkOperator.id ?? '',
          label: parkOperator.name
        }));
        this.operatorsLoading = false;
      },
      error: (error: unknown) => {
        console.error('Error loading operators', error);
        this.operatorsLoading = false;
      }
    });
  }

  private loadPark(id: string): void {
    this.apiService.getParkById(id).subscribe({
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
      this.apiService.updatePark(this.parkId, payload).subscribe({
        next: (updated: Park) => {
          this.isSaving = false;
          this.afterSuccessfulSave(updated, mode, scope);
        },
        error: (error: unknown) => {
          console.error('Error updating park', error);
          this.isSaving = false;
          this.showSaveErrorMessage();
        }
      });
      return;
    }

    this.apiService.createPark(payload).subscribe({
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

    this.apiService.getImages(ImageOwnerType.PARK, parkId, ImageCategory.PARK_LOGO).subscribe({
      next: (images: ImageDto[]) => {
        this.parkLogos = images.map((image: ImageDto) => this.toParkLogoItem(image));
        this.currentLogo = this.parkLogos.find((item: ParkLogoItem) => item.isCurrent) ?? null;
        this.logosLoading = false;
      },
      error: (error: unknown) => {
        console.error('Error loading logos', error);
        this.logosLoading = false;
      }
    });
  }

  private async uploadLogoAsync(file: File, setAsCurrent: boolean): Promise<void> {
    const uploaded: UploadedImage = await firstValueFrom(
      this.apiService.uploadImage(
        file,
        ImageCategory.PARK_LOGO,
        false,
        `${this.form.get('name')?.value || ''}`
      )
    );

    const image: ImageDto = await firstValueFrom(
      this.apiService.linkImage({
        imageId: uploaded.id,
        ownerType: ImageOwnerType.PARK,
        ownerId: this.parkId as string,
        description: this.newLogoDescription || undefined,
        setAsCurrent
      })
    );

    const item: ParkLogoItem = this.toParkLogoItem(image);

    this.parkLogos = this.parkLogos.map((logo: ParkLogoItem) => ({
      ...logo,
      isCurrent: logo.id === item.id
    }));

    const existingIndex: number = this.parkLogos.findIndex((logo: ParkLogoItem) => logo.id === item.id);

    if (existingIndex >= 0) {
      this.parkLogos[existingIndex] = item;
    } else {
      this.parkLogos.unshift(item);
    }

    this.currentLogo = item;
  }

  private toParkLogoItem(image: ImageDto): ParkLogoItem {
    return {
      id: image.id,
      imageUrl: this.apiService.buildImageUrl(image.id),
      description: image.description,
      isCurrent: image.isCurrent,
      createdAt: image.createdAt
    };
  }
}
