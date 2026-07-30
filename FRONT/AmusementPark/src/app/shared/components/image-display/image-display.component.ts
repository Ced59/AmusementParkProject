import { ChangeDetectionStrategy, ChangeDetectorRef, Component, Input, OnChanges, OnDestroy, SimpleChanges } from '@angular/core';
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
export class ImageDisplayComponent implements OnChanges, OnDestroy {
  private static readonly MaxRetryAttempts: number = 2;
  private static readonly RetryBaseDelayMilliseconds: number = 350;

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

  private retryAttempt: number = 0;
  private retryTimeoutId: ReturnType<typeof setTimeout> | null = null;
  private canRetryImageLoad: boolean = false;

  constructor(
    private readonly imagesApiService: ImagesApiService,
    private readonly changeDetectorRef: ChangeDetectorRef
  ) {
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['imageId'] || changes['imagePathOrUrl']) {
      this.cancelPendingRetry();
      this.retryAttempt = 0;
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

  ngOnDestroy(): void {
    this.cancelPendingRetry();
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
    this.cancelPendingRetry();

    if (this.canRetryImageLoad && this.retryAttempt < ImageDisplayComponent.MaxRetryAttempts) {
      this.imageLoadFailed = true;
      this.retryAttempt += 1;
      const currentRetryAttempt: number = this.retryAttempt;

      this.retryTimeoutId = setTimeout((): void => {
        this.retryTimeoutId = null;
        this.refreshResolvedImage(currentRetryAttempt);
        this.imageLoadFailed = this.resolvedImageUrl === null;
        this.changeDetectorRef.markForCheck();
      }, ImageDisplayComponent.RetryBaseDelayMilliseconds * currentRetryAttempt);
      return;
    }

    this.imageLoadFailed = true;
  }

  private refreshResolvedImage(retryAttempt: number = 0): void {
    const rawValue: string | undefined = this.imagePathOrUrl?.trim() || this.imageId?.trim();

    if (!rawValue) {
      this.resolvedImageUrl = null;
      this.resolvedImageSrcSet = null;
      this.resolvedImageSizes = null;
      this.canRetryImageLoad = false;
      return;
    }

    this.resolvedImageUrl = retryAttempt > 0
      ? this.imagesApiService.resolveImageUrl(rawValue, { width: this.srcWidth, retryAttempt })
      : this.imagesApiService.resolveImageUrl(rawValue, { width: this.srcWidth });
    this.resolvedImageSrcSet = retryAttempt > 0
      ? this.imagesApiService.buildImageSrcSet(rawValue, this.responsiveWidths, { retryAttempt })
      : this.imagesApiService.buildImageSrcSet(rawValue, this.responsiveWidths);
    this.resolvedImageSizes = this.resolvedImageSrcSet ? this.sizes : null;
    this.canRetryImageLoad = !!this.imageId?.trim() || this.resolvedImageSrcSet !== null;
  }

  private cancelPendingRetry(): void {
    if (this.retryTimeoutId === null) {
      return;
    }

    clearTimeout(this.retryTimeoutId);
    this.retryTimeoutId = null;
  }
}
