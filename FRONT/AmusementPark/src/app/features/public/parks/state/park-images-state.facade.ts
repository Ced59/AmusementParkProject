import { DestroyRef, Inject, Injectable, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { forkJoin } from 'rxjs';

import { ImageCategory } from '@app/models/images/image-category';
import { ImageDto } from '@app/models/images/image-dto';
import { ImageOwnerType } from '@app/models/images/image-owner-type';
import { ImageTagDto } from '@app/models/images/image-tag-dto';
import { ParkDetailSummary } from '@app/models/parks/park-detail-summary';
import { anonymousHttpOptions } from '@core/http/auth/anonymous-http-options';
import { hasHttpStatus } from '@core/http/http-error-status.helpers';
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import { PagedResult, PaginationContract } from '@shared/models/contracts';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';
import { UiPhotoCarouselCategoryOption, UiPhotoCarouselImage } from '@ui/media';
import { ParkDetailPhotoViewModel } from '../models/park-detail-view.model';
import { buildPhotoCategories, buildPhotos } from '../mappers/park-detail-gallery.mapper';
import {
  PARK_IMAGES_IMAGES_PORT,
  PARK_IMAGES_PARKS_PORT,
  ParkImagesImagesPort,
  ParkImagesParksPort
} from './park-images-data.ports';

interface ParkImagesPageData {
  summary: ParkDetailSummary;
  images: ImageDto[];
  imageTags: ImageTagDto[];
  pagination: PaginationContract;
}

@Injectable()
export class ParkImagesStateFacade {
  private static readonly PageSize: number = 100;
  private readonly screenStateStore = new SignalScreenStateStore<ParkImagesPageData>();
  private readonly loadingMoreSignal = signal(false);
  private readonly currentLanguageSignal = signal('en');

  public readonly state = this.screenStateStore.state;
  public readonly data = this.screenStateStore.data;
  public readonly loadingMore: Signal<boolean> = this.loadingMoreSignal.asReadonly();
  public readonly park = computed(() => this.screenStateStore.data()?.summary.park ?? null);
  public readonly totalImages = computed(() => this.screenStateStore.data()?.pagination.totalItems ?? 0);
  public readonly canLoadMore = computed(() => {
    const pagination: PaginationContract | undefined = this.screenStateStore.data()?.pagination;
    if (!pagination) {
      return false;
    }

    return pagination.currentPage < pagination.totalPages;
  });
  private readonly galleryPhotos: Signal<ParkDetailPhotoViewModel[]> = computed(() => {
    const currentData: ParkImagesPageData | undefined = this.screenStateStore.data();
    if (!currentData) {
      return [];
    }

    return buildPhotos(currentData.summary.park, currentData.images, [], currentData.imageTags, this.currentLanguageSignal());
  });
  public readonly photos: Signal<UiPhotoCarouselImage[]> = computed(() => this.galleryPhotos());
  public readonly categories: Signal<UiPhotoCarouselCategoryOption[]> = computed(() => buildPhotoCategories(this.galleryPhotos()));

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

    forkJoin({
      summary: this.parksPort.getParkDetailSummary(parkId, anonymousHttpOptions()),
      imagePage: this.imagesPort.getImagesPage(ImageOwnerType.PARK, parkId, ImageCategory.PARK, 1, ParkImagesStateFacade.PageSize, anonymousHttpOptions()),
      imageTags: this.imagesPort.getImageTags(anonymousHttpOptions())
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (response: { summary: ParkDetailSummary; imagePage: PagedResult<ImageDto>; imageTags: ImageTagDto[] }) => {
        this.screenStateStore.setReady({
          summary: response.summary,
          images: response.imagePage.items,
          imageTags: response.imageTags,
          pagination: response.imagePage.pagination
        });
      },
      error: (error: unknown) => {
        console.error('Error loading park images page', error);

        if (hasHttpStatus(error, 404)) {
          this.ssrHttpStatusService.setNotFound();
        }

        this.screenStateStore.setError('parks.imagesPage.errorMessage', previousData);
      }
    });
  }

  loadNextPage(): void {
    const currentData: ParkImagesPageData | undefined = this.screenStateStore.data();
    const parkId: string | null | undefined = currentData?.summary.park.id;
    if (!currentData || !parkId || this.loadingMoreSignal() || !this.canLoadMore()) {
      return;
    }

    const nextPage: number = currentData.pagination.currentPage + 1;
    this.loadingMoreSignal.set(true);

    this.imagesPort.getImagesPage(ImageOwnerType.PARK, parkId, ImageCategory.PARK, nextPage, ParkImagesStateFacade.PageSize, anonymousHttpOptions())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (imagePage: PagedResult<ImageDto>) => {
          this.loadingMoreSignal.set(false);
          this.screenStateStore.setReady({
            summary: currentData.summary,
            images: [...currentData.images, ...imagePage.items],
            imageTags: currentData.imageTags,
            pagination: imagePage.pagination
          });
        },
        error: (error: unknown) => {
          console.error('Error loading additional park images', error);
          this.loadingMoreSignal.set(false);
          this.screenStateStore.setError('parks.imagesPage.errorMessage', currentData);
        }
      });
  }
}
