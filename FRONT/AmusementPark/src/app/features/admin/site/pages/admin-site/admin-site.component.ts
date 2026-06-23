import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';

import { AdminImageSortDirection, AdminImageSortField } from '@app/models/images/admin-image-search-query';
import { ImageCategory } from '@app/models/images/image-category';
import { ImageDto } from '@app/models/images/image-dto';
import { ImageOwnerType } from '@app/models/images/image-owner-type';
import { ImageTagDto } from '@app/models/images/image-tag-dto';
import { LocalizedItemDto } from '@app/models/shared/localized-item-dto';
import { ImagesApiService } from '@data-access/images/images-api.service';
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
    public readonly imagesApiService: ImagesApiService,
    private readonly stateFacade: AdminSiteStateFacade
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

  protected async saveImage(): Promise<void> {
    const selectedImage: ImageDto | null = this.selectedImage();

    if (!selectedImage) {
      return;
    }

    try {
      await firstValueFrom(this.imagesApiService.updateAdminImage(selectedImage.id, {
        description: selectedImage.description,
        category: selectedImage.category,
        ownerType: selectedImage.ownerType,
        ownerId: selectedImage.ownerType === ImageOwnerType.NONE ? null : selectedImage.ownerId,
        isCurrent: selectedImage.isCurrent,
        geoLocation: selectedImage.geoLocation ?? null,
        altTexts: selectedImage.altTexts ?? [],
        captions: selectedImage.captions ?? [],
        credits: selectedImage.credits ?? [],
        tagIds: selectedImage.tagIds ?? [],
        isPublished: selectedImage.isPublished,
        sourceUrl: selectedImage.sourceUrl ?? null,
      }));
      this.reload();
    } catch {
      this.stateFacade.setError();
    }
  }

  protected async applyWatermarkToSelectedImage(): Promise<void> {
    const selectedImage: ImageDto | null = this.selectedImage();

    if (!selectedImage || !this.canApplyWatermark(selectedImage)) {
      return;
    }

    try {
      await firstValueFrom(this.imagesApiService.applyWatermark(selectedImage.id));
      this.reload();
    } catch {
      this.stateFacade.setError();
    }
  }

  protected async createTag(): Promise<void> {
    const slug: string = this.newTagSlug.trim().toLowerCase();

    if (!slug) {
      return;
    }

    try {
      await firstValueFrom(this.imagesApiService.createAdminImageTag({
        slug,
        labels: [{ languageCode: this.defaultLanguageCode, value: slug }],
        descriptions: [],
      }));
      this.newTagSlug = '';
      this.reload();
    } catch {
      this.stateFacade.setError();
    }
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

    if (!selectedImage?.geoLocation) {
      return;
    }

    this.stateFacade.updateSelectedImage({
      geoLocation: {
        ...selectedImage.geoLocation,
        latitude: Number(latitude),
      },
    });
  }

  protected updateSelectedImageLongitude(longitude: number | string): void {
    const selectedImage: ImageDto | null = this.selectedImage();

    if (!selectedImage?.geoLocation) {
      return;
    }

    this.stateFacade.updateSelectedImage({
      geoLocation: {
        ...selectedImage.geoLocation,
        longitude: Number(longitude),
      },
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
      return '—';
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
      return '—';
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
