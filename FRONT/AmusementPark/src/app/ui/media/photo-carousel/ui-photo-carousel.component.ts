import { ChangeDetectionStrategy, Component, HostBinding, HostListener, Input, OnChanges, SimpleChanges } from '@angular/core';
import { NgFor, NgIf } from '@angular/common';
import { CdkTrapFocus } from '@angular/cdk/a11y';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { LocalizedPluralPipe } from '@shared/pipes';

import { MapMarker } from '@app/models/map/map-marker';
import { LeafletMapComponent } from '@shared/components/leaflet-map/leaflet-map.component';
import { ImageDisplayComponent } from '@shared/components/image-display/image-display.component';
import { SafeExternalUrlPipe } from '@shared/pipes';
import { UiChipComponent, UiSectionHeaderComponent, UiSurfaceDirective } from '@ui/primitives';
import {
  UiPhotoCarouselAxisOption,
  UiPhotoCarouselCategoryOption,
  UiPhotoCarouselImage,
  UiPhotoCarouselTagLabel
} from '../models/ui-photo-carousel.model';

@Component({
  selector: 'app-ui-photo-carousel',
  templateUrl: './ui-photo-carousel.component.html',
  styleUrls: ['./ui-photo-carousel.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    NgIf,
    NgFor,
    CdkTrapFocus,
    ImageDisplayComponent,
    LeafletMapComponent,
    TranslateModule,
    LocalizedPluralPipe,
    UiChipComponent,
    UiSectionHeaderComponent,
    UiSurfaceDirective,
    RouterLink,
    SafeExternalUrlPipe
  ]
})
export class UiPhotoCarouselComponent implements OnChanges {
  private static readonly UnknownYearKey = 'unknown';
  private static readonly DefaultMapCenter: [number, number] = [0, 0];
  private static readonly DefaultMapZoom = 2;

  @Input() photos: UiPhotoCarouselImage[] = [];
  @Input() categories: UiPhotoCarouselCategoryOption[] = [];
  @Input() displayLimits: number[] = [4, 8, 12, 0];
  @Input() defaultDisplayLimit: number = 4;
  @Input() tone: string = 'primary';
  @Input() language: string = 'en';

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

  @Input() yearAriaLabelKey: string = 'ui.photoCarousel.axes.years';
  @Input() tagAriaLabelKey: string = 'ui.photoCarousel.axes.tags';
  @Input() allYearsLabelKey: string = 'ui.photoCarousel.axes.allYears';
  @Input() allTagsLabelKey: string = 'ui.photoCarousel.axes.allTags';
  @Input() mapTitleKey: string = 'ui.photoCarousel.map.title';
  @Input() mapSubtitleKey: string = 'ui.photoCarousel.map.subtitle';
  @Input() mapEmptyKey: string = 'ui.photoCarousel.map.empty';
  @Input() emptySelectionKey: string = 'ui.photoCarousel.selection.empty';
  @Input() selectionCountKey: string = 'ui.photoCarousel.selection.count';

  @HostBinding('class.ui-photo-carousel') protected readonly hostClass: boolean = true;

  @HostBinding('attr.data-photo-tone') protected get hostTone(): string {
    return this.tone;
  }

  selectedYearKey: string | null = null;
  selectedTagKey: string | null = null;
  selectedCategoryKey: string | null = null;
  selectedLimit: number = this.defaultDisplayLimit;
  activePhotoIndex: number = 0;
  lightboxOpen: boolean = false;
  readonly stageResponsiveWidths: readonly number[] = [480, 640, 800, 960, 1280, 1600];
  readonly thumbResponsiveWidths: readonly number[] = [240, 320, 480];

