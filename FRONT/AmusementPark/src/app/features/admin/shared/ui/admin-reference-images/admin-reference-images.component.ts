import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, DestroyRef, Input, OnChanges, OnInit, SimpleChanges } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslateModule } from '@ngx-translate/core';
import { ButtonDirective } from '@shared/primeless/button';
import { forkJoin } from 'rxjs';

import { ImageDisplayComponent } from '@shared/components/image-display/image-display.component';
import { OwnerImageUploadDialogComponent } from '@shared/components/owner-image-upload-dialog/owner-image-upload-dialog.component';
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
  @Input() categoryOptions: ImageCategory[] = [];
  @Input() titleKey: string = 'admin.references.images.title';
  @Input() helpKey: string = 'admin.references.images.help';

  protected images: ImageDto[] = [];
  protected isLoading: boolean = false;
  protected uploadDialogVisible: boolean = false;
  protected readonly ImageCategory = ImageCategory;
  protected readonly ImageOwnerType = ImageOwnerType;

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
    if (changes['ownerId'] || changes['ownerType'] || changes['category'] || changes['categoryOptions']) {
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

  protected get uploadCategoryOptions(): ImageCategory[] {
    return this.resolveCategories();
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
    const categories: ImageCategory[] = this.resolveCategories();
    const requests = categories.map((category: ImageCategory) => this.imagesApiService.getImages(this.ownerType, this.ownerId!, category, 1, 100));

    forkJoin(requests)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (imageGroups: ImageDto[][]) => {
          commitViewUpdate(this.changeDetectorRef, () => {
            this.images = this.mergeImages(imageGroups);
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

  private resolveCategories(): ImageCategory[] {
    const categories: ImageCategory[] = this.categoryOptions.length > 0 ? this.categoryOptions : [this.category];
    return Array.from(new Set(categories));
  }

  private mergeImages(imageGroups: ImageDto[][]): ImageDto[] {
    const imagesById: Map<string, ImageDto> = new Map<string, ImageDto>();
    imageGroups.flat().forEach((image: ImageDto) => imagesById.set(image.id, image));
    return Array.from(imagesById.values()).sort((left: ImageDto, right: ImageDto) =>
      new Date(right.createdAt).getTime() - new Date(left.createdAt).getTime());
  }
}
