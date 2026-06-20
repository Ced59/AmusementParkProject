import { DestroyRef, Inject, Injectable, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable, forkJoin, of } from 'rxjs';
import { catchError, switchMap } from 'rxjs/operators';

import { ImageCategory } from '@app/models/images/image-category';
import { ImageDto } from '@app/models/images/image-dto';
import { ImageOwnerType } from '@app/models/images/image-owner-type';
import { Park } from '@app/models/parks/park';
import { ParkDetailSummary } from '@app/models/parks/park-detail-summary';
import { ParkItem } from '@app/models/parks/park-item';
import { VideoDto } from '@app/models/videos/video-dto';
import { VideoOwnerType } from '@app/models/videos/video-owner-type';
import { VideoTagDto } from '@app/models/videos/video-tag-dto';
import { anonymousHttpOptions } from '@core/http/auth/anonymous-http-options';
import { hasHttpStatus } from '@core/http/http-error-status.helpers';
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import { buildPublicParkItemVideoRouteCommands } from '@shared/utils/routing/public-detail-route.helpers';
import { PagedResult, PaginationContract } from '@shared/models/contracts';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';
import {
  buildPublicVideoCards,
  buildVideoTagOptions,
  buildVideoTypeOptions
} from '@features/public/videos/mappers/public-video.mapper';
import {
  PublicVideoCardViewModel,
  PublicVideoFilterState,
  PublicVideoSelectOption
} from '@features/public/videos/models/public-video-view.model';
import {
  PARK_ITEM_VIDEOS_ITEMS_PORT,
  PARK_ITEM_VIDEOS_IMAGES_PORT,
  PARK_ITEM_VIDEOS_PARKS_PORT,
  PARK_ITEM_VIDEOS_VIDEOS_PORT,
  ParkItemVideosImagesPort,
  ParkItemVideosItemsPort,
  ParkItemVideosParksPort,
  ParkItemVideosVideosPort
} from './park-item-videos-data.ports';

interface ParkItemVideosPageData {
  item: ParkItem;
  park: Park;
  itemImageId: string | null;
  parkImageId: string | null;
  videos: VideoDto[];
  videoTags: VideoTagDto[];
  pagination: PaginationContract;
  filters: PublicVideoFilterState;
}

@Injectable()
export class ParkItemVideosStateFacade {
  private static readonly PageSize: number = 24;
  private readonly screenStateStore = new SignalScreenStateStore<ParkItemVideosPageData>();
  private readonly loadingMoreSignal = signal(false);
  private readonly currentLanguageSignal = signal('en');

  public readonly state = this.screenStateStore.state;
  public readonly data = this.screenStateStore.data;
  public readonly loadingMore: Signal<boolean> = this.loadingMoreSignal.asReadonly();
  public readonly item = computed(() => this.screenStateStore.data()?.item ?? null);
  public readonly park = computed(() => this.screenStateStore.data()?.park ?? null);
  public readonly itemImageId = computed(() => this.screenStateStore.data()?.itemImageId ?? null);
  public readonly parkImageId = computed(() => this.screenStateStore.data()?.parkImageId ?? null);
  public readonly totalVideos = computed(() => this.screenStateStore.data()?.pagination.totalItems ?? 0);
  public readonly filters = computed(() => this.screenStateStore.data()?.filters ?? createEmptyFilters());
  public readonly canLoadMore = computed(() => {
    const pagination: PaginationContract | undefined = this.screenStateStore.data()?.pagination;
    if (!pagination) {
      return false;
    }

    return pagination.currentPage < pagination.totalPages;
  });
  public readonly typeOptions: Signal<PublicVideoSelectOption[]> = computed(() => buildVideoTypeOptions());
  public readonly tagOptions: Signal<PublicVideoSelectOption[]> = computed(() => {
    const currentData: ParkItemVideosPageData | undefined = this.screenStateStore.data();
    return currentData ? buildVideoTagOptions(currentData.videoTags, this.currentLanguageSignal()) : [];
  });
  public readonly videoCards: Signal<PublicVideoCardViewModel[]> = computed(() => {
    const currentData: ParkItemVideosPageData | undefined = this.screenStateStore.data();
    if (!currentData) {
      return [];
    }

    return buildPublicVideoCards(
      currentData.videos,
      currentData.videoTags,
      this.currentLanguageSignal(),
      (video: VideoDto) => buildPublicParkItemVideoRouteCommands({
        language: this.currentLanguageSignal(),
        parkId: currentData.park.id,
        parkName: currentData.park.name,
        itemId: currentData.item.id,
        itemName: currentData.item.name,
        videoId: video.id,
        videoTitle: resolveVideoRouteTitle(video)
      })
    );
  });

  constructor(
    @Inject(PARK_ITEM_VIDEOS_ITEMS_PORT) private readonly itemsPort: ParkItemVideosItemsPort,
    @Inject(PARK_ITEM_VIDEOS_PARKS_PORT) private readonly parksPort: ParkItemVideosParksPort,
    @Inject(PARK_ITEM_VIDEOS_IMAGES_PORT) private readonly imagesPort: ParkItemVideosImagesPort,
    @Inject(PARK_ITEM_VIDEOS_VIDEOS_PORT) private readonly videosPort: ParkItemVideosVideosPort,
    private readonly destroyRef: DestroyRef,
    private readonly ssrHttpStatusService: SsrHttpStatusService
  ) {
  }

