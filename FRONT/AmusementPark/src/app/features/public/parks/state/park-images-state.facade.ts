import { DestroyRef, Inject, Injectable, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable, forkJoin, of } from 'rxjs';
import { map, switchMap } from 'rxjs/operators';

import { ImageCategory } from '@app/models/images/image-category';
import { ImageDto } from '@app/models/images/image-dto';
import { ImageOwnerType } from '@app/models/images/image-owner-type';
import { ImageTagDto } from '@app/models/images/image-tag-dto';
import { ParkItemImageDto } from '@app/models/images/park-item-image-dto';
import { ParkDetailSummary } from '@app/models/parks/park-detail-summary';
import { anonymousHttpOptions } from '@core/http/auth/anonymous-http-options';
import { hasHttpStatus } from '@core/http/http-error-status.helpers';
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import { PagedResult, PaginationContract } from '@shared/models/contracts';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';
import { UiPhotoCarouselCategoryOption, UiPhotoCarouselImage } from '@ui/media';
import { ParkDetailPhotoViewModel } from '../models/park-detail-view.model';
import { ParkImagesGalleryTab } from '../models/park-images-view.model';
import { buildPhotoCategories, buildPhotos } from '../mappers/park-detail-gallery.mapper';
import { ParkDetailItemPhotoSource } from '../mappers/park-detail-mapping.model';
import {
  PARK_IMAGES_IMAGES_PORT,
  PARK_IMAGES_PARKS_PORT,
  ParkImagesImagesPort,
  ParkImagesParksPort
} from './park-images-data.ports';

interface ParkImagesPageData {
  summary: ParkDetailSummary;
  parkImages: ImageDto[];
  logoImages: ImageDto[];
  itemImages: ParkItemImageDto[];
  itemPreviewImage: ParkItemImageDto | null;
  imageTags: ImageTagDto[];
  parkPagination: PaginationContract;
  logoPagination: PaginationContract;
  itemPagination: PaginationContract;
  itemImagesLoaded: boolean;
}

interface ParkImagesInitialResponse {
  summary: ParkDetailSummary;
  imagePage: PagedResult<ImageDto>;
  logoPage: PagedResult<ImageDto>;
  itemImageProbe: PagedResult<ParkItemImageDto>;
  imageTags: ImageTagDto[];
}

interface ParkImagesInitialData extends ParkImagesInitialResponse {
  itemImagePage: PagedResult<ParkItemImageDto> | null;
}

@Injectable()
export class ParkImagesStateFacade {
  private static readonly PageSize: number = 100;
  private readonly screenStateStore = new SignalScreenStateStore<ParkImagesPageData>();
  private readonly loadingMoreSignal = signal(false);
  private readonly itemImagesLoadingSignal = signal(false);
  private readonly currentLanguageSignal = signal('en');
  private readonly activeTabSignal = signal<ParkImagesGalleryTab>('park');

  public readonly state = this.screenStateStore.state;
  public readonly data = this.screenStateStore.data;
  public readonly loadingMore: Signal<boolean> = this.loadingMoreSignal.asReadonly();
  public readonly itemImagesLoading: Signal<boolean> = this.itemImagesLoadingSignal.asReadonly();
  public readonly activeTab: Signal<ParkImagesGalleryTab> = this.activeTabSignal.asReadonly();
  public readonly park = computed(() => this.screenStateStore.data()?.summary.park ?? null);
  public readonly parkTabImageCount = computed(() => {
    const currentData: ParkImagesPageData | undefined = this.screenStateStore.data();
    return (currentData?.logoPagination.totalItems ?? 0) + (currentData?.parkPagination.totalItems ?? 0);
  });
  public readonly itemTabImageCount = computed(() => this.screenStateStore.data()?.itemPagination.totalItems ?? 0);
  public readonly totalImages = computed(() => this.parkTabImageCount() + this.itemTabImageCount());
  public readonly showItemTab = computed(() => this.itemTabImageCount() > 0 || this.activeTabSignal() === 'items');
  public readonly socialImageId = computed(() => {
    const currentData: ParkImagesPageData | undefined = this.screenStateStore.data();
    return this.parkGalleryPhotos()[0]?.imageId
      ?? this.itemGalleryPhotos()[0]?.imageId
      ?? currentData?.itemPreviewImage?.image.id
      ?? null;
  });
  public readonly canLoadMore = computed(() => {
    const currentData: ParkImagesPageData | undefined = this.screenStateStore.data();
    if (!currentData) {
      return false;
    }

    if (this.activeTabSignal() === 'items') {
      if (!currentData.itemImagesLoaded || this.itemImagesLoadingSignal()) {
        return false;
      }

      return currentData.itemPagination.currentPage < currentData.itemPagination.totalPages;
    }

    const pagination: PaginationContract | undefined = currentData.parkPagination;
    if (!pagination) {
      return false;
    }

    return pagination.currentPage < pagination.totalPages;
  });
  private readonly parkGalleryPhotos: Signal<ParkDetailPhotoViewModel[]> = computed(() => {
    const currentData: ParkImagesPageData | undefined = this.screenStateStore.data();
    if (!currentData) {
      return [];
    }

    return buildPhotos(
      currentData.summary.park,
      currentData.parkImages,
      [],
      currentData.imageTags,
      this.currentLanguageSignal(),
      currentData.logoImages
    );
  });
  private readonly itemGalleryPhotos: Signal<ParkDetailPhotoViewModel[]> = computed(() => {
    const currentData: ParkImagesPageData | undefined = this.screenStateStore.data();
    if (!currentData) {
      return [];
    }

    return buildPhotos(
      currentData.summary.park,
      [],
      this.buildItemPhotoSources(currentData.itemImages),
      currentData.imageTags,
      this.currentLanguageSignal()
    );
  });
  private readonly activeGalleryPhotos: Signal<ParkDetailPhotoViewModel[]> = computed(() => {
    return this.activeTabSignal() === 'items' ? this.itemGalleryPhotos() : this.parkGalleryPhotos();
  });
  public readonly photos: Signal<UiPhotoCarouselImage[]> = computed(() => this.activeGalleryPhotos());
  public readonly categories: Signal<UiPhotoCarouselCategoryOption[]> = computed(() => buildPhotoCategories(this.activeGalleryPhotos()));

