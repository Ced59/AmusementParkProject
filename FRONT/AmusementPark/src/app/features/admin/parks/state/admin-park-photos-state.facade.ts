import {
  DestroyRef,
  Injectable,
  Signal,
  computed,
  signal,
  Inject,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { firstValueFrom } from 'rxjs';
import { PaginatorState } from 'primeng/paginator';
import { TranslateService } from '@ngx-translate/core';

import { ImageCategory } from '@app/models/images/image-category';
import { ImageDto } from '@app/models/images/image-dto';
import { ImageOwnerType } from '@app/models/images/image-owner-type';
import { ImageTagDto } from '@app/models/images/image-tag-dto';
import { UploadedImage } from '@app/models/images/uploaded-image';
import { ToastMessageService } from '@app/services/messages/toast-message.service';
import { OwnedImageItem } from '@shared/models/images/owned-image-item.model';
import { mapImageDtoToOwnedImageItem } from '@shared/utils/images/owned-image-item.mapper';
import { ImageUploadSecurityService } from '@shared/utils/security';
import { AdminParkPhotoCategoryOption, PARK_PHOTO_CATEGORY_OPTIONS } from '../models/admin-park-edit.model';

import {
  ADMIN_PARK_PHOTOS_STATE_IMAGES_API_SERVICE_PORT,
  AdminParkPhotosStateImagesApiServicePort
} from './admin-park-photos-state-data.ports';
@Injectable()
export class AdminParkPhotosStateFacade {
  private readonly currentLanguageSignal = signal('en');
  private readonly parkPhotosSignal = signal<OwnedImageItem[]>([]);
  private readonly currentPhotoSignal = signal<OwnedImageItem | null>(null);
  private readonly photosLoadingSignal = signal(false);
  private readonly photosUploadingSignal = signal(false);
  private readonly photosPageSignal = signal(0);
  private readonly photosPageSizeSignal = signal(8);
  private readonly selectedPhotoFilesSignal = signal<File[]>([]);
  private readonly newPhotoDescriptionSignal = signal('');
  private readonly remotePhotoSourceUrlSignal = signal('');
  private readonly photoWithWatermarkSignal = signal(true);
  private readonly remotePhotoWithWatermarkSignal = signal(false);
  private readonly selectedPhotoCategorySlugSignal = signal(PARK_PHOTO_CATEGORY_OPTIONS[0].slug);
  private readonly photoTagIdsBySlugSignal = signal<Record<string, string>>({});

  public readonly parkPhotos: Signal<OwnedImageItem[]> = this.parkPhotosSignal.asReadonly();
  public readonly currentPhoto: Signal<OwnedImageItem | null> = this.currentPhotoSignal.asReadonly();
  public readonly photosLoading: Signal<boolean> = this.photosLoadingSignal.asReadonly();
  public readonly photosUploading: Signal<boolean> = this.photosUploadingSignal.asReadonly();
  public readonly photosPageSize: Signal<number> = this.photosPageSizeSignal.asReadonly();
  public readonly newPhotoDescription: Signal<string> = this.newPhotoDescriptionSignal.asReadonly();
  public readonly remotePhotoSourceUrl: Signal<string> = this.remotePhotoSourceUrlSignal.asReadonly();
  public readonly photoWithWatermark: Signal<boolean> = this.photoWithWatermarkSignal.asReadonly();
  public readonly remotePhotoWithWatermark: Signal<boolean> = this.remotePhotoWithWatermarkSignal.asReadonly();
  public readonly selectedPhotoCategorySlug: Signal<string> = this.selectedPhotoCategorySlugSignal.asReadonly();
  public readonly photoCategoryOptions: Signal<AdminParkPhotoCategoryOption[]> = signal([...PARK_PHOTO_CATEGORY_OPTIONS]).asReadonly();
  public readonly selectedPhotoCount: Signal<number> = computed(() => this.selectedPhotoFilesSignal().length);
  public readonly pagedPhotos: Signal<OwnedImageItem[]> = computed(() => {
    const start: number = this.photosPageSignal() * this.photosPageSizeSignal();
    return this.parkPhotosSignal().slice(start, start + this.photosPageSizeSignal());
  });

  constructor(
    @Inject(ADMIN_PARK_PHOTOS_STATE_IMAGES_API_SERVICE_PORT) private readonly imagesApiService: AdminParkPhotosStateImagesApiServicePort,
    private readonly translateService: TranslateService,
    private readonly toastMessageService: ToastMessageService,
    private readonly imageUploadSecurityService: ImageUploadSecurityService,
    private readonly destroyRef: DestroyRef
  ) {
  }

  setCurrentLanguage(languageCode: string): void {
    this.currentLanguageSignal.set(languageCode);
  }

  reset(): void {
    this.parkPhotosSignal.set([]);
    this.currentPhotoSignal.set(null);
    this.photosLoadingSignal.set(false);
    this.photosUploadingSignal.set(false);
    this.photosPageSignal.set(0);
    this.photosPageSizeSignal.set(8);
    this.selectedPhotoFilesSignal.set([]);
    this.newPhotoDescriptionSignal.set('');
    this.remotePhotoSourceUrlSignal.set('');
    this.photoWithWatermarkSignal.set(true);
    this.remotePhotoWithWatermarkSignal.set(false);
    this.selectedPhotoCategorySlugSignal.set(PARK_PHOTO_CATEGORY_OPTIONS[0].slug);
    this.photoTagIdsBySlugSignal.set({});
  }

  loadPhotos(parkId: string): void {
    this.photosLoadingSignal.set(true);
    this.ensurePhotoCategoryTags();

    this.imagesApiService.getImages(ImageOwnerType.PARK, parkId, ImageCategory.PARK).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (images: ImageDto[]) => {
        const photoItems: OwnedImageItem[] = images.map((image: ImageDto) => this.toOwnedImageItem(image));
        this.parkPhotosSignal.set(photoItems);
        this.currentPhotoSignal.set(photoItems.find((item: OwnedImageItem) => item.isCurrent) ?? null);
        this.photosPageSignal.set(0);
        this.photosLoadingSignal.set(false);
      },
      error: (error: unknown) => {
        console.error('Error loading park photos', error);
        this.photosLoadingSignal.set(false);
      }
    });
  }

  selectPhotoFiles(event: Event): void {
    const inputElement: HTMLInputElement = event.target as HTMLInputElement;

    if (!inputElement.files || inputElement.files.length === 0) {
      this.selectedPhotoFilesSignal.set([]);
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

    this.selectedPhotoFilesSignal.set(validFiles);
    inputElement.value = '';
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
    const isKnownSlug: boolean = PARK_PHOTO_CATEGORY_OPTIONS.some((option: AdminParkPhotoCategoryOption) => option.slug === slug);
    this.selectedPhotoCategorySlugSignal.set(isKnownSlug ? slug : PARK_PHOTO_CATEGORY_OPTIONS[0].slug);
  }

  onPhotosPageChange(event: PaginatorState): void {
    this.photosPageSignal.set(event.page ?? 0);
    this.photosPageSizeSignal.set(event.rows ?? this.photosPageSizeSignal());
  }

  async uploadSelectedPhotos(parkId: string, parkName: string): Promise<void> {
    if (this.selectedPhotoFilesSignal().length === 0 || this.photosUploadingSignal()) {
      return;
    }

    this.photosUploadingSignal.set(true);
    await this.ensurePhotoCategoryTagsAsync();
    const files: File[] = [...this.selectedPhotoFilesSignal()];
    const hadNoPhotoInitially: boolean = this.parkPhotosSignal().length === 0;
    let uploadedCount: number = 0;

    try {
      for (let index: number = 0; index < files.length; index++) {
        const shouldSetCurrent: boolean = hadNoPhotoInitially && index === 0;
        await this.uploadPhotoAsync(files[index], parkId, parkName, shouldSetCurrent);
        uploadedCount++;
      }

      this.selectedPhotoFilesSignal.set([]);
      this.remotePhotoSourceUrlSignal.set('');
      this.newPhotoDescriptionSignal.set('');
      this.photoWithWatermarkSignal.set(true);
      this.remotePhotoWithWatermarkSignal.set(false);
      this.selectedPhotoCategorySlugSignal.set(PARK_PHOTO_CATEGORY_OPTIONS[0].slug);
      this.toastMessageService.add(
        'success',
        this.translateService.instant('admin.parks.saveMessages.successSummary'),
        this.translateService.instant('admin.parks.photos.uploadSuccess', { count: uploadedCount })
      );
    } catch (error: unknown) {
      console.error('Error uploading park images', error);
      this.toastMessageService.add(
        'error',
        this.translateService.instant('admin.parks.saveMessages.errorSummary'),
        this.translateService.instant('admin.parks.photos.uploadError', { count: uploadedCount })
      );
    } finally {
      this.photosUploadingSignal.set(false);
    }
  }

  async importRemotePhoto(parkId: string, parkName: string): Promise<void> {
    const sourceUrl: string = this.remotePhotoSourceUrlSignal().trim();
    if (!sourceUrl || this.photosUploadingSignal()) {
      return;
    }

    this.photosUploadingSignal.set(true);
    await this.ensurePhotoCategoryTagsAsync();
    const shouldSetCurrent: boolean = this.parkPhotosSignal().length === 0;

    try {
      const importedImage: ImageDto = await firstValueFrom(this.imagesApiService.importRemoteImage({
        sourceUrl,
        category: ImageCategory.PARK,
        ownerType: ImageOwnerType.PARK,
        ownerId: parkId,
        description: this.newPhotoDescriptionSignal() || parkName || undefined,
        withWatermark: this.remotePhotoWithWatermarkSignal(),
        setAsCurrent: shouldSetCurrent
      }));
      const taggedImage: ImageDto = await this.applySelectedPhotoCategoryAsync(importedImage);
      this.upsertPhoto(this.toOwnedImageItem(taggedImage));
      this.remotePhotoSourceUrlSignal.set('');
      this.newPhotoDescriptionSignal.set('');
      this.photoWithWatermarkSignal.set(true);
      this.remotePhotoWithWatermarkSignal.set(false);
      this.selectedPhotoCategorySlugSignal.set(PARK_PHOTO_CATEGORY_OPTIONS[0].slug);
      this.toastMessageService.add(
        'success',
        this.translateService.instant('admin.parks.saveMessages.successSummary'),
        this.translateService.instant('admin.parks.photos.uploadSuccess', { count: 1 })
      );
    } catch (error: unknown) {
      console.error('Error importing remote park image', error);
      this.toastMessageService.add(
        'error',
        this.translateService.instant('admin.parks.saveMessages.errorSummary'),
        this.translateService.instant('admin.parks.photos.uploadError', { count: 0 })
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
        const updatedItems: OwnedImageItem[] = this.parkPhotosSignal().map((item: OwnedImageItem) => ({
          ...item,
          isCurrent: item.id === updatedPhoto.id
        }));

        this.parkPhotosSignal.set(updatedItems);
        this.currentPhotoSignal.set(updatedPhoto);
        this.toastMessageService.add(
          'success',
          this.translateService.instant('admin.parks.saveMessages.successSummary'),
          this.translateService.instant('admin.parks.photos.currentSetSuccess')
        );
      },
      error: (error: unknown) => {
        console.error('Error setting current park photo', error);
      }
    });
  }

  deletePhoto(photo: OwnedImageItem): void {
    this.imagesApiService.deleteImage(photo.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        const updatedItems: OwnedImageItem[] = this.parkPhotosSignal().filter((item: OwnedImageItem) => item.id !== photo.id);
        this.parkPhotosSignal.set(updatedItems);
        this.currentPhotoSignal.set(updatedItems.find((item: OwnedImageItem) => item.isCurrent) ?? null);
        this.toastMessageService.add(
          'success',
          this.translateService.instant('admin.parks.saveMessages.successSummary'),
          this.translateService.instant('admin.parks.photos.deleteSuccess')
        );
      },
      error: (error: unknown) => {
        console.error('Error deleting park photo', error);
      }
    });
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

    for (const option of PARK_PHOTO_CATEGORY_OPTIONS) {
      const existingTag: ImageTagDto | undefined = existingTags.find((tag: ImageTagDto) => tag.slug === option.slug);

      if (existingTag) {
        idsBySlug[option.slug] = existingTag.id;
        continue;
      }

      const createdTag: ImageTagDto = await firstValueFrom(this.imagesApiService.createAdminImageTag({
        slug: option.slug,
        labels: [
          { languageCode: 'fr', value: this.translateService.instant(option.labelKey) },
          { languageCode: 'en', value: this.translateService.instant(option.labelKey) }
        ],
        descriptions: []
      }));

      idsBySlug[option.slug] = createdTag.id;
    }

    this.photoTagIdsBySlugSignal.set(idsBySlug);
  }

  private async uploadPhotoAsync(file: File, parkId: string, parkName: string, setAsCurrent: boolean): Promise<void> {
    const uploadedImage: UploadedImage = await firstValueFrom(
      this.imagesApiService.uploadImage(
        file,
        ImageCategory.PARK,
        this.photoWithWatermarkSignal(),
        parkName
      )
    );

    const linkedImage: ImageDto = await firstValueFrom(
      this.imagesApiService.linkImage({
        imageId: uploadedImage.id,
        ownerType: ImageOwnerType.PARK,
        ownerId: parkId,
        description: this.newPhotoDescriptionSignal() || undefined,
        setAsCurrent
      })
    );

    const taggedImage: ImageDto = await this.applySelectedPhotoCategoryAsync(linkedImage);
    this.upsertPhoto(this.toOwnedImageItem(taggedImage));
  }

  private async applySelectedPhotoCategoryAsync(image: ImageDto): Promise<ImageDto> {
    const selectedTagId: string | undefined = this.photoTagIdsBySlugSignal()[this.selectedPhotoCategorySlugSignal()];

    if (!selectedTagId) {
      return image;
    }

    return firstValueFrom(this.imagesApiService.updateAdminImage(image.id, {
      description: image.description,
      geoLocation: image.geoLocation ?? null,
      altTexts: image.altTexts ?? [],
      captions: image.captions ?? [],
      credits: image.credits ?? [],
      tagIds: [...new Set([...(image.tagIds ?? []), selectedTagId])],
      isPublished: image.isPublished !== false,
      sourceUrl: image.sourceUrl ?? null
    }));
  }

  private upsertPhoto(item: OwnedImageItem): void {
    const normalizedItems: OwnedImageItem[] = item.isCurrent
      ? this.parkPhotosSignal().map((photo: OwnedImageItem) => ({
        ...photo,
        isCurrent: photo.id === item.id
      }))
      : [...this.parkPhotosSignal()];
    const existingIndex: number = normalizedItems.findIndex((photo: OwnedImageItem) => photo.id === item.id);

    if (existingIndex >= 0) {
      normalizedItems[existingIndex] = item;
    } else {
      normalizedItems.unshift(item);
    }

    this.parkPhotosSignal.set(normalizedItems);
    this.currentPhotoSignal.set(normalizedItems.find((photo: OwnedImageItem) => photo.isCurrent) ?? item);
    this.photosPageSignal.set(0);
  }

  private toOwnedImageItem(image: ImageDto): OwnedImageItem {
    return mapImageDtoToOwnedImageItem(image, this.currentLanguageSignal());
  }
}
