import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { NgClass, NgIf } from '@angular/common';

@Component({
  selector: 'app-image-display-view',
  templateUrl: './image-display-view.component.html',
  styleUrls: ['./image-display.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NgIf, NgClass]
})
export class ImageDisplayViewComponent {
  @Input() showImage: boolean = false;
  @Input() resolvedImageUrl: string | null = null;
  @Input() resolvedImageSrcSet: string | null = null;
  @Input() resolvedImageSizes: string | null = null;
  @Input() alt: string = '';
  @Input() fallbackAlt: string = 'AMUSEMENT-PARKS.fun';
  @Input() imgClass: string = '';
  @Input() placeholderClass: string = '';
  @Input() placeholderIconClass: string = 'pi pi-image';
  @Input() loading: 'eager' | 'lazy' = 'lazy';
  @Input() fetchPriority: 'high' | 'low' | 'auto' | null = null;

  @Output() imageError: EventEmitter<void> = new EventEmitter<void>();

  get resolvedAlt(): string {
    const normalizedAlt: string = this.alt?.trim() ?? '';
    if (normalizedAlt.length > 0) {
      return normalizedAlt;
    }

    const normalizedFallbackAlt: string = this.fallbackAlt?.trim() ?? '';
    return normalizedFallbackAlt.length > 0 ? normalizedFallbackAlt : 'AMUSEMENT-PARKS.fun';
  }

  onImageError(): void {
    this.imageError.emit();
  }
}
