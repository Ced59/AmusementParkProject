import { ChangeDetectionStrategy, Component, DestroyRef, ElementRef, EventEmitter, Input, OnDestroy, Output, ViewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize, Subscription, switchMap } from 'rxjs';

import { ImagesApiService } from '@data-access/images/images-api.service';
import { ImageCategory } from '@app/models/images/image-category';
import { ImageOwnerType } from '@app/models/images/image-owner-type';
import { ImageDto } from '@app/models/images/image-dto';
import { UploadedImage } from '@app/models/images/uploaded-image';
import { Bind } from 'primeng/bind';
import { Dialog } from 'primeng/dialog';
import { ButtonDirective } from 'primeng/button';
import { FormsModule } from '@angular/forms';
import { InputText } from 'primeng/inputtext';
import { PrimeTemplate } from 'primeng/api';
import { TranslateModule } from '@ngx-translate/core';
import { ImageDisplayComponent } from '../image-display/image-display.component';
import { ImageUploadSecurityService } from '@shared/utils/security';

@Component({
    selector: 'app-owner-image-upload-dialog',
    templateUrl: './owner-image-upload-dialog.component.html',
    styleUrls: ['./owner-image-upload-dialog.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [Bind, Dialog, ButtonDirective, FormsModule, InputText, PrimeTemplate, TranslateModule, ImageDisplayComponent]
})
export class OwnerImageUploadDialogComponent implements OnDestroy {
  @Input() visible: boolean = false;
  @Output() visibleChange: EventEmitter<boolean> = new EventEmitter<boolean>();

  @Input() ownerId: string = '';
  @Input() ownerType: ImageOwnerType = ImageOwnerType.USER;
  @Input() category: ImageCategory = ImageCategory.AVATAR;
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
  @Input() withWatermark: boolean = false;
  @Input() maxFileSizeBytes: number = 5 * 1024 * 1024;

  @Output() uploaded: EventEmitter<ImageDto> = new EventEmitter<ImageDto>();

  @ViewChild('fileInput') private fileInput?: ElementRef<HTMLInputElement>;

  selectedFile: File | null = null;
  previewUrl: string | null = null;
  description: string = '';
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

    if (!this.selectedFile || !this.ownerId) {
      this.errorTranslationKey = this.noImageSelectedKey;
      return;
    }

    this.errorTranslationKey = null;
    this.isUploading = true;

    this.uploadSubscription?.unsubscribe();
    this.uploadSubscription = this.imagesApiService.uploadImage(
      this.selectedFile,
      this.category,
      this.withWatermark,
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

  private processSelectedFile(file: File | null): void {
    this.errorTranslationKey = null;

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
    this.setPreviewFromFile(file);
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
    this.description = '';
    this.errorTranslationKey = null;
    this.isDragging = false;
    this.cleanupPreviewUrl();

    if (this.fileInput?.nativeElement) {
      this.fileInput.nativeElement.value = '';
    }

    this.visible = false;
    this.visibleChange.emit(false);
  }
}
