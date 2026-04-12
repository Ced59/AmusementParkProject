import { ChangeDetectionStrategy, Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import { ImagesApiService } from '@data-access/images/images-api.service';
import { ImageDisplayViewComponent } from './image-display-view.component';

@Component({
  selector: 'app-image-display',
  templateUrl: './image-display.component.html',
  styleUrls: ['./image-display.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ImageDisplayViewComponent]
})
export class ImageDisplayComponent implements OnChanges {
  @Input() imageId: string | null = null;
  @Input() alt: string = '';
  @Input() imgClass: string = '';

  imageLoadFailed: boolean = false;

  constructor(private readonly imagesApiService: ImagesApiService) {
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['imageId']) {
      this.imageLoadFailed = false;
    }
  }

  get resolvedImageUrl(): string | null {
    const rawValue: string | undefined = this.imageId?.trim();

    if (!rawValue) {
      return null;
    }

    if (/^https?:\/\//i.test(rawValue)) {
      return rawValue;
    }

    if (rawValue.startsWith('/images/')) {
      const entityId: string = rawValue.replace(/^\/images\//, '');
      return this.imagesApiService.buildImageUrl(entityId);
    }

    return this.imagesApiService.buildImageUrl(rawValue);
  }

  get showImage(): boolean {
    return !!this.resolvedImageUrl && !this.imageLoadFailed;
  }

  onImageError(): void {
    this.imageLoadFailed = true;
  }
}
