import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, DestroyRef, Input, OnChanges, OnInit, SimpleChanges } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslateModule } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';

import { ImageDisplayComponent } from '@app/components/shared/image-display/image-display.component';
import { OwnerImageUploadDialogComponent } from '@app/components/shared/owner-image-upload-dialog/owner-image-upload-dialog.component';
import { ImageCategory } from '@app/models/images/image-category';
import { ImageDto } from '@app/models/images/image-dto';
import { ImageOwnerType } from '@app/models/images/image-owner-type';
import { ImagesApiService } from '@data-access/images/images-api.service';
import { commitViewUpdate } from '@shared/utils/angular';

@Component({
  selector: 'app-admin-reference-images',
  standalone: true,
  imports: [CommonModule, ButtonDirective, TranslateModule, ImageDisplayComponent, OwnerImageUploadDialogComponent],
  templateUrl: './admin-reference-images.component.html',
  styleUrls: ['./admin-reference-images.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AdminReferenceImagesComponent implements OnInit, OnChanges {
  @Input({ required: true }) ownerId!: string | null;
  @Input({ required: true }) ownerType!: ImageOwnerType;
  @Input({ required: true }) category!: ImageCategory;
  @Input() titleKey: string = 'admin.references.images.title';
  @Input() helpKey: string = 'admin.references.images.help';

  protected images: ImageDto[] = [];
  protected isLoading: boolean = false;
  protected uploadDialogVisible: boolean = false;

  constructor(
    private readonly imagesApiService: ImagesApiService,
    private readonly changeDetectorRef: ChangeDetectorRef,
    private readonly destroyRef: DestroyRef
  ) {
  }

  ngOnInit(): void {
    this.loadImages();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['ownerId'] || changes['ownerType'] || changes['category']) {
      this.loadImages();
    }
  }

  protected openUploadDialog(): void {
    if (!this.ownerId) {
      return;
    }

    this.uploadDialogVisible = true;
  }

  protected onImageUploaded(image: ImageDto): void {
    this.images = [image, ...this.images.filter((item: ImageDto) => item.id !== image.id)];
    this.changeDetectorRef.markForCheck();
  }

  protected deleteImage(imageId: string | null | undefined): void {
    if (!imageId) {
      return;
    }

    this.imagesApiService.deleteImage(imageId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          commitViewUpdate(this.changeDetectorRef, () => {
            this.images = this.images.filter((image: ImageDto) => image.id !== imageId);
          });
        },
        error: (error: unknown) => {
          console.error('Error deleting reference image', error);
        }
      });
  }

  private loadImages(): void {
    if (!this.ownerId || !this.ownerType || !this.category) {
      this.images = [];
      return;
    }

    this.isLoading = true;
    this.imagesApiService.getImages(this.ownerType, this.ownerId, this.category, 1, 100)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (images: ImageDto[]) => {
          commitViewUpdate(this.changeDetectorRef, () => {
            this.images = images;
            this.isLoading = false;
          });
        },
        error: (error: unknown) => {
          console.error('Error loading reference images', error);
          commitViewUpdate(this.changeDetectorRef, () => {
            this.images = [];
            this.isLoading = false;
          });
        }
      });
  }
}
