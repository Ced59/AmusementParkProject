import { DestroyRef, Inject, Injectable, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable, forkJoin, of } from 'rxjs';
import { switchMap } from 'rxjs/operators';

import { ImageCategory } from '@app/models/images/image-category';
import { ImageDto } from '@app/models/images/image-dto';
import { ImageOwnerType } from '@app/models/images/image-owner-type';
import { ImageTagDto } from '@app/models/images/image-tag-dto';
import { Park } from '@app/models/parks/park';
import { ParkItem } from '@app/models/parks/park-item';
import { anonymousHttpOptions } from '@core/http/auth/anonymous-http-options';
import { hasHttpStatus } from '@core/http/http-error-status.helpers';
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import { PagedResult, PaginationContract } from '@shared/models/contracts';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';
import { UiPhotoCarouselCategoryOption, UiPhotoCarouselImage } from '@ui/media';
import { buildPhotoCategories, buildPhotos } from '../mappers/park-item-detail-photos.mapper';
import { ParkItemPhotoViewModel } from '../models/park-item-detail-view.model';
import {
  PARK_ITEM_IMAGES_IMAGES_PORT,
  PARK_ITEM_IMAGES_ITEMS_PORT,
  PARK_ITEM_IMAGES_PARKS_PORT,
  ParkItemImagesImagesPort,
  ParkItemImagesItemsPort,
  ParkItemImagesParksPort
} from './park-item-images-data.ports';

interface ParkItemImagesPageData {
  item: ParkItem;
  park: Park;
  images: ImageDto[];
  imageTags: ImageTagDto[];
  pagination: PaginationContract;
}

@Injectable()
export class ParkItemImagesStateFacade {
  private static readonly PageSize: number = 100;
  private readonly screenStateStore = new SignalScreenStateStore<ParkItemImagesPageData>();
  private readonly loadingMoreSignal = signal(false);
  private readonly currentLanguageSignal = signal('en');

  public readonly state = this.screenStateStore.state;
  public readonly data = this.screenStateStore.data;
  public readonly loadingMore: Signal<boolean> = this.loadingMoreSignal.asReadonly();
  public readonly item = computed(() => this.screenStateStore.data()?.item ?? null);
  public readonly park = computed(() => this.screenStateStore.data()?.park ?? null);
  public readonly totalImages = computed(() => this.screenStateStore.data()?.pagination.totalItems ?? 0);
  public readonly canLoadMore = computed(() => {
    const pagination: PaginationContract | undefined = this.screenStateStore.data()?.pagination;
    if (!pagination) {
      return false;
    }

    return pagination.currentPage < pagination.totalPages;
  });
  private readonly galleryPhotos: Signal<ParkItemPhotoViewModel[]> = computed(() => {
    const currentData: ParkItemImagesPageData | undefined = this.screenStateStore.data();
    if (!currentData) {
      return [];
    }

    return buildPhotos(currentData.images, currentData.imageTags, this.currentLanguageSignal());
  });
  public readonly photos: Signal<UiPhotoCarouselImage[]> = computed(() => this.galleryPhotos());
  public readonly categories: Signal<UiPhotoCarouselCategoryOption[]> = computed(() => buildPhotoCategories(this.galleryPhotos()));

  constructor(
    @Inject(PARK_ITEM_IMAGES_ITEMS_PORT) private readonly itemsPort: ParkItemImagesItemsPort,
    @Inject(PARK_ITEM_IMAGES_PARKS_PORT) private readonly parksPort: ParkItemImagesParksPort,
    @Inject(PARK_ITEM_IMAGES_IMAGES_PORT) private readonly imagesPort: ParkItemImagesImagesPort,
    private readonly destroyRef: DestroyRef,
    private readonly ssrHttpStatusService: SsrHttpStatusService
  ) {
  }

  setCurrentLanguage(language: string): void {
    this.currentLanguageSignal.set(language || 'en');
  }

  loadItemImages(itemId: string): void {
    const previousData: ParkItemImagesPageData | undefined = this.screenStateStore.data();
    this.screenStateStore.setLoading(previousData);

    this.itemsPort.getParkItemById(itemId, anonymousHttpOptions()).pipe(
      switchMap((item: ParkItem) => this.loadPageData(item, itemId, 1)),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: (response: { item: ParkItem; park: Park; imagePage: PagedResult<ImageDto>; imageTags: ImageTagDto[] }) => {
        this.screenStateStore.setReady({
          item: response.item,
          park: response.park,
          images: response.imagePage.items,
          imageTags: response.imageTags,
          pagination: response.imagePage.pagination
        });
      },
      error: (error: unknown) => {
        console.error('Error loading park item images page', error);

        if (hasHttpStatus(error, 404)) {
          this.ssrHttpStatusService.setNotFound();
        }

        this.screenStateStore.setError('parkItems.imagesPage.errorMessage', previousData);
      }
    });
  }

  loadNextPage(): void {
    const currentData: ParkItemImagesPageData | undefined = this.screenStateStore.data();
    const itemId: string | null = this.resolveItemId(currentData?.item ?? null);
    if (!currentData || !itemId || this.loadingMoreSignal() || !this.canLoadMore()) {
      return;
    }

    const nextPage: number = currentData.pagination.currentPage + 1;
    this.loadingMoreSignal.set(true);

    this.imagesPort.getImagesPage(ImageOwnerType.ATTRACTION, itemId, ImageCategory.ATTRACTION, nextPage, ParkItemImagesStateFacade.PageSize, anonymousHttpOptions())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (imagePage: PagedResult<ImageDto>) => {
          this.loadingMoreSignal.set(false);
          this.screenStateStore.setReady({
            ...currentData,
            images: [...currentData.images, ...imagePage.items],
            pagination: imagePage.pagination
          });
        },
        error: (error: unknown) => {
          console.error('Error loading additional park item images', error);
          this.loadingMoreSignal.set(false);
          this.screenStateStore.setError('parkItems.imagesPage.errorMessage', currentData);
        }
      });
  }

  private loadPageData(item: ParkItem, routeItemId: string, page: number): Observable<{ item: ParkItem; park: Park; imagePage: PagedResult<ImageDto>; imageTags: ImageTagDto[] }> {
    const itemId: string = this.resolveItemId(item) ?? routeItemId;

    return forkJoin({
      item: of(item),
      park: this.parksPort.getParkById(item.parkId, anonymousHttpOptions()),
      imagePage: this.imagesPort.getImagesPage(ImageOwnerType.ATTRACTION, itemId, ImageCategory.ATTRACTION, page, ParkItemImagesStateFacade.PageSize, anonymousHttpOptions()),
      imageTags: this.imagesPort.getImageTags(anonymousHttpOptions())
    });
  }

  private resolveItemId(item: ParkItem | null): string | null {
    const normalizedItemId: string = item?.id?.trim() ?? '';
    return normalizedItemId.length > 0 ? normalizedItemId : null;
  }
}
