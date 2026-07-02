import { ChangeDetectionStrategy, Component, DestroyRef, ElementRef, EventEmitter, Input, OnChanges, OnDestroy, Output, SimpleChanges, ViewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize, Subscription, switchMap } from 'rxjs';

import { ImagesApiService } from '@data-access/images/images-api.service';
import { ImageCategory } from '@app/models/images/image-category';
import { ImageOwnerType } from '@app/models/images/image-owner-type';
import { ImageDto } from '@app/models/images/image-dto';
import { UploadedImage } from '@app/models/images/uploaded-image';
import { Dialog } from '@shared/primeless/dialog';
import { ButtonDirective } from '@shared/primeless/button';
import { FormsModule } from '@angular/forms';
import { InputText } from '@shared/primeless/inputtext';
import { PrimeTemplate } from '@shared/primeless/api';
import { TranslateModule } from '@ngx-translate/core';
import { ImageDisplayComponent } from '../image-display/image-display.component';
import { ImageUploadSecurityService } from '@shared/utils/security';

@Component({
    selector: 'app-owner-image-upload-dialog',
    templateUrl: './owner-image-upload-dialog.component.html',
    styleUrls: ['./owner-image-upload-dialog.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [Dialog, ButtonDirective, FormsModule, InputText, PrimeTemplate, TranslateModule, ImageDisplayComponent]
})
export class OwnerImageUploadDialogComponent implements OnChanges, OnDestroy {
  @Input() visible: boolean = false;
  @Output() visibleChange: EventEmitter<boolean> = new EventEmitter<boolean>();

  @Input() ownerId: string = '';
  @Input() ownerType: ImageOwnerType = ImageOwnerType.USER;
  @Input() category: ImageCategory = ImageCategory.AVATAR;
  @Input() categoryOptions: ImageCategory[] = [];
  @Input() showCategoryChoice: boolean = false;
  @Input() categoryLabelKey: string = 'admin.images.category';
  @Input() titleKey: string = 'shared.imageUpload.title';
  @Input() chooseButtonKey: string = 'shared.imageUpload.choose';
  @Input() uploadButtonKey: string = 'shared.imageUpload.upload';
  @Input() cancelButtonKey: string = 'shared.imageUpload.cancel';
  @Input() dragAndDropHintKey: string = 'shared.imageUpload.dragAndDrop';
  @Input() selectedFileLabelKey: string = 'shared.imageUpload.selectedFile';
  @Input() invalidFileKey: string = 'shared.imageUpload.invalidFile';
  @Input() invalidSizeKey: string = 'shared.imageUpload.invalidSize';
  @Input() noImageSelectedKey: string = 'shared.imageUpload.noImageSelected';
  @Input() uploadingKey: string = 'shared.imageUpload.uploading';
  @Input() showDescription: boolean = false;
  @Input() descriptionPlaceholderKey: string = 'shared.imageUpload.descriptionPlaceholder';
  @Input() allowRemoteImport: boolean = false;
  @Input() remoteSourceUrlPlaceholderKey: string = 'shared.imageUpload.remoteSourceUrlPlaceholder';
  @Input() remoteImportButtonKey: string = 'shared.imageUpload.remoteImport';
  @Input() remotePreviewAltKey: string = 'shared.imageUpload.remotePreviewAlt';
  @Input() withWatermark: boolean = false;
  @Input() allowWatermarkChoice: boolean = false;
  @Input() watermarkLabelKey: string = 'shared.imageUpload.withWatermark';
  @Input() maxFileSizeBytes: number = 5 * 1024 * 1024;

  @Output() uploaded: EventEmitter<ImageDto> = new EventEmitter<ImageDto>();

  @ViewChild('fileInput') private fileInput?: ElementRef<HTMLInputElement>;

  selectedFile: File | null = null;
  previewUrl: string | null = null;
  remoteSourceUrl: string = '';
  description: string = '';
  selectedCategory: ImageCategory = this.category;
  isUploading: boolean = false;
  isDragging: boolean = false;
  errorTranslationKey: string | null = null;

  private uploadSubscription: Subscription | null = null;

  constructor(
    private readonly imagesApiService: ImagesApiService,
    private readonly imageUploadSecurityService: ImageUploadSecurityService,
    private readonly destroyRef: DestroyRef
  ) {
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['category'] || changes['categoryOptions']) {
      this.selectedCategory = this.resolveInitialCategory();
      this.applyWatermarkDefaults();
    }
  }

  ngOnDestroy(): void {
    this.cleanupPreviewUrl();
    this.uploadSubscription?.unsubscribe();
  }

  onDialogHide(): void {
    if (!this.isUploading) {
      this.resetAndClose();
    }
  }

  openFilePicker(): void {
    if (this.isUploading) {
      return;
    }

    this.fileInput?.nativeElement.click();
  }

  onFileInputChange(event: Event): void {
    const inputElement: HTMLInputElement = event.target as HTMLInputElement;
    const file: File | null = inputElement.files && inputElement.files.length > 0
      ? inputElement.files[0]
      : null;

    this.processSelectedFile(file);
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    if (this.isUploading) {
      return;
    }

    this.isDragging = true;
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    this.isDragging = false;
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.isDragging = false;

    if (this.isUploading) {
      return;
    }

    const file: File | null = event.dataTransfer && event.dataTransfer.files.length > 0
      ? event.dataTransfer.files[0]
      : null;

    this.processSelectedFile(file);
  }

  upload(): void {
    if (this.isUploading) {
      return;
    }

    const category: ImageCategory = this.currentCategory;
    const normalizedRemoteSourceUrl: string = this.remoteSourceUrl.trim();
    if (this.allowRemoteImport && normalizedRemoteSourceUrl.length > 0) {
      this.importRemote(normalizedRemoteSourceUrl, category);
      return;
    }

    if (!this.selectedFile || !this.ownerId) {
      this.errorTranslationKey = this.noImageSelectedKey;
      return;
    }

    this.errorTranslationKey = null;
    this.isUploading = true;

    this.uploadSubscription?.unsubscribe();
    this.uploadSubscription = this.imagesApiService.uploadImage(
      this.selectedFile,
      category,
      this.shouldApplyWatermark(category),
      this.showDescription ? this.description : undefined)
      .pipe(
        switchMap((uploadedImage: UploadedImage) => {
          return this.imagesApiService.linkImage({
            imageId: uploadedImage.id,
            ownerType: this.ownerType,
            ownerId: this.ownerId,
            description: this.showDescription ? this.description : undefined,
            setAsCurrent: true
          });
        }),
        finalize(() => {
          this.isUploading = false;
        }))
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: (image: ImageDto) => {
          this.uploaded.emit(image);
          this.resetAndClose();
        },
        error: () => {
          this.errorTranslationKey = 'shared.imageUpload.uploadError';
        }
      });
  }

  cancel(): void {
    if (!this.isUploading) {
      this.resetAndClose();
    }
  }

  setRemoteSourceUrl(value: string): void {
    const hadRemoteSourceUrl: boolean = this.remoteSourceUrl.trim().length > 0;
    this.remoteSourceUrl = value;
    this.errorTranslationKey = null;

    if (this.remoteSourceUrl.trim().length > 0) {
      if (!hadRemoteSourceUrl && this.canChooseWatermark) {
        this.withWatermark = false;
      }

      this.selectedFile = null;
      this.cleanupPreviewUrl();
      if (this.fileInput?.nativeElement) {
        this.fileInput.nativeElement.value = '';
      }
    }
  }

  get remotePreviewUrl(): string | null {
    const normalizedRemoteSourceUrl: string = this.remoteSourceUrl.trim();
    return normalizedRemoteSourceUrl.length > 0 ? normalizedRemoteSourceUrl : null;
  }

  get visibleCategoryOptions(): ImageCategory[] {
    const options: ImageCategory[] = this.categoryOptions.length > 0 ? this.categoryOptions : [this.category];
    return Array.from(new Set(options));
  }

  get currentCategory(): ImageCategory {
    return this.selectedCategory || this.category;
  }

  get canChooseWatermark(): boolean {
    return this.allowWatermarkChoice && this.currentCategory !== ImageCategory.LOGO;
  }

  setCategory(value: string): void {
    this.selectedCategory = value as ImageCategory;
    this.applyWatermarkDefaults();
  }

  private processSelectedFile(file: File | null): void {
    this.errorTranslationKey = null;
    this.remoteSourceUrl = '';

    if (!file) {
      this.selectedFile = null;
      this.cleanupPreviewUrl();
      return;
    }

    const validationResult = this.imageUploadSecurityService.validateImageFile(file, this.maxFileSizeBytes);
    if (!validationResult.isValid) {
      this.selectedFile = null;
      this.cleanupPreviewUrl();
      this.errorTranslationKey = validationResult.errorKey === 'shared.imageUpload.invalidSize'
        ? this.invalidSizeKey
        : this.invalidFileKey;
      return;
    }

    this.selectedFile = file;
    if (this.canChooseWatermark) {
      this.withWatermark = true;
    } else {
      this.withWatermark = false;
    }

    this.setPreviewFromFile(file);
  }

  private importRemote(sourceUrl: string, category: ImageCategory): void {
    if (!this.ownerId) {
      this.errorTranslationKey = this.noImageSelectedKey;
      return;
    }

    this.errorTranslationKey = null;
    this.isUploading = true;
    this.uploadSubscription?.unsubscribe();
    this.uploadSubscription = this.imagesApiService.importRemoteImage({
      sourceUrl,
      category,
      ownerType: this.ownerType,
      ownerId: this.ownerId,
      description: this.showDescription ? this.description : undefined,
      withWatermark: this.shouldApplyWatermark(category),
      setAsCurrent: true
    })
      .pipe(finalize(() => {
        this.isUploading = false;
      }))
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: (image: ImageDto) => {
          this.uploaded.emit(image);
          this.resetAndClose();
        },
        error: () => {
          this.errorTranslationKey = 'shared.imageUpload.uploadError';
        }
      });
  }

  private setPreviewFromFile(file: File): void {
    this.cleanupPreviewUrl();
    this.previewUrl = URL.createObjectURL(file);
  }

  private cleanupPreviewUrl(): void {
    if (this.previewUrl) {
      URL.revokeObjectURL(this.previewUrl);
      this.previewUrl = null;
    }
  }

  private resetAndClose(): void {
    this.selectedFile = null;
    this.remoteSourceUrl = '';
    this.description = '';
    this.errorTranslationKey = null;
    this.isDragging = false;
    if (this.canChooseWatermark) {
      this.withWatermark = true;
    } else {
      this.withWatermark = false;
    }

    this.cleanupPreviewUrl();

    if (this.fileInput?.nativeElement) {
      this.fileInput.nativeElement.value = '';
    }

    this.visible = false;
    this.visibleChange.emit(false);
  }

  private resolveInitialCategory(): ImageCategory {
    const options: ImageCategory[] = this.visibleCategoryOptions;
    return options.includes(this.category) ? this.category : options[0] ?? this.category;
  }

  private applyWatermarkDefaults(): void {
    if (this.currentCategory === ImageCategory.LOGO) {
      this.withWatermark = false;
      return;
    }

    if (this.selectedFile && this.allowWatermarkChoice) {
      this.withWatermark = true;
      return;
    }

    if (this.remoteSourceUrl.trim().length > 0) {
      this.withWatermark = false;
    }
  }

  private shouldApplyWatermark(category: ImageCategory): boolean {
    return category !== ImageCategory.LOGO && this.withWatermark;
  }
}