  private resolvedSelectedYearKeyValue: string | null = null;
  private resolvedSelectedTagKeyValue: string | null = null;
  private yearOptionsValue: UiPhotoCarouselAxisOption[] = [];
  private tagOptionsValue: UiPhotoCarouselAxisOption[] = [];
  private filteredPhotosValue: UiPhotoCarouselImage[] = [];
  private displayedPhotosValue: UiPhotoCarouselImage[] = [];
  private activePhotoValue: UiPhotoCarouselImage | null = null;
  private mapMarkersValue: MapMarker[] = [];
  private mapCenterValue: [number, number] = UiPhotoCarouselComponent.DefaultMapCenter;

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['defaultDisplayLimit']) {
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

  selectYear(yearKey: string | null): void {
    this.selectedYearKey = yearKey;
    this.activePhotoIndex = 0;
    this.refreshDerivedPhotos();
  }

  selectTag(tagKey: string | null): void {
    this.selectedTagKey = tagKey;
    this.selectedCategoryKey = tagKey;
    this.activePhotoIndex = 0;
    this.refreshDerivedPhotos();
  }

  selectCategory(categoryKey: string | null): void {
    this.selectTag(categoryKey);
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
    this.refreshMap();
  }

  selectPhotoById(photoId: string | null | undefined): void {
    const normalizedPhotoId: string = photoId?.trim() ?? '';
    if (!normalizedPhotoId) {
      return;
    }

    const nextIndex: number = this.displayedPhotosValue.findIndex((photo: UiPhotoCarouselImage) => photo.imageId === normalizedPhotoId);
    if (nextIndex < 0) {
      return;
    }

    this.selectPhoto(nextIndex);
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
    this.refreshMap();
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
    this.refreshMap();
  }

  hasSelectedYear(yearKey: string | null): boolean {
    return this.resolvedSelectedYearKeyValue === yearKey;
  }

  hasSelectedTag(tagKey: string | null): boolean {
    return this.resolvedSelectedTagKeyValue === tagKey;
  }

  hasSelectedCategory(categoryKey: string | null): boolean {
    return this.hasSelectedTag(categoryKey);
  }

  onMapMarkerClick(marker: MapMarker): void {
    this.selectPhotoById(marker.id);
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

  get yearOptions(): UiPhotoCarouselAxisOption[] {
    return this.yearOptionsValue;
  }

  get tagOptions(): UiPhotoCarouselAxisOption[] {
    return this.tagOptionsValue;
  }

  get mapMarkers(): MapMarker[] {
    return this.mapMarkersValue;
  }

  get mapCenter(): [number, number] {
    return this.mapCenterValue;
  }

  get mapZoom(): number {
    return UiPhotoCarouselComponent.DefaultMapZoom;
  }

  get selectedMarkerId(): string | null {
    return this.activePhotoValue && this.hasUsableCoordinates(this.activePhotoValue)
      ? this.activePhotoValue.imageId
      : null;
  }

  get limitLabelKey(): string {
    return this.selectedLimit <= 0 ? this.allLimitLabelKey : this.countLimitLabelKey;
  }

  get selectedCount(): number {
    return this.displayedPhotosValue.length;
  }

  getLimitLabelKey(limit: number): string {
    return limit <= 0 ? this.allLimitLabelKey : this.countLimitLabelKey;
  }

  getCategoryCountLabel(category: UiPhotoCarouselCategoryOption): string {
    return `${category.count}`;
  }

  getAxisCountLabel(option: UiPhotoCarouselAxisOption): string {
    return `${option.count}`;
  }

  getPrimaryTag(photo: UiPhotoCarouselImage): UiPhotoCarouselTagLabel | null {
    return photo.tagLabels[0] ?? null;
  }

  trackByCategory(_index: number, category: UiPhotoCarouselCategoryOption): string {
    return category.key;
  }

  trackByAxisOption(_index: number, option: UiPhotoCarouselAxisOption): string {
    return option.key;
  }

  trackByDisplayLimit(_index: number, limit: number): number {
    return limit;
  }

  trackByPhoto(_index: number, photo: UiPhotoCarouselImage): string {
    return photo.imageId;
  }

  trackByTagLabel(_index: number, tag: UiPhotoCarouselTagLabel): string {
    return tag.key;
  }

  private refreshDerivedPhotos(): void {
    const previousActiveImageId: string | null = this.activePhotoValue?.imageId ?? null;

    this.yearOptionsValue = this.buildYearOptions();
    this.tagOptionsValue = this.buildTagOptions();
    this.resolvedSelectedYearKeyValue = this.resolveSelectedYearKey();
    this.resolvedSelectedTagKeyValue = this.resolveSelectedTagKey();
    this.selectedCategoryKey = this.resolvedSelectedTagKeyValue;
    this.filteredPhotosValue = this.photos.filter((photo: UiPhotoCarouselImage) => this.matchesSelectedAxes(photo));
    this.displayedPhotosValue = this.selectedLimit <= 0
      ? this.filteredPhotosValue
      : this.filteredPhotosValue.slice(0, this.selectedLimit);

    this.restoreActivePhoto(previousActiveImageId);
    this.clampActivePhotoIndex();
    this.refreshActivePhoto();
    this.refreshMap();
  }

  private restoreActivePhoto(previousActiveImageId: string | null): void {
    if (!previousActiveImageId) {
      return;
    }

    const previousPhotoIndex: number = this.displayedPhotosValue.findIndex((photo: UiPhotoCarouselImage) => photo.imageId === previousActiveImageId);
    if (previousPhotoIndex >= 0) {
      this.activePhotoIndex = previousPhotoIndex;
    }
  }

  private matchesSelectedAxes(photo: UiPhotoCarouselImage): boolean {
    const matchesYear: boolean = !this.resolvedSelectedYearKeyValue || photo.year === this.resolvedSelectedYearKeyValue;
    const matchesTag: boolean = !this.resolvedSelectedTagKeyValue || photo.tagKeys.includes(this.resolvedSelectedTagKeyValue);
    return matchesYear && matchesTag;
  }

  private resolveSelectedYearKey(): string | null {
    if (!this.selectedYearKey) {
      return null;
    }

    const hasSelectedYear: boolean = this.yearOptionsValue.some((option: UiPhotoCarouselAxisOption) => option.key === this.selectedYearKey);
    return hasSelectedYear ? this.selectedYearKey : null;
  }

  private resolveSelectedTagKey(): string | null {
    const selectedKey: string | null = this.selectedTagKey ?? this.selectedCategoryKey;
    if (!selectedKey) {
      return null;
    }

    const hasSelectedTag: boolean = this.tagOptionsValue.some((option: UiPhotoCarouselAxisOption) => option.key === selectedKey);
    return hasSelectedTag ? selectedKey : null;
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

  private refreshMap(): void {
    this.mapMarkersValue = this.buildPhotoMapMarkers();
    this.mapCenterValue = this.resolveMapCenter();
  }

  private buildYearOptions(): UiPhotoCarouselAxisOption[] {
    const optionsByKey: Map<string, UiPhotoCarouselAxisOption> = new Map<string, UiPhotoCarouselAxisOption>();

    for (const photo of this.photos) {
      const key: string = photo.year || UiPhotoCarouselComponent.UnknownYearKey;
      const currentOption: UiPhotoCarouselAxisOption | undefined = optionsByKey.get(key);

      optionsByKey.set(key, {
        key,
        label: photo.yearLabel || key,
        labelKey: photo.yearLabelKey ?? null,
        count: (currentOption?.count ?? 0) + 1
      });
    }

    return Array.from(optionsByKey.values()).sort((first: UiPhotoCarouselAxisOption, second: UiPhotoCarouselAxisOption): number => {
      if (first.key === UiPhotoCarouselComponent.UnknownYearKey) {
        return 1;
      }

      if (second.key === UiPhotoCarouselComponent.UnknownYearKey) {
        return -1;
      }

      return second.key.localeCompare(first.key);
    });
  }

  private buildTagOptions(): UiPhotoCarouselAxisOption[] {
    const optionsByKey: Map<string, UiPhotoCarouselAxisOption> = new Map<string, UiPhotoCarouselAxisOption>();

    for (const photo of this.photos) {
      for (const tag of photo.tagLabels) {
        const currentOption: UiPhotoCarouselAxisOption | undefined = optionsByKey.get(tag.key);

        optionsByKey.set(tag.key, {
          key: tag.key,
          label: tag.label,
          labelKey: tag.labelKey ?? null,
          count: (currentOption?.count ?? 0) + 1
        });
      }
    }

    return Array.from(optionsByKey.values()).sort((first: UiPhotoCarouselAxisOption, second: UiPhotoCarouselAxisOption): number => {
      if (first.count !== second.count) {
        return second.count - first.count;
      }

      return first.label.localeCompare(second.label, this.language || 'en');
    });
  }

  private buildPhotoMapMarkers(): MapMarker[] {
    return this.displayedPhotosValue
      .filter((photo: UiPhotoCarouselImage) => this.hasUsableCoordinates(photo))
      .map((photo: UiPhotoCarouselImage): MapMarker => {
        const credits: string[] = photo.credit ? [photo.credit] : [];
        const tagDetails: string[] = photo.tagLabels
          .filter((tag: UiPhotoCarouselTagLabel) => !tag.labelKey)
          .map((tag: UiPhotoCarouselTagLabel) => tag.label);
        const tagTranslationKeys: string[] = photo.tagLabels
          .map((tag: UiPhotoCarouselTagLabel) => tag.labelKey)
          .filter((labelKey: string | null | undefined): labelKey is string => !!labelKey);

        return {
          id: photo.imageId,
          lat: photo.latitude as number,
          lng: photo.longitude as number,
          title: photo.caption ?? photo.description ?? photo.alt,
          subtitle: photo.sourceTitle ?? null,
          details: [...credits, ...tagDetails],
          detailTranslationKeys: tagTranslationKeys,
          iconKind: 'photo'
        };
      });
  }

  private resolveMapCenter(): [number, number] {
    if (this.activePhotoValue && this.hasUsableCoordinates(this.activePhotoValue)) {
      return [this.activePhotoValue.latitude as number, this.activePhotoValue.longitude as number];
    }

    const firstMarker: MapMarker | undefined = this.mapMarkersValue[0];
    return firstMarker ? [firstMarker.lat, firstMarker.lng] : UiPhotoCarouselComponent.DefaultMapCenter;
  }

  private hasUsableCoordinates(photo: UiPhotoCarouselImage): boolean {
    const latitude: number | null | undefined = photo.latitude;
    const longitude: number | null | undefined = photo.longitude;

    return typeof latitude === 'number'
      && typeof longitude === 'number'
      && Number.isFinite(latitude)
      && Number.isFinite(longitude)
      && latitude >= -90
      && latitude <= 90
      && longitude >= -180
      && longitude <= 180;
  }
}
