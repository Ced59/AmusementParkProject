import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { resolveLocalizedValue } from '../../../../../commons/localized-item.utils';
import { ImageCategory } from '../../../../../models/images/image-category';
import { ImageDto } from '../../../../../models/images/image-dto';
import { ImageOwnerType } from '../../../../../models/images/image-owner-type';
import { UploadedImage } from '../../../../../models/images/uploaded-image';
import { AttractionDetails } from '../../../../../models/parks/attraction-details';
import { AttractionLocationPoint } from '../../../../../models/parks/attraction-location-point';
import { AttractionLocations } from '../../../../../models/parks/attraction-locations';
import { ParkItem } from '../../../../../models/parks/park-item';
import { ParkItemCategory } from '../../../../../models/parks/park-item-category';
import { ParkItemType } from '../../../../../models/parks/park-item-type';
import { ParkZone } from '../../../../../models/parks/park-zone';
import { ApiService } from '../../../../../services/api.service';
import {PaginatorState} from "primeng/paginator";

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

@Component({
  selector: 'app-admin-park-item-edit',
  templateUrl: './admin-park-item-edit.component.html',
  styleUrls: ['./admin-park-item-edit.component.scss']
})
export class AdminParkItemEditComponent implements OnInit {
  form!: FormGroup;
  parkId: string = '';
  itemId: string | null = null;
  currentLang: string = 'en';
  zones: { id: string; label: string }[] = [];
  filteredTypeOptions: Option<ParkItemType>[] = [];

  selectedPhotoFile: File | null = null;
  newPhotoDescription: string = '';
  photosUploading: boolean = false;
  photosLoading: boolean = false;
  attractionPhotos: AttractionPhotoItem[] = [];
  currentPhoto: AttractionPhotoItem | null = null;
  photosPage: number = 0;
  photosPageSize: number = 8;

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

  constructor(
    private readonly fb: FormBuilder,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly apiService: ApiService,
    private readonly translateService: TranslateService
  ) {
  }

  ngOnInit(): void {
    this.currentLang = this.route.root.firstChild?.snapshot.params['lang'] ?? 'en';
    this.parkId = this.route.snapshot.paramMap.get('idPark') ?? '';
    this.itemId = this.route.snapshot.paramMap.get('idItem');

    this.form = this.fb.group({
      parkId: [this.parkId, Validators.required],
      zoneId: [null],
      name: ['', Validators.required],
      category: ['Attraction', Validators.required],
      type: ['Attraction', Validators.required],
      subtype: [''],
      latitude: [0, Validators.required],
      longitude: [0, Validators.required],
      descriptions: [[]],
      attractionDetails: this.fb.group({
        manufacturer: [''],
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
        minimumHeightInCm: [null],
        maximumHeightInCm: [null],
        minimumAge: [null],
        trainCount: [null],
        carsPerTrain: [null],
        ridersPerVehicle: [null],
        hasSingleRider: [false],
        hasFastPass: [false],
        isAccessibleForReducedMobility: [false],
        isIndoor: [false],
        isWaterAttraction: [false]
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

    this.form.get('category')?.valueChanges.subscribe((categoryValue: unknown) => {
      this.applyCategorySelection(categoryValue as ParkItemCategory);
    });

    this.apiService.getParkZonesByParkId(this.parkId).subscribe((zones: ParkZone[]) => {
      this.zones = zones
        .filter((zone: ParkZone) => !!zone.id)
        .map((zone: ParkZone) => ({
          id: zone.id!,
          label: resolveLocalizedValue(zone.names, this.currentLang) ?? zone.name ?? zone.id!
        }));
    });

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
        });

        this.patchAttractionDetails(item.attractionDetails ?? null);
        this.patchAttractionLocations(item.attractionLocations ?? null);
        this.applyCategorySelection(item.category);

        if (item.category === 'Attraction') {
          this.loadAttractionPhotos();
        }
      });
    }
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const payload: ParkItem = this.buildPayload();

    if (this.itemId) {
      this.apiService.updateParkItem(this.itemId, payload).subscribe(() => this.goBack());
      return;
    }

