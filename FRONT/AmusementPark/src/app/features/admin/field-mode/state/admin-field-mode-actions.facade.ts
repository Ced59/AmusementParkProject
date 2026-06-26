import { Inject, Injectable, Signal, computed, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { ImageCategory } from '@app/models/images/image-category';
import { ImageDto } from '@app/models/images/image-dto';
import { ImageOwnerType } from '@app/models/images/image-owner-type';
import { ImageTagDto } from '@app/models/images/image-tag-dto';
import { UploadedImage } from '@app/models/images/uploaded-image';
import { AttractionLocationPoint } from '@app/models/parks/attraction-location-point';
import { AttractionLocations } from '@app/models/parks/attraction-locations';
import { ParkItem } from '@app/models/parks/park-item';
import { ImageUploadSecurityService } from '@shared/utils/security';

import {
  ADMIN_FIELD_MODE_GEOLOCATION_PORT,
  ADMIN_FIELD_MODE_IMAGES_API_SERVICE_PORT,
  ADMIN_FIELD_MODE_PARK_ITEMS_API_SERVICE_PORT,
  AdminFieldModeGeolocationPort,
  AdminFieldModeImagesApiServicePort,
  AdminFieldModeParkItemsApiServicePort
} from './admin-field-mode-data.ports';
import {
  ADMIN_FIELD_MODE_GPS_MAX_AGE_MS,
  ADMIN_FIELD_MODE_LOCATION_OPTIONS,
  ADMIN_FIELD_MODE_PHOTO_CATEGORY_OPTIONS,
  AdminFieldModeGpsStatus,
  AdminFieldModeLocationKey,
  AdminFieldModePhotoCategoryOption,
  AdminFieldModePosition
} from '../models/admin-field-mode.model';

@Injectable()
export class AdminFieldModeActionsFacade {
  private readonly statusSignal = signal<AdminFieldModeGpsStatus>('idle');
  private readonly statusMessageKeySignal = signal<string | null>(null);
  private readonly positionSignal = signal<AdminFieldModePosition | null>(null);
  private readonly fileSignal = signal<File | null>(null);
  private readonly photoCategorySlugSignal = signal(ADMIN_FIELD_MODE_PHOTO_CATEGORY_OPTIONS[0].slug);
  private readonly photoDescriptionSignal = signal('');
  private readonly locationKeySignal = signal<AdminFieldModeLocationKey>('general');
  private readonly busySignal = signal(false);
  private photoTagIdsBySlug: Record<string, string> = {};

  public readonly status: Signal<AdminFieldModeGpsStatus> = this.statusSignal.asReadonly();
  public readonly statusMessageKey: Signal<string | null> = this.statusMessageKeySignal.asReadonly();
  public readonly position: Signal<AdminFieldModePosition | null> = this.positionSignal.asReadonly();
  public readonly selectedFile: Signal<File | null> = this.fileSignal.asReadonly();
  public readonly photoCategorySlug: Signal<string> = this.photoCategorySlugSignal.asReadonly();
  public readonly photoDescription: Signal<string> = this.photoDescriptionSignal.asReadonly();
  public readonly locationKey: Signal<AdminFieldModeLocationKey> = this.locationKeySignal.asReadonly();
  public readonly busy: Signal<boolean> = this.busySignal.asReadonly();
  public readonly readyForPhoto: Signal<boolean> = computed(() => this.isPositionFresh(this.positionSignal()));
  public readonly photoCategoryOptions = signal(ADMIN_FIELD_MODE_PHOTO_CATEGORY_OPTIONS).asReadonly();

  constructor(
    @Inject(ADMIN_FIELD_MODE_IMAGES_API_SERVICE_PORT) private readonly imagesApiService: AdminFieldModeImagesApiServicePort,
    @Inject(ADMIN_FIELD_MODE_PARK_ITEMS_API_SERVICE_PORT) private readonly parkItemsApiService: AdminFieldModeParkItemsApiServicePort,
    @Inject(ADMIN_FIELD_MODE_GEOLOCATION_PORT) private readonly positionService: AdminFieldModeGeolocationPort,
    private readonly imageUploadSecurityService: ImageUploadSecurityService
  ) {
  }

  async refreshPosition(): Promise<void> {
    await this.captureFreshPosition();
  }

  selectFile(event: Event): void {
    const input: HTMLInputElement = event.target as HTMLInputElement;
    const file: File | null = input.files?.[0] ?? null;
    input.value = '';
    const validation = this.imageUploadSecurityService.validateImageFile(file);

    if (!validation.isValid || !file) {
      this.fileSignal.set(null);
      this.statusMessageKeySignal.set(validation.errorKey ?? 'admin.fieldMode.messages.invalidImage');
      return;
    }

    if (!this.readyForPhoto()) {
      this.fileSignal.set(null);
      this.statusMessageKeySignal.set('admin.fieldMode.messages.positionRequired');
      return;
    }

    this.fileSignal.set(file);
    this.statusMessageKeySignal.set(null);
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

  async addPhoto(item: ParkItem, shouldSetCurrent: boolean): Promise<boolean> {
    const file: File | null = this.fileSignal();
    if (!item.id || !file || this.busySignal()) {
      return false;
    }

    this.busySignal.set(true);
    try {
      const position: AdminFieldModePosition = await this.captureFreshPosition();
      await this.ensurePhotoCategoryTags();
      const description: string = this.photoDescriptionSignal().trim();
      const uploaded: UploadedImage = await firstValueFrom(this.imagesApiService.uploadImage(file, ImageCategory.PARK_ITEM, true, description || item.name));
      const linked: ImageDto = await firstValueFrom(this.imagesApiService.linkImage({
        imageId: uploaded.id,
        ownerType: ImageOwnerType.PARK_ITEM,
        ownerId: item.id,
        description: description || undefined,
        setAsCurrent: shouldSetCurrent
      }));
      await this.applyPhotoMetadata(linked, position, description);
      this.fileSignal.set(null);
      this.photoDescriptionSignal.set('');
      this.statusMessageKeySignal.set('admin.fieldMode.messages.photoAdded');
      return true;
    } catch (error: unknown) {
      console.error('Error adding field mode photo', error);
      this.statusMessageKeySignal.set('admin.fieldMode.messages.photoFailed');
      return false;
    } finally {
      this.busySignal.set(false);
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
      this.statusMessageKeySignal.set('admin.fieldMode.messages.locationFailed');
      return null;
    } finally {
      this.busySignal.set(false);
    }
  }

  private async captureFreshPosition(): Promise<AdminFieldModePosition> {
    this.statusSignal.set('checking');
    try {
      const position: GeolocationPosition = await this.positionService.getCurrentPosition({ enableHighAccuracy: true, maximumAge: 0, timeout: 15000 });
      const value: AdminFieldModePosition = {
        latitude: position.coords.latitude,
        longitude: position.coords.longitude,
        accuracy: Number.isFinite(position.coords.accuracy) ? position.coords.accuracy : null,
        capturedAt: Date.now()
      };
      this.positionSignal.set(value);
      this.statusSignal.set('ready');
      this.statusMessageKeySignal.set(null);
      return value;
    } catch (error: unknown) {
      this.statusSignal.set('error');
      this.statusMessageKeySignal.set('admin.fieldMode.messages.positionUnavailable');
      throw error;
    }
  }

  private isPositionFresh(position: AdminFieldModePosition | null): boolean {
    return !!position && Date.now() - position.capturedAt <= ADMIN_FIELD_MODE_GPS_MAX_AGE_MS;
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

  private async applyPhotoMetadata(image: ImageDto, position: AdminFieldModePosition, description: string): Promise<ImageDto> {
    const selectedTagId: string | undefined = this.photoTagIdsBySlug[this.photoCategorySlugSignal()];
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
