import { ChangeDetectionStrategy, Component, HostBinding, HostListener, Input, OnChanges, SimpleChanges } from '@angular/core';
import { NgFor, NgIf } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { MapMarker } from '@app/models/map/map-marker';
import { LeafletMapComponent } from '@shared/components/leaflet-map/leaflet-map.component';
import { ImageDisplayComponent } from '@shared/components/image-display/image-display.component';
import { SafeExternalUrlPipe } from '@shared/pipes';
import { UiChipComponent, UiSectionHeaderComponent, UiSurfaceDirective } from '@ui/primitives';
import {
  UiPhotoCarouselAxisOption,
  UiPhotoCarouselCategoryOption,
  UiPhotoCarouselImage,
  UiPhotoCarouselMetadataRow,
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
    ImageDisplayComponent,
    LeafletMapComponent,
    TranslateModule,
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
  @Input() metadataTitleKey: string = 'ui.photoCarousel.metadata.title';
  @Input() metadataSubtitleKey: string = 'ui.photoCarousel.metadata.subtitle';
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
  private metadataRowsValue: UiPhotoCarouselMetadataRow[] = [];
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
    this.refreshMetadataAndMap();
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
    this.refreshMetadataAndMap();
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
    this.refreshMetadataAndMap();
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

  get metadataRows(): UiPhotoCarouselMetadataRow[] {
    return this.metadataRowsValue;
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

  trackByMetadataRow(_index: number, row: UiPhotoCarouselMetadataRow): string {
    return row.key;
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
    this.refreshMetadataAndMap();
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

  private refreshMetadataAndMap(): void {
    this.metadataRowsValue = this.activePhotoValue ? this.buildMetadataRows(this.activePhotoValue) : [];
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

  private buildMetadataRows(photo: UiPhotoCarouselImage): UiPhotoCarouselMetadataRow[] {
    const rows: UiPhotoCarouselMetadataRow[] = [];

    this.addMetadataRow(rows, 'takenOn', 'ui.photoCarousel.metadata.takenOn', this.formatDateTime(photo.takenOn), 'pi pi-calendar');
    this.addMetadataRow(rows, 'uploadedAt', 'ui.photoCarousel.metadata.uploadedAt', this.formatDateTime(photo.uploadedAt), 'pi pi-cloud-upload');
    this.addMetadataRow(rows, 'location', 'ui.photoCarousel.metadata.location', this.formatCoordinates(photo), 'pi pi-map-marker');
    this.addMetadataRow(rows, 'dimensions', 'ui.photoCarousel.metadata.dimensions', this.formatDimensions(photo), 'pi pi-expand');
    this.addMetadataRow(rows, 'camera', 'ui.photoCarousel.metadata.camera', this.formatCamera(photo), 'pi pi-camera');
    this.addMetadataRow(rows, 'settings', 'ui.photoCarousel.metadata.settings', this.formatCameraSettings(photo), 'pi pi-sliders-h');
    this.addMetadataRow(rows, 'credit', 'ui.photoCarousel.metadata.credit', photo.credit ?? null, 'pi pi-user');
    this.addMetadataRow(rows, 'file', 'ui.photoCarousel.metadata.file', this.formatFile(photo), 'pi pi-file');

    return rows;
  }

  private addMetadataRow(rows: UiPhotoCarouselMetadataRow[], key: string, labelKey: string, value: string | null, iconClass: string): void {
    if (!value) {
      return;
    }

    rows.push({
      key,
      labelKey,
      value,
      iconClass
    });
  }

  private buildPhotoMapMarkers(): MapMarker[] {
    return this.displayedPhotosValue
      .filter((photo: UiPhotoCarouselImage) => this.hasUsableCoordinates(photo))
      .map((photo: UiPhotoCarouselImage): MapMarker => {
        const rawDetails: string[] = [
          this.formatDateTime(photo.takenOn),
          this.formatDimensions(photo)
        ].filter((value: string | null): value is string => !!value);
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
          title: photo.caption ?? photo.alt,
          subtitle: photo.sourceTitle ?? null,
          details: [...rawDetails, ...tagDetails],
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

  private formatDateTime(value: string | null | undefined): string | null {
    const normalizedValue: string = value?.trim() ?? '';
    if (!normalizedValue) {
      return null;
    }

    const date: Date = new Date(normalizedValue);
    if (Number.isNaN(date.getTime())) {
      return normalizedValue;
    }

    try {
      return new Intl.DateTimeFormat(this.language || 'en', { dateStyle: 'medium' }).format(date);
    } catch {
      return normalizedValue;
    }
  }

  private formatCoordinates(photo: UiPhotoCarouselImage): string | null {
    if (!this.hasUsableCoordinates(photo)) {
      return null;
    }

    return `${(photo.latitude as number).toFixed(5)}, ${(photo.longitude as number).toFixed(5)}`;
  }

  private formatDimensions(photo: UiPhotoCarouselImage): string | null {
    if (!photo.width || !photo.height) {
      return null;
    }

    return `${photo.width} x ${photo.height}px`;
  }

  private formatCamera(photo: UiPhotoCarouselImage): string | null {
    return [photo.cameraMaker, photo.cameraModel]
      .map((value: string | null | undefined) => value?.trim() ?? '')
      .filter((value: string) => value.length > 0)
      .join(' ') || null;
  }

  private formatCameraSettings(photo: UiPhotoCarouselImage): string | null {
    const settings: string[] = [];

    if (typeof photo.focalLength === 'number') {
      settings.push(`${this.formatNumber(photo.focalLength)}mm`);
    }

    if (typeof photo.aperture === 'number') {
      settings.push(`f/${this.formatNumber(photo.aperture)}`);
    }

    if (typeof photo.exposureTime === 'number') {
      settings.push(`${this.formatExposure(photo.exposureTime)}s`);
    }

    if (typeof photo.iso === 'number') {
      settings.push(`ISO ${Math.round(photo.iso)}`);
    }

    return settings.join(' - ') || null;
  }

  private formatExposure(value: number): string {
    if (value > 0 && value < 1) {
      const denominator: number = Math.round(1 / value);
      return `1/${denominator}`;
    }

    return this.formatNumber(value);
  }

  private formatFile(photo: UiPhotoCarouselImage): string | null {
    const parts: string[] = [];

    if (photo.originalFileName) {
      parts.push(photo.originalFileName);
    }

    if (photo.contentType) {
      parts.push(photo.contentType);
    }

    if (typeof photo.sizeInBytes === 'number') {
      parts.push(this.formatFileSize(photo.sizeInBytes));
    }

    return parts.join(' - ') || null;
  }

  private formatFileSize(value: number): string {
    if (value >= 1024 * 1024) {
      return `${this.formatNumber(value / (1024 * 1024))} MB`;
    }

    if (value >= 1024) {
      return `${this.formatNumber(value / 1024)} KB`;
    }

    return `${Math.round(value)} B`;
  }

  private formatNumber(value: number): string {
    try {
      return new Intl.NumberFormat(this.language || 'en', { maximumFractionDigits: 1 }).format(value);
    } catch {
      return String(Math.round(value * 10) / 10);
    }
  }
}
