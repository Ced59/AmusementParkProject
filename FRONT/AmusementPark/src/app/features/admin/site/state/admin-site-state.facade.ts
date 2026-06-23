import {
  DestroyRef,
  Injectable,
  Signal,
  computed,
  Inject,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { forkJoin } from 'rxjs';

import { AdminImageBulkMetadataUpdate } from '@app/models/images/admin-image-bulk-metadata-update';
import { AdminImageSearchQuery } from '@app/models/images/admin-image-search-query';
import { ImageCategory } from '@app/models/images/image-category';
import { ImageDto } from '@app/models/images/image-dto';
import { ImageGeoLocation } from '@app/models/images/image-geo-location';
import { ImageOwnerType } from '@app/models/images/image-owner-type';
import { ImageTagDto } from '@app/models/images/image-tag-dto';
import { DEFAULT_PAGINATION, PagedResult, PaginationContract } from '@shared/models/contracts';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';

import {
  ADMIN_SITE_STATE_IMAGES_API_SERVICE_PORT,
  AdminSiteStateImagesApiServicePort
} from './admin-site-state-data.ports';

interface AdminSiteViewModel {
  images: ImageDto[];
  tags: ImageTagDto[];
  selectedImage: ImageDto | null;
  pagination: PaginationContract;
  query: AdminImageSearchQuery;
  selectedImageIds: string[];
  operationErrorKey: string | null;
}

const DEFAULT_IMAGE_QUERY: AdminImageSearchQuery = {
  page: 1,
  size: 40,
  search: null,
  category: null,
  ownerType: null,
  ownerId: null,
  tagId: null,
  isPublished: null,
  hasOwner: null,
  hasGeoLocation: null,
  sortBy: 'created',
  sortDirection: 'desc',
};

const DEFAULT_LANGUAGE_CODE = 'fr';

@Injectable()
export class AdminSiteStateFacade {
  private readonly screenStateStore = new SignalScreenStateStore<AdminSiteViewModel>();

  public readonly state = this.screenStateStore.state;
  public readonly images: Signal<ImageDto[]> = computed(() => this.screenStateStore.data()?.images ?? []);
  public readonly tags: Signal<ImageTagDto[]> = computed(() => this.screenStateStore.data()?.tags ?? []);
  public readonly selectedImage: Signal<ImageDto | null> = computed(() => this.screenStateStore.data()?.selectedImage ?? null);
  public readonly pagination: Signal<PaginationContract> = computed(() => this.screenStateStore.data()?.pagination ?? DEFAULT_PAGINATION);
  public readonly query: Signal<AdminImageSearchQuery> = computed(() => this.screenStateStore.data()?.query ?? DEFAULT_IMAGE_QUERY);
  public readonly selectedImageIds: Signal<string[]> = computed(() => this.screenStateStore.data()?.selectedImageIds ?? []);
  public readonly operationErrorKey: Signal<string | null> = computed(() => this.screenStateStore.data()?.operationErrorKey ?? null);
  public readonly selectedCount: Signal<number> = computed(() => this.selectedImageIds().length);
  public readonly isEveryPageImageSelected: Signal<boolean> = computed(() => {
    const imageIds: string[] = this.images().map((image: ImageDto) => image.id);

    if (imageIds.length === 0) {
      return false;
    }

    const selectedIds: Set<string> = new Set(this.selectedImageIds());
    return imageIds.every((imageId: string) => selectedIds.has(imageId));
  });

  constructor(
    @Inject(ADMIN_SITE_STATE_IMAGES_API_SERVICE_PORT) private readonly imagesApiService: AdminSiteStateImagesApiServicePort,
    private readonly destroyRef: DestroyRef
  ) {
  }

  reload(): void {
    const previousData: AdminSiteViewModel | undefined = this.screenStateStore.data();
    const query: AdminImageSearchQuery = previousData?.query ?? DEFAULT_IMAGE_QUERY;
    this.screenStateStore.setLoading(previousData);

    forkJoin({
      page: this.imagesApiService.getAdminImages(query),
      tags: this.imagesApiService.getAdminImageTags(),
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: ({ page, tags }: { page: PagedResult<ImageDto>; tags: ImageTagDto[] }) => {
        const selectedImageIds: string[] = this.filterSelection(previousData?.selectedImageIds ?? [], page.items);
        const selectedImage: ImageDto | null = this.resolveSelectedImage(page.items, previousData?.selectedImage?.id ?? null);
        this.screenStateStore.setReady({
          images: page.items,
          tags,
          selectedImage,
          pagination: page.pagination,
          query,
          selectedImageIds,
          operationErrorKey: null,
        });
      },
      error: (error: unknown) => {
        console.error('Error loading admin image data', error);
        this.screenStateStore.setError('common.errorMessage', previousData);
      },
    });
  }

  updateQuery(patch: Partial<AdminImageSearchQuery>, resetPage: boolean = true): void {
    const currentData: AdminSiteViewModel | undefined = this.screenStateStore.data();
    const currentQuery: AdminImageSearchQuery = currentData?.query ?? DEFAULT_IMAGE_QUERY;
    const query: AdminImageSearchQuery = {
      ...currentQuery,
      ...patch,
      page: resetPage ? 1 : (patch.page ?? currentQuery.page),
    };

    this.screenStateStore.setReady({
      images: currentData?.images ?? [],
      tags: currentData?.tags ?? [],
      selectedImage: currentData?.selectedImage ?? null,
      pagination: currentData?.pagination ?? DEFAULT_PAGINATION,
      query,
      selectedImageIds: currentData?.selectedImageIds ?? [],
      operationErrorKey: null,
    });
  }

  applyQuery(): void {
    this.reload();
  }

  clearFilters(): void {
    const currentData: AdminSiteViewModel | undefined = this.screenStateStore.data();
    this.screenStateStore.setReady({
      images: currentData?.images ?? [],
      tags: currentData?.tags ?? [],
      selectedImage: currentData?.selectedImage ?? null,
      pagination: currentData?.pagination ?? DEFAULT_PAGINATION,
      query: DEFAULT_IMAGE_QUERY,
      selectedImageIds: [],
      operationErrorKey: null,
    });
    this.reload();
  }

  changePage(page: number): void {
    this.updateQuery({ page }, false);
    this.reload();
  }

  changePageSize(size: number): void {
    this.updateQuery({ size, page: 1 }, false);
    this.reload();
  }

  selectImage(image: ImageDto): void {
    const currentData: AdminSiteViewModel | undefined = this.screenStateStore.data();

    if (!currentData) {
      return;
    }

    this.screenStateStore.setReady({
      ...currentData,
      selectedImage: this.cloneImage(image),
      operationErrorKey: null,
    });
  }

  updateSelectedImage(patch: Partial<ImageDto>): void {
    const currentData: AdminSiteViewModel | undefined = this.screenStateStore.data();

    if (!currentData?.selectedImage) {
      return;
    }

    this.screenStateStore.setReady({
      ...currentData,
      selectedImage: {
        ...currentData.selectedImage,
        ...patch,
      },
      operationErrorKey: null,
    });
  }

  toggleTag(tagId: string, checked: boolean): void {
    const currentData: AdminSiteViewModel | undefined = this.screenStateStore.data();

    if (!currentData?.selectedImage) {
      return;
    }

    const currentTags: Set<string> = new Set(currentData.selectedImage.tagIds ?? []);

    if (checked) {
      currentTags.add(tagId);
    } else {
      currentTags.delete(tagId);
    }

    this.screenStateStore.setReady({
      ...currentData,
      selectedImage: {
        ...currentData.selectedImage,
        tagIds: Array.from(currentTags),
      },
      operationErrorKey: null,
    });
  }

  toggleImageSelection(imageId: string, checked: boolean): void {
    const currentData: AdminSiteViewModel | undefined = this.screenStateStore.data();

    if (!currentData) {
      return;
    }

    const selectedIds: Set<string> = new Set(currentData.selectedImageIds);

    if (checked) {
      selectedIds.add(imageId);
    } else {
      selectedIds.delete(imageId);
    }

    this.screenStateStore.setReady({
      ...currentData,
      selectedImageIds: Array.from(selectedIds),
      operationErrorKey: null,
    });
  }

  toggleCurrentPageSelection(checked: boolean): void {
    const currentData: AdminSiteViewModel | undefined = this.screenStateStore.data();

    if (!currentData) {
      return;
    }

    const selectedIds: Set<string> = new Set(currentData.selectedImageIds);
    currentData.images.forEach((image: ImageDto) => {
      if (checked) {
        selectedIds.add(image.id);
      } else {
        selectedIds.delete(image.id);
      }
    });

    this.screenStateStore.setReady({
      ...currentData,
      selectedImageIds: Array.from(selectedIds),
      operationErrorKey: null,
    });
  }

  clearSelection(): void {
    const currentData: AdminSiteViewModel | undefined = this.screenStateStore.data();

    if (!currentData) {
      return;
    }

    this.screenStateStore.setReady({
      ...currentData,
      selectedImageIds: [],
      operationErrorKey: null,
    });
  }

  applyBulkMetadata(patch: Omit<AdminImageBulkMetadataUpdate, 'imageIds'>): void {
    const currentData: AdminSiteViewModel | undefined = this.screenStateStore.data();
    const imageIds: string[] = currentData?.selectedImageIds ?? [];

    if (imageIds.length === 0) {
      return;
    }

    this.screenStateStore.setLoading(currentData);
    this.imagesApiService.updateAdminImagesBulkMetadata({
      imageIds,
      ...patch,
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.reload();
      },
      error: (error: unknown) => {
        console.error('Error applying bulk image metadata', error);
        this.setOperationError(currentData);
      },
    });
  }

  saveSelectedImage(): void {
    const currentData: AdminSiteViewModel | undefined = this.screenStateStore.data();
    const selectedImage: ImageDto | null | undefined = currentData?.selectedImage;

    if (!currentData || !selectedImage) {
      return;
    }

    this.imagesApiService.updateAdminImage(selectedImage.id, {
      description: selectedImage.description,
      category: selectedImage.category,
      ownerType: selectedImage.ownerType,
      ownerId: selectedImage.ownerType === ImageOwnerType.NONE ? null : selectedImage.ownerId,
      isCurrent: selectedImage.isCurrent,
      geoLocation: this.normalizeGeoLocation(selectedImage.geoLocation),
      altTexts: selectedImage.altTexts ?? [],
      captions: selectedImage.captions ?? [],
      credits: selectedImage.credits ?? [],
      tagIds: selectedImage.tagIds ?? [],
      isPublished: selectedImage.isPublished,
      sourceUrl: selectedImage.sourceUrl ?? null,
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.reload();
      },
      error: (error: unknown) => {
        console.error('Error saving admin image metadata', error);
        this.setOperationError(currentData);
      },
    });
  }

  applyWatermarkToSelectedImage(): void {
    const currentData: AdminSiteViewModel | undefined = this.screenStateStore.data();
    const selectedImage: ImageDto | null | undefined = currentData?.selectedImage;

    if (!currentData || !selectedImage || !this.canApplyWatermark(selectedImage)) {
      return;
    }

    this.imagesApiService.applyWatermark(selectedImage.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.reload();
      },
      error: (error: unknown) => {
        console.error('Error applying image watermark', error);
        this.setOperationError(currentData);
      },
    });
  }

  createTag(slugValue: string): boolean {
    const currentData: AdminSiteViewModel | undefined = this.screenStateStore.data();
    const slug: string = slugValue.trim().toLowerCase();

    if (!currentData || !slug) {
      return false;
    }

    this.imagesApiService.createAdminImageTag({
      slug,
      labels: [{ languageCode: DEFAULT_LANGUAGE_CODE, value: slug }],
      descriptions: [],
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.reload();
      },
      error: (error: unknown) => {
        console.error('Error creating admin image tag', error);
        this.setOperationError(currentData);
      },
    });

    return true;
  }

  deleteImages(imageIds: readonly string[]): void {
    const currentData: AdminSiteViewModel | undefined = this.screenStateStore.data();
    const uniqueImageIds: string[] = Array.from(new Set(
      imageIds
        .map((imageId: string) => imageId.trim())
        .filter((imageId: string) => imageId.length > 0)
    ));

    if (!currentData || uniqueImageIds.length === 0) {
      return;
    }

    forkJoin(uniqueImageIds.map((imageId: string) => this.imagesApiService.deleteImage(imageId)))
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.reload();
        },
        error: (error: unknown) => {
          console.error('Error deleting admin images', error);
          this.setOperationError(currentData);
        },
      });
  }

  setError(): void {
    this.setOperationError(this.screenStateStore.data());
  }

  private resolveSelectedImage(images: ImageDto[], previousSelectionId: string | null): ImageDto | null {
    if (previousSelectionId) {
      const refreshedSelection: ImageDto | undefined = images.find((image: ImageDto) => image.id === previousSelectionId);

      if (refreshedSelection) {
        return this.cloneImage(refreshedSelection);
      }
    }

    if (images[0]) {
      return this.cloneImage(images[0]);
    }

    return null;
  }

  private filterSelection(selectedImageIds: string[], images: ImageDto[]): string[] {
    const pageImageIds: Set<string> = new Set(images.map((image: ImageDto) => image.id));
    return selectedImageIds.filter((imageId: string) => pageImageIds.has(imageId));
  }

  private cloneImage(image: ImageDto): ImageDto {
    const clonedImage: ImageDto = JSON.parse(JSON.stringify(image)) as ImageDto;

    clonedImage.tagIds = clonedImage.tagIds ?? [];
    clonedImage.altTexts = clonedImage.altTexts ?? [];
    clonedImage.captions = clonedImage.captions ?? [];
    clonedImage.credits = clonedImage.credits ?? [];

    return clonedImage;
  }

  private canApplyWatermark(image: ImageDto): boolean {
    return image.category !== ImageCategory.LOGO && !image.isWatermarked && !!image.path;
  }

  private normalizeGeoLocation(geoLocation: ImageGeoLocation | null | undefined): ImageGeoLocation | null {
    if (!geoLocation) {
      return null;
    }

    const latitude: number = Number(geoLocation.latitude);
    const longitude: number = Number(geoLocation.longitude);

    if (!Number.isFinite(latitude) || !Number.isFinite(longitude)) {
      return null;
    }

    return {
      latitude,
      longitude,
    };
  }

  private setOperationError(previousData: AdminSiteViewModel | undefined): void {
    const currentData: AdminSiteViewModel | undefined = previousData ?? this.screenStateStore.data();

    if (!currentData) {
      this.screenStateStore.setError('common.errorMessage');
      return;
    }

    this.screenStateStore.setReady({
      ...currentData,
      operationErrorKey: 'common.errorMessage',
    });
  }
}
