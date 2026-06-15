import { ChangeDetectionStrategy, Component, HostBinding, HostListener, Input, OnChanges, SimpleChanges } from '@angular/core';
import { NgFor, NgIf } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { ImageDisplayComponent } from '@shared/components/image-display/image-display.component';
import { UiChipComponent, UiSectionHeaderComponent, UiSurfaceDirective } from '@ui/primitives';
import { UiPhotoCarouselCategoryOption, UiPhotoCarouselImage } from '../models/ui-photo-carousel.model';

@Component({
  selector: 'app-ui-photo-carousel',
  templateUrl: './ui-photo-carousel.component.html',
  styleUrls: ['./ui-photo-carousel.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    NgIf,
    NgFor,
    ImageDisplayComponent,
    TranslateModule,
    UiChipComponent,
    UiSectionHeaderComponent,
    UiSurfaceDirective,
    RouterLink
  ]
})
export class UiPhotoCarouselComponent implements OnChanges {
  @Input() photos: UiPhotoCarouselImage[] = [];
  @Input() categories: UiPhotoCarouselCategoryOption[] = [];
  @Input() displayLimits: number[] = [4, 8, 12, 0];
  @Input() defaultDisplayLimit: number = 4;
  @Input() tone: string = 'primary';

  @Input() kickerIconClass: string = 'pi pi-images';
  @Input() kickerLabelKey: string = 'ui.photoCarousel.kicker';
  @Input() titleKey: string = 'ui.photoCarousel.title';
  @Input() subtitleKey: string | null = 'ui.photoCarousel.subtitle';

  @Input() categoryAriaLabelKey: string = 'ui.photoCarousel.controls.category';
  @Input() allCategoriesLabelKey: string = 'ui.photoCarousel.controls.allCategories';
  @Input() displayCountLabelKey: string = 'ui.photoCarousel.controls.displayCount';
  @Input() countLimitLabelKey: string = 'ui.photoCarousel.controls.count';
  @Input() allLimitLabelKey: string = 'ui.photoCarousel.controls.all';
  @Input() previousLabelKey: string = 'ui.photoCarousel.controls.previous';
  @Input() nextLabelKey: string = 'ui.photoCarousel.controls.next';
  @Input() openFullscreenLabelKey: string = 'ui.photoCarousel.controls.openFullscreen';
  @Input() closeFullscreenLabelKey: string = 'ui.photoCarousel.controls.closeFullscreen';
  @Input() lightboxTitleKey: string = 'ui.photoCarousel.lightbox.title';
  @Input() currentLabelKey: string = 'ui.photoCarousel.current';

  @HostBinding('class.ui-photo-carousel') protected readonly hostClass: boolean = true;

  @HostBinding('attr.data-photo-tone') protected get hostTone(): string {
    return this.tone;
  }

  selectedCategoryKey: string | null = null;
  selectedLimit: number = this.defaultDisplayLimit;
  activePhotoIndex: number = 0;
  lightboxOpen: boolean = false;