  constructor(
    @Inject(PARK_IMAGES_PARKS_PORT) private readonly parksPort: ParkImagesParksPort,
    @Inject(PARK_IMAGES_IMAGES_PORT) private readonly imagesPort: ParkImagesImagesPort,
    private readonly destroyRef: DestroyRef,
    private readonly ssrHttpStatusService: SsrHttpStatusService
  ) {
  }

  setCurrentLanguage(language: string): void {
    this.currentLanguageSignal.set(language || 'en');
  }

  loadParkImages(parkId: string): void {
    const previousData: ParkImagesPageData | undefined = this.screenStateStore.data();
    this.screenStateStore.setLoading(previousData);
    this.loadingMoreSignal.set(false);
    this.itemImagesLoadingSignal.set(false);
    this.activeTabSignal.set('park');

    forkJoin({
      summary: this.parksPort.getParkDetailSummary(parkId, anonymousHttpOptions()),
      imagePage: this.imagesPort.getImagesPage(ImageOwnerType.PARK, parkId, ImageCategory.PARK, 1, ParkImagesStateFacade.PageSize, anonymousHttpOptions()),
      logoPage: this.imagesPort.getImagesPage(ImageOwnerType.PARK, parkId, ImageCategory.LOGO, 1, ParkImagesStateFacade.PageSize, anonymousHttpOptions()),
      itemImageProbe: this.imagesPort.getParkItemImagesByPark(parkId, 1, 1, anonymousHttpOptions()),
      imageTags: this.imagesPort.getImageTags(anonymousHttpOptions())
    }).pipe(
      switchMap((response: ParkImagesInitialResponse) => this.loadDefaultItemTabWhenParkTabIsEmpty(parkId, response)),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: (response: ParkImagesInitialData) => {
        this.itemImagesLoadingSignal.set(false);
        this.screenStateStore.setReady({
          summary: response.summary,
          parkImages: response.imagePage.items,
          logoImages: response.logoPage.items,
          itemImages: response.itemImagePage?.items ?? [],
          itemPreviewImage: response.itemImageProbe.items[0] ?? null,
          imageTags: response.imageTags,
          parkPagination: response.imagePage.pagination,
          logoPagination: response.logoPage.pagination,
          itemPagination: response.itemImagePage?.pagination ?? response.itemImageProbe.pagination,
          itemImagesLoaded: response.itemImagePage !== null
        });
      },
      error: (error: unknown) => {
        console.error('Error loading park images page', error);
        this.itemImagesLoadingSignal.set(false);

        if (hasHttpStatus(error, 404)) {
          this.ssrHttpStatusService.setNotFound();
        }

        this.screenStateStore.setError('parks.imagesPage.errorMessage', previousData);
      }
    });
  }

  selectTab(tab: ParkImagesGalleryTab): void {
    this.activeTabSignal.set(tab);

    if (tab !== 'items') {
      return;
    }

    const currentData: ParkImagesPageData | undefined = this.screenStateStore.data();
    const parkId: string | null = this.resolveParkId(currentData);
    if (!currentData || !parkId || currentData.itemImagesLoaded || this.itemImagesLoadingSignal()) {
      return;
    }

    this.loadItemImagesPage(currentData, parkId, 1, false);
  }

  loadNextPage(): void {
    const currentData: ParkImagesPageData | undefined = this.screenStateStore.data();
    const parkId: string | null = this.resolveParkId(currentData);
    if (!currentData || !parkId || this.loadingMoreSignal() || !this.canLoadMore()) {
      return;
    }

    if (this.activeTabSignal() === 'items') {
      this.loadItemImagesPage(currentData, parkId, currentData.itemPagination.currentPage + 1, true);
      return;
    }

    const nextPage: number = currentData.parkPagination.currentPage + 1;
    this.loadingMoreSignal.set(true);

    this.imagesPort.getImagesPage(ImageOwnerType.PARK, parkId, ImageCategory.PARK, nextPage, ParkImagesStateFacade.PageSize, anonymousHttpOptions())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (imagePage: PagedResult<ImageDto>) => {
          this.loadingMoreSignal.set(false);
          this.screenStateStore.setReady({
            summary: currentData.summary,
            parkImages: [...currentData.parkImages, ...imagePage.items],
            logoImages: currentData.logoImages,
            itemImages: currentData.itemImages,
            itemPreviewImage: currentData.itemPreviewImage,
            imageTags: currentData.imageTags,
            parkPagination: imagePage.pagination,
            logoPagination: currentData.logoPagination,
            itemPagination: currentData.itemPagination,
            itemImagesLoaded: currentData.itemImagesLoaded
          });
        },
        error: (error: unknown) => {
          console.error('Error loading additional park images', error);
          this.loadingMoreSignal.set(false);
          this.screenStateStore.setError('parks.imagesPage.errorMessage', currentData);
        }
      });
  }

  private loadDefaultItemTabWhenParkTabIsEmpty(parkId: string, response: ParkImagesInitialResponse): Observable<ParkImagesInitialData> {
    const parkTabTotal: number = response.logoPage.pagination.totalItems + response.imagePage.pagination.totalItems;
    const itemTabTotal: number = response.itemImageProbe.pagination.totalItems;

    if (parkTabTotal > 0 || itemTabTotal <= 0) {
      return of({ ...response, itemImagePage: null });
    }

    this.activeTabSignal.set('items');
    this.itemImagesLoadingSignal.set(true);
    return this.imagesPort.getParkItemImagesByPark(parkId, 1, ParkImagesStateFacade.PageSize, anonymousHttpOptions()).pipe(
      map((itemImagePage: PagedResult<ParkItemImageDto>) => ({ ...response, itemImagePage }))
    );
  }

  private loadItemImagesPage(currentData: ParkImagesPageData, parkId: string, page: number, append: boolean): void {
    this.itemImagesLoadingSignal.set(!append);
    this.loadingMoreSignal.set(append);

    this.imagesPort.getParkItemImagesByPark(parkId, page, ParkImagesStateFacade.PageSize, anonymousHttpOptions())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (itemImagePage: PagedResult<ParkItemImageDto>) => {
          this.itemImagesLoadingSignal.set(false);
          this.loadingMoreSignal.set(false);
          this.screenStateStore.setReady({
            summary: currentData.summary,
            parkImages: currentData.parkImages,
            logoImages: currentData.logoImages,
            itemImages: append ? [...currentData.itemImages, ...itemImagePage.items] : itemImagePage.items,
            itemPreviewImage: currentData.itemPreviewImage ?? itemImagePage.items[0] ?? null,
            imageTags: currentData.imageTags,
            parkPagination: currentData.parkPagination,
            logoPagination: currentData.logoPagination,
            itemPagination: itemImagePage.pagination,
            itemImagesLoaded: true
          });
        },
        error: (error: unknown) => {
          console.error('Error loading park item images for park gallery', error);
          this.itemImagesLoadingSignal.set(false);
          this.loadingMoreSignal.set(false);
          this.screenStateStore.setError('parks.imagesPage.errorMessage', currentData);
        }
      });
  }

  private buildItemPhotoSources(itemImages: ParkItemImageDto[]): ParkDetailItemPhotoSource[] {
    const sourcesByItemId: Map<string, ParkDetailItemPhotoSource> = new Map<string, ParkDetailItemPhotoSource>();

    for (const entry of itemImages) {
      const itemId: string | null = this.normalizeOptionalString(entry.item.id ?? entry.image.ownerId ?? null);
      if (!itemId) {
        continue;
      }

      let source: ParkDetailItemPhotoSource | undefined = sourcesByItemId.get(itemId);
      if (!source) {
        source = {
          item: entry.item,
          photos: []
        };
        sourcesByItemId.set(itemId, source);
      }

      source.photos.push(entry.image);
    }

    return Array.from(sourcesByItemId.values());
  }

  private resolveParkId(data: ParkImagesPageData | undefined): string | null {
    return this.normalizeOptionalString(data?.summary.park.id ?? null);
  }

  private normalizeOptionalString(value: string | null | undefined): string | null {
    const normalizedValue: string = value?.trim() ?? '';
    return normalizedValue.length > 0 ? normalizedValue : null;
  }
}
