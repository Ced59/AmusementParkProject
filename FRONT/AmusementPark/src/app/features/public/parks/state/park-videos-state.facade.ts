import { DestroyRef, Inject, Injectable, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable, forkJoin, of } from 'rxjs';
import { map, switchMap } from 'rxjs/operators';

import { ParkDetailSummary } from '@app/models/parks/park-detail-summary';
import { ParkItem } from '@app/models/parks/park-item';
import { ParkItemVideoDto } from '@app/models/videos/park-item-video-dto';
import { VideoDto } from '@app/models/videos/video-dto';
import { VideoOwnerType } from '@app/models/videos/video-owner-type';
import { VideoSearchQuery } from '@app/models/videos/video-search-query';
import { VideoTagDto } from '@app/models/videos/video-tag-dto';
import { anonymousHttpOptions } from '@core/http/auth/anonymous-http-options';
import { hasHttpStatus } from '@core/http/http-error-status.helpers';
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import { buildPublicParkItemVideoRouteCommands, buildPublicParkVideoRouteCommands } from '@shared/utils/routing/public-detail-route.helpers';
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
  PARK_VIDEOS_PARKS_PORT,
  PARK_VIDEOS_VIDEOS_PORT,
  ParkVideosParksPort,
  ParkVideosVideosPort
} from './park-videos-data.ports';
import { ParkVideosGalleryTab } from '../models/park-videos-view.model';

interface ParkVideosPageData {
  summary: ParkDetailSummary;
  parkVideos: VideoDto[];
  itemVideos: ParkItemVideoDto[];
  videoTags: VideoTagDto[];
  parkPagination: PaginationContract;
  itemPagination: PaginationContract;
  itemVideosLoaded: boolean;
  filters: PublicVideoFilterState;
}

interface ParkVideosInitialResponse {
  summary: ParkDetailSummary;
  parkVideoPage: PagedResult<VideoDto>;
  itemVideoProbe: PagedResult<ParkItemVideoDto>;
  videoTags: VideoTagDto[];
}

interface ParkVideosInitialData extends ParkVideosInitialResponse {
  itemVideoPage: PagedResult<ParkItemVideoDto> | null;
}

@Injectable()
export class ParkVideosStateFacade {
  private static readonly PageSize: number = 24;
  private readonly screenStateStore = new SignalScreenStateStore<ParkVideosPageData>();
  private readonly loadingMoreSignal = signal(false);
  private readonly itemVideosLoadingSignal = signal(false);
  private readonly currentLanguageSignal = signal('en');
  private readonly activeTabSignal = signal<ParkVideosGalleryTab>('park');

  public readonly state = this.screenStateStore.state;
  public readonly data = this.screenStateStore.data;
  public readonly loadingMore: Signal<boolean> = this.loadingMoreSignal.asReadonly();
  public readonly itemVideosLoading: Signal<boolean> = this.itemVideosLoadingSignal.asReadonly();
  public readonly activeTab: Signal<ParkVideosGalleryTab> = this.activeTabSignal.asReadonly();
  public readonly park = computed(() => this.screenStateStore.data()?.summary.park ?? null);
  public readonly parkImageId = computed(() => this.screenStateStore.data()?.summary.mainImage?.id ?? null);
  public readonly parkTabVideoCount = computed(() => this.screenStateStore.data()?.parkPagination.totalItems ?? 0);
  public readonly itemTabVideoCount = computed(() => this.screenStateStore.data()?.itemPagination.totalItems ?? 0);
  public readonly totalVideos = computed(() => this.parkTabVideoCount() + this.itemTabVideoCount());
  public readonly showItemTab = computed(() => this.itemTabVideoCount() > 0 || this.activeTabSignal() === 'items');
  public readonly filters = computed(() => this.screenStateStore.data()?.filters ?? createEmptyFilters());
  public readonly canLoadMore = computed(() => {
    const currentData: ParkVideosPageData | undefined = this.screenStateStore.data();
    if (!currentData) {
      return false;
    }

    if (this.activeTabSignal() === 'items') {
      if (!currentData.itemVideosLoaded || this.itemVideosLoadingSignal()) {
        return false;
      }

      return currentData.itemPagination.currentPage < currentData.itemPagination.totalPages;
    }

    return currentData.parkPagination.currentPage < currentData.parkPagination.totalPages;
  });
  public readonly typeOptions: Signal<PublicVideoSelectOption[]> = computed(() => buildVideoTypeOptions());
  public readonly tagOptions: Signal<PublicVideoSelectOption[]> = computed(() => {
    const currentData: ParkVideosPageData | undefined = this.screenStateStore.data();
    return currentData ? buildVideoTagOptions(currentData.videoTags, this.currentLanguageSignal()) : [];
  });
  public readonly videoCards: Signal<PublicVideoCardViewModel[]> = computed(() => {
    const currentData: ParkVideosPageData | undefined = this.screenStateStore.data();
    if (!currentData) {
      return [];
    }

    return this.activeTabSignal() === 'items'
      ? this.buildItemVideoCards(currentData)
      : this.buildParkVideoCards(currentData);
  });

