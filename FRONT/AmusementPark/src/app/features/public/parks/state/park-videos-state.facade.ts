import { DestroyRef, Inject, Injectable, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { forkJoin } from 'rxjs';

import { ParkDetailSummary } from '@app/models/parks/park-detail-summary';
import { VideoDto } from '@app/models/videos/video-dto';
import { VideoOwnerType } from '@app/models/videos/video-owner-type';
import { VideoTagDto } from '@app/models/videos/video-tag-dto';
import { anonymousHttpOptions } from '@core/http/auth/anonymous-http-options';
import { hasHttpStatus } from '@core/http/http-error-status.helpers';
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import { buildPublicParkVideoRouteCommands } from '@shared/utils/routing/public-detail-route.helpers';
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

interface ParkVideosPageData {
  summary: ParkDetailSummary;
  videos: VideoDto[];
  videoTags: VideoTagDto[];
  pagination: PaginationContract;
  filters: PublicVideoFilterState;
}

@Injectable()
export class ParkVideosStateFacade {
  private static readonly PageSize: number = 24;
  private readonly screenStateStore = new SignalScreenStateStore<ParkVideosPageData>();
  private readonly loadingMoreSignal = signal(false);
  private readonly currentLanguageSignal = signal('en');

  public readonly state = this.screenStateStore.state;
  public readonly data = this.screenStateStore.data;
  public readonly loadingMore: Signal<boolean> = this.loadingMoreSignal.asReadonly();
  public readonly park = computed(() => this.screenStateStore.data()?.summary.park ?? null);
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
    const currentData: ParkVideosPageData | undefined = this.screenStateStore.data();
    return currentData ? buildVideoTagOptions(currentData.videoTags, this.currentLanguageSignal()) : [];
  });
  public readonly videoCards: Signal<PublicVideoCardViewModel[]> = computed(() => {
    const currentData: ParkVideosPageData | undefined = this.screenStateStore.data();
    if (!currentData) {
      return [];
    }

    return buildPublicVideoCards(
      currentData.videos,
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
    const normalizedFilters: PublicVideoFilterState = normalizeFilters(filters);
    this.screenStateStore.setLoading(previousData);

    forkJoin({
      summary: this.parksPort.getParkDetailSummary(parkId, anonymousHttpOptions()),
      videoPage: this.videosPort.getVideosPage(this.buildVideoQuery(parkId, normalizedFilters, 1), anonymousHttpOptions()),
      videoTags: this.videosPort.getVideoTags(anonymousHttpOptions())
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (response: { summary: ParkDetailSummary; videoPage: PagedResult<VideoDto>; videoTags: VideoTagDto[] }) => {
        this.screenStateStore.setReady({
          summary: response.summary,
          videos: response.videoPage.items,
          videoTags: response.videoTags,
          pagination: response.videoPage.pagination,
          filters: normalizedFilters
        });
      },
      error: (error: unknown) => {
        console.error('Error loading park videos page', error);

        if (hasHttpStatus(error, 404)) {
          this.ssrHttpStatusService.setNotFound();
        }

        this.screenStateStore.setError('videos.list.errorMessage', previousData);
      }
    });
  }

  loadNextPage(): void {
    const currentData: ParkVideosPageData | undefined = this.screenStateStore.data();
    const parkId: string | null = normalizeOptionalString(currentData?.summary.park.id);
    if (!currentData || !parkId || this.loadingMoreSignal() || !this.canLoadMore()) {
      return;
    }

    const nextPage: number = currentData.pagination.currentPage + 1;
    this.loadingMoreSignal.set(true);

    this.videosPort.getVideosPage(this.buildVideoQuery(parkId, currentData.filters, nextPage), anonymousHttpOptions())
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
          console.error('Error loading additional park videos', error);
          this.loadingMoreSignal.set(false);
          this.screenStateStore.setError('videos.list.errorMessage', currentData);
        }
      });
  }

  private buildVideoQuery(parkId: string, filters: PublicVideoFilterState, page: number) {
    return {
      page,
      size: ParkVideosStateFacade.PageSize,
      ownerType: VideoOwnerType.PARK,
      ownerId: parkId,
      type: filters.type,
      tagId: filters.tagId,
      creatorName: filters.creatorName,
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
