import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

import { AdminImageSortDirection, AdminImageSortField } from '@app/models/images/admin-image-search-query';
import { ImageCategory } from '@app/models/images/image-category';
import { ImageDto } from '@app/models/images/image-dto';
import { ImageOwnerType } from '@app/models/images/image-owner-type';
import { ImageTagDto } from '@app/models/images/image-tag-dto';
import { LocalizedItemDto } from '@app/models/shared/localized-item-dto';
import { AdminSiteStateFacade } from '@features/admin/site/state/admin-site-state.facade';
import { ImageDisplayComponent } from '@shared/components/image-display/image-display.component';
import { PageStateComponent } from '@shared/components/page-state/page-state.component';

@Component({
  selector: 'app-admin-site',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslateModule, PageStateComponent, ImageDisplayComponent],
  templateUrl: './admin-site.component.html',
  styleUrl: './admin-site.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [AdminSiteStateFacade],
})
export class AdminSiteComponent implements OnInit {
  protected readonly state = this.stateFacade.state;
  protected readonly images = this.stateFacade.images;
  protected readonly tags = this.stateFacade.tags;
  protected readonly selectedImage = this.stateFacade.selectedImage;
  protected readonly query = this.stateFacade.query;
  protected readonly pagination = this.stateFacade.pagination;
  protected readonly selectedImageIds = this.stateFacade.selectedImageIds;
  protected readonly operationErrorKey = this.stateFacade.operationErrorKey;
  protected readonly selectedCount = this.stateFacade.selectedCount;
  protected readonly isEveryPageImageSelected = this.stateFacade.isEveryPageImageSelected;
  protected readonly categories: ImageCategory[] = [
    ImageCategory.LOGO,
    ImageCategory.AVATAR,
    ImageCategory.PARK,
    ImageCategory.PARK_ITEM,
    ImageCategory.OPERATOR,
    ImageCategory.MANUFACTURER,
    ImageCategory.FOUNDER,
    ImageCategory.VIDEO_THUMBNAIL
  ];
  protected readonly ownerTypes: ImageOwnerType[] = [
    ImageOwnerType.NONE,
    ImageOwnerType.PARK,
    ImageOwnerType.USER,
    ImageOwnerType.PARK_ITEM,
    ImageOwnerType.PARK_OPERATOR,
    ImageOwnerType.ATTRACTION_MANUFACTURER,
    ImageOwnerType.PARK_FOUNDER,
    ImageOwnerType.VIDEO
  ];
  protected readonly sortFields: AdminImageSortField[] = ['created', 'updated', 'filename', 'size', 'dimensions'];
  protected readonly pageSizes: number[] = [20, 40, 60, 100];
  protected readonly defaultLanguageCode: string = 'fr';

  protected newTagSlug: string = '';
  protected bulkTagId: string = '';
  protected bulkCategory: ImageCategory | '' = '';

  constructor(
    private readonly stateFacade: AdminSiteStateFacade,
    private readonly translate: TranslateService
  ) {
  }

  ngOnInit(): void {
    this.reload();
  }

  protected reload(): void {
    this.stateFacade.reload();
  }

  protected applyQuery(): void {
    this.stateFacade.applyQuery();
  }

  protected clearFilters(): void {
    this.bulkTagId = '';
    this.bulkCategory = '';
    this.stateFacade.clearFilters();
  }

  protected updateSearch(value: string): void {
    this.stateFacade.updateQuery({ search: value || null });
  }

  protected updateCategory(value: string): void {
    this.stateFacade.updateQuery({ category: value ? value as ImageCategory : null });
  }

  protected updateOwnerType(value: string): void {
    this.stateFacade.updateQuery({ ownerType: value ? value as ImageOwnerType : null });
  }

  protected updateOwnerId(value: string): void {
    this.stateFacade.updateQuery({ ownerId: value || null });
  }

  protected updateTag(value: string): void {
    this.stateFacade.updateQuery({ tagId: value || null });
  }

  protected updatePublishedFilter(value: string): void {
    this.stateFacade.updateQuery({ isPublished: this.parseBooleanFilter(value) });
  }

  protected updateHasOwnerFilter(value: string): void {
    this.stateFacade.updateQuery({ hasOwner: this.parseBooleanFilter(value) });
  }

  protected updateHasGeoLocationFilter(value: string): void {
    this.stateFacade.updateQuery({ hasGeoLocation: this.parseBooleanFilter(value) });
  }

  protected updateSortBy(value: string): void {
    this.stateFacade.updateQuery({ sortBy: value as AdminImageSortField }, false);
  }

  protected updateSortDirection(value: string): void {
    this.stateFacade.updateQuery({ sortDirection: value as AdminImageSortDirection }, false);
  }

