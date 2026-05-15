import { Injectable, Signal, computed, signal } from '@angular/core';
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

@Injectable()
export class AdminParkLogosStateFacade {
  private readonly currentLanguageSignal = signal('en');
  private readonly parkLogosSignal = signal<OwnedImageItem[]>([]);
  private readonly currentLogoSignal = signal<OwnedImageItem | null>(null);
  private readonly logosLoadingSignal = signal(false);
  private readonly logosUploadingSignal = signal(false);
  private readonly logosPageSignal = signal(0);
  private readonly logosPageSizeSignal = signal(8);
  private readonly selectedLogoFilesSignal = signal<File[]>([]);
  private readonly newLogoDescriptionSignal = signal('');

  public readonly parkLogos: Signal<OwnedImageItem[]> = this.parkLogosSignal.asReadonly();
  public readonly currentLogo: Signal<OwnedImageItem | null> = this.currentLogoSignal.asReadonly();
  public readonly logosLoading: Signal<boolean> = this.logosLoadingSignal.asReadonly();
  public readonly logosUploading: Signal<boolean> = this.logosUploadingSignal.asReadonly();
  public readonly logosPageSize: Signal<number> = this.logosPageSizeSignal.asReadonly();
  public readonly newLogoDescription: Signal<string> = this.newLogoDescriptionSignal.asReadonly();
  public readonly selectedLogoCount: Signal<number> = computed(() => this.selectedLogoFilesSignal().length);
  public readonly pagedLogos: Signal<OwnedImageItem[]> = computed(() => {
    const start: number = this.logosPageSignal() * this.logosPageSizeSignal();
    return this.parkLogosSignal().slice(start, start + this.logosPageSizeSignal());
  });

  constructor(
    private readonly imagesApiService: ImagesApiService,
    private readonly translateService: TranslateService,
    private readonly toastMessageService: ToastMessageService,
    private readonly imageUploadSecurityService: ImageUploadSecurityService
  ) {
  }

  setCurrentLanguage(languageCode: string): void {
    this.currentLanguageSignal.set(languageCode);
  }

  reset(): void {
    this.parkLogosSignal.set([]);
    this.currentLogoSignal.set(null);
    this.logosLoadingSignal.set(false);
    this.logosUploadingSignal.set(false);
    this.logosPageSignal.set(0);
    this.selectedLogoFilesSignal.set([]);
    this.newLogoDescriptionSignal.set('');
  }

  loadLogos(parkId: string): void {
    this.logosLoadingSignal.set(true);

    this.imagesApiService.getImages(ImageOwnerType.PARK, parkId, ImageCategory.PARK_LOGO).subscribe({
      next: (images: ImageDto[]) => {
        const logoItems: OwnedImageItem[] = images.map((image: ImageDto) => this.toOwnedImageItem(image));
        this.parkLogosSignal.set(logoItems);
        this.currentLogoSignal.set(logoItems.find((item: OwnedImageItem) => item.isCurrent) ?? null);
        this.logosPageSignal.set(0);
        this.logosLoadingSignal.set(false);
      },
      error: (error: unknown) => {
        console.error('Error loading logos', error);
        this.logosLoadingSignal.set(false);
      }
    });
  }

