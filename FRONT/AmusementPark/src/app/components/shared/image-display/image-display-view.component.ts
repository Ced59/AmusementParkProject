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
  @Input() alt: string = '';
  @Input() imgClass: string = '';
  @Input() placeholderClass: string = '';
  @Input() placeholderIconClass: string = 'pi pi-image';

  @Output() imageError: EventEmitter<void> = new EventEmitter<void>();

  onImageError(): void {
    this.imageError.emit();
  }
}
