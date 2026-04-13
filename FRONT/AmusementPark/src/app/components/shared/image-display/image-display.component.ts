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
  @Input() imagePathOrUrl: string | null = null;
  @Input() alt: string = '';
  @Input() imgClass: string = '';
  @Input() placeholderClass: string = '';
  @Input() placeholderIconClass: string = 'pi pi-image';

  imageLoadFailed: boolean = false;

  constructor(private readonly imagesApiService: ImagesApiService) {
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['imageId'] || changes['imagePathOrUrl']) {
      this.imageLoadFailed = false;
    }
  }

  get resolvedImageUrl(): string | null {
    const rawValue: string | undefined = this.imagePathOrUrl?.trim() || this.imageId?.trim();

    if (!rawValue) {
      return null;
    }

    return this.imagesApiService.resolveImageUrl(rawValue);
  }

  get showImage(): boolean {
    return !!this.resolvedImageUrl && !this.imageLoadFailed;
  }

  onImageError(): void {
    this.imageLoadFailed = true;
  }
}