  protected changePage(page: number): void {
    const totalPages: number = this.pagination().totalPages || 1;
    const targetPage: number = Math.min(Math.max(1, page), totalPages);
    this.stateFacade.changePage(targetPage);
  }

  protected changePageSize(size: string): void {
    this.stateFacade.changePageSize(Number(size));
  }

  protected selectImage(image: ImageDto): void {
    this.stateFacade.selectImage(image);
  }

  protected toggleImageSelection(imageId: string, checked: boolean): void {
    this.stateFacade.toggleImageSelection(imageId, checked);
  }

  protected toggleCurrentPageSelection(checked: boolean): void {
    this.stateFacade.toggleCurrentPageSelection(checked);
  }

  protected clearSelection(): void {
    this.stateFacade.clearSelection();
  }

  protected isImageSelected(imageId: string): boolean {
    return this.selectedImageIds().includes(imageId);
  }

  protected saveImage(): void {
    this.stateFacade.saveSelectedImage();
  }

  protected applyWatermarkToSelectedImage(): void {
    this.stateFacade.applyWatermarkToSelectedImage();
  }

  protected createTag(): void {
    if (this.stateFacade.createTag(this.newTagSlug)) {
      this.newTagSlug = '';
    }
  }

  protected deleteImage(image: ImageDto): void {
    const confirmed: boolean = confirm(this.translate.instant('admin.images.deleteConfirm', {
      name: image.originalFileName || image.id,
    }));

    if (!confirmed) {
      return;
    }

    this.stateFacade.deleteImages([image.id]);
  }

  protected deleteSelectedImages(): void {
    const imageIds: string[] = this.selectedImageIds();

    if (imageIds.length === 0) {
      return;
    }

    const confirmed: boolean = confirm(this.translate.instant('admin.images.bulkDeleteConfirm', {
      count: imageIds.length,
    }));

    if (!confirmed) {
      return;
    }

    this.stateFacade.deleteImages(imageIds);
  }

  protected updateSelectedImageDescription(value: string): void {
    this.stateFacade.updateSelectedImage({ description: value });
  }

  protected updateSelectedImagePublished(isPublished: boolean): void {
    this.stateFacade.updateSelectedImage({ isPublished });
  }

  protected updateSelectedImageCategory(category: string): void {
    this.stateFacade.updateSelectedImage({ category: category as ImageCategory });
  }

  protected updateSelectedImageOwnerType(ownerType: string): void {
    const normalizedOwnerType: ImageOwnerType = ownerType as ImageOwnerType;
    const currentImage: ImageDto | null = this.selectedImage();
    this.stateFacade.updateSelectedImage({
      ownerType: normalizedOwnerType,
      ownerId: normalizedOwnerType === ImageOwnerType.NONE ? undefined : currentImage?.ownerId,
      isCurrent: normalizedOwnerType === ImageOwnerType.NONE ? false : (currentImage?.isCurrent ?? false),
    });
  }

  protected updateSelectedImageOwnerId(ownerId: string): void {
    this.stateFacade.updateSelectedImage({ ownerId: ownerId || undefined });
  }

  protected updateSelectedImageCurrent(isCurrent: boolean): void {
    this.stateFacade.updateSelectedImage({ isCurrent });
  }

  protected updateSelectedImageSourceUrl(sourceUrl: string): void {
    this.stateFacade.updateSelectedImage({ sourceUrl: sourceUrl || null });
  }

  protected updateSelectedImageLatitude(latitude: number | string): void {
    const selectedImage: ImageDto | null = this.selectedImage();

    this.stateFacade.updateSelectedImage({
      geoLocation: {
        latitude: Number(latitude),
        longitude: selectedImage?.geoLocation?.longitude ?? 0,
      },
    });
  }

  protected updateSelectedImageLongitude(longitude: number | string): void {
    const selectedImage: ImageDto | null = this.selectedImage();

    this.stateFacade.updateSelectedImage({
      geoLocation: {
        latitude: selectedImage?.geoLocation?.latitude ?? 0,
        longitude: Number(longitude),
      },
    });
  }

  protected clearSelectedImageGeoLocation(): void {
    this.stateFacade.updateSelectedImage({ geoLocation: null });
  }

  protected getOwnerSummary(image: ImageDto): string {
    if (image.ownerType === ImageOwnerType.NONE || !image.ownerId) {
      return this.translate.instant('admin.images.noOwner');
    }

    const ownerType: string = this.translate.instant(`images.ownerTypes.${image.ownerType}`);
    return `${ownerType} - ${image.ownerId}`;
  }

  protected getTagSummary(image: ImageDto): string {
    const tagIds: string[] = image.tagIds ?? [];

    if (tagIds.length === 0) {
      return this.translate.instant('admin.images.noTags');
    }

    return tagIds.map((tagId: string) => this.getTagSlug(tagId)).join(', ');
  }