    this.apiService.createParkItem(payload).subscribe(() => this.goBack());
  }

  onPhotoFileSelected(event: Event): void {
    const input: HTMLInputElement = event.target as HTMLInputElement;

    if (!input.files || input.files.length === 0) {
      this.selectedPhotoFile = null;
      return;
    }

    this.selectedPhotoFile = input.files[0];
  }

  onUploadPhoto(): void {
    if (!this.itemId || !this.selectedPhotoFile || !this.isAttractionCategory) {
      return;
    }

    this.photosUploading = true;

    this.apiService
      .uploadImage(
        this.selectedPhotoFile,
        ImageCategory.ATTRACTION,
        false,
        `${this.form.get('name')?.value || ''}`
      )
      .subscribe({
        next: (uploaded: UploadedImage) => {
          this.apiService.linkImage({
            imageId: uploaded.id,
            ownerType: ImageOwnerType.ATTRACTION,
            ownerId: this.itemId as string,
            description: this.newPhotoDescription || undefined,
            setAsCurrent: this.attractionPhotos.length === 0
          }).subscribe({
            next: (image: ImageDto) => {
              const item: AttractionPhotoItem = this.toAttractionPhotoItem(image);

              if (image.isCurrent) {
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
              this.selectedPhotoFile = null;
              this.newPhotoDescription = '';
              this.photosUploading = false;
            },
            error: (error: unknown) => {
              console.error('Error linking uploaded attraction image', error);
              this.photosUploading = false;
            }
          });
        },
        error: (error: unknown) => {
          console.error('Error uploading attraction image', error);
          this.photosUploading = false;
        }
      });
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

  private createLocationGroup(): FormGroup {
    return this.fb.group({
      latitude: [null],
      longitude: [null]
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
      manufacturer: details?.manufacturer ?? '',
      model: details?.model ?? '',
      openingDate: details?.openingDate ?? '',
      closingDate: details?.closingDate ?? '',
      durationInSeconds: details?.durationInSeconds ?? null,
      capacityPerHour: details?.capacityPerHour ?? null,
      heightInMeters: details?.heightInMeters ?? null,
      lengthInMeters: details?.lengthInMeters ?? null,
      speedInKmH: details?.speedInKmH ?? null,
      dropInMeters: details?.dropInMeters ?? null,
      inversionCount: details?.inversionCount ?? null,
      minimumHeightInCm: details?.minimumHeightInCm ?? null,
      maximumHeightInCm: details?.maximumHeightInCm ?? null,
      minimumAge: details?.minimumAge ?? null,
      trainCount: details?.trainCount ?? null,
      carsPerTrain: details?.carsPerTrain ?? null,
      ridersPerVehicle: details?.ridersPerVehicle ?? null,
      hasSingleRider: details?.hasSingleRider ?? false,
      hasFastPass: details?.hasFastPass ?? false,
      isAccessibleForReducedMobility: details?.isAccessibleForReducedMobility ?? false,
      isIndoor: details?.isIndoor ?? false,
      isWaterAttraction: details?.isWaterAttraction ?? false
    });
  }

  private patchAttractionLocations(locations: AttractionLocations | null): void {
    this.patchLocation('entrance', locations?.entrance ?? null);
    this.patchLocation('exit', locations?.exit ?? null);
    this.patchLocation('fastPassEntrance', locations?.fastPassEntrance ?? null);
    this.patchLocation('reducedMobilityEntrance', locations?.reducedMobilityEntrance ?? null);
  }

  private patchLocation(controlName: string, point: AttractionLocationPoint | null): void {
    this.form.get(['attractionLocations', controlName])?.patchValue({
      latitude: point?.latitude ?? null,
      longitude: point?.longitude ?? null
    });
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
      manufacturer: this.toNullableText(raw?.manufacturer),
      model: this.toNullableText(raw?.model),
      openingDate: this.toNullableText(raw?.openingDate),
      closingDate: this.toNullableText(raw?.closingDate),
      durationInSeconds: this.toNullableInteger(raw?.durationInSeconds),
      capacityPerHour: this.toNullableInteger(raw?.capacityPerHour),
      heightInMeters: this.toNullableNumber(raw?.heightInMeters),
      lengthInMeters: this.toNullableNumber(raw?.lengthInMeters),
      speedInKmH: this.toNullableNumber(raw?.speedInKmH),
      dropInMeters: this.toNullableNumber(raw?.dropInMeters),
      inversionCount: this.toNullableInteger(raw?.inversionCount),
      minimumHeightInCm: this.toNullableInteger(raw?.minimumHeightInCm),
      maximumHeightInCm: this.toNullableInteger(raw?.maximumHeightInCm),
      minimumAge: this.toNullableInteger(raw?.minimumAge),
      trainCount: this.toNullableInteger(raw?.trainCount),
      carsPerTrain: this.toNullableInteger(raw?.carsPerTrain),
      ridersPerVehicle: this.toNullableInteger(raw?.ridersPerVehicle),
      hasSingleRider: raw?.hasSingleRider ?? false,
      hasFastPass: raw?.hasFastPass ?? false,
      isAccessibleForReducedMobility: raw?.isAccessibleForReducedMobility ?? false,
      isIndoor: raw?.isIndoor ?? false,
      isWaterAttraction: raw?.isWaterAttraction ?? false
    };

    return this.hasAtLeastOneAttractionDetail(details) ? details : null;
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
    return Object.values(details).some((value: string | number | boolean | null | undefined) => {
      if (typeof value === 'boolean') {
        return value === true;
      }

      return value !== null && value !== undefined && value !== '';
    });
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
      },
      error: (error: unknown) => {
        console.error('Error loading attraction photos', error);
        this.photosLoading = false;
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

  private toNullableText(value: unknown): string | null {
    const normalized: string = String(value ?? '').trim();
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

  private toRequiredNumber(value: unknown): number {
    const parsed: number = Number(value);
    return Number.isFinite(parsed) ? parsed : 0;
  }
}
