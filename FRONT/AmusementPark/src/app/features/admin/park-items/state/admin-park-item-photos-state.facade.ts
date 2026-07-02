import {
  Injectable,
  Signal,
  computed,
  signal,
  DestroyRef,
  Inject,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { firstValueFrom } from 'rxjs';
import { PaginatorState } from '@shared/primeless/paginator';
import { TranslateService } from '@ngx-translate/core';

import { ToastMessageService } from '@app/services/messages/toast-message.service';
import { UploadedImage } from '@app/models/images/uploaded-image';
import { ImageGeoLocation } from '@app/models/images/image-geo-location';
import { ImageCategory } from '@app/models/images/image-category';
import { ImageDto } from '@app/models/images/image-dto';
import { ImageOwnerType } from '@app/models/images/image-owner-type';
import { ImageTagDto } from '@app/models/images/image-tag-dto';
import { OwnedImageItem } from '@shared/models/images/owned-image-item.model';
import { mapImageDtoToOwnedImageItem } from '@shared/utils/images/owned-image-item.mapper';
import { PhotoGpsMetadataService, PhotoGpsPosition } from '@shared/utils/images/photo-gps-metadata.service';
import { ImageUploadSecurityService } from '@shared/utils/security';
import {
  AdminParkItemPhotoCategoryOption,
  AdminParkItemPhotoUploadPreview,
  AdminParkItemPhotoUploadProgress,
  PARK_ITEM_PHOTO_CATEGORY_OPTIONS
} from '../models/admin-park-item-edit.model';

import {
  ADMIN_PARK_ITEM_PHOTOS_STATE_IMAGES_API_SERVICE_PORT,
  AdminParkItemPhotosStateImagesApiServicePort
} from './admin-park-item-photos-state-data.ports';

interface AdminParkItemPhotoFileSelection extends AdminParkItemPhotoUploadPreview {
  file: File;
  geoLocation: ImageGeoLocation | null;
}

@Injectable()
export class AdminParkItemPhotosStateFacade {
  private static nextPhotoSelectionId: number = 0;
  private static readonly concurrentUploadLimit: number = 2;

  private readonly currentLanguageSignal = signal('en');
  private readonly attractionPhotosSignal = signal<OwnedImageItem[]>([]);
  private readonly currentPhotoSignal = signal<OwnedImageItem | null>(null);
  private readonly photosLoadingSignal = signal(false);
  private readonly photosUploadingSignal = signal(false);
  private readonly photosPageSignal = signal(0);
  private readonly photosPageSizeSignal = signal(8);
  private readonly selectedPhotoSelectionsSignal = signal<AdminParkItemPhotoFileSelection[]>([]);
  private readonly newPhotoDescriptionSignal = signal('');
  private readonly remotePhotoSourceUrlSignal = signal('');
  private readonly photoWithWatermarkSignal = signal(true);
  private readonly remotePhotoWithWatermarkSignal = signal(false);
  private readonly selectedPhotoCategorySlugSignal = signal(PARK_ITEM_PHOTO_CATEGORY_OPTIONS[0].slug);
  private readonly photoTagIdsBySlugSignal = signal<Record<string, string>>({});
  private readonly photoUploadProgressSignal = signal<AdminParkItemPhotoUploadProgress | null>(null);

  public readonly attractionPhotos: Signal<OwnedImageItem[]> = this.attractionPhotosSignal.asReadonly();
  public readonly currentPhoto: Signal<OwnedImageItem | null> = this.currentPhotoSignal.asReadonly();
  public readonly photosLoading: Signal<boolean> = this.photosLoadingSignal.asReadonly();
  public readonly photosUploading: Signal<boolean> = this.photosUploadingSignal.asReadonly();
  public readonly photosPageSize: Signal<number> = this.photosPageSizeSignal.asReadonly();
  public readonly newPhotoDescription: Signal<string> = this.newPhotoDescriptionSignal.asReadonly();
  public readonly remotePhotoSourceUrl: Signal<string> = this.remotePhotoSourceUrlSignal.asReadonly();
  public readonly photoWithWatermark: Signal<boolean> = this.photoWithWatermarkSignal.asReadonly();
  public readonly remotePhotoWithWatermark: Signal<boolean> = this.remotePhotoWithWatermarkSignal.asReadonly();
  public readonly selectedPhotoCategorySlug: Signal<string> = this.selectedPhotoCategorySlugSignal.asReadonly();
  public readonly photoCategoryOptions: Signal<AdminParkItemPhotoCategoryOption[]> = signal([...PARK_ITEM_PHOTO_CATEGORY_OPTIONS]).asReadonly();
  public readonly selectedPhotoCount: Signal<number> = computed(() => this.selectedPhotoSelectionsSignal().length);
  public readonly selectedPhotoPreviews: Signal<AdminParkItemPhotoUploadPreview[]> = computed(() => {
    return this.selectedPhotoSelectionsSignal().map((selection: AdminParkItemPhotoFileSelection) => ({
      id: selection.id,
      fileName: selection.fileName,
      sizeInBytes: selection.sizeInBytes,
      previewUrl: selection.previewUrl,
      isAnalyzing: selection.isAnalyzing,
      hasGeoLocation: selection.hasGeoLocation,
      latitude: selection.latitude,
      longitude: selection.longitude
    }));
  });
  public readonly selectedPhotoAnalysisPending: Signal<boolean> = computed(() => {
    return this.selectedPhotoSelectionsSignal().some((selection: AdminParkItemPhotoFileSelection) => selection.isAnalyzing);
  });
  public readonly selectedPhotoMissingGeoLocationCount: Signal<number> = computed(() => {
    return this.selectedPhotoSelectionsSignal().filter((selection: AdminParkItemPhotoFileSelection) => selection.hasGeoLocation === false).length;
  });
  public readonly photoUploadProgress: Signal<AdminParkItemPhotoUploadProgress | null> = this.photoUploadProgressSignal.asReadonly();
  public readonly photoCategoryLabelKeyByTagId: Signal<Record<string, string>> = computed(() => {
    const idsBySlug: Record<string, string> = this.photoTagIdsBySlugSignal();
    const result: Record<string, string> = {};

    for (const option of PARK_ITEM_PHOTO_CATEGORY_OPTIONS) {
      result[option.slug] = option.labelKey;

      const tagId: string | undefined = idsBySlug[option.slug];
      if (tagId) {
        result[tagId] = option.labelKey;
      }
    }

    return result;
  });
  public readonly pagedPhotos: Signal<OwnedImageItem[]> = computed(() => {
    const start: number = this.photosPageSignal() * this.photosPageSizeSignal();
    return this.attractionPhotosSignal().slice(start, start + this.photosPageSizeSignal());
  });

  constructor(
    @Inject(ADMIN_PARK_ITEM_PHOTOS_STATE_IMAGES_API_SERVICE_PORT) private readonly imagesApiService: AdminParkItemPhotosStateImagesApiServicePort,
    private readonly translateService: TranslateService,
    private readonly toastMessageService: ToastMessageService,
    private readonly imageUploadSecurityService: ImageUploadSecurityService,
    private readonly photoGpsMetadataService: PhotoGpsMetadataService,
    private readonly destroyRef: DestroyRef
  ) {
  }

  setCurrentLanguage(languageCode: string): void {
    this.currentLanguageSignal.set(languageCode);
  }

  reset(): void {
    this.attractionPhotosSignal.set([]);
    this.currentPhotoSignal.set(null);
    this.photosLoadingSignal.set(false);
    this.photosUploadingSignal.set(false);
    this.photosPageSignal.set(0);
    this.photosPageSizeSignal.set(8);
    this.clearSelectedPhotoSelection();
    this.newPhotoDescriptionSignal.set('');
    this.remotePhotoSourceUrlSignal.set('');
    this.photoWithWatermarkSignal.set(true);
    this.remotePhotoWithWatermarkSignal.set(false);
    this.selectedPhotoCategorySlugSignal.set(PARK_ITEM_PHOTO_CATEGORY_OPTIONS[0].slug);
    this.photoTagIdsBySlugSignal.set({});
    this.photoUploadProgressSignal.set(null);
  }

  loadPhotos(itemId: string): void {
    this.photosLoadingSignal.set(true);
    this.ensurePhotoCategoryTags();

    this.imagesApiService.getImages(ImageOwnerType.PARK_ITEM, itemId, ImageCategory.PARK_ITEM).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (images: ImageDto[]) => {
        const photoItems: OwnedImageItem[] = images.map((image: ImageDto) => this.toOwnedImageItem(image));
        this.attractionPhotosSignal.set(photoItems);
        this.currentPhotoSignal.set(photoItems.find((item: OwnedImageItem) => item.isCurrent) ?? null);
        this.photosPageSignal.set(0);
        this.photosLoadingSignal.set(false);
      },
      error: (error: unknown) => {
        console.error('Error loading attraction photos', error);
        this.photosLoadingSignal.set(false);
      }
    });
  }

  selectPhotoFiles(event: Event): void {
    const inputElement: HTMLInputElement = event.target as HTMLInputElement;

    if (!inputElement.files || inputElement.files.length === 0) {
      this.clearSelectedPhotoSelection();
      return;
    }

    const files: File[] = Array.from(inputElement.files);
    const validFiles: File[] = this.imageUploadSecurityService.filterValidImageFiles(files);

    if (validFiles.length !== files.length) {
      this.toastMessageService.add(
        'warn',
        this.translateService.instant('common.security.uploadRejectedSummary'),
        this.translateService.instant('common.security.invalidImageUploadMessage')
      );
    }

    this.setSelectedPhotoFiles(validFiles);
    inputElement.value = '';
  }

  removeSelectedPhoto(selectionId: string): void {
    const selection: AdminParkItemPhotoFileSelection | undefined =
      this.selectedPhotoSelectionsSignal().find((item: AdminParkItemPhotoFileSelection) => item.id === selectionId);

    if (!selection) {
      return;
    }

    URL.revokeObjectURL(selection.previewUrl);
    this.selectedPhotoSelectionsSignal.set(
      this.selectedPhotoSelectionsSignal().filter((item: AdminParkItemPhotoFileSelection) => item.id !== selectionId)
    );
  }

  setNewPhotoDescription(description: string): void {
    this.newPhotoDescriptionSignal.set(description);
  }

  setRemotePhotoSourceUrl(sourceUrl: string): void {
    this.remotePhotoSourceUrlSignal.set(sourceUrl);
  }

  setPhotoWithWatermark(withWatermark: boolean): void {
    this.photoWithWatermarkSignal.set(withWatermark);
  }

  setRemotePhotoWithWatermark(withWatermark: boolean): void {
    this.remotePhotoWithWatermarkSignal.set(withWatermark);
  }

  setSelectedPhotoCategorySlug(slug: string): void {
    const isKnownSlug: boolean = PARK_ITEM_PHOTO_CATEGORY_OPTIONS.some((option: AdminParkItemPhotoCategoryOption) => option.slug === slug);
    this.selectedPhotoCategorySlugSignal.set(isKnownSlug ? slug : PARK_ITEM_PHOTO_CATEGORY_OPTIONS[0].slug);
  }

  onPhotosPageChange(event: PaginatorState): void {
    this.photosPageSignal.set(event.page ?? 0);
    this.photosPageSizeSignal.set(event.rows ?? this.photosPageSizeSignal());
  }

  async uploadSelectedPhotos(itemId: string, itemName: string): Promise<void> {
    const selections: AdminParkItemPhotoFileSelection[] = [...this.selectedPhotoSelectionsSignal()];
    if (selections.length === 0 || this.photosUploadingSignal() || this.selectedPhotoAnalysisPending()) {
      return;
    }

    this.photosUploadingSignal.set(true);
    await this.ensurePhotoCategoryTagsAsync();
    const hadNoPhotoInitially: boolean = this.attractionPhotosSignal().length === 0;
    let uploadedCount: number = 0;
    let failedCount: number = 0;
    let completedCount: number = 0;
    let nextIndex: number = 0;
    this.photoUploadProgressSignal.set({ completed: 0, total: selections.length });

    try {
      const uploadWorker = async (): Promise<void> => {
        while (nextIndex < selections.length) {
          const selectionIndex: number = nextIndex;
          nextIndex++;

          const shouldSetCurrent: boolean = hadNoPhotoInitially && selectionIndex === 0;
          try {
            await this.uploadPhotoAsync(selections[selectionIndex], itemId, itemName, shouldSetCurrent);
            uploadedCount++;
          } catch (error: unknown) {
            failedCount++;
            console.error('Error uploading attraction image', error);
          } finally {
            completedCount++;
            this.photoUploadProgressSignal.set({ completed: completedCount, total: selections.length });
          }
        }
      };

      const workers: Promise<void>[] = Array.from(
        { length: Math.min(AdminParkItemPhotosStateFacade.concurrentUploadLimit, selections.length) },
        () => uploadWorker()
      );
      await Promise.all(workers);

      this.clearSelectedPhotoSelection();
      this.remotePhotoSourceUrlSignal.set('');
      this.newPhotoDescriptionSignal.set('');
      this.photoWithWatermarkSignal.set(true);
      this.remotePhotoWithWatermarkSignal.set(false);
      this.selectedPhotoCategorySlugSignal.set(PARK_ITEM_PHOTO_CATEGORY_OPTIONS[0].slug);
      this.showUploadResultMessage(uploadedCount, failedCount);
    } catch (error: unknown) {
      console.error('Error uploading attraction images', error);
      this.toastMessageService.add(
        'error',
        this.translateService.instant('admin.parks.items.saveMessages.errorSummary'),
        this.translateService.instant('admin.parks.items.photos.uploadError', { count: uploadedCount })
      );
    } finally {
      this.photosUploadingSignal.set(false);
      this.photoUploadProgressSignal.set(null);
    }
  }

  async importRemotePhoto(itemId: string, itemName: string): Promise<void> {
    const sourceUrl: string = this.remotePhotoSourceUrlSignal().trim();
    if (!sourceUrl || this.photosUploadingSignal()) {
      return;
    }

    this.photosUploadingSignal.set(true);
    await this.ensurePhotoCategoryTagsAsync();
    const shouldSetCurrent: boolean = this.attractionPhotosSignal().length === 0;

    try {
      const importedImage: ImageDto = await firstValueFrom(this.imagesApiService.importRemoteImage({
        sourceUrl,
        category: ImageCategory.PARK_ITEM,
        ownerType: ImageOwnerType.PARK_ITEM,
        ownerId: itemId,
        description: this.newPhotoDescriptionSignal() || itemName || undefined,
        withWatermark: this.remotePhotoWithWatermarkSignal(),
        setAsCurrent: shouldSetCurrent
      }));
      const taggedImage: ImageDto = await this.applySelectedPhotoCategoryAsync(importedImage);
      this.upsertPhoto(this.toOwnedImageItem(taggedImage));
      this.remotePhotoSourceUrlSignal.set('');
      this.newPhotoDescriptionSignal.set('');
      this.photoWithWatermarkSignal.set(true);
      this.remotePhotoWithWatermarkSignal.set(false);
      this.selectedPhotoCategorySlugSignal.set(PARK_ITEM_PHOTO_CATEGORY_OPTIONS[0].slug);
      this.toastMessageService.add(
        'success',
        this.translateService.instant('admin.parks.items.saveMessages.successSummary'),
        this.translateService.instant('admin.parks.items.photos.uploadSuccess', { count: 1 })
      );
    } catch (error: unknown) {
      console.error('Error importing remote attraction image', error);
      this.toastMessageService.add(
        'error',
        this.translateService.instant('admin.parks.items.saveMessages.errorSummary'),
        this.translateService.instant('admin.parks.items.photos.uploadError', { count: 0 })
      );
    } finally {
      this.photosUploadingSignal.set(false);
    }
  }

  setCurrentPhoto(photo: OwnedImageItem): void {
    if (photo.isCurrent) {
      return;
    }

    this.imagesApiService.setCurrentImage(photo.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (image: ImageDto) => {
        const updatedPhoto: OwnedImageItem = this.toOwnedImageItem(image);
        const updatedItems: OwnedImageItem[] = this.attractionPhotosSignal().map((item: OwnedImageItem) => ({
          ...item,
          isCurrent: item.id === updatedPhoto.id
        }));

        this.attractionPhotosSignal.set(updatedItems);
        this.currentPhotoSignal.set(updatedPhoto);
        this.toastMessageService.add(
          'success',
          this.translateService.instant('admin.parks.items.saveMessages.successSummary'),
          this.translateService.instant('admin.parks.items.photos.currentSetSuccess')
        );
      },
      error: (error: unknown) => {
        console.error('Error setting current attraction image', error);
      }
    });
  }

  deletePhoto(photo: OwnedImageItem): void {
    this.imagesApiService.deleteImage(photo.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        const updatedItems: OwnedImageItem[] = this.attractionPhotosSignal().filter((item: OwnedImageItem) => item.id !== photo.id);
        this.attractionPhotosSignal.set(updatedItems);
        this.currentPhotoSignal.set(updatedItems.find((item: OwnedImageItem) => item.isCurrent) ?? null);
        this.toastMessageService.add(
          'success',
          this.translateService.instant('admin.parks.items.saveMessages.successSummary'),
          this.translateService.instant('admin.parks.items.photos.deleteSuccess')
        );
      },
      error: (error: unknown) => {
        console.error('Error deleting attraction image', error);
      }
    });
  }

  private setSelectedPhotoFiles(files: File[]): void {
    this.clearSelectedPhotoSelection();

    const selections: AdminParkItemPhotoFileSelection[] = files.map((file: File) => ({
      id: `park-item-photo-${AdminParkItemPhotosStateFacade.nextPhotoSelectionId++}`,
      file,
      fileName: file.name,
      sizeInBytes: file.size,
      previewUrl: URL.createObjectURL(file),
      isAnalyzing: true,
      hasGeoLocation: null,
      latitude: null,
      longitude: null,
      geoLocation: null
    }));

    this.selectedPhotoSelectionsSignal.set(selections);
    void this.analyzeSelectedPhotoFilesAsync(selections);
  }

  private async analyzeSelectedPhotoFilesAsync(selections: AdminParkItemPhotoFileSelection[]): Promise<void> {
    for (const selection of selections) {
      try {
        const position: PhotoGpsPosition | null = await this.photoGpsMetadataService.readPosition(selection.file);
        this.updateSelectedPhotoSelection(selection.id, {
          isAnalyzing: false,
          hasGeoLocation: !!position,
          latitude: position?.latitude ?? null,
          longitude: position?.longitude ?? null,
          geoLocation: position ? { latitude: position.latitude, longitude: position.longitude } : null
        });
      } catch (error: unknown) {
        console.error('Error reading photo GPS metadata', error);
        this.updateSelectedPhotoSelection(selection.id, {
          isAnalyzing: false,
          hasGeoLocation: false,
          latitude: null,
          longitude: null,
          geoLocation: null
        });
      }
    }
  }

  private updateSelectedPhotoSelection(selectionId: string, patch: Partial<AdminParkItemPhotoFileSelection>): void {
    const selections: AdminParkItemPhotoFileSelection[] = this.selectedPhotoSelectionsSignal();
    if (!selections.some((selection: AdminParkItemPhotoFileSelection) => selection.id === selectionId)) {
      return;
    }

    this.selectedPhotoSelectionsSignal.set(
      selections.map((selection: AdminParkItemPhotoFileSelection) => selection.id === selectionId ? { ...selection, ...patch } : selection)
    );
  }

  private clearSelectedPhotoSelection(): void {
    for (const selection of this.selectedPhotoSelectionsSignal()) {
      URL.revokeObjectURL(selection.previewUrl);
    }

    this.selectedPhotoSelectionsSignal.set([]);
  }

  private ensurePhotoCategoryTags(): void {
    void this.ensurePhotoCategoryTagsAsync();
  }

  private async ensurePhotoCategoryTagsAsync(): Promise<void> {
    try {
      const tags: ImageTagDto[] = await firstValueFrom(this.imagesApiService.getAdminImageTags());
      await this.ensureMissingPhotoCategoryTagsAsync(tags);
    } catch (error: unknown) {
      console.error('Error loading image tags', error);
    }
  }

  private async ensureMissingPhotoCategoryTagsAsync(existingTags: ImageTagDto[]): Promise<void> {
    const idsBySlug: Record<string, string> = {};

    for (const option of PARK_ITEM_PHOTO_CATEGORY_OPTIONS) {
      const existingTag: ImageTagDto | undefined = existingTags.find((tag: ImageTagDto) => tag.slug === option.slug);

      if (existingTag) {
        idsBySlug[option.slug] = existingTag.id;
        continue;
      }

      const createdTag: ImageTagDto = await firstValueFrom(this.imagesApiService.createAdminImageTag({
        slug: option.slug,
        labels: [
          { languageCode: 'fr', value: option.labelFr },
          { languageCode: 'en', value: option.labelEn }
        ],
        descriptions: []
      }));

      idsBySlug[option.slug] = createdTag.id;
    }

    this.photoTagIdsBySlugSignal.set(idsBySlug);
  }

  private async applySelectedPhotoCategoryAsync(image: ImageDto, uploadedGeoLocation: ImageGeoLocation | null = null): Promise<ImageDto> {
    const selectedTagId: string | undefined = this.photoTagIdsBySlugSignal()[this.selectedPhotoCategorySlugSignal()];
    const geoLocation: ImageGeoLocation | null = image.geoLocation ?? uploadedGeoLocation;

    if (!selectedTagId && !uploadedGeoLocation) {
      return image;
    }

    return firstValueFrom(this.imagesApiService.updateAdminImage(image.id, {
      description: image.description,
      geoLocation,
      altTexts: image.altTexts ?? [],
      captions: image.captions ?? [],
      credits: image.credits ?? [],
      tagIds: selectedTagId ? [...new Set([...(image.tagIds ?? []), selectedTagId])] : image.tagIds ?? [],
      isPublished: image.isPublished !== false,
      sourceUrl: image.sourceUrl ?? null
    }));
  }

  private async uploadPhotoAsync(selection: AdminParkItemPhotoFileSelection, itemId: string, itemName: string, setAsCurrent: boolean): Promise<void> {
    const uploadedImage: UploadedImage = await firstValueFrom(
      this.imagesApiService.uploadImage(
        selection.file,
        ImageCategory.PARK_ITEM,
        this.photoWithWatermarkSignal(),
        itemName
      )
    );
    const uploadedGeoLocation: ImageGeoLocation | null = this.resolveUploadedGeoLocation(uploadedImage) ?? selection.geoLocation;
    if (!uploadedGeoLocation) {
      this.showMissingGeoWarning();
    }

    const linkedImage: ImageDto = await firstValueFrom(
      this.imagesApiService.linkImage({
        imageId: uploadedImage.id,
        ownerType: ImageOwnerType.PARK_ITEM,
        ownerId: itemId,
        description: this.newPhotoDescriptionSignal() || undefined,
        setAsCurrent
      })
    );

    const taggedImage: ImageDto = await this.applySelectedPhotoCategoryAsync(linkedImage, uploadedGeoLocation);
    this.upsertPhoto(this.toOwnedImageItem(taggedImage));
  }

  private showUploadResultMessage(uploadedCount: number, failedCount: number): void {
    if (failedCount === 0) {
      this.toastMessageService.add(
        'success',
        this.translateService.instant('admin.parks.items.saveMessages.successSummary'),
        this.translateService.instant('admin.parks.items.photos.uploadSuccess', { count: uploadedCount })
      );
      return;
    }

    this.toastMessageService.add(
      uploadedCount > 0 ? 'warn' : 'error',
      this.translateService.instant('admin.parks.items.saveMessages.errorSummary'),
      this.translateService.instant('admin.parks.items.photos.uploadError', { count: uploadedCount })
    );
  }

  private resolveUploadedGeoLocation(uploadedImage: UploadedImage): ImageGeoLocation | null {
    if (uploadedImage.latitude === null || uploadedImage.latitude === undefined || uploadedImage.longitude === null || uploadedImage.longitude === undefined) {
      return null;
    }

    const latitude: number = Number(uploadedImage.latitude);
    const longitude: number = Number(uploadedImage.longitude);
    return Number.isFinite(latitude) && Number.isFinite(longitude)
      ? { latitude, longitude }
      : null;
  }

  private showMissingGeoWarning(): void {
    this.toastMessageService.add(
      'warn',
      this.translateService.instant('common.warning'),
      this.translateService.instant('admin.contextualBlocks.drawer.photoMetadataGeoMissing')
    );
  }

  private upsertPhoto(item: OwnedImageItem): void {
    const normalizedItems: OwnedImageItem[] = item.isCurrent
      ? this.attractionPhotosSignal().map((photo: OwnedImageItem) => ({
        ...photo,
        isCurrent: photo.id === item.id
      }))
      : [...this.attractionPhotosSignal()];
    const existingIndex: number = normalizedItems.findIndex((photo: OwnedImageItem) => photo.id === item.id);

    if (existingIndex >= 0) {
      normalizedItems[existingIndex] = item;
    } else {
      normalizedItems.unshift(item);
    }

    this.attractionPhotosSignal.set(normalizedItems);
    this.currentPhotoSignal.set(normalizedItems.find((photo: OwnedImageItem) => photo.isCurrent) ?? item);
    this.photosPageSignal.set(0);
  }

  private toOwnedImageItem(image: ImageDto): OwnedImageItem {
    return mapImageDtoToOwnedImageItem(image, this.currentLanguageSignal());
  }
}