  private resolvedSelectedCategoryKeyValue: string | null = null;
  private filteredPhotosValue: UiPhotoCarouselImage[] = [];
  private displayedPhotosValue: UiPhotoCarouselImage[] = [];
  private activePhotoValue: UiPhotoCarouselImage | null = null;

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['defaultDisplayLimit'] && !changes['defaultDisplayLimit'].isFirstChange()) {
      this.selectedLimit = this.defaultDisplayLimit;
    }

    this.refreshDerivedPhotos();
  }

  @HostListener('document:keydown', ['$event'])
  onDocumentKeydown(event: KeyboardEvent): void {
    if (!this.lightboxOpen) {
      return;
    }

    if (event.key === 'Escape') {
      this.closeLightbox();
      return;
    }

    if (event.key === 'ArrowLeft') {
      event.preventDefault();
      this.previousPhoto();
      return;
    }

    if (event.key === 'ArrowRight') {
      event.preventDefault();
      this.nextPhoto();
    }
  }

  selectCategory(categoryKey: string | null): void {
    this.selectedCategoryKey = categoryKey;
    this.activePhotoIndex = 0;
    this.refreshDerivedPhotos();
  }

  setLimit(limit: number): void {
    this.selectedLimit = limit;
    this.activePhotoIndex = 0;
    this.refreshDerivedPhotos();
  }

  selectPhoto(photoIndex: number): void {
    this.activePhotoIndex = Math.max(0, photoIndex);
    this.clampActivePhotoIndex();
    this.refreshActivePhoto();
  }

  openLightbox(photoIndex: number): void {
    if (this.displayedPhotosValue.length === 0) {
      return;
    }

    this.selectPhoto(photoIndex);
    this.lightboxOpen = true;
  }

  closeLightbox(): void {
    this.lightboxOpen = false;
  }

  previousPhoto(): void {
    const currentPhotos: UiPhotoCarouselImage[] = this.displayedPhotosValue;

    if (currentPhotos.length <= 1) {
      return;
    }

    this.activePhotoIndex = this.activePhotoIndex === 0
      ? currentPhotos.length - 1
      : this.activePhotoIndex - 1;
    this.refreshActivePhoto();
  }

  nextPhoto(): void {
    const currentPhotos: UiPhotoCarouselImage[] = this.displayedPhotosValue;

    if (currentPhotos.length <= 1) {
      return;
    }

    this.activePhotoIndex = this.activePhotoIndex >= currentPhotos.length - 1
      ? 0
      : this.activePhotoIndex + 1;
    this.refreshActivePhoto();
  }

  hasSelectedCategory(categoryKey: string | null): boolean {
    return this.resolvedSelectedCategoryKeyValue === categoryKey;
  }

  get activePhoto(): UiPhotoCarouselImage | null {
    return this.activePhotoValue;
  }

  get displayedPhotos(): UiPhotoCarouselImage[] {
    return this.displayedPhotosValue;
  }

  get filteredPhotos(): UiPhotoCarouselImage[] {
    return this.filteredPhotosValue;
  }

  get limitLabelKey(): string {
    return this.selectedLimit <= 0 ? this.allLimitLabelKey : this.countLimitLabelKey;
  }

  getLimitLabelKey(limit: number): string {
    return limit <= 0 ? this.allLimitLabelKey : this.countLimitLabelKey;
  }

  getCategoryCountLabel(category: UiPhotoCarouselCategoryOption): string {
    return `${category.count}`;
  }

  trackByCategory(_index: number, category: UiPhotoCarouselCategoryOption): string {
    return category.key;
  }

  trackByDisplayLimit(_index: number, limit: number): number {
    return limit;
  }

  trackByPhoto(_index: number, photo: UiPhotoCarouselImage): string {
    return photo.imageId;
  }

  private resolveSelectedCategoryKey(): string | null {
    if (!this.selectedCategoryKey) {
      return null;
    }

    const hasSelectedCategory: boolean = this.photos.some((photo: UiPhotoCarouselImage) => photo.categoryKey === this.selectedCategoryKey);
    return hasSelectedCategory ? this.selectedCategoryKey : null;
  }

  private refreshDerivedPhotos(): void {
    this.resolvedSelectedCategoryKeyValue = this.resolveSelectedCategoryKey();
    this.filteredPhotosValue = this.resolvedSelectedCategoryKeyValue
      ? this.photos.filter((photo: UiPhotoCarouselImage) => photo.categoryKey === this.resolvedSelectedCategoryKeyValue)
      : this.photos;
    this.displayedPhotosValue = this.selectedLimit <= 0
      ? this.filteredPhotosValue
      : this.filteredPhotosValue.slice(0, this.selectedLimit);
    this.clampActivePhotoIndex();
    this.refreshActivePhoto();
  }

  private clampActivePhotoIndex(): void {
    const currentPhotos: UiPhotoCarouselImage[] = this.displayedPhotosValue;

    if (currentPhotos.length === 0) {
      this.activePhotoIndex = 0;
      return;
    }

    this.activePhotoIndex = Math.min(Math.max(this.activePhotoIndex, 0), currentPhotos.length - 1);
  }

  private refreshActivePhoto(): void {
    if (this.displayedPhotosValue.length === 0) {
      this.activePhotoValue = null;
      return;
    }

    this.activePhotoValue = this.displayedPhotosValue[Math.min(this.activePhotoIndex, this.displayedPhotosValue.length - 1)] ?? this.displayedPhotosValue[0];
  }
}