  selectLogoFiles(event: Event): void {
    const inputElement: HTMLInputElement = event.target as HTMLInputElement;

    if (!inputElement.files || inputElement.files.length === 0) {
      this.selectedLogoFilesSignal.set([]);
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

    this.selectedLogoFilesSignal.set(validFiles);
    inputElement.value = '';
  }

  setNewLogoDescription(description: string): void {
    this.newLogoDescriptionSignal.set(description);
  }

  onLogosPageChange(event: PaginatorState): void {
    this.logosPageSignal.set(event.page ?? 0);
    this.logosPageSizeSignal.set(event.rows ?? this.logosPageSizeSignal());
  }

  async uploadSelectedLogos(parkId: string, parkName: string): Promise<void> {
    if (this.selectedLogoFilesSignal().length === 0 || this.logosUploadingSignal()) {
      return;
    }

    this.logosUploadingSignal.set(true);
    const files: File[] = [...this.selectedLogoFilesSignal()];
    let uploadedCount: number = 0;

    try {
      for (const file of files) {
        await this.uploadLogoAsync(file, parkId, parkName, true);
        uploadedCount++;
      }

      this.selectedLogoFilesSignal.set([]);
      this.newLogoDescriptionSignal.set('');
      this.toastMessageService.add(
        'success',
        this.translateService.instant('admin.parks.saveMessages.successSummary'),
        this.translateService.instant('admin.parks.logos.uploadSuccess', { count: uploadedCount })
      );
    } catch (error: unknown) {
      console.error('Error uploading logo images', error);
      this.toastMessageService.add(
        'error',
        this.translateService.instant('admin.parks.saveMessages.errorSummary'),
        this.translateService.instant('admin.parks.logos.uploadError', { count: uploadedCount })
      );
    } finally {
      this.logosUploadingSignal.set(false);
    }
  }

  setCurrentLogo(logo: OwnedImageItem): void {
    if (logo.isCurrent) {
      return;
    }

    this.imagesApiService.setCurrentImage(logo.id).subscribe({
      next: (image: ImageDto) => {
        const updatedLogo: OwnedImageItem = this.toOwnedImageItem(image);
        const updatedItems: OwnedImageItem[] = this.parkLogosSignal().map((item: OwnedImageItem) => ({
          ...item,
          isCurrent: item.id === updatedLogo.id
        }));

        this.parkLogosSignal.set(updatedItems);
        this.currentLogoSignal.set(updatedLogo);
        this.toastMessageService.add(
          'success',
          this.translateService.instant('admin.parks.saveMessages.successSummary'),
          this.translateService.instant('admin.parks.logos.currentSetSuccess')
        );
      },
      error: (error: unknown) => {
        console.error('Error setting current park logo', error);
      }
    });
  }

  deleteLogo(logo: OwnedImageItem): void {
    this.imagesApiService.deleteImage(logo.id).subscribe({
      next: () => {
        const updatedItems: OwnedImageItem[] = this.parkLogosSignal().filter((item: OwnedImageItem) => item.id !== logo.id);
        this.parkLogosSignal.set(updatedItems);
        this.currentLogoSignal.set(updatedItems.find((item: OwnedImageItem) => item.isCurrent) ?? null);
        this.toastMessageService.add(
          'success',
          this.translateService.instant('admin.parks.saveMessages.successSummary'),
          this.translateService.instant('admin.parks.logos.deleteSuccess')
        );
      },
      error: (error: unknown) => {
        console.error('Error deleting park logo', error);
      }
    });
  }

  private async uploadLogoAsync(file: File, parkId: string, parkName: string, setAsCurrent: boolean): Promise<void> {
    const uploadedImage: UploadedImage = await firstValueFrom(
      this.imagesApiService.uploadImage(
        file,
        ImageCategory.PARK_LOGO,
        false,
        parkName
      )
    );

    const linkedImage: ImageDto = await firstValueFrom(
      this.imagesApiService.linkImage({
        imageId: uploadedImage.id,
        ownerType: ImageOwnerType.PARK,
        ownerId: parkId,
        description: this.newLogoDescriptionSignal() || undefined,
        setAsCurrent
      })
    );

    const taggedImage: ImageDto = await this.tryApplyLogoTagAsync(linkedImage);
    const item: OwnedImageItem = this.toOwnedImageItem(taggedImage);
    const normalizedItems: OwnedImageItem[] = this.parkLogosSignal().map((logo: OwnedImageItem) => ({
      ...logo,
      isCurrent: logo.id === item.id
    }));
    const existingIndex: number = normalizedItems.findIndex((logo: OwnedImageItem) => logo.id === item.id);

    if (existingIndex >= 0) {
      normalizedItems[existingIndex] = item;
    } else {
      normalizedItems.unshift(item);
    }

    this.parkLogosSignal.set(normalizedItems);
    this.currentLogoSignal.set(item);
    this.logosPageSignal.set(0);
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
    return mapImageDtoToOwnedImageItem(image, this.currentLanguageSignal());
  }
}
