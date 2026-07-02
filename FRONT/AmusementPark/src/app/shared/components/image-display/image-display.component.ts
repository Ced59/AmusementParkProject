import { ChangeDetectionStrategy, Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import { TranslateModule } from '@ngx-translate/core';
import { ImagesApiService } from '@data-access/images/images-api.service';
import { ImageDisplayViewComponent } from './image-display-view.component';
import { ImageFallbackKind, resolveImageFallbackIconClass } from '@shared/utils/images/image-fallback.helpers';

@Component({
  selector: 'app-image-display',
  templateUrl: './image-display.component.html',
  styleUrls: ['./image-display.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ImageDisplayViewComponent, TranslateModule]
})
export class ImageDisplayComponent implements OnChanges {
  @Input() imageId: string | null = null;
  @Input() imagePathOrUrl: string | null = null;
  @Input() alt: string = '';
  @Input() imgClass: string = '';
  @Input() placeholderClass: string = '';
  @Input() placeholderIconClass: string | null = null;
  @Input() placeholderKind: ImageFallbackKind = 'generic';
  @Input() loading: 'eager' | 'lazy' = 'lazy';
  @Input() fetchPriority: 'high' | 'low' | 'auto' | null = null;
  @Input() sizes: string = '100vw';
  @Input() srcWidth: number | null = null;
  @Input() responsiveWidths: readonly number[] = [320, 480, 640, 800, 960, 1280, 1600, 1920];

  imageLoadFailed: boolean = false;
  resolvedImageUrl: string | null = null;
  resolvedImageSrcSet: string | null = null;
  resolvedImageSizes: string | null = null;

  constructor(private readonly imagesApiService: ImagesApiService) {
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['imageId'] || changes['imagePathOrUrl']) {
      this.imageLoadFailed = false;
    }

    if (
      changes['imageId'] ||
      changes['imagePathOrUrl'] ||
      changes['responsiveWidths'] ||
      changes['srcWidth'] ||
      changes['sizes']
    ) {
      this.refreshResolvedImage();
    }
  }

  get showImage(): boolean {
    return !!this.resolvedImageUrl && !this.imageLoadFailed;
  }

  get resolvedPlaceholderIconClass(): string {
    const explicitIconClass: string = this.placeholderIconClass?.trim() ?? '';

    if (explicitIconClass.length > 0) {
      return explicitIconClass;
    }

    return resolveImageFallbackIconClass(this.placeholderKind);
  }

  onImageError(): void {
    this.imageLoadFailed = true;
  }

  private refreshResolvedImage(): void {
    const rawValue: string | undefined = this.imagePathOrUrl?.trim() || this.imageId?.trim();

    if (!rawValue) {
      this.resolvedImageUrl = null;
      this.resolvedImageSrcSet = null;
      this.resolvedImageSizes = null;
      return;
    }

    this.resolvedImageUrl = this.imagesApiService.resolveImageUrl(rawValue, { width: this.srcWidth });
    this.resolvedImageSrcSet = this.imagesApiService.buildImageSrcSet(rawValue, this.responsiveWidths);
    this.resolvedImageSizes = this.resolvedImageSrcSet ? this.sizes : null;
  }
}
