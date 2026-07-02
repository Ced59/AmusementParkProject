import { Inject, Injectable, Signal, computed, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { TranslateService } from '@ngx-translate/core';

import { ImageCategory } from '@app/models/images/image-category';
import { ImageDto } from '@app/models/images/image-dto';
import { ImageOwnerType } from '@app/models/images/image-owner-type';
import { ImageTagDto } from '@app/models/images/image-tag-dto';
import { UploadedImage } from '@app/models/images/uploaded-image';
import { ToastMessageService } from '@app/services/messages/toast-message.service';
import { AttractionLocationPoint } from '@app/models/parks/attraction-location-point';
import { AttractionLocations } from '@app/models/parks/attraction-locations';
import { ParkItem } from '@app/models/parks/park-item';
import { ImageUploadSecurityService } from '@shared/utils/security';
import { PhotoGpsMetadataService } from '@shared/utils/images/photo-gps-metadata.service';

import {
  ADMIN_FIELD_MODE_GEOLOCATION_PORT,
  ADMIN_FIELD_MODE_IMAGES_API_SERVICE_PORT,
  ADMIN_FIELD_MODE_PARK_ITEMS_API_SERVICE_PORT,
  AdminFieldModeGeolocationPermissionState,
  AdminFieldModeGeolocationPort,
  AdminFieldModeImagesApiServicePort,
  AdminFieldModeParkItemsApiServicePort
} from './admin-field-mode-data.ports';
import {
  ADMIN_FIELD_MODE_LOCATION_OPTIONS,
  ADMIN_FIELD_MODE_PHOTO_CATEGORY_OPTIONS,
  ADMIN_FIELD_MODE_GPS_TARGET_ACCURACY_METERS,
  AdminFieldModeGpsStatus,
  AdminFieldModeLocationKey,
  AdminFieldModePhotoCategoryOption,
  AdminFieldModePhotoInspection,
  AdminFieldModePhotoInspectionStatus,
  AdminFieldModePhotoSelection,
  AdminFieldModePosition
} from '../models/admin-field-mode.model';

interface ImageDimensions {
  width: number;
  height: number;
}

@Injectable()
export class AdminFieldModeActionsFacade {
  private static nextPhotoSelectionId = 0;

  private readonly statusSignal = signal<AdminFieldModeGpsStatus>('idle');
  private readonly statusMessageKeySignal = signal<string | null>(null);
  private readonly positionSignal = signal<AdminFieldModePosition | null>(null);
  private readonly photoPositionSignal = signal<AdminFieldModePosition | null>(null);
  private readonly photoSelectionsSignal = signal<AdminFieldModePhotoSelection[]>([]);
  private readonly photoInspectionsSignal = signal<AdminFieldModePhotoInspection[]>([]);
  private readonly photoCategorySlugSignal = signal<string>(ADMIN_FIELD_MODE_PHOTO_CATEGORY_OPTIONS[0].slug);
  private readonly photoDescriptionSignal = signal('');
  private readonly locationKeySignal = signal<AdminFieldModeLocationKey>('general');
  private readonly busySignal = signal(false);
  private readonly uploadingCountSignal = signal(0);
  private photoTagIdsBySlug: Record<string, string> = {};

  public readonly status: Signal<AdminFieldModeGpsStatus> = this.statusSignal.asReadonly();
  public readonly statusMessageKey: Signal<string | null> = this.statusMessageKeySignal.asReadonly();
  public readonly position: Signal<AdminFieldModePosition | null> = this.positionSignal.asReadonly();
  public readonly photoPosition: Signal<AdminFieldModePosition | null> = this.photoPositionSignal.asReadonly();
  public readonly selectedFile: Signal<File | null> = computed(() => this.photoSelectionsSignal()[0]?.file ?? null);
  public readonly selectedPhotos: Signal<AdminFieldModePhotoSelection[]> = this.photoSelectionsSignal.asReadonly();
  public readonly photoInspections: Signal<AdminFieldModePhotoInspection[]> = this.photoInspectionsSignal.asReadonly();
  public readonly photoCategorySlug: Signal<string> = this.photoCategorySlugSignal.asReadonly();
  public readonly photoDescription: Signal<string> = this.photoDescriptionSignal.asReadonly();
  public readonly locationKey: Signal<AdminFieldModeLocationKey> = this.locationKeySignal.asReadonly();
  public readonly busy: Signal<boolean> = this.busySignal.asReadonly();
  public readonly uploadingCount: Signal<number> = this.uploadingCountSignal.asReadonly();
  public readonly readyForPhoto: Signal<boolean> = computed(() => this.photoSelectionsSignal().length > 0);
  public readonly photoCategoryOptions = signal(ADMIN_FIELD_MODE_PHOTO_CATEGORY_OPTIONS).asReadonly();

  constructor(
    @Inject(ADMIN_FIELD_MODE_IMAGES_API_SERVICE_PORT) private readonly imagesApiService: AdminFieldModeImagesApiServicePort,
    @Inject(ADMIN_FIELD_MODE_PARK_ITEMS_API_SERVICE_PORT) private readonly parkItemsApiService: AdminFieldModeParkItemsApiServicePort,
    @Inject(ADMIN_FIELD_MODE_GEOLOCATION_PORT) private readonly positionService: AdminFieldModeGeolocationPort,
    private readonly imageUploadSecurityService: ImageUploadSecurityService,
    private readonly photoGpsService: PhotoGpsMetadataService,
    private readonly toastMessageService: ToastMessageService,
    private readonly translateService: TranslateService
  ) {
  }

  async refreshPosition(): Promise<void> {
    await this.captureFreshPosition();
  }

  async selectFiles(event: Event): Promise<void> {
    const input: HTMLInputElement = event.target as HTMLInputElement;
    const files: File[] = Array.from(input.files ?? []);
    input.value = '';

    this.clearSelectedPhotos();
    this.photoPositionSignal.set(null);

    if (files.length === 0) {
      this.statusMessageKeySignal.set('admin.fieldMode.messages.photoRequired');
      return;
    }

    const validSelections: AdminFieldModePhotoSelection[] = [];
    const inspections: AdminFieldModePhotoInspection[] = [];
    let invalidImageCount = 0;
    let missingGpsCount = 0;

    for (const file of files) {
      const selectionId: string = `field-photo-${AdminFieldModeActionsFacade.nextPhotoSelectionId++}`;
      const validation = this.imageUploadSecurityService.validateImageFile(file);
      const imageDimensions: ImageDimensions | null = validation.isValid ? await this.readImageDimensions(file) : null;

      if (!validation.isValid) {
        invalidImageCount++;
        inspections.push(this.createPhotoInspection(selectionId, file, 'invalid', imageDimensions, null));
        continue;
      }

      const photoPosition: AdminFieldModePosition | null = await this.photoGpsService.readPosition(file);
      if (!photoPosition) {
        missingGpsCount++;
        inspections.push(this.createPhotoInspection(selectionId, file, 'missingGps', imageDimensions, null));
        continue;
      }

      inspections.push(this.createPhotoInspection(selectionId, file, 'accepted', imageDimensions, photoPosition));
      validSelections.push({
        id: selectionId,
        file,
        position: photoPosition,
        previewUrl: URL.createObjectURL(file)
      });
    }

    this.photoInspectionsSignal.set(inspections);

    if (validSelections.length === 0) {
      this.statusMessageKeySignal.set(missingGpsCount > 0 ? 'admin.fieldMode.messages.photoMissingGps' : 'admin.fieldMode.messages.invalidImage');
      return;
    }

    this.photoSelectionsSignal.set(validSelections);
    this.photoPositionSignal.set(validSelections[0].position);

    if (invalidImageCount + missingGpsCount > 0) {
      this.statusMessageKeySignal.set('admin.fieldMode.messages.photosPartiallyReady');
      return;
    }

    this.statusMessageKeySignal.set('admin.fieldMode.messages.photoGpsReady');
  }

  async selectFile(event: Event): Promise<void> {
    await this.selectFiles(event);
  }

  removeSelectedPhoto(selectionId: string): void {
    const selection: AdminFieldModePhotoSelection | undefined = this.photoSelectionsSignal().find((item: AdminFieldModePhotoSelection) => item.id === selectionId);
    if (!selection) {
      return;
    }

    URL.revokeObjectURL(selection.previewUrl);
    const selections: AdminFieldModePhotoSelection[] = this.photoSelectionsSignal().filter((item: AdminFieldModePhotoSelection) => item.id !== selectionId);
    this.photoSelectionsSignal.set(selections);
    this.photoInspectionsSignal.set(this.photoInspectionsSignal().filter((item: AdminFieldModePhotoInspection) => item.id !== selectionId));
    this.photoPositionSignal.set(selections[0]?.position ?? null);
  }

  setPhotoCategorySlug(slug: string): void {
    const known: boolean = ADMIN_FIELD_MODE_PHOTO_CATEGORY_OPTIONS.some((option: AdminFieldModePhotoCategoryOption) => option.slug === slug);
    this.photoCategorySlugSignal.set(known ? slug : ADMIN_FIELD_MODE_PHOTO_CATEGORY_OPTIONS[0].slug);
  }

  setPhotoDescription(description: string): void {
    this.photoDescriptionSignal.set(description);
  }

  setLocationKey(locationKey: AdminFieldModeLocationKey): void {
    const known: boolean = ADMIN_FIELD_MODE_LOCATION_OPTIONS.some((option) => option.key === locationKey);
    this.locationKeySignal.set(known ? locationKey : 'general');
  }

  addPhoto(item: ParkItem, shouldSetCurrent: boolean): boolean {
    if (!item.id) {
      return false;
    }

    const itemId: string = item.id;
    const selections: AdminFieldModePhotoSelection[] = [...this.photoSelectionsSignal()];
    if (selections.length === 0) {
      this.statusMessageKeySignal.set('admin.fieldMode.messages.photoRequired');
      return false;
    }

    const description: string = this.photoDescriptionSignal().trim();
    const categorySlug: string = this.photoCategorySlugSignal();
    this.clearSelectedPhotos();
    this.photoDescriptionSignal.set('');
    this.statusMessageKeySignal.set('admin.fieldMode.messages.photosQueued');
    void this.uploadPhotosInBackground(item, itemId, selections, description, categorySlug, shouldSetCurrent);
    return true;
  }

  private async uploadPhotosInBackground(
    item: ParkItem,
    itemId: string,
    selections: AdminFieldModePhotoSelection[],
    description: string,
    categorySlug: string,
    shouldSetCurrent: boolean
  ): Promise<void> {
    this.uploadingCountSignal.update((count: number) => count + selections.length);
    let uploadedCount = 0;
    try {
      await this.ensurePhotoCategoryTags();
      for (let index: number = 0; index < selections.length; index++) {
        const selection: AdminFieldModePhotoSelection = selections[index];
        const uploaded: UploadedImage = await firstValueFrom(this.imagesApiService.uploadImage(selection.file, ImageCategory.PARK_ITEM, true, description || item.name));
        const linked: ImageDto = await firstValueFrom(this.imagesApiService.linkImage({
          imageId: uploaded.id,
          ownerType: ImageOwnerType.PARK_ITEM,
          ownerId: itemId,
          description: description || undefined,
          setAsCurrent: shouldSetCurrent && index === 0
        }));
        const metadataPosition: AdminFieldModePosition = this.resolveUploadedImagePosition(uploaded, selection.position);
        await this.applyPhotoMetadata(linked, metadataPosition, description, categorySlug);
        uploadedCount++;
      }

      this.statusMessageKeySignal.set('admin.fieldMode.messages.photoAdded');
      this.toastMessageService.add(
        'success',
        this.fieldText('Photos envoyées', 'Photos uploaded'),
        this.fieldText(`${uploadedCount} photo(s) ajoutée(s) à ${item.name}.`, `${uploadedCount} photo(s) added to ${item.name}.`)
      );
    } catch (error: unknown) {
      console.error('Error adding field mode photo', error);
      this.statusMessageKeySignal.set('admin.fieldMode.messages.photoFailed');
      this.toastMessageService.add(
        'error',
        this.fieldText('Envoi photo échoué', 'Photo upload failed'),
        this.fieldText(`${uploadedCount}/${selections.length} photo(s) envoyée(s) pour ${item.name}.`, `${uploadedCount}/${selections.length} photo(s) uploaded for ${item.name}.`)
      );
    } finally {
      for (const selection of selections) {
        URL.revokeObjectURL(selection.previewUrl);
      }
      this.uploadingCountSignal.update((count: number) => Math.max(0, count - selections.length));
    }
  }

  async saveLocation(item: ParkItem, locationKey: AdminFieldModeLocationKey): Promise<ParkItem | null> {
    if (!item.id || this.busySignal()) {
      return null;
    }

    this.busySignal.set(true);
    try {
      const position: AdminFieldModePosition = await this.captureFreshPosition();
      const updatedItem: ParkItem = this.applyLocation(item, locationKey, position);
      const savedItem: ParkItem = await firstValueFrom(this.parkItemsApiService.updateParkItem(item.id, updatedItem));
      this.statusMessageKeySignal.set('admin.fieldMode.messages.locationSaved');
      return savedItem;
    } catch (error: unknown) {
      console.error('Error saving field mode location', error);
      if (!this.statusMessageKeySignal()?.startsWith('admin.fieldMode.messages.position')) {
        this.statusMessageKeySignal.set('admin.fieldMode.messages.locationFailed');
      }
      return null;
    } finally {
      this.busySignal.set(false);
    }
  }

  private async captureFreshPosition(): Promise<AdminFieldModePosition> {
    this.statusSignal.set('checking');
    try {
      const permissionState: AdminFieldModeGeolocationPermissionState = await this.positionService.getPermissionState();
      const blockingMessageKey: string | null = this.getBlockingPermissionMessageKey(permissionState);
      if (blockingMessageKey) {
        this.statusSignal.set('error');
        this.statusMessageKeySignal.set(blockingMessageKey);
        throw new Error(blockingMessageKey);
      }

      if (permissionState === 'prompt') {
        this.statusMessageKeySignal.set('admin.fieldMode.messages.positionPrompt');
      }

      const position: GeolocationPosition = await this.requestCurrentPositionWithFallback();
      const value: AdminFieldModePosition = {
        latitude: position.coords.latitude,
        longitude: position.coords.longitude,
        accuracy: Number.isFinite(position.coords.accuracy) ? position.coords.accuracy : null,
        capturedAt: Date.now()
      };
      this.positionSignal.set(value);
      this.statusSignal.set('ready');
      this.statusMessageKeySignal.set(
        value.accuracy !== null && value.accuracy > ADMIN_FIELD_MODE_GPS_TARGET_ACCURACY_METERS
          ? 'admin.fieldMode.messages.positionBestEffort'
          : 'admin.fieldMode.messages.positionReady'
      );
      return value;
    } catch (error: unknown) {
      this.statusSignal.set('error');
      if (!this.statusMessageKeySignal()?.startsWith('admin.fieldMode.messages.position')) {
        this.statusMessageKeySignal.set(this.getPositionErrorMessageKey(error));
      }
      throw error;
    }
  }

  private async requestCurrentPositionWithFallback(): Promise<GeolocationPosition> {
    try {
      return await this.requestBestPosition({ enableHighAccuracy: true, maximumAge: 0, timeout: 12000 }, 12000);
    } catch (error: unknown) {
      if (this.getPositionErrorCode(error) !== 3) {
        throw error;
      }

      this.statusMessageKeySignal.set('admin.fieldMode.messages.positionRetryLowAccuracy');
      return this.requestBestPosition({ enableHighAccuracy: false, maximumAge: 0, timeout: 6000 }, 6000);
    }
  }

  private requestBestPosition(options: PositionOptions, maxWaitMilliseconds: number): Promise<GeolocationPosition> {
    return new Promise((resolve: (position: GeolocationPosition) => void, reject: (error: unknown) => void): void => {
      let bestPosition: GeolocationPosition | null = null;
      let settled = false;
      let watchId = -1;

      const cleanup = (): void => {
        if (watchId >= 0) {
          this.positionService.clearWatch(watchId);
        }
      };

      const settleWithPosition = (position: GeolocationPosition): void => {
        if (settled) {
          return;
        }

        settled = true;
        cleanup();
        resolve(position);
      };

      const settleWithError = (error: unknown): void => {
        if (settled) {
          return;
        }

        settled = true;
        cleanup();
        reject(error);
      };

      const timeoutId: ReturnType<typeof setTimeout> = setTimeout((): void => {
        if (bestPosition) {
          settleWithPosition(bestPosition);
          return;
        }

        settleWithError({ code: 3, message: 'Position timed out.' });
      }, maxWaitMilliseconds);

      const clearTimeoutAndSettle = (position: GeolocationPosition): void => {
        clearTimeout(timeoutId);
        settleWithPosition(position);
      };

      watchId = this.positionService.watchPosition(
        (position: GeolocationPosition): void => {
          if (!bestPosition || this.getPositionAccuracy(position) < this.getPositionAccuracy(bestPosition)) {
            bestPosition = position;
          }

          if (this.getPositionAccuracy(position) <= ADMIN_FIELD_MODE_GPS_TARGET_ACCURACY_METERS) {
            clearTimeoutAndSettle(position);
          }
        },
        (error: GeolocationPositionError): void => {
          if (bestPosition) {
            clearTimeoutAndSettle(bestPosition);
            return;
          }

          clearTimeout(timeoutId);
          settleWithError(error);
        },
        options);
    });
  }

  private getPositionAccuracy(position: GeolocationPosition): number {
    return Number.isFinite(position.coords.accuracy) ? position.coords.accuracy : Number.MAX_SAFE_INTEGER;
  }

  private getBlockingPermissionMessageKey(permissionState: AdminFieldModeGeolocationPermissionState): string | null {
    if (permissionState === 'blocked-by-policy') {
      return 'admin.fieldMode.messages.positionBlockedByPolicy';
    }

    if (permissionState === 'denied') {
      return 'admin.fieldMode.messages.positionDenied';
    }

    if (permissionState === 'insecure-context') {
      return 'admin.fieldMode.messages.positionInsecureContext';
    }

    if (permissionState === 'unavailable') {
      return 'admin.fieldMode.messages.positionUnavailable';
    }

    return null;
  }

  private getPositionErrorMessageKey(error: unknown): string {
    const code: number | undefined = this.getPositionErrorCode(error);

    if (code === 1) {
      return 'admin.fieldMode.messages.positionDenied';
    }

    if (code === 3) {
      return 'admin.fieldMode.messages.positionTimeout';
    }

    if (code === 2) {
      return 'admin.fieldMode.messages.positionUnavailable';
    }

    return 'admin.fieldMode.messages.positionUnavailable';
  }

  private getPositionErrorCode(error: unknown): number | undefined {
    return typeof error === 'object' && error !== null && 'code' in error
      ? Number((error as { code?: unknown }).code)
      : undefined;
  }

  private clearSelectedPhotos(): void {
    for (const selection of this.photoSelectionsSignal()) {
      URL.revokeObjectURL(selection.previewUrl);
    }

    this.photoSelectionsSignal.set([]);
    this.photoInspectionsSignal.set([]);
    this.photoPositionSignal.set(null);
  }

  private async ensurePhotoCategoryTags(): Promise<void> {
    const tags: ImageTagDto[] = await firstValueFrom(this.imagesApiService.getAdminImageTags());
    const idsBySlug: Record<string, string> = {};

    for (const option of ADMIN_FIELD_MODE_PHOTO_CATEGORY_OPTIONS) {
      const existingTag: ImageTagDto | undefined = tags.find((tag: ImageTagDto) => tag.slug === option.slug);
      if (existingTag) {
        idsBySlug[option.slug] = existingTag.id;
        continue;
      }

      const createdTag: ImageTagDto = await firstValueFrom(this.imagesApiService.createAdminImageTag({
        slug: option.slug,
        labels: [
          { languageCode: 'en', value: option.labelEn },
          { languageCode: 'fr', value: option.labelFr }
        ],
        descriptions: []
      }));
      idsBySlug[option.slug] = createdTag.id;
    }

    this.photoTagIdsBySlug = idsBySlug;
  }

  private async applyPhotoMetadata(image: ImageDto, position: AdminFieldModePosition, description: string, categorySlug: string): Promise<ImageDto> {
    const selectedTagId: string | undefined = this.photoTagIdsBySlug[categorySlug];
    return firstValueFrom(this.imagesApiService.updateAdminImage(image.id, {
      description: image.description ?? (description || undefined),
      geoLocation: {
        latitude: position.latitude,
        longitude: position.longitude
      },
      altTexts: image.altTexts ?? [],
      captions: image.captions ?? [],
      credits: image.credits ?? [],
      tagIds: selectedTagId ? [...new Set([...(image.tagIds ?? []), selectedTagId])] : image.tagIds ?? [],
      isPublished: image.isPublished !== false,
      sourceUrl: image.sourceUrl ?? null
    }));
  }

  private resolveUploadedImagePosition(uploaded: UploadedImage, fallback: AdminFieldModePosition): AdminFieldModePosition {
    if (Number.isFinite(uploaded.latitude) && Number.isFinite(uploaded.longitude)) {
      return {
        latitude: uploaded.latitude as number,
        longitude: uploaded.longitude as number,
        accuracy: fallback.accuracy,
        capturedAt: fallback.capturedAt
      };
    }

    return fallback;
  }

  private createPhotoInspection(
    id: string,
    file: File,
    status: AdminFieldModePhotoInspectionStatus,
    dimensions: ImageDimensions | null,
    position: AdminFieldModePosition | null
  ): AdminFieldModePhotoInspection {
    return {
      id,
      fileName: file.name,
      sizeInBytes: file.size,
      contentType: file.type || null,
      lastModified: Number.isFinite(file.lastModified) ? file.lastModified : null,
      width: dimensions?.width ?? null,
      height: dimensions?.height ?? null,
      gpsDetected: position !== null,
      latitude: position?.latitude ?? null,
      longitude: position?.longitude ?? null,
      status
    };
  }

  private readImageDimensions(file: File): Promise<ImageDimensions | null> {
    return new Promise((resolve: (value: ImageDimensions | null) => void): void => {
      const image = new Image();
      const objectUrl: string = URL.createObjectURL(file);
      const cleanup = (): void => URL.revokeObjectURL(objectUrl);

      image.onload = (): void => {
        const width: number = image.naturalWidth || image.width;
        const height: number = image.naturalHeight || image.height;
        cleanup();
        resolve(width > 0 && height > 0 ? { width, height } : null);
      };

      image.onerror = (): void => {
        cleanup();
        resolve(null);
      };

      image.src = objectUrl;
    });
  }

  private fieldText(fr: string, en: string): string {
    return this.translateService.currentLang === 'fr' ? fr : en;
  }

  private applyLocation(item: ParkItem, locationKey: AdminFieldModeLocationKey, position: AdminFieldModePosition): ParkItem {
    if (locationKey === 'general') {
      return {
        ...item,
        latitude: position.latitude,
        longitude: position.longitude
      };
    }

    const point: AttractionLocationPoint = {
      latitude: position.latitude,
      longitude: position.longitude
    };
    const locations: AttractionLocations = {
      entrance: item.attractionLocations?.entrance ?? null,
      exit: item.attractionLocations?.exit ?? null,
      fastPassEntrance: item.attractionLocations?.fastPassEntrance ?? null,
      reducedMobilityEntrance: item.attractionLocations?.reducedMobilityEntrance ?? null,
      [locationKey]: point
    };

    return {
      ...item,
      attractionLocations: locations
    };
  }
}