  setCurrentLanguage(language: string): void {
    this.currentLanguageSignal.set(language || 'en');
  }

  loadItemVideos(itemId: string, filters: PublicVideoFilterState): void {
    const previousData: ParkItemVideosPageData | undefined = this.screenStateStore.data();
    const normalizedFilters: PublicVideoFilterState = normalizeFilters(filters);
    this.screenStateStore.setLoading(previousData);

    this.itemsPort.getParkItemById(itemId, anonymousHttpOptions()).pipe(
      switchMap((item: ParkItem) => this.loadPageData(item, itemId, normalizedFilters, 1)),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: (response: { item: ParkItem; summary: ParkDetailSummary; itemImages: ImageDto[]; videoPage: PagedResult<VideoDto>; videoTags: VideoTagDto[] }) => {
        this.screenStateStore.setReady({
          item: response.item,
          park: response.summary.park,
          itemImageId: resolveItemSocialImageId(response.itemImages),
          parkImageId: response.summary.mainImage?.id ?? null,
          videos: response.videoPage.items,
          videoTags: response.videoTags,
          pagination: response.videoPage.pagination,
          filters: normalizedFilters
        });
      },
      error: (error: unknown) => {
        console.error('Error loading park item videos page', error);

        if (hasHttpStatus(error, 404)) {
          this.ssrHttpStatusService.setNotFound();
        }

        this.screenStateStore.setError('videos.list.errorMessage', previousData);
      }
    });
  }

  loadNextPage(): void {
    const currentData: ParkItemVideosPageData | undefined = this.screenStateStore.data();
    const itemId: string | null = normalizeOptionalString(currentData?.item.id);
    if (!currentData || !itemId || this.loadingMoreSignal() || !this.canLoadMore()) {
      return;
    }

    const nextPage: number = currentData.pagination.currentPage + 1;
    this.loadingMoreSignal.set(true);

    this.videosPort.getVideosPage(this.buildVideoQuery(itemId, currentData.filters, nextPage), anonymousHttpOptions())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (videoPage: PagedResult<VideoDto>) => {
          this.loadingMoreSignal.set(false);
          this.screenStateStore.setReady({
            ...currentData,
            videos: [...currentData.videos, ...videoPage.items],
            pagination: videoPage.pagination
          });
        },
        error: (error: unknown) => {
          console.error('Error loading additional park item videos', error);
          this.loadingMoreSignal.set(false);
          this.screenStateStore.setError('videos.list.errorMessage', currentData);
        }
      });
  }

  private loadPageData(
    item: ParkItem,
    routeItemId: string,
    filters: PublicVideoFilterState,
    page: number
  ): Observable<{ item: ParkItem; summary: ParkDetailSummary; itemImages: ImageDto[]; videoPage: PagedResult<VideoDto>; videoTags: VideoTagDto[] }> {
    const itemId: string = normalizeOptionalString(item.id) ?? routeItemId;

    return forkJoin({
      item: of(item),
      summary: this.parksPort.getParkDetailSummary(item.parkId, anonymousHttpOptions()),
      itemImages: this.imagesPort.getImages(ImageOwnerType.PARK_ITEM, itemId, ImageCategory.PARK_ITEM, 1, 1, anonymousHttpOptions())
        .pipe(catchError(() => of([] as ImageDto[]))),
      videoPage: this.videosPort.getVideosPage(this.buildVideoQuery(itemId, filters, page), anonymousHttpOptions()),
      videoTags: this.videosPort.getVideoTags(anonymousHttpOptions())
    });
  }

  private buildVideoQuery(itemId: string, filters: PublicVideoFilterState, page: number) {
    return {
      page,
      size: ParkItemVideosStateFacade.PageSize,
      ownerType: VideoOwnerType.PARK_ITEM,
      ownerId: itemId,
      type: filters.type,
      tagId: filters.tagId,
      creatorName: filters.creatorName,
      languageCode: this.currentLanguageSignal(),
      sortBy: 'published',
      sortDirection: 'desc'
    };
  }
}

function resolveVideoRouteTitle(video: VideoDto): string {
  return normalizeOptionalString(video.title)
    ?? normalizeOptionalString(video.titles?.[0]?.value)
    ?? video.id;
}

function createEmptyFilters(): PublicVideoFilterState {
  return { type: null, tagId: null, creatorName: '' };
}

function normalizeFilters(filters: PublicVideoFilterState): PublicVideoFilterState {
  return {
    type: filters.type ?? null,
    tagId: normalizeOptionalString(filters.tagId),
    creatorName: normalizeOptionalString(filters.creatorName) ?? ''
  };
}

function normalizeOptionalString(value: string | null | undefined): string | null {
  const normalized: string = value?.trim() ?? '';
  return normalized.length > 0 ? normalized : null;
}

function resolveItemSocialImageId(images: ImageDto[]): string | null {
  const image: ImageDto | undefined = images.find((candidate: ImageDto) => {
    return candidate.isPublished !== false && normalizeOptionalString(candidate.id) !== null;
  });

  return normalizeOptionalString(image?.id);
}
