import {
  DestroyRef,
  Inject,
  Injectable,
  OnDestroy,
  Signal,
  computed,
  signal
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { firstValueFrom } from 'rxjs';
import { TranslateService } from '@ngx-translate/core';

import { ImageCategory } from '@app/models/images/image-category';
import { ImageDto } from '@app/models/images/image-dto';
import { ImageGeoLocation } from '@app/models/images/image-geo-location';
import { ImageOwnerType } from '@app/models/images/image-owner-type';
import { ImageTagDto } from '@app/models/images/image-tag-dto';
import { ParkItemImageDto } from '@app/models/images/park-item-image-dto';
import { UploadedImage } from '@app/models/images/uploaded-image';
import { Park } from '@app/models/parks/park';
import { ParkItemAdminRow } from '@app/models/parks/park-item-admin-row';
import { ParksApiResponse } from '@app/models/parks/parks_api_response';
import { LocalizedItemDto } from '@app/models/shared/localized-item-dto';
import { ApiResponse } from '@app/models/shared/api_reponse';
import { ToastMessageService } from '@app/services/messages/toast-message.service';
import { AdminContextualPhotoMetadataPreview } from '@features/admin/contextual-editing/services/admin-contextual-photo-metadata-reader.service';
import { AdminContextualPhotoMetadataReaderService } from '@features/admin/contextual-editing/services/admin-contextual-photo-metadata-reader.service';
import { PARK_ITEM_PHOTO_CATEGORY_OPTIONS } from '@features/admin/park-items/models/admin-park-item-edit.model';
import { PARK_PHOTO_CATEGORY_OPTIONS } from '@features/admin/parks/models/admin-park-edit.model';
import { ImageUploadSecurityService } from '@shared/utils/security';
import { PagedResult, PaginationContract } from '@shared/models/contracts';

import {
  AdminPhotoBatchCategorySets,
  AdminPhotoBatchOwnerKind,
  AdminPhotoBatchParkItemOption,
  AdminPhotoBatchParkOption,
  AdminPhotoBatchPhoto,
  AdminPhotoBatchSection,
  AdminPhotoBatchUploadProgress,
  AdminPhotoBatchUploadSelection
} from '../models/admin-photo-batch.model';
import {
  ADMIN_PHOTO_BATCH_IMAGES_PORT,
  ADMIN_PHOTO_BATCH_PARK_ITEMS_PORT,
  ADMIN_PHOTO_BATCH_PARKS_PORT,
  AdminPhotoBatchImagesPort,
  AdminPhotoBatchParkItemsPort,
  AdminPhotoBatchParksPort
} from './admin-photo-batch-state-data.ports';

type PhotoPatch = Partial<Pick<AdminPhotoBatchPhoto,
  'draftOwnerKind' |
  'draftParkItemId' |
  'draftCategorySlug' |
  'isSaving'>>;

interface WorkspaceImagePage<TItem> {
  items: TItem[];
  page: number;
  canLoadMore: boolean;
}

const DEFAULT_PARK_PHOTO_CATEGORY_SLUG: string = PARK_PHOTO_CATEGORY_OPTIONS[0].slug;
const DEFAULT_PARK_ITEM_PHOTO_CATEGORY_SLUG: string = PARK_ITEM_PHOTO_CATEGORY_OPTIONS[0].slug;
const PARKS_PAGE_SIZE: number = 60;
const WORKSPACE_PAGE_SIZE: number = 100;
const UPLOAD_CONCURRENCY_LIMIT: number = 2;

@Injectable()
export class AdminPhotoBatchStateFacade implements OnDestroy {
  private readonly parksSignal = signal<AdminPhotoBatchParkOption[]>([]);
  private readonly parksLoadingSignal = signal(false);
  private readonly parkSearchSignal = signal('');
  private readonly selectedParkIdSignal = signal<string | null>(null);
  private readonly selectedParkNameSignal = signal<string | null>(null);
  private readonly parkItemsSignal = signal<AdminPhotoBatchParkItemOption[]>([]);
  private readonly parkItemsLoadingSignal = signal(false);
  private readonly photosSignal = signal<AdminPhotoBatchPhoto[]>([]);
  private readonly photosLoadingSignal = signal(false);
  private readonly parkPhotosPageSignal = signal(0);
  private readonly parkItemPhotosPageSignal = signal(0);
  private readonly parkPhotosCanLoadMoreSignal = signal(false);
  private readonly parkItemPhotosCanLoadMoreSignal = signal(false);
  private readonly parkPhotosLoadingMoreSignal = signal(false);
  private readonly parkItemPhotosLoadingMoreSignal = signal(false);
  private readonly selectedFilesSignal = signal<AdminPhotoBatchUploadSelection[]>([]);
  private readonly uploadingSignal = signal(false);
  private readonly uploadProgressSignal = signal<AdminPhotoBatchUploadProgress | null>(null);
  private readonly withWatermarkSignal = signal(true);
  private readonly categoryTagIdsBySlugSignal = signal<Record<string, string>>({});
  private readonly categorySlugByTagIdSignal = signal<Record<string, string>>({});
  private readonly metadataRequestsBySelectionId = new Map<string, Promise<AdminContextualPhotoMetadataPreview | null>>();
  private categoryTagsRequest: Promise<void> | null = null;

  public readonly parks: Signal<AdminPhotoBatchParkOption[]> = this.parksSignal.asReadonly();
  public readonly parksLoading: Signal<boolean> = this.parksLoadingSignal.asReadonly();
  public readonly parkSearch: Signal<string> = this.parkSearchSignal.asReadonly();
  public readonly selectedParkId: Signal<string | null> = this.selectedParkIdSignal.asReadonly();
  public readonly selectedParkName: Signal<string | null> = this.selectedParkNameSignal.asReadonly();
  public readonly parkItems: Signal<AdminPhotoBatchParkItemOption[]> = this.parkItemsSignal.asReadonly();
  public readonly parkItemsLoading: Signal<boolean> = this.parkItemsLoadingSignal.asReadonly();
  public readonly photosLoading: Signal<boolean> = this.photosLoadingSignal.asReadonly();
  public readonly canLoadMoreParkPhotos: Signal<boolean> = this.parkPhotosCanLoadMoreSignal.asReadonly();
  public readonly canLoadMoreParkItemPhotos: Signal<boolean> = this.parkItemPhotosCanLoadMoreSignal.asReadonly();
  public readonly parkPhotosLoadingMore: Signal<boolean> = this.parkPhotosLoadingMoreSignal.asReadonly();
  public readonly parkItemPhotosLoadingMore: Signal<boolean> = this.parkItemPhotosLoadingMoreSignal.asReadonly();
  public readonly selectedFiles: Signal<AdminPhotoBatchUploadSelection[]> = this.selectedFilesSignal.asReadonly();
  public readonly selectedFileCount: Signal<number> = computed(() => this.selectedFilesSignal().length);
  public readonly selectedFilesAnalyzing: Signal<boolean> = computed(() =>
    this.selectedFilesSignal().some((selection: AdminPhotoBatchUploadSelection) => selection.metadataStatus === 'pending')
  );
  public readonly selectedFilesWithoutGeoCount: Signal<number> = computed(() =>
    this.selectedFilesSignal().filter((selection: AdminPhotoBatchUploadSelection) =>
      selection.metadataStatus === 'ready' && selection.geoLocation === null
    ).length
  );
  public readonly uploading: Signal<boolean> = this.uploadingSignal.asReadonly();
  public readonly uploadProgress: Signal<AdminPhotoBatchUploadProgress | null> = this.uploadProgressSignal.asReadonly();
  public readonly uploadPercent: Signal<number> = computed(() => {
    const progress: AdminPhotoBatchUploadProgress | null = this.uploadProgressSignal();
    return progress && progress.total > 0
      ? Math.round((progress.completed / progress.total) * 100)
      : 0;
  });
  public readonly withWatermark: Signal<boolean> = this.withWatermarkSignal.asReadonly();
  public readonly categorySets: Signal<AdminPhotoBatchCategorySets> = signal<AdminPhotoBatchCategorySets>({
    park: PARK_PHOTO_CATEGORY_OPTIONS,
    parkItem: PARK_ITEM_PHOTO_CATEGORY_OPTIONS
  }).asReadonly();
  public readonly uncategorizedPhotos: Signal<AdminPhotoBatchPhoto[]> = computed(() =>
    this.photosSignal().filter((photo: AdminPhotoBatchPhoto) => photo.section === 'uncategorized')
  );
  public readonly parkPhotos: Signal<AdminPhotoBatchPhoto[]> = computed(() =>
    this.photosSignal().filter((photo: AdminPhotoBatchPhoto) => photo.section === 'park')
  );
  public readonly parkItemPhotos: Signal<AdminPhotoBatchPhoto[]> = computed(() =>
    this.photosSignal().filter((photo: AdminPhotoBatchPhoto) => photo.section === 'parkItem')
  );

  constructor(
    @Inject(ADMIN_PHOTO_BATCH_IMAGES_PORT) private readonly imagesPort: AdminPhotoBatchImagesPort,
    @Inject(ADMIN_PHOTO_BATCH_PARK_ITEMS_PORT) private readonly parkItemsPort: AdminPhotoBatchParkItemsPort,
    @Inject(ADMIN_PHOTO_BATCH_PARKS_PORT) private readonly parksPort: AdminPhotoBatchParksPort,
    private readonly metadataReader: AdminContextualPhotoMetadataReaderService,
    private readonly imageUploadSecurityService: ImageUploadSecurityService,
    private readonly toastMessageService: ToastMessageService,
    private readonly translateService: TranslateService,
    private readonly destroyRef: DestroyRef
  ) {
  }

  ngOnDestroy(): void {
    this.cleanupSelectedFilePreviews(this.selectedFilesSignal());
    this.metadataRequestsBySelectionId.clear();
  }

  loadInitialData(): void {
    this.loadParks();
    void this.ensureCategoryTagsAsync();
  }

  setParkSearch(query: string): void {
    this.parkSearchSignal.set(query);
  }

  loadParks(): void {
    this.parksLoadingSignal.set(true);
    const query: string = this.parkSearchSignal().trim();
    const request = query.length > 0
      ? this.parksPort.searchParks(query, 1, PARKS_PAGE_SIZE, false)
      : this.parksPort.getParksPaginated(1, PARKS_PAGE_SIZE, false);

    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (response: ParksApiResponse): void => {
        const options: AdminPhotoBatchParkOption[] = (response.data ?? [])
          .filter((park: Park): park is Park & { id: string } => Boolean(park.id?.trim()))
          .map((park: Park & { id: string }): AdminPhotoBatchParkOption => ({
            id: park.id.trim(),
            name: park.name?.trim() || park.id.trim()
          }));

        this.parksSignal.set(options);
        this.parksLoadingSignal.set(false);
      },
      error: (error: unknown): void => {
        console.error('Error loading parks for photo batch', error);
        this.parksLoadingSignal.set(false);
        this.toastMessageService.add(
          'error',
          this.translateService.instant('common.errorTitle'),
          this.translateService.instant('admin.images.batch.toasts.parksLoadError')
        );
      }
    });
  }

  selectPark(parkId: string): void {
    const normalizedParkId: string = parkId.trim();
    const selectedPark: AdminPhotoBatchParkOption | undefined = this.parksSignal().find((park: AdminPhotoBatchParkOption) => park.id === normalizedParkId);

    this.selectedParkIdSignal.set(normalizedParkId || null);
    this.selectedParkNameSignal.set(selectedPark?.name ?? null);
    this.photosSignal.set([]);
    this.parkItemsSignal.set([]);
    this.resetPhotoPagination();
    this.clearSelectedFiles();

    if (!normalizedParkId) {
      return;
    }

    this.toastMessageService.add(
      'info',
      this.translateService.instant('admin.images.batch.toasts.parkSelectedSummary'),
      this.translateService.instant('admin.images.batch.toasts.parkSelectedDetail', { name: selectedPark?.name ?? normalizedParkId })
    );
    void this.loadParkWorkspaceAsync(normalizedParkId);
  }

  refreshSelectedPark(): void {
    const parkId: string | null = this.selectedParkIdSignal();
    if (!parkId) {
      return;
    }

    this.resetPhotoPagination();
    void this.loadParkWorkspaceAsync(parkId);
  }

  selectFiles(event: Event): void {
    const inputElement: HTMLInputElement = event.target as HTMLInputElement;
    const files: File[] = inputElement.files ? Array.from(inputElement.files) : [];
    inputElement.value = '';

    if (files.length === 0) {
      return;
    }

    const validFiles: File[] = this.imageUploadSecurityService.filterValidImageFiles(files);
    if (validFiles.length !== files.length) {
      this.toastMessageService.add(
        'warn',
        this.translateService.instant('common.security.uploadRejectedSummary'),
        this.translateService.instant('common.security.invalidImageUploadMessage')
      );
    }

    const selections: AdminPhotoBatchUploadSelection[] = validFiles.map((file: File): AdminPhotoBatchUploadSelection => ({
      id: `${Date.now()}-${Math.random().toString(36).slice(2)}`,
      file,
      previewUrl: URL.createObjectURL(file),
      metadataStatus: 'pending',
      fileName: file.name,
      contentType: file.type || null,
      sizeInBytes: file.size,
      width: null,
      height: null,
      geoLocation: null
    }));

    this.cleanupSelectedFilePreviews(this.selectedFilesSignal());
    this.metadataRequestsBySelectionId.clear();
    this.selectedFilesSignal.set(selections);
    selections.forEach((selection: AdminPhotoBatchUploadSelection) => this.readSelectionMetadata(selection));

    if (selections.length > 0) {
      this.toastMessageService.add(
        'info',
        this.translateService.instant('admin.images.batch.toasts.filesSelectedSummary'),
        this.translateService.instant('admin.images.batch.toasts.filesSelectedDetail', { count: selections.length })
      );
    }
  }

  removeSelectedFile(selectionId: string): void {
    const currentSelections: AdminPhotoBatchUploadSelection[] = this.selectedFilesSignal();
    const removedSelection: AdminPhotoBatchUploadSelection | undefined = currentSelections.find((selection: AdminPhotoBatchUploadSelection) => selection.id === selectionId);

    if (removedSelection) {
      URL.revokeObjectURL(removedSelection.previewUrl);
    }

    this.metadataRequestsBySelectionId.delete(selectionId);
    this.selectedFilesSignal.set(currentSelections.filter((selection: AdminPhotoBatchUploadSelection) => selection.id !== selectionId));
  }

  clearSelectedFiles(): void {
    this.cleanupSelectedFilePreviews(this.selectedFilesSignal());
    this.metadataRequestsBySelectionId.clear();
    this.selectedFilesSignal.set([]);
  }

  setWithWatermark(withWatermark: boolean): void {
    this.withWatermarkSignal.set(withWatermark);
  }

  async uploadSelectedFiles(): Promise<void> {
    const parkId: string | null = this.selectedParkIdSignal();
    const parkName: string = this.selectedParkNameSignal() ?? parkId ?? '';
    const selections: AdminPhotoBatchUploadSelection[] = [...this.selectedFilesSignal()];

    if (!parkId || selections.length === 0 || this.uploadingSignal()) {
      return;
    }

    this.uploadingSignal.set(true);
    this.uploadProgressSignal.set({ completed: 0, total: selections.length, activeIndex: 0, currentFileName: null });
    this.toastMessageService.add(
      'info',
      this.translateService.instant('admin.images.batch.toasts.uploadStartedSummary'),
      this.translateService.instant('admin.images.batch.toasts.uploadStartedDetail', { count: selections.length })
    );

    let completedCount: number = 0;
    let failedCount: number = 0;
    let missingGeoCount: number = 0;
    let shouldReloadWorkspace: boolean = false;

    try {
      await this.ensureCategoryTagsAsync();
      await this.runWithConcurrency(selections, async (selection: AdminPhotoBatchUploadSelection, index: number): Promise<void> => {
        this.uploadProgressSignal.set({
          completed: completedCount,
          total: selections.length,
          activeIndex: index + 1,
          currentFileName: selection.fileName
        });

        try {
          const uploadedPhoto: ImageDto = await this.uploadSelectionAsync(selection, parkId, parkName);
          if (!uploadedPhoto.geoLocation) {
            missingGeoCount++;
          }
          this.upsertPhoto(this.toBatchPhoto(uploadedPhoto));
        } catch (error: unknown) {
          failedCount++;
          console.error('Error uploading photo batch selection', error);
        } finally {
          completedCount++;
          this.uploadProgressSignal.set({
            completed: completedCount,
            total: selections.length,
            activeIndex: Math.min(completedCount + 1, selections.length),
            currentFileName: selection.fileName
          });
        }
      });

      shouldReloadWorkspace = completedCount > failedCount;
      this.clearSelectedFiles();
      this.toastMessageService.add(
        failedCount === 0 ? 'success' : 'warn',
        this.translateService.instant(failedCount === 0 ? 'admin.images.batch.toasts.uploadFinishedSummary' : 'admin.images.batch.toasts.uploadPartialSummary'),
        this.translateService.instant(
          failedCount === 0 ? 'admin.images.batch.toasts.uploadFinishedDetail' : 'admin.images.batch.toasts.uploadPartialDetail',
          { count: completedCount - failedCount, failed: failedCount }
        )
      );

      if (missingGeoCount > 0) {
        this.toastMessageService.add(
          'warn',
          this.translateService.instant('common.warning'),
          this.translateService.instant('admin.images.batch.toasts.missingGeoDetail', { count: missingGeoCount })
        );
      }
    } catch (error: unknown) {
      console.error('Error preparing photo batch upload', error);
      this.toastMessageService.add(
        'error',
        this.translateService.instant('common.errorTitle'),
        this.translateService.instant('shared.imageUpload.uploadError')
      );
    } finally {
      this.uploadingSignal.set(false);
      this.uploadProgressSignal.set(null);
    }

    if (shouldReloadWorkspace) {
      await this.reloadParkWorkspaceAsync(parkId);
    }
  }

  async loadMoreParkPhotos(): Promise<void> {
    const parkId: string | null = this.selectedParkIdSignal();
    if (!parkId || this.photosLoadingSignal() || this.parkPhotosLoadingMoreSignal() || !this.parkPhotosCanLoadMoreSignal()) {
      return;
    }

    const nextPage: number = this.parkPhotosPageSignal() + 1;
    this.parkPhotosLoadingMoreSignal.set(true);

    try {
      const pageResult: WorkspaceImagePage<ImageDto> = await this.loadParkPhotosPageAsync(parkId, nextPage);
      if (this.selectedParkIdSignal() !== parkId) {
        return;
      }

      const itemNameById: Map<string, string> = this.createParkItemNameById();
      const photos: AdminPhotoBatchPhoto[] = pageResult.items.map((image: ImageDto): AdminPhotoBatchPhoto =>
        this.toBatchPhoto(image, itemNameById)
      );
      this.appendPhotos(photos);
      this.parkPhotosPageSignal.set(pageResult.page);
      this.parkPhotosCanLoadMoreSignal.set(pageResult.canLoadMore);
    } catch (error: unknown) {
      console.error('Error loading more park batch photos', error);
      this.toastMessageService.add(
        'error',
        this.translateService.instant('common.errorTitle'),
        this.translateService.instant('admin.images.batch.toasts.loadMoreError')
      );
    } finally {
      if (this.selectedParkIdSignal() === parkId) {
        this.parkPhotosLoadingMoreSignal.set(false);
      }
    }
  }

  async loadMoreParkItemPhotos(): Promise<void> {
    const parkId: string | null = this.selectedParkIdSignal();
    if (!parkId || this.photosLoadingSignal() || this.parkItemPhotosLoadingMoreSignal() || !this.parkItemPhotosCanLoadMoreSignal()) {
      return;
    }

    const nextPage: number = this.parkItemPhotosPageSignal() + 1;
    this.parkItemPhotosLoadingMoreSignal.set(true);

    try {
      const pageResult: WorkspaceImagePage<ParkItemImageDto> = await this.loadParkItemPhotosPageAsync(parkId, nextPage);
      if (this.selectedParkIdSignal() !== parkId) {
        return;
      }

      const itemNameById: Map<string, string> = this.createParkItemNameById();
      const photos: AdminPhotoBatchPhoto[] = pageResult.items.map((itemImage: ParkItemImageDto): AdminPhotoBatchPhoto =>
        this.toBatchPhoto(itemImage.image, itemNameById, itemImage.item.name)
      );
      this.appendPhotos(photos);
      this.parkItemPhotosPageSignal.set(pageResult.page);
      this.parkItemPhotosCanLoadMoreSignal.set(pageResult.canLoadMore);
    } catch (error: unknown) {
      console.error('Error loading more park item batch photos', error);
      this.toastMessageService.add(
        'error',
        this.translateService.instant('common.errorTitle'),
        this.translateService.instant('admin.images.batch.toasts.loadMoreError')
      );
    } finally {
      if (this.selectedParkIdSignal() === parkId) {
        this.parkItemPhotosLoadingMoreSignal.set(false);
      }
    }
  }

  setPhotoDraftOwnerKind(photoId: string, ownerKind: AdminPhotoBatchOwnerKind): void {
    const fallbackParkItemId: string | null = this.parkItemsSignal()[0]?.id ?? null;
    this.patchPhoto(photoId, {
      draftOwnerKind: ownerKind,
      draftParkItemId: ownerKind === 'parkItem' ? fallbackParkItemId : null,
      draftCategorySlug: ownerKind === 'parkItem' ? DEFAULT_PARK_ITEM_PHOTO_CATEGORY_SLUG : DEFAULT_PARK_PHOTO_CATEGORY_SLUG
    });
  }

  setPhotoDraftParkItemId(photoId: string, parkItemId: string): void {
    this.patchPhoto(photoId, { draftParkItemId: parkItemId || null });
  }

  setPhotoDraftCategorySlug(photoId: string, categorySlug: string): void {
    this.patchPhoto(photoId, { draftCategorySlug: categorySlug });
  }

  async savePhotoCategorization(photoId: string): Promise<void> {
    const parkId: string | null = this.selectedParkIdSignal();
    const photo: AdminPhotoBatchPhoto | undefined = this.photosSignal().find((item: AdminPhotoBatchPhoto) => item.id === photoId);

    if (!parkId || !photo || photo.isSaving) {
      return;
    }

    if (photo.draftOwnerKind === 'parkItem' && !photo.draftParkItemId) {
      this.toastMessageService.add(
        'warn',
        this.translateService.instant('common.warning'),
        this.translateService.instant('admin.images.batch.toasts.missingParkItem')
      );
      return;
    }

    await this.ensureCategoryTagsAsync();
    const ownerType: ImageOwnerType = photo.draftOwnerKind === 'parkItem' ? ImageOwnerType.PARK_ITEM : ImageOwnerType.PARK;
    const ownerId: string = photo.draftOwnerKind === 'parkItem' ? photo.draftParkItemId! : parkId;
    const category: ImageCategory = photo.draftOwnerKind === 'parkItem' ? ImageCategory.PARK_ITEM : ImageCategory.PARK;
    const selectedTagId: string | undefined = this.categoryTagIdsBySlugSignal()[photo.draftCategorySlug];
    const nextTagIds: string[] = [
      ...this.getNonCategoryTagIds(photo.image),
      ...(selectedTagId ? [selectedTagId] : [])
    ];
    const sameScope: boolean =
      photo.image.ownerType === ownerType &&
      photo.image.ownerId === ownerId &&
      photo.image.category === category;

    this.patchPhoto(photoId, { isSaving: true });

    try {
      const updatedImage: ImageDto = await firstValueFrom(this.imagesPort.updateAdminImage(photo.image.id, {
        category,
        ownerType,
        ownerId,
        isCurrent: sameScope ? photo.image.isCurrent : false,
        description: photo.image.description,
        geoLocation: photo.image.geoLocation ?? null,
        altTexts: photo.image.altTexts ?? [],
        captions: photo.image.captions ?? [],
        credits: photo.image.credits ?? [],
        tagIds: nextTagIds,
        isPublished: photo.image.isPublished === true,
        sourceUrl: photo.image.sourceUrl ?? null
      }));

      this.upsertPhoto(this.toBatchPhoto(updatedImage));
      this.toastMessageService.add(
        'success',
        this.translateService.instant('admin.images.batch.toasts.categorizedSummary'),
        this.translateService.instant('admin.images.batch.toasts.categorizedDetail')
      );
      await this.reloadParkWorkspaceAsync(parkId);
    } catch (error: unknown) {
      console.error('Error categorizing batch photo', error);
      this.patchPhoto(photoId, { isSaving: false });
      this.toastMessageService.add(
        'error',
        this.translateService.instant('common.errorTitle'),
        this.translateService.instant('admin.images.batch.toasts.categorizedError')
      );
    }
  }

  async movePhotoToUncategorized(photoId: string): Promise<void> {
    const parkId: string | null = this.selectedParkIdSignal();
    const photo: AdminPhotoBatchPhoto | undefined = this.photosSignal().find((item: AdminPhotoBatchPhoto) => item.id === photoId);

    if (!parkId || !photo || photo.isSaving) {
      return;
    }

    this.patchPhoto(photoId, { isSaving: true });

    try {
      const updatedImage: ImageDto = await firstValueFrom(this.imagesPort.updateAdminImage(photo.image.id, {
        category: ImageCategory.PARK,
        ownerType: ImageOwnerType.PARK,
        ownerId: parkId,
        isCurrent: false,
        description: photo.image.description,
        geoLocation: photo.image.geoLocation ?? null,
        altTexts: photo.image.altTexts ?? [],
        captions: photo.image.captions ?? [],
        credits: photo.image.credits ?? [],
        tagIds: this.getNonCategoryTagIds(photo.image),
        isPublished: photo.image.isPublished === true,
        sourceUrl: photo.image.sourceUrl ?? null
      }));

      this.upsertPhoto(this.toBatchPhoto(updatedImage));
      this.toastMessageService.add(
        'success',
        this.translateService.instant('admin.images.batch.toasts.uncategorizedSummary'),
        this.translateService.instant('admin.images.batch.toasts.uncategorizedDetail')
      );
      await this.reloadParkWorkspaceAsync(parkId);
    } catch (error: unknown) {
      console.error('Error moving batch photo to uncategorized', error);
      this.patchPhoto(photoId, { isSaving: false });
      this.toastMessageService.add(
        'error',
        this.translateService.instant('common.errorTitle'),
        this.translateService.instant('admin.images.batch.toasts.categorizedError')
      );
    }
  }

  async togglePublished(photoId: string): Promise<void> {
    const photo: AdminPhotoBatchPhoto | undefined = this.photosSignal().find((item: AdminPhotoBatchPhoto) => item.id === photoId);
    if (!photo || photo.isSaving) {
      return;
    }

    if (photo.image.isPublished !== true && photo.section === 'uncategorized') {
      this.toastMessageService.add(
        'warn',
        this.translateService.instant('common.warning'),
        this.translateService.instant('admin.images.batch.toasts.visibilityNeedsCategory')
      );
      return;
    }

    this.patchPhoto(photoId, { isSaving: true });

    try {
      const nextPublishedState: boolean = photo.image.isPublished !== true;
      const updatedImage: ImageDto = await firstValueFrom(this.imagesPort.updateAdminImage(photo.image.id, {
        description: photo.image.description,
        geoLocation: photo.image.geoLocation ?? null,
        altTexts: photo.image.altTexts ?? [],
        captions: photo.image.captions ?? [],
        credits: photo.image.credits ?? [],
        tagIds: photo.image.tagIds ?? [],
        isPublished: nextPublishedState,
        sourceUrl: photo.image.sourceUrl ?? null
      }));

      this.upsertPhoto(this.toBatchPhoto(updatedImage));
      this.toastMessageService.add(
        'success',
        this.translateService.instant('admin.images.batch.toasts.visibilitySummary'),
        this.translateService.instant(nextPublishedState
          ? 'admin.images.batch.toasts.visibilityPublicDetail'
          : 'admin.images.batch.toasts.visibilityHiddenDetail')
      );
    } catch (error: unknown) {
      console.error('Error toggling batch photo publication', error);
      this.patchPhoto(photoId, { isSaving: false });
      this.toastMessageService.add(
        'error',
        this.translateService.instant('common.errorTitle'),
        this.translateService.instant('admin.images.batch.toasts.visibilityError')
      );
    }
  }

  async deletePhoto(photoId: string): Promise<void> {
    const parkId: string | null = this.selectedParkIdSignal();
    const photo: AdminPhotoBatchPhoto | undefined = this.photosSignal().find((item: AdminPhotoBatchPhoto) => item.id === photoId);
    if (!photo || photo.isSaving) {
      return;
    }

    this.patchPhoto(photoId, { isSaving: true });

    try {
      const deleted: boolean = await firstValueFrom(this.imagesPort.deleteImage(photo.image.id));
      if (!deleted) {
        throw new Error('Image deletion returned false.');
      }

      this.photosSignal.set(this.photosSignal().filter((item: AdminPhotoBatchPhoto) => item.id !== photoId));
      this.toastMessageService.add(
        'success',
        this.translateService.instant('admin.images.batch.toasts.deleteSummary'),
        this.translateService.instant('admin.images.batch.toasts.deleteDetail')
      );
      await this.reloadParkWorkspaceAsync(parkId);
    } catch (error: unknown) {
      console.error('Error deleting batch photo', error);
      this.patchPhoto(photoId, { isSaving: false });
      this.toastMessageService.add(
        'error',
        this.translateService.instant('common.errorTitle'),
        this.translateService.instant('admin.images.batch.toasts.deleteError')
      );
    }
  }

  private async loadParkWorkspaceAsync(parkId: string): Promise<void> {
    this.parkItemsLoadingSignal.set(true);
    this.photosLoadingSignal.set(true);

    try {
      await this.ensureCategoryTagsAsync();
      const [parkItemRows, parkImagesPage, parkItemImagesPage]: [
        ParkItemAdminRow[],
        WorkspaceImagePage<ImageDto>,
        WorkspaceImagePage<ParkItemImageDto>
      ] = await Promise.all([
        this.loadAllParkItemsAsync(parkId),
        this.loadParkPhotosPageAsync(parkId, 1),
        this.loadParkItemPhotosPageAsync(parkId, 1)
      ]);

      if (this.selectedParkIdSignal() !== parkId) {
        return;
      }

      const parkItems: AdminPhotoBatchParkItemOption[] = parkItemRows
        .map((item: ParkItemAdminRow): AdminPhotoBatchParkItemOption => ({
          id: item.id,
          name: item.name || item.id
        }));
      const itemNameById: Map<string, string> = new Map<string, string>(
        parkItems.map((item: AdminPhotoBatchParkItemOption) => [item.id, item.name])
      );
      const photos: AdminPhotoBatchPhoto[] = [
        ...parkImagesPage.items.map((image: ImageDto): AdminPhotoBatchPhoto => this.toBatchPhoto(image, itemNameById)),
        ...parkItemImagesPage.items.map((itemImage: ParkItemImageDto): AdminPhotoBatchPhoto =>
          this.toBatchPhoto(itemImage.image, itemNameById, itemImage.item.name)
        )
      ];

      this.parkItemsSignal.set(parkItems);
      this.photosSignal.set(this.dedupePhotos(photos));
      this.parkPhotosPageSignal.set(parkImagesPage.page);
      this.parkPhotosCanLoadMoreSignal.set(parkImagesPage.canLoadMore);
      this.parkItemPhotosPageSignal.set(parkItemImagesPage.page);
      this.parkItemPhotosCanLoadMoreSignal.set(parkItemImagesPage.canLoadMore);
    } catch (error: unknown) {
      console.error('Error loading photo batch workspace', error);
      this.toastMessageService.add(
        'error',
        this.translateService.instant('common.errorTitle'),
        this.translateService.instant('admin.images.batch.toasts.workspaceLoadError')
      );
    } finally {
      this.parkItemsLoadingSignal.set(false);
      this.photosLoadingSignal.set(false);
    }
  }

  private async reloadParkWorkspaceAsync(parkId: string | null): Promise<void> {
    if (!parkId || this.selectedParkIdSignal() !== parkId) {
      return;
    }

    this.resetPhotoPagination();
    await this.loadParkWorkspaceAsync(parkId);
  }

  private async loadAllParkItemsAsync(parkId: string): Promise<ParkItemAdminRow[]> {
    const rows: ParkItemAdminRow[] = [];
    let page: number = 1;
    let shouldLoadNextPage: boolean = true;

    while (shouldLoadNextPage) {
      const response: ApiResponse<ParkItemAdminRow> = await firstValueFrom(this.parkItemsPort.getParkItemsPaginated(
        page,
        WORKSPACE_PAGE_SIZE,
        parkId,
        '',
        null,
        { sortBy: 'name', sortDirection: 'asc' }
      ));
      const pageRows: ParkItemAdminRow[] = response.data ?? [];
      rows.push(...pageRows);
      shouldLoadNextPage = this.shouldLoadNextPage(response.pagination, page, pageRows.length);
      page++;
    }

    return rows;
  }

  private async loadParkPhotosPageAsync(parkId: string, page: number): Promise<WorkspaceImagePage<ImageDto>> {
    const response: PagedResult<ImageDto> = await firstValueFrom(this.imagesPort.getAdminImages({
      page,
      size: WORKSPACE_PAGE_SIZE,
      category: ImageCategory.PARK,
      ownerType: ImageOwnerType.PARK,
      ownerId: parkId,
      sortBy: 'created',
      sortDirection: 'desc'
    }));
    const pageImages: ImageDto[] = response.items ?? [];

    return {
      items: pageImages,
      page,
      canLoadMore: this.shouldLoadNextPage(response.pagination, page, pageImages.length)
    };
  }

  private async loadParkItemPhotosPageAsync(parkId: string, page: number): Promise<WorkspaceImagePage<ParkItemImageDto>> {
    const response: PagedResult<ParkItemImageDto> = await firstValueFrom(this.imagesPort.getParkItemImagesByPark(parkId, page, WORKSPACE_PAGE_SIZE));
    const pageImages: ParkItemImageDto[] = response.items ?? [];

    return {
      items: pageImages,
      page,
      canLoadMore: this.shouldLoadNextPage(response.pagination, page, pageImages.length)
    };
  }

  private shouldLoadNextPage(pagination: PaginationContract | null | undefined, currentPage: number, currentItemCount: number): boolean {
    if (pagination && pagination.totalPages > 0) {
      return currentPage < pagination.totalPages;
    }

    return currentItemCount >= WORKSPACE_PAGE_SIZE;
  }

  private async uploadSelectionAsync(selection: AdminPhotoBatchUploadSelection, parkId: string, parkName: string): Promise<ImageDto> {
    const metadata: AdminContextualPhotoMetadataPreview | null = await this.waitForSelectionMetadata(selection.id);
    const uploadedImage: UploadedImage = await firstValueFrom(this.imagesPort.uploadImage(
      selection.file,
      ImageCategory.PARK,
      this.withWatermarkSignal(),
      parkName || selection.fileName
    ));
    const uploadedGeoLocation: ImageGeoLocation | null = this.resolveUploadedGeoLocation(uploadedImage);
    const geoLocation: ImageGeoLocation | null = uploadedGeoLocation ?? metadata?.geoLocation ?? null;
    const linkedImage: ImageDto = await firstValueFrom(this.imagesPort.linkImage({
      imageId: uploadedImage.id,
      ownerType: ImageOwnerType.PARK,
      ownerId: parkId,
      description: selection.fileName,
      setAsCurrent: false
    }));

    return firstValueFrom(this.imagesPort.updateAdminImage(linkedImage.id, {
      category: ImageCategory.PARK,
      ownerType: ImageOwnerType.PARK,
      ownerId: parkId,
      isCurrent: false,
      description: linkedImage.description || selection.fileName,
      geoLocation: linkedImage.geoLocation ?? geoLocation,
      altTexts: linkedImage.altTexts ?? [],
      captions: linkedImage.captions ?? [],
      credits: linkedImage.credits ?? [],
      tagIds: this.getNonCategoryTagIds(linkedImage),
      isPublished: false,
      sourceUrl: linkedImage.sourceUrl ?? null
    }));
  }

  private async runWithConcurrency(
    selections: AdminPhotoBatchUploadSelection[],
    worker: (selection: AdminPhotoBatchUploadSelection, index: number) => Promise<void>
  ): Promise<void> {
    let nextIndex: number = 0;
    const workerCount: number = Math.min(UPLOAD_CONCURRENCY_LIMIT, selections.length);
    const workers: Promise<void>[] = Array.from({ length: workerCount }, async (): Promise<void> => {
      while (nextIndex < selections.length) {
        const currentIndex: number = nextIndex;
        nextIndex++;
        await worker(selections[currentIndex], currentIndex);
      }
    });

    await Promise.all(workers);
  }

  private readSelectionMetadata(selection: AdminPhotoBatchUploadSelection): void {
    const request: Promise<AdminContextualPhotoMetadataPreview | null> = this.metadataReader.readFile(selection.file)
      .then((metadata: AdminContextualPhotoMetadataPreview): AdminContextualPhotoMetadataPreview => {
        this.patchSelection(selection.id, {
          metadataStatus: 'ready',
          contentType: metadata.contentType,
          sizeInBytes: metadata.sizeInBytes ?? selection.sizeInBytes,
          width: metadata.width,
          height: metadata.height,
          geoLocation: metadata.geoLocation
        });
        return metadata;
      })
      .catch((error: unknown): null => {
        console.error('Error reading photo batch metadata', error);
        this.patchSelection(selection.id, { metadataStatus: 'failed' });
        return null;
      });

    this.metadataRequestsBySelectionId.set(selection.id, request);
  }

  private waitForSelectionMetadata(selectionId: string): Promise<AdminContextualPhotoMetadataPreview | null> {
    return this.metadataRequestsBySelectionId.get(selectionId) ?? Promise.resolve(null);
  }

  private patchSelection(selectionId: string, patch: Partial<AdminPhotoBatchUploadSelection>): void {
    this.selectedFilesSignal.set(this.selectedFilesSignal().map((selection: AdminPhotoBatchUploadSelection) =>
      selection.id === selectionId
        ? { ...selection, ...patch }
        : selection
    ));
  }

  private async ensureCategoryTagsAsync(): Promise<void> {
    const existingMapping: Record<string, string> = this.categoryTagIdsBySlugSignal();
    const expectedSlugs: string[] = [
      ...PARK_PHOTO_CATEGORY_OPTIONS.map((option) => option.slug),
      ...PARK_ITEM_PHOTO_CATEGORY_OPTIONS.map((option) => option.slug)
    ];

    if (expectedSlugs.every((slug: string) => Boolean(existingMapping[slug]))) {
      return;
    }

    if (this.categoryTagsRequest) {
      return this.categoryTagsRequest;
    }

    this.categoryTagsRequest = this.loadCategoryTagsAsync();
    try {
      await this.categoryTagsRequest;
    } finally {
      this.categoryTagsRequest = null;
    }
  }

  private async loadCategoryTagsAsync(): Promise<void> {
    const existingTags: ImageTagDto[] = await firstValueFrom(this.imagesPort.getAdminImageTags());
    const tags: ImageTagDto[] = [...existingTags];
    const idsBySlug: Record<string, string> = {};
    const slugsById: Record<string, string> = {};
    const options = [
      ...PARK_PHOTO_CATEGORY_OPTIONS,
      ...PARK_ITEM_PHOTO_CATEGORY_OPTIONS
    ];

    for (const option of options) {
      let tag: ImageTagDto | undefined = tags.find((candidate: ImageTagDto) => candidate.slug === option.slug);

      if (!tag) {
        tag = await firstValueFrom(this.imagesPort.createAdminImageTag({
          slug: option.slug,
          labels: [
            { languageCode: 'fr', value: option.labelFr },
            { languageCode: 'en', value: option.labelEn }
          ],
          descriptions: []
        }));
        tags.push(tag);
      }

      idsBySlug[option.slug] = tag.id;
      slugsById[tag.id] = option.slug;
    }

    this.categoryTagIdsBySlugSignal.set(idsBySlug);
    this.categorySlugByTagIdSignal.set(slugsById);
  }

  private toBatchPhoto(image: ImageDto, itemNameById: Map<string, string> = new Map<string, string>(), explicitItemName: string | null = null): AdminPhotoBatchPhoto {
    const categorySlug: string | null = this.resolveCategorySlug(image);
    const ownerKind: AdminPhotoBatchOwnerKind = image.ownerType === ImageOwnerType.PARK_ITEM ? 'parkItem' : 'park';
    const section: AdminPhotoBatchSection = this.resolveSection(image, categorySlug);
    const parkItemId: string | null = image.ownerType === ImageOwnerType.PARK_ITEM ? image.ownerId ?? null : null;
    const parkItemName: string | null = explicitItemName ?? (parkItemId ? itemNameById.get(parkItemId) ?? parkItemId : null);
    const draftOwnerKind: AdminPhotoBatchOwnerKind = ownerKind;

    return {
      id: image.id,
      image,
      section,
      categorySlug,
      categoryLabelKey: this.resolveCategoryLabelKey(categorySlug),
      parkItemId,
      parkItemName,
      draftOwnerKind,
      draftParkItemId: parkItemId,
      draftCategorySlug: categorySlug ?? (draftOwnerKind === 'parkItem' ? DEFAULT_PARK_ITEM_PHOTO_CATEGORY_SLUG : DEFAULT_PARK_PHOTO_CATEGORY_SLUG),
      isSaving: false
    };
  }

  private resolveCategorySlug(image: ImageDto): string | null {
    const slugsById: Record<string, string> = this.categorySlugByTagIdSignal();
    const tagIds: string[] = image.tagIds ?? [];

    for (const tagId of tagIds) {
      const slug: string | undefined = slugsById[tagId];
      if (slug) {
        return slug;
      }
    }

    return null;
  }

  private resolveCategoryLabelKey(categorySlug: string | null): string | null {
    if (!categorySlug) {
      return null;
    }

    return PARK_PHOTO_CATEGORY_OPTIONS.find((option) => option.slug === categorySlug)?.labelKey
      ?? PARK_ITEM_PHOTO_CATEGORY_OPTIONS.find((option) => option.slug === categorySlug)?.labelKey
      ?? null;
  }

  private resolveSection(image: ImageDto, categorySlug: string | null): AdminPhotoBatchSection {
    if (image.ownerType === ImageOwnerType.PARK_ITEM) {
      return 'parkItem';
    }

    if (categorySlug) {
      return 'park';
    }

    return 'uncategorized';
  }

  private upsertPhoto(photo: AdminPhotoBatchPhoto): void {
    const currentPhotos: AdminPhotoBatchPhoto[] = this.photosSignal();
    const existingIndex: number = currentPhotos.findIndex((item: AdminPhotoBatchPhoto) => item.id === photo.id);

    if (existingIndex < 0) {
      this.photosSignal.set([photo, ...currentPhotos]);
      return;
    }

    const nextPhotos: AdminPhotoBatchPhoto[] = [...currentPhotos];
    nextPhotos[existingIndex] = photo;
    this.photosSignal.set(nextPhotos);
  }

  private appendPhotos(photos: AdminPhotoBatchPhoto[]): void {
    if (photos.length === 0) {
      return;
    }

    this.photosSignal.set(this.dedupePhotos([...this.photosSignal(), ...photos]));
  }

  private patchPhoto(photoId: string, patch: PhotoPatch): void {
    this.photosSignal.set(this.photosSignal().map((photo: AdminPhotoBatchPhoto) =>
      photo.id === photoId
        ? { ...photo, ...patch }
        : photo
    ));
  }

  private resetPhotoPagination(): void {
    this.parkPhotosPageSignal.set(0);
    this.parkItemPhotosPageSignal.set(0);
    this.parkPhotosCanLoadMoreSignal.set(false);
    this.parkItemPhotosCanLoadMoreSignal.set(false);
    this.parkPhotosLoadingMoreSignal.set(false);
    this.parkItemPhotosLoadingMoreSignal.set(false);
  }

  private createParkItemNameById(): Map<string, string> {
    return new Map<string, string>(
      this.parkItemsSignal().map((item: AdminPhotoBatchParkItemOption) => [item.id, item.name])
    );
  }

  private getNonCategoryTagIds(image: ImageDto): string[] {
    const categoryTagIds: Set<string> = new Set(Object.values(this.categoryTagIdsBySlugSignal()));
    return (image.tagIds ?? []).filter((tagId: string) => !categoryTagIds.has(tagId));
  }

  private resolveUploadedGeoLocation(uploadedImage: UploadedImage): ImageGeoLocation | null {
    if (uploadedImage.latitude === null || uploadedImage.latitude === undefined || uploadedImage.longitude === null || uploadedImage.longitude === undefined) {
      return null;
    }

    const latitude: number = Number(uploadedImage.latitude);
    const longitude: number = Number(uploadedImage.longitude);
    return Number.isFinite(latitude) && Number.isFinite(longitude)
      ? { latitude, longitude }
      : null;
  }

  private cleanupSelectedFilePreviews(selections: AdminPhotoBatchUploadSelection[]): void {
    selections.forEach((selection: AdminPhotoBatchUploadSelection): void => URL.revokeObjectURL(selection.previewUrl));
  }

  private dedupePhotos(photos: AdminPhotoBatchPhoto[]): AdminPhotoBatchPhoto[] {
    const photoById: Map<string, AdminPhotoBatchPhoto> = new Map<string, AdminPhotoBatchPhoto>();
    photos.forEach((photo: AdminPhotoBatchPhoto): void => {
      photoById.set(photo.id, photo);
    });

    return Array.from(photoById.values());
  }
}