  protected hasGeoLocation(image: ImageDto): boolean {
    return Number.isFinite(image.geoLocation?.latitude) && Number.isFinite(image.geoLocation?.longitude);
  }

  protected getGeoSummary(image: ImageDto): string {
    if (!this.hasGeoLocation(image)) {
      return this.translate.instant('admin.images.noGeo');
    }

    return `${image.geoLocation?.latitude}, ${image.geoLocation?.longitude}`;
  }

  protected getDimensionsSummary(image: ImageDto): string {
    return `${image.width} x ${image.height}`;
  }

  protected getImageTitle(image: ImageDto): string {
    return image.description || image.originalFileName || image.id;
  }

  protected getImageSubtitle(image: ImageDto): string {
    return image.originalFileName && image.description ? image.originalFileName : image.id;
  }

  protected getImageAlt(image: ImageDto): string {
    return image.description || image.originalFileName || image.id;
  }

  protected getCardAriaLabel(image: ImageDto): string {
    return `${this.getImageTitle(image)} - ${this.getOwnerSummary(image)}`;
  }

  protected getSelectionAriaLabel(image: ImageDto): string {
    return this.translate.instant('admin.images.selectImageAria', {
      name: image.originalFileName || image.id,
    });
  }

  protected getDeleteAriaLabel(image: ImageDto): string {
    return this.translate.instant('admin.images.deleteImageAria', {
      name: image.originalFileName || image.id,
    });
  }

  protected toggleTag(tagId: string, checked: boolean): void {
    this.stateFacade.toggleTag(tagId, checked);
  }

  protected updateLocalizedField(field: 'altTexts' | 'captions' | 'credits', value: string): void {
    const selectedImage: ImageDto | null = this.selectedImage();

    if (!selectedImage) {
      return;
    }

    const values: LocalizedItemDto<string>[] = this.upsertLocalizedValue(selectedImage[field] ?? [], value);
    this.stateFacade.updateSelectedImage({ [field]: values } as Partial<ImageDto>);
  }

  protected getLocalizedField(image: ImageDto, field: 'altTexts' | 'captions' | 'credits'): string {
    return image[field]?.find((item: LocalizedItemDto<string>) => item.languageCode === this.defaultLanguageCode)?.value ?? '';
  }

  protected applyBulkPublished(isPublished: boolean): void {
    this.stateFacade.applyBulkMetadata({ isPublished });
  }

  protected applyBulkCategory(): void {
    if (!this.bulkCategory) {
      return;
    }

    this.stateFacade.applyBulkMetadata({ category: this.bulkCategory });
  }

  protected applyBulkAddTag(): void {
    if (!this.bulkTagId) {
      return;
    }

    this.stateFacade.applyBulkMetadata({ addTagIds: [this.bulkTagId] });
  }

  protected applyBulkRemoveTag(): void {
    if (!this.bulkTagId) {
      return;
    }

    this.stateFacade.applyBulkMetadata({ removeTagIds: [this.bulkTagId] });
  }

  protected getTagSlug(tagId: string): string {
    return this.tags().find((tag: ImageTagDto) => tag.id === tagId)?.slug ?? tagId;
  }

  protected canApplyWatermark(image: ImageDto): boolean {
    return image.category !== ImageCategory.LOGO && !image.isWatermarked && !!image.path;
  }

  protected formatBytes(value: number | null | undefined): string {
    const bytes: number = value ?? 0;

    if (bytes <= 0) {
      return '-';
    }

    const units: string[] = ['B', 'KB', 'MB', 'GB'];
    let size: number = bytes;
    let unitIndex: number = 0;

    while (size >= 1024 && unitIndex < units.length - 1) {
      size = size / 1024;
      unitIndex++;
    }

    return `${size.toFixed(unitIndex === 0 ? 0 : 1)} ${units[unitIndex]}`;
  }

  protected formatDate(value: string | null | undefined): string {
    if (!value) {
      return '-';
    }

    return new Intl.DateTimeFormat('fr-FR', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    }).format(new Date(value));
  }

  protected trackById(_: number, item: { id: string }): string {
    return item.id;
  }

  protected booleanFilterValue(value: boolean | null | undefined): string {
    if (value === true) {
      return 'true';
    }

    if (value === false) {
      return 'false';
    }

    return '';
  }

  private parseBooleanFilter(value: string): boolean | null {
    if (value === 'true') {
      return true;
    }

    if (value === 'false') {
      return false;
    }

    return null;
  }

  private upsertLocalizedValue(values: LocalizedItemDto<string>[], value: string): LocalizedItemDto<string>[] {
    const others: LocalizedItemDto<string>[] = values.filter((item: LocalizedItemDto<string>) => item.languageCode !== this.defaultLanguageCode);

    if (!value.trim()) {
      return others;
    }

    return [
      ...others,
      {
        languageCode: this.defaultLanguageCode,
        value,
      },
    ];
  }
}
