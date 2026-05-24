import { DestroyRef, Injectable, Signal, computed } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { forkJoin } from 'rxjs';

import { AdminImageBulkMetadataUpdate } from '@app/models/images/admin-image-bulk-metadata-update';
import { AdminImageSearchQuery } from '@app/models/images/admin-image-search-query';
import { ImageDto } from '@app/models/images/image-dto';
import { ImageTagDto } from '@app/models/images/image-tag-dto';
import { ImagesApiService } from '@data-access/images/images-api.service';
import { DEFAULT_PAGINATION, PagedResult, PaginationContract } from '@shared/models/contracts';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';

interface AdminSiteViewModel {
  images: ImageDto[];
  tags: ImageTagDto[];
  selectedImage: ImageDto | null;
  pagination: PaginationContract;
  query: AdminImageSearchQuery;
  selectedImageIds: string[];
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
    private readonly imagesApiService: ImagesApiService,
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
        this.screenStateStore.setError('common.errorMessage', currentData);
      },
    });
  }

  setError(): void {
    this.screenStateStore.setError('common.errorMessage', this.screenStateStore.data());
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

    if (!clonedImage.geoLocation) {
      clonedImage.geoLocation = { latitude: 0, longitude: 0 };
    }

    clonedImage.tagIds = clonedImage.tagIds ?? [];
    clonedImage.altTexts = clonedImage.altTexts ?? [];
    clonedImage.captions = clonedImage.captions ?? [];
    clonedImage.credits = clonedImage.credits ?? [];

    return clonedImage;
  }
}