  constructor(
    @Inject(PARK_VIDEOS_PARKS_PORT) private readonly parksPort: ParkVideosParksPort,
    @Inject(PARK_VIDEOS_VIDEOS_PORT) private readonly videosPort: ParkVideosVideosPort,
    private readonly destroyRef: DestroyRef,
    private readonly ssrHttpStatusService: SsrHttpStatusService
  ) {
  }

  setCurrentLanguage(language: string): void {
    this.currentLanguageSignal.set(language || 'en');
  }

  loadParkVideos(parkId: string, filters: PublicVideoFilterState): void {
    const previousData: ParkVideosPageData | undefined = this.screenStateStore.data();
    const preferredTab: ParkVideosGalleryTab = this.activeTabSignal();
    const normalizedFilters: PublicVideoFilterState = normalizeFilters(filters);
    this.screenStateStore.setLoading(previousData);
    this.loadingMoreSignal.set(false);
    this.itemVideosLoadingSignal.set(false);

    forkJoin({
      summary: this.parksPort.getParkDetailSummary(parkId, anonymousHttpOptions()),
      parkVideoPage: this.videosPort.getVideosPage(this.buildParkVideoQuery(parkId, normalizedFilters, 1), anonymousHttpOptions()),
      itemVideoProbe: this.videosPort.getParkItemVideosByPark(parkId, this.buildItemVideoQuery(normalizedFilters, 1, 1), anonymousHttpOptions()),
      videoTags: this.videosPort.getVideoTags(anonymousHttpOptions())
    }).pipe(
      switchMap((response: ParkVideosInitialResponse) => this.loadDefaultItemTabWhenNeeded(parkId, normalizedFilters, response, preferredTab)),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: (response: ParkVideosInitialData) => {
        this.itemVideosLoadingSignal.set(false);
        this.screenStateStore.setReady({
          summary: response.summary,
          parkVideos: response.parkVideoPage.items,
          itemVideos: response.itemVideoPage?.items ?? [],
          videoTags: response.videoTags,
          parkPagination: response.parkVideoPage.pagination,
          itemPagination: response.itemVideoPage?.pagination ?? response.itemVideoProbe.pagination,
          itemVideosLoaded: response.itemVideoPage !== null,
          filters: normalizedFilters
        });
      },
      error: (error: unknown) => {
        console.error('Error loading park videos page', error);
        this.itemVideosLoadingSignal.set(false);

        if (hasHttpStatus(error, 404)) {
          this.ssrHttpStatusService.setNotFound();
        }

        this.screenStateStore.setError('videos.list.errorMessage', previousData);
      }
    });
  }

  selectTab(tab: ParkVideosGalleryTab): void {
    this.activeTabSignal.set(tab);

    if (tab !== 'items') {
      return;
    }

    const currentData: ParkVideosPageData | undefined = this.screenStateStore.data();
    const parkId: string | null = normalizeOptionalString(currentData?.summary.park.id);
    if (!currentData || !parkId || currentData.itemVideosLoaded || this.itemVideosLoadingSignal()) {
      return;
    }

    this.loadItemVideosPage(currentData, parkId, 1, false);
  }

  loadNextPage(): void {
    const currentData: ParkVideosPageData | undefined = this.screenStateStore.data();
    const parkId: string | null = normalizeOptionalString(currentData?.summary.park.id);
    if (!currentData || !parkId || this.loadingMoreSignal() || !this.canLoadMore()) {
      return;
    }

    if (this.activeTabSignal() === 'items') {
      this.loadItemVideosPage(currentData, parkId, currentData.itemPagination.currentPage + 1, true);
      return;
    }

    const nextPage: number = currentData.parkPagination.currentPage + 1;
    this.loadingMoreSignal.set(true);

    this.videosPort.getVideosPage(this.buildParkVideoQuery(parkId, currentData.filters, nextPage), anonymousHttpOptions())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (videoPage: PagedResult<VideoDto>) => {
          this.loadingMoreSignal.set(false);
          this.screenStateStore.setReady({
            ...currentData,
            parkVideos: [...currentData.parkVideos, ...videoPage.items],
            parkPagination: videoPage.pagination
          });
        },
        error: (error: unknown) => {
          console.error('Error loading additional park videos', error);
          this.loadingMoreSignal.set(false);
          this.screenStateStore.setError('videos.list.errorMessage', currentData);
        }
      });
  }

  private buildParkVideoCards(currentData: ParkVideosPageData): PublicVideoCardViewModel[] {
    return buildPublicVideoCards(
      currentData.parkVideos,
      currentData.videoTags,
      this.currentLanguageSignal(),
      (video: VideoDto) => buildPublicParkVideoRouteCommands({
        language: this.currentLanguageSignal(),
        parkId: currentData.summary.park.id,
        parkName: currentData.summary.park.name,
        videoId: video.id,
        videoTitle: resolveVideoRouteTitle(video)
      })
    );
  }

  private buildItemVideoCards(currentData: ParkVideosPageData): PublicVideoCardViewModel[] {
    return buildPublicVideoCards(
      currentData.itemVideos.map((entry: ParkItemVideoDto) => entry.video),
      currentData.videoTags,
      this.currentLanguageSignal(),
      (video: VideoDto) => {
        const entry: ParkItemVideoDto | undefined = currentData.itemVideos.find((candidate: ParkItemVideoDto) => candidate.video.id === video.id);
        const item: ParkItem | null = entry?.item ?? null;
        if (!item) {
          return null;
        }

        return buildPublicParkItemVideoRouteCommands({
          language: this.currentLanguageSignal(),
          parkId: currentData.summary.park.id,
          parkName: currentData.summary.park.name,
          itemId: item.id,
          itemName: item.name,
          videoId: video.id,
          videoTitle: resolveVideoRouteTitle(video)
        });
      }
    );
  }

  private loadDefaultItemTabWhenNeeded(
    parkId: string,
    filters: PublicVideoFilterState,
    response: ParkVideosInitialResponse,
    preferredTab: ParkVideosGalleryTab
  ): Observable<ParkVideosInitialData> {
    const parkTabTotal: number = response.parkVideoPage.pagination.totalItems;
    const itemTabTotal: number = response.itemVideoProbe.pagination.totalItems;
    const shouldLoadItems: boolean = itemTabTotal > 0 && (preferredTab === 'items' || parkTabTotal <= 0);

    if (!shouldLoadItems) {
      this.activeTabSignal.set('park');
      return of({ ...response, itemVideoPage: null });
    }

    this.activeTabSignal.set('items');
    this.itemVideosLoadingSignal.set(true);
    return this.videosPort.getParkItemVideosByPark(parkId, this.buildItemVideoQuery(filters, 1, ParkVideosStateFacade.PageSize), anonymousHttpOptions()).pipe(
      map((itemVideoPage: PagedResult<ParkItemVideoDto>) => ({ ...response, itemVideoPage }))
    );
  }

  private loadItemVideosPage(currentData: ParkVideosPageData, parkId: string, page: number, append: boolean): void {
    this.itemVideosLoadingSignal.set(!append);
    this.loadingMoreSignal.set(append);

    this.videosPort.getParkItemVideosByPark(parkId, this.buildItemVideoQuery(currentData.filters, page, ParkVideosStateFacade.PageSize), anonymousHttpOptions())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (itemVideoPage: PagedResult<ParkItemVideoDto>) => {
          this.itemVideosLoadingSignal.set(false);
          this.loadingMoreSignal.set(false);
          this.screenStateStore.setReady({
            ...currentData,
            itemVideos: append ? [...currentData.itemVideos, ...itemVideoPage.items] : itemVideoPage.items,
            itemPagination: itemVideoPage.pagination,
            itemVideosLoaded: true
          });
        },
        error: (error: unknown) => {
          console.error('Error loading park item videos for park videos page', error);
          this.itemVideosLoadingSignal.set(false);
          this.loadingMoreSignal.set(false);
          this.screenStateStore.setError('videos.list.errorMessage', currentData);
        }
      });
  }

  private buildParkVideoQuery(parkId: string, filters: PublicVideoFilterState, page: number): VideoSearchQuery {
    return {
      page,
      size: ParkVideosStateFacade.PageSize,
      ownerType: VideoOwnerType.PARK,
      ownerId: parkId,
      type: filters.type,
      tagId: filters.tagId,
      creatorName: filters.creatorName,
      languageCode: this.currentLanguageSignal(),
      sortBy: 'published',
      sortDirection: 'desc'
    };
  }

  private buildItemVideoQuery(filters: PublicVideoFilterState, page: number, size: number): VideoSearchQuery {
    return {
      page,
      size,
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
