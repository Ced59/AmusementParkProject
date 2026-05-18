import { ChangeDetectionStrategy, Component, HostBinding, HostListener, Input, OnChanges, SimpleChanges } from '@angular/core';
import { NgFor, NgIf } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';

import { ImageDisplayComponent } from '@app/components/shared/image-display/image-display.component';
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
    UiSurfaceDirective
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

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['defaultDisplayLimit'] && !changes['defaultDisplayLimit'].isFirstChange()) {
      this.selectedLimit = this.defaultDisplayLimit;
    }

    this.clampActivePhotoIndex();
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
    this.clampActivePhotoIndex();
  }

  setLimit(limit: number): void {
    this.selectedLimit = limit;
    this.activePhotoIndex = 0;
    this.clampActivePhotoIndex();
  }

  selectPhoto(photoIndex: number): void {
    this.activePhotoIndex = Math.max(0, photoIndex);
    this.clampActivePhotoIndex();
  }

  openLightbox(photoIndex: number): void {
    if (this.displayedPhotos.length === 0) {
      return;
    }

    this.selectPhoto(photoIndex);
    this.lightboxOpen = true;
  }

  closeLightbox(): void {
    this.lightboxOpen = false;
  }

  previousPhoto(): void {
    const currentPhotos: UiPhotoCarouselImage[] = this.displayedPhotos;

    if (currentPhotos.length <= 1) {
      return;
    }

    this.activePhotoIndex = this.activePhotoIndex === 0
      ? currentPhotos.length - 1
      : this.activePhotoIndex - 1;
  }

  nextPhoto(): void {
    const currentPhotos: UiPhotoCarouselImage[] = this.displayedPhotos;

    if (currentPhotos.length <= 1) {
      return;
    }

    this.activePhotoIndex = this.activePhotoIndex >= currentPhotos.length - 1
      ? 0
      : this.activePhotoIndex + 1;
  }

  hasSelectedCategory(categoryKey: string | null): boolean {
    return this.resolvedSelectedCategoryKey === categoryKey;
  }

  get activePhoto(): UiPhotoCarouselImage | null {
    const currentPhotos: UiPhotoCarouselImage[] = this.displayedPhotos;

    if (currentPhotos.length === 0) {
      return null;
    }

    return currentPhotos[Math.min(this.activePhotoIndex, currentPhotos.length - 1)] ?? currentPhotos[0];
  }

  get displayedPhotos(): UiPhotoCarouselImage[] {
    const filteredPhotos: UiPhotoCarouselImage[] = this.filteredPhotos;

    if (this.selectedLimit <= 0) {
      return filteredPhotos;
    }

    return filteredPhotos.slice(0, this.selectedLimit);
  }

  get filteredPhotos(): UiPhotoCarouselImage[] {
    const selectedCategoryKey: string | null = this.resolvedSelectedCategoryKey;

    if (!selectedCategoryKey) {
      return this.photos;
    }

    return this.photos.filter((photo: UiPhotoCarouselImage) => photo.categoryKey === selectedCategoryKey);
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

  private get resolvedSelectedCategoryKey(): string | null {
    if (!this.selectedCategoryKey) {
      return null;
    }

    const hasSelectedCategory: boolean = this.photos.some((photo: UiPhotoCarouselImage) => photo.categoryKey === this.selectedCategoryKey);
    return hasSelectedCategory ? this.selectedCategoryKey : null;
  }

  private clampActivePhotoIndex(): void {
    const currentPhotos: UiPhotoCarouselImage[] = this.displayedPhotos;

    if (currentPhotos.length === 0) {
      this.activePhotoIndex = 0;
      return;
    }

    this.activePhotoIndex = Math.min(Math.max(this.activePhotoIndex, 0), currentPhotos.length - 1);
  }
}
