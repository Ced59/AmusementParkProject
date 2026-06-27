import { DestroyRef, Inject, Injectable, Signal, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';

import { ImageCategory } from '@app/models/images/image-category';
import { ImageDto } from '@app/models/images/image-dto';
import { ImageOwnerType } from '@app/models/images/image-owner-type';
import { ImageTagDto } from '@app/models/images/image-tag-dto';
import { UploadedImage } from '@app/models/images/uploaded-image';
import { LocalizedItemDto } from '@app/models/shared/localized-item-dto';
import { PARK_ITEM_PHOTO_CATEGORY_OPTIONS } from '@features/admin/park-items/models/admin-park-item-edit.model';
import { PARK_PHOTO_CATEGORY_OPTIONS } from '@features/admin/parks/models/admin-park-edit.model';
import { ImageUploadSecurityService } from '@shared/utils/security';
import { AdminContextualBlockInstance } from '../models/admin-contextual-block.model';
import {
  AdminContextualPhotoMetadataPreview,
  AdminContextualPhotoMetadataReaderService
} from '../services/admin-contextual-photo-metadata-reader.service';
import { AdminContextualBlockRefreshEvents } from './admin-contextual-block-refresh-events.service';
import {
  ADMIN_CONTEXTUAL_BLOCK_PHOTO_ADD_IMAGES_PORT,
  AdminContextualBlockPhotoAddImagesPort
} from './admin-contextual-block-photo-add-data.ports';

export interface AdminContextualBlockPhotoCategoryOption {
  readonly slug: string;
  readonly labelKey: string;
  readonly labelFr?: string;
  readonly labelEn?: string;
}

export interface AdminContextualBlockPhotoTagOption {
  readonly id: string;
  readonly slug: string;
  readonly label: string;
  readonly isCategoryTag: boolean;
}

export interface AdminContextualBlockPhotoMetadataRow {
  readonly labelKey: string;
  readonly value: string;
  readonly tone: 'neutral' | 'success' | 'warning';
}

export type AdminContextualPhotoSourceMode = 'file' | 'remote';

@Injectable({
  providedIn: 'root'
})
export class AdminContextualBlockPhotoAddFacade {
  private readonly sourceModeSignal = signal<AdminContextualPhotoSourceMode>('file');
  private readonly selectedFileSignal = signal<File | null>(null);
  private readonly remoteSourceUrlSignal = signal<string>('');
  private readonly previewUrlSignal = signal<string | null>(null);
  private readonly metadataPreviewSignal = signal<AdminContextualPhotoMetadataPreview | null>(null);
  private readonly metadataRowsSignal = signal<readonly AdminContextualBlockPhotoMetadataRow[]>([]);
  private readonly descriptionSignal = signal<string>('');
  private readonly withWatermarkSignal = signal<boolean>(true);
  private readonly isPublishedSignal = signal<boolean>(true);
  private readonly setAsCurrentSignal = signal<boolean>(false);
  private readonly categoryOptionsSignal = signal<readonly AdminContextualBlockPhotoCategoryOption[]>([]);
  private readonly selectedCategorySlugSignal = signal<string>('');
  private readonly tagOptionsSignal = signal<readonly AdminContextualBlockPhotoTagOption[]>([]);
  private readonly selectedTagIdsSignal = signal<readonly string[]>([]);
  private readonly isLoadingTagsSignal = signal<boolean>(false);
  private readonly isReadingMetadataSignal = signal<boolean>(false);
  private readonly isUploadingSignal = signal<boolean>(false);
  private readonly errorKeySignal = signal<string | null>(null);
  private readonly successKeySignal = signal<string | null>(null);
  private activeBlockId: string | null = null;
  private categoryTagIdsBySlug: Record<string, string> = {};

  public readonly sourceMode: Signal<AdminContextualPhotoSourceMode> = this.sourceModeSignal.asReadonly();
  public readonly selectedFile: Signal<File | null> = this.selectedFileSignal.asReadonly();
  public readonly remoteSourceUrl: Signal<string> = this.remoteSourceUrlSignal.asReadonly();
  public readonly previewUrl: Signal<string | null> = this.previewUrlSignal.asReadonly();
  public readonly metadataRows: Signal<readonly AdminContextualBlockPhotoMetadataRow[]> = this.metadataRowsSignal.asReadonly();
  public readonly description: Signal<string> = this.descriptionSignal.asReadonly();
  public readonly withWatermark: Signal<boolean> = this.withWatermarkSignal.asReadonly();
  public readonly isPublished: Signal<boolean> = this.isPublishedSignal.asReadonly();
  public readonly setAsCurrent: Signal<boolean> = this.setAsCurrentSignal.asReadonly();
  public readonly categoryOptions: Signal<readonly AdminContextualBlockPhotoCategoryOption[]> = this.categoryOptionsSignal.asReadonly();
  public readonly selectedCategorySlug: Signal<string> = this.selectedCategorySlugSignal.asReadonly();
  public readonly tagOptions: Signal<readonly AdminContextualBlockPhotoTagOption[]> = this.tagOptionsSignal.asReadonly();
  public readonly selectedTagIds: Signal<readonly string[]> = this.selectedTagIdsSignal.asReadonly();
  public readonly isLoadingTags: Signal<boolean> = this.isLoadingTagsSignal.asReadonly();
  public readonly isReadingMetadata: Signal<boolean> = this.isReadingMetadataSignal.asReadonly();
  public readonly isUploading: Signal<boolean> = this.isUploadingSignal.asReadonly();
  public readonly errorKey: Signal<string | null> = this.errorKeySignal.asReadonly();
  public readonly successKey: Signal<string | null> = this.successKeySignal.asReadonly();

  constructor(
    @Inject(ADMIN_CONTEXTUAL_BLOCK_PHOTO_ADD_IMAGES_PORT) private readonly imagesPort: AdminContextualBlockPhotoAddImagesPort,
    private readonly metadataReader: AdminContextualPhotoMetadataReaderService,
    private readonly imageUploadSecurityService: ImageUploadSecurityService,
    private readonly translateService: TranslateService,
    private readonly refreshEvents: AdminContextualBlockRefreshEvents,
    private readonly destroyRef: DestroyRef
  ) {
  }

  resetForBlock(block: AdminContextualBlockInstance | null): void {
    const nextBlockId: string | null = block?.id ?? null;
    if (this.activeBlockId === nextBlockId) {
      return;
    }

    this.activeBlockId = nextBlockId;
    this.cleanupPreviewUrl();
    this.selectedFileSignal.set(null);
    this.remoteSourceUrlSignal.set('');
    this.metadataPreviewSignal.set(null);
    this.metadataRowsSignal.set([]);
    this.descriptionSignal.set('');
    this.withWatermarkSignal.set(this.getDefaultWatermarkForSourceMode('file'));
    this.isPublishedSignal.set(true);
    this.setAsCurrentSignal.set(false);
    this.tagOptionsSignal.set([]);
    this.selectedTagIdsSignal.set([]);
    this.categoryTagIdsBySlug = {};
    this.errorKeySignal.set(null);
    this.successKeySignal.set(null);
    this.isLoadingTagsSignal.set(false);
    this.isReadingMetadataSignal.set(false);
    this.isUploadingSignal.set(false);

    if (!block || !this.canAddPhoto(block)) {
      this.categoryOptionsSignal.set([]);
      this.selectedCategorySlugSignal.set('');
      this.sourceModeSignal.set('file');
      return;
    }

    const categoryOptions: readonly AdminContextualBlockPhotoCategoryOption[] = this.resolveCategoryOptions(block);
    this.categoryOptionsSignal.set(categoryOptions);
    this.selectedCategorySlugSignal.set(categoryOptions[0]?.slug ?? '');
    this.sourceModeSignal.set('file');
    this.loadTags(block);
  }

  canAddPhoto(block: AdminContextualBlockInstance): boolean {
    return block.capabilities.includes('contextualPhotoAdd') &&
      (block.type === 'park.images' || block.type === 'parkItem.images');
  }

  setSourceMode(mode: AdminContextualPhotoSourceMode): void {
    if (this.sourceModeSignal() === mode) {
      return;
    }

    this.sourceModeSignal.set(mode);
    this.withWatermarkSignal.set(this.getDefaultWatermarkForSourceMode(mode));
    this.clearSource();
  }

  selectFile(file: File | null): void {
    this.errorKeySignal.set(null);
    this.successKeySignal.set(null);
    this.clearPreviewState();

    if (!file) {
      this.selectedFileSignal.set(null);
      return;
    }

    const validation = this.imageUploadSecurityService.validateImageFile(file);
    if (!validation.isValid) {
      this.selectedFileSignal.set(null);
      this.errorKeySignal.set(validation.errorKey);
      return;
    }

    this.sourceModeSignal.set('file');
    this.withWatermarkSignal.set(this.getDefaultWatermarkForSourceMode('file'));
    this.selectedFileSignal.set(file);
    this.remoteSourceUrlSignal.set('');
    this.previewUrlSignal.set(URL.createObjectURL(file));
    this.readFileMetadata(file);
  }

  updateRemoteSourceUrl(sourceUrl: string): void {
    if (this.sourceModeSignal() !== 'remote') {
      this.withWatermarkSignal.set(this.getDefaultWatermarkForSourceMode('remote'));
    }

    this.sourceModeSignal.set('remote');
    this.remoteSourceUrlSignal.set(sourceUrl);
    this.selectedFileSignal.set(null);
    this.clearPreviewState();
    this.errorKeySignal.set(null);
    this.successKeySignal.set(null);
  }

  previewRemoteSourceUrl(): void {
    const sourceUrl: string = this.remoteSourceUrlSignal().trim();
    if (!this.isSupportedRemoteUrl(sourceUrl)) {
      this.errorKeySignal.set('admin.contextualBlocks.drawer.photoInvalidRemoteUrl');
      return;
    }

    this.errorKeySignal.set(null);
    this.successKeySignal.set(null);
    this.previewUrlSignal.set(sourceUrl);
    this.isReadingMetadataSignal.set(true);
    this.metadataReader.readRemoteUrl(sourceUrl)
      .then((metadata: AdminContextualPhotoMetadataPreview): void => this.setMetadataPreview(metadata))
      .catch((): void => {
        this.metadataPreviewSignal.set(null);
        this.metadataRowsSignal.set([]);
      })
      .finally((): void => this.isReadingMetadataSignal.set(false));
  }

  updateDescription(description: string): void {
    this.descriptionSignal.set(description);
    this.errorKeySignal.set(null);
    this.successKeySignal.set(null);
  }

  updateWithWatermark(withWatermark: boolean): void {
    this.withWatermarkSignal.set(withWatermark);
    this.errorKeySignal.set(null);
    this.successKeySignal.set(null);
  }

  updateSelectedCategorySlug(slug: string): void {
    const isKnownCategory: boolean = this.categoryOptionsSignal().some((option: AdminContextualBlockPhotoCategoryOption) => option.slug === slug);
    if (isKnownCategory) {
      this.selectedCategorySlugSignal.set(slug);
    }
  }

  toggleTag(tagId: string, checked: boolean): void {
    const currentTagIds: readonly string[] = this.selectedTagIdsSignal();
    if (checked) {
      this.selectedTagIdsSignal.set([...new Set([...currentTagIds, tagId])]);
      return;
    }

    this.selectedTagIdsSignal.set(currentTagIds.filter((currentTagId: string) => currentTagId !== tagId));
  }

  updateIsPublished(isPublished: boolean): void {
    this.isPublishedSignal.set(isPublished);
  }

  updateSetAsCurrent(setAsCurrent: boolean): void {
    this.setAsCurrentSignal.set(setAsCurrent);
  }

  uploadPhoto(block: AdminContextualBlockInstance): void {
    if (!this.canAddPhoto(block) || this.isUploadingSignal()) {
      return;
    }

    const sourceMode: AdminContextualPhotoSourceMode = this.sourceModeSignal();
    if (sourceMode === 'file' && !this.selectedFileSignal()) {
      this.errorKeySignal.set('admin.contextualBlocks.drawer.photoSourceRequired');
      return;
    }

    if (sourceMode === 'remote' && !this.isSupportedRemoteUrl(this.remoteSourceUrlSignal().trim())) {
      this.errorKeySignal.set('admin.contextualBlocks.drawer.photoInvalidRemoteUrl');
      return;
    }

    this.isUploadingSignal.set(true);
    this.errorKeySignal.set(null);
    this.successKeySignal.set(null);

    void this.uploadPhotoAsync(block)
      .then((image: ImageDto): void => {
        this.refreshEvents.notifyBlockApplied({
          blockType: block.type,
          entityType: block.entityType,
          entityId: block.entityId,
          appliedAtUtc: new Date().toISOString()
        });
        this.resetSourceAfterSuccess(image);
        this.successKeySignal.set('admin.contextualBlocks.drawer.photoUploadSucceeded');
      })
      .catch((): void => this.errorKeySignal.set('admin.contextualBlocks.drawer.photoUploadError'))
      .finally((): void => this.isUploadingSignal.set(false));
  }

  private loadTags(block: AdminContextualBlockInstance): void {
    this.isLoadingTagsSignal.set(true);
    this.imagesPort.getAdminImageTags()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (tags: ImageTagDto[]): void => {
          void this.ensureMissingCategoryTagsAsync(tags, block)
            .then((allTags: ImageTagDto[]): void => {
              this.tagOptionsSignal.set(this.buildTagOptions(allTags));
              this.isLoadingTagsSignal.set(false);
            })
            .catch((): void => {
              this.errorKeySignal.set('admin.contextualBlocks.drawer.photoTagsLoadError');
              this.isLoadingTagsSignal.set(false);
            });
        },
        error: (): void => {
          this.errorKeySignal.set('admin.contextualBlocks.drawer.photoTagsLoadError');
          this.isLoadingTagsSignal.set(false);
        }
      });
  }

  private async uploadPhotoAsync(block: AdminContextualBlockInstance): Promise<ImageDto> {
    const image: ImageDto = this.sourceModeSignal() === 'file'
      ? await this.uploadFileAsync(block)
      : await this.importRemoteAsync(block);

    return this.applyPhotoMetadataAsync(image);
  }

  private async uploadFileAsync(block: AdminContextualBlockInstance): Promise<ImageDto> {
    const selectedFile: File | null = this.selectedFileSignal();
    if (!selectedFile) {
      throw new Error('Missing file.');
    }

    const uploadedImage: UploadedImage = await firstValueFrom(this.imagesPort.uploadImage(
      selectedFile,
      this.resolveImageCategory(block),
      this.withWatermarkSignal(),
      this.descriptionSignal().trim() || block.contextLabel
    ));

    return firstValueFrom(this.imagesPort.linkImage({
      imageId: uploadedImage.id,
      ownerType: this.resolveOwnerType(block),
      ownerId: block.entityId,
      description: this.descriptionSignal().trim() || undefined,
      setAsCurrent: this.setAsCurrentSignal()
    }));
  }

  private importRemoteAsync(block: AdminContextualBlockInstance): Promise<ImageDto> {
    return firstValueFrom(this.imagesPort.importRemoteImage({
      sourceUrl: this.remoteSourceUrlSignal().trim(),
      category: this.resolveImageCategory(block),
      ownerType: this.resolveOwnerType(block),
      ownerId: block.entityId,
      description: this.descriptionSignal().trim() || block.contextLabel,
      withWatermark: this.withWatermarkSignal(),
      setAsCurrent: this.setAsCurrentSignal()
    }));
  }

  private applyPhotoMetadataAsync(image: ImageDto): Promise<ImageDto> {
    return firstValueFrom(this.imagesPort.updateAdminImage(image.id, {
      description: this.descriptionSignal().trim() || image.description,
      geoLocation: image.geoLocation ?? null,
      altTexts: image.altTexts ?? [],
      captions: image.captions ?? [],
      credits: image.credits ?? [],
      tagIds: this.buildSelectedTagIds(image),
      isPublished: this.isPublishedSignal(),
      sourceUrl: image.sourceUrl ?? null
    }));
  }

  private buildSelectedTagIds(image: ImageDto): string[] {
    const selectedCategoryTagId: string | undefined = this.categoryTagIdsBySlug[this.selectedCategorySlugSignal()];
    return [...new Set([
      ...(image.tagIds ?? []),
      ...(selectedCategoryTagId ? [selectedCategoryTagId] : []),
      ...this.selectedTagIdsSignal()
    ])];
  }

  private async ensureMissingCategoryTagsAsync(existingTags: ImageTagDto[], block: AdminContextualBlockInstance): Promise<ImageTagDto[]> {
    const categoryOptions: readonly AdminContextualBlockPhotoCategoryOption[] = this.resolveCategoryOptions(block);
    const tags: ImageTagDto[] = [...existingTags];
    const idsBySlug: Record<string, string> = {};

    for (const option of categoryOptions) {
      const existingTag: ImageTagDto | undefined = tags.find((tag: ImageTagDto) => tag.slug === option.slug);
      if (existingTag) {
        idsBySlug[option.slug] = existingTag.id;
        continue;
      }

      const createdTag: ImageTagDto = await firstValueFrom(this.imagesPort.createAdminImageTag({
        slug: option.slug,
        labels: [
          { languageCode: 'fr', value: option.labelFr ?? this.translateService.instant(option.labelKey) },
          { languageCode: 'en', value: option.labelEn ?? this.translateService.instant(option.labelKey) }
        ],
        descriptions: []
      }));
      tags.push(createdTag);
      idsBySlug[option.slug] = createdTag.id;
    }

    this.categoryTagIdsBySlug = idsBySlug;
    return tags;
  }

  private buildTagOptions(tags: ImageTagDto[]): readonly AdminContextualBlockPhotoTagOption[] {
    const categorySlugs: Set<string> = new Set(this.categoryOptionsSignal().map((option: AdminContextualBlockPhotoCategoryOption) => option.slug));

    return tags
      .filter((tag: ImageTagDto) => tag.isActive !== false)
      .sort((left: ImageTagDto, right: ImageTagDto) => left.slug.localeCompare(right.slug))
      .map((tag: ImageTagDto): AdminContextualBlockPhotoTagOption => ({
        id: tag.id,
        slug: tag.slug,
        label: this.resolveTagLabel(tag),
        isCategoryTag: categorySlugs.has(tag.slug)
      }));
  }

  private resolveTagLabel(tag: ImageTagDto): string {
    const currentLanguage: string = this.translateService.currentLang || 'en';
    const localizedLabel: LocalizedItemDto<string> | undefined = tag.labels?.find((label: LocalizedItemDto<string>) => label.languageCode === currentLanguage)
      ?? tag.labels?.find((label: LocalizedItemDto<string>) => label.languageCode === 'en')
      ?? tag.labels?.[0];

    return localizedLabel?.value?.trim() || tag.slug;
  }

  private readFileMetadata(file: File): void {
    this.isReadingMetadataSignal.set(true);
    this.metadataReader.readFile(file)
      .then((metadata: AdminContextualPhotoMetadataPreview): void => this.setMetadataPreview(metadata))
      .catch((): void => {
        this.metadataPreviewSignal.set(null);
        this.metadataRowsSignal.set([]);
      })
      .finally((): void => this.isReadingMetadataSignal.set(false));
  }

  private setMetadataPreview(metadata: AdminContextualPhotoMetadataPreview): void {
    this.metadataPreviewSignal.set(metadata);
    this.metadataRowsSignal.set(this.buildMetadataRows(metadata));
  }

  private buildMetadataRows(metadata: AdminContextualPhotoMetadataPreview): readonly AdminContextualBlockPhotoMetadataRow[] {
    const rows: AdminContextualBlockPhotoMetadataRow[] = [];

    this.addMetadataRow(rows, 'admin.contextualBlocks.drawer.photoMetadataFileName', metadata.fileName, 'neutral');
    this.addMetadataRow(rows, 'admin.contextualBlocks.drawer.photoMetadataContentType', metadata.contentType, 'neutral');
    this.addMetadataRow(rows, 'admin.contextualBlocks.drawer.photoMetadataSize', this.formatSize(metadata.sizeInBytes), 'neutral');
    this.addMetadataRow(rows, 'admin.contextualBlocks.drawer.photoMetadataDimensions', this.formatDimensions(metadata.width, metadata.height), 'neutral');

    if (metadata.geoLocation) {
      rows.push({
        labelKey: 'admin.contextualBlocks.drawer.photoMetadataGeoLocation',
        value: `${metadata.geoLocation.latitude.toFixed(6)}, ${metadata.geoLocation.longitude.toFixed(6)}`,
        tone: 'success'
      });
    } else if (metadata.geoStatus === 'unavailable') {
      rows.push({
        labelKey: 'admin.contextualBlocks.drawer.photoMetadataGeoLocation',
        value: this.translateService.instant('admin.contextualBlocks.drawer.photoMetadataGeoUnavailable'),
        tone: 'warning'
      });
    } else {
      rows.push({
        labelKey: 'admin.contextualBlocks.drawer.photoMetadataGeoLocation',
        value: this.translateService.instant('admin.contextualBlocks.drawer.photoMetadataGeoMissing'),
        tone: 'warning'
      });
    }

    return rows;
  }

  private addMetadataRow(rows: AdminContextualBlockPhotoMetadataRow[], labelKey: string, value: string | null, tone: 'neutral' | 'success' | 'warning'): void {
    if (!value) {
      return;
    }

    rows.push({ labelKey, value, tone });
  }

  private formatSize(sizeInBytes: number | null): string | null {
    if (sizeInBytes === null) {
      return null;
    }

    if (sizeInBytes < 1024) {
      return `${sizeInBytes} B`;
    }

    if (sizeInBytes < 1024 * 1024) {
      return `${(sizeInBytes / 1024).toFixed(1)} KB`;
    }

    return `${(sizeInBytes / 1024 / 1024).toFixed(2)} MB`;
  }

  private formatDimensions(width: number | null, height: number | null): string | null {
    return width !== null && height !== null ? `${width} x ${height} px` : null;
  }

  private clearSource(): void {
    this.selectedFileSignal.set(null);
    this.remoteSourceUrlSignal.set('');
    this.clearPreviewState();
    this.errorKeySignal.set(null);
    this.successKeySignal.set(null);
  }

  private clearPreviewState(): void {
    this.cleanupPreviewUrl();
    this.metadataPreviewSignal.set(null);
    this.metadataRowsSignal.set([]);
  }

  private cleanupPreviewUrl(): void {
    const previewUrl: string | null = this.previewUrlSignal();
    if (previewUrl?.startsWith('blob:')) {
      URL.revokeObjectURL(previewUrl);
    }

    this.previewUrlSignal.set(null);
  }

  private resetSourceAfterSuccess(image: ImageDto): void {
    this.clearSource();
    this.withWatermarkSignal.set(this.getDefaultWatermarkForSourceMode(this.sourceModeSignal()));
    this.setAsCurrentSignal.set(image.isCurrent);
  }

  private getDefaultWatermarkForSourceMode(mode: AdminContextualPhotoSourceMode): boolean {
    return mode === 'file';
  }

  private resolveCategoryOptions(block: AdminContextualBlockInstance): readonly AdminContextualBlockPhotoCategoryOption[] {
    return block.type === 'park.images' ? PARK_PHOTO_CATEGORY_OPTIONS : PARK_ITEM_PHOTO_CATEGORY_OPTIONS;
  }

  private resolveImageCategory(block: AdminContextualBlockInstance): ImageCategory {
    return block.type === 'park.images' ? ImageCategory.PARK : ImageCategory.PARK_ITEM;
  }

  private resolveOwnerType(block: AdminContextualBlockInstance): ImageOwnerType {
    return block.type === 'park.images' ? ImageOwnerType.PARK : ImageOwnerType.PARK_ITEM;
  }

  private isSupportedRemoteUrl(sourceUrl: string): boolean {
    if (!sourceUrl) {
      return false;
    }

    try {
      const url: URL = new URL(sourceUrl);
      return url.protocol === 'https:' || url.protocol === 'http:';
    } catch {
      return false;
    }
  }
}
