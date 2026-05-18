import { Injectable, Signal, computed, signal, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { firstValueFrom } from 'rxjs';
import { PaginatorState } from 'primeng/paginator';
import { TranslateService } from '@ngx-translate/core';

import { ImagesApiService } from '@data-access/images/images-api.service';
import { ToastMessageService } from '@app/services/messages/toast-message.service';
import { UploadedImage } from '@app/models/images/uploaded-image';
import { ImageCategory } from '@app/models/images/image-category';
import { ImageDto } from '@app/models/images/image-dto';
import { ImageOwnerType } from '@app/models/images/image-owner-type';
import { ImageTagDto } from '@app/models/images/image-tag-dto';
import { OwnedImageItem } from '@shared/models/images/owned-image-item.model';
import { mapImageDtoToOwnedImageItem } from '@shared/utils/images/owned-image-item.mapper';
import { ImageUploadSecurityService } from '@shared/utils/security';
import { AdminParkItemPhotoCategoryOption, PARK_ITEM_PHOTO_CATEGORY_OPTIONS } from '../models/admin-park-item-edit.model';

@Injectable()
export class AdminParkItemPhotosStateFacade {
  private readonly currentLanguageSignal = signal('en');
  private readonly attractionPhotosSignal = signal<OwnedImageItem[]>([]);
  private readonly currentPhotoSignal = signal<OwnedImageItem | null>(null);
  private readonly photosLoadingSignal = signal(false);
  private readonly photosUploadingSignal = signal(false);
  private readonly photosPageSignal = signal(0);
  private readonly photosPageSizeSignal = signal(8);
  private readonly selectedPhotoFilesSignal = signal<File[]>([]);
  private readonly newPhotoDescriptionSignal = signal('');
  private readonly selectedPhotoCategorySlugSignal = signal(PARK_ITEM_PHOTO_CATEGORY_OPTIONS[0].slug);
  private readonly photoTagIdsBySlugSignal = signal<Record<string, string>>({});

  public readonly attractionPhotos: Signal<OwnedImageItem[]> = this.attractionPhotosSignal.asReadonly();
  public readonly currentPhoto: Signal<OwnedImageItem | null> = this.currentPhotoSignal.asReadonly();
  public readonly photosLoading: Signal<boolean> = this.photosLoadingSignal.asReadonly();
  public readonly photosUploading: Signal<boolean> = this.photosUploadingSignal.asReadonly();
  public readonly photosPageSize: Signal<number> = this.photosPageSizeSignal.asReadonly();
  public readonly newPhotoDescription: Signal<string> = this.newPhotoDescriptionSignal.asReadonly();
  public readonly selectedPhotoCategorySlug: Signal<string> = this.selectedPhotoCategorySlugSignal.asReadonly();
  public readonly photoCategoryOptions: Signal<AdminParkItemPhotoCategoryOption[]> = signal([...PARK_ITEM_PHOTO_CATEGORY_OPTIONS]).asReadonly();
  public readonly selectedPhotoCount: Signal<number> = computed(() => this.selectedPhotoFilesSignal().length);
  public readonly pagedPhotos: Signal<OwnedImageItem[]> = computed(() => {
    const start: number = this.photosPageSignal() * this.photosPageSizeSignal();
    return this.attractionPhotosSignal().slice(start, start + this.photosPageSizeSignal());
  });

  constructor(
    private readonly imagesApiService: ImagesApiService,
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
    this.attractionPhotosSignal.set([]);
    this.currentPhotoSignal.set(null);
    this.photosLoadingSignal.set(false);
    this.photosUploadingSignal.set(false);
    this.photosPageSignal.set(0);
    this.photosPageSizeSignal.set(8);
    this.selectedPhotoFilesSignal.set([]);
    this.newPhotoDescriptionSignal.set('');
    this.selectedPhotoCategorySlugSignal.set(PARK_ITEM_PHOTO_CATEGORY_OPTIONS[0].slug);
    this.photoTagIdsBySlugSignal.set({});
  }

  loadPhotos(itemId: string): void {
    this.photosLoadingSignal.set(true);
    this.ensurePhotoCategoryTags();

    this.imagesApiService.getImages(ImageOwnerType.ATTRACTION, itemId, ImageCategory.ATTRACTION).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
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

  setSelectedPhotoCategorySlug(slug: string): void {
    const isKnownSlug: boolean = PARK_ITEM_PHOTO_CATEGORY_OPTIONS.some((option: AdminParkItemPhotoCategoryOption) => option.slug === slug);
    this.selectedPhotoCategorySlugSignal.set(isKnownSlug ? slug : PARK_ITEM_PHOTO_CATEGORY_OPTIONS[0].slug);
  }

  onPhotosPageChange(event: PaginatorState): void {
    this.photosPageSignal.set(event.page ?? 0);
    this.photosPageSizeSignal.set(event.rows ?? this.photosPageSizeSignal());
  }

  async uploadSelectedPhotos(itemId: string, itemName: string): Promise<void> {
    if (this.selectedPhotoFilesSignal().length === 0 || this.photosUploadingSignal()) {
      return;
    }

    this.photosUploadingSignal.set(true);
    const files: File[] = [...this.selectedPhotoFilesSignal()];
    const hadNoPhotoInitially: boolean = this.attractionPhotosSignal().length === 0;
    let uploadedCount: number = 0;

    try {
      for (let index: number = 0; index < files.length; index++) {
        const shouldSetCurrent: boolean = hadNoPhotoInitially && index === 0;
        await this.uploadPhotoAsync(files[index], itemId, itemName, shouldSetCurrent);
        uploadedCount++;
      }

      this.selectedPhotoFilesSignal.set([]);
      this.newPhotoDescriptionSignal.set('');
    this.selectedPhotoCategorySlugSignal.set(PARK_ITEM_PHOTO_CATEGORY_OPTIONS[0].slug);
    this.photoTagIdsBySlugSignal.set({});
      this.toastMessageService.add(
        'success',
        this.translateService.instant('admin.parks.items.saveMessages.successSummary'),
        this.translateService.instant('admin.parks.items.photos.uploadSuccess', { count: uploadedCount })
      );
    } catch (error: unknown) {
      console.error('Error uploading attraction images', error);
      this.toastMessageService.add(
        'error',
        this.translateService.instant('admin.parks.items.saveMessages.errorSummary'),
        this.translateService.instant('admin.parks.items.photos.uploadError', { count: uploadedCount })
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


  private ensurePhotoCategoryTags(): void {
    this.imagesApiService.getAdminImageTags().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (tags: ImageTagDto[]) => {
        void this.ensureMissingPhotoCategoryTagsAsync(tags);
      },
      error: (error: unknown) => {
        console.error('Error loading image tags', error);
      }
    });
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
          { languageCode: 'fr', value: this.translateService.instant(option.labelKey) },
          { languageCode: 'en', value: this.translateService.instant(option.labelKey) }
        ],
        descriptions: []
      }));

      idsBySlug[option.slug] = createdTag.id;
    }

    this.photoTagIdsBySlugSignal.set(idsBySlug);
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
      isPublished: image.isPublished
    }));
  }

  private async uploadPhotoAsync(file: File, itemId: string, itemName: string, setAsCurrent: boolean): Promise<void> {
    const uploadedImage: UploadedImage = await firstValueFrom(
      this.imagesApiService.uploadImage(
        file,
        ImageCategory.ATTRACTION,
        false,
        itemName
      )
    );

    const linkedImage: ImageDto = await firstValueFrom(
      this.imagesApiService.linkImage({
        imageId: uploadedImage.id,
        ownerType: ImageOwnerType.ATTRACTION,
        ownerId: itemId,
        description: this.newPhotoDescriptionSignal() || undefined,
        setAsCurrent
      })
    );

    const taggedImage: ImageDto = await this.applySelectedPhotoCategoryAsync(linkedImage);
    this.upsertPhoto(this.toOwnedImageItem(taggedImage));
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
