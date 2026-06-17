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
import { PagedResult } from '@shared/models/contracts';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';
import {
  buildPublicVideoNavigation,
  buildPublicVideoWatchView
} from '@features/public/videos/mappers/public-video.mapper';
import {
  PublicVideoNavigationItem,
  PublicVideoWatchViewModel
} from '@features/public/videos/models/public-video-view.model';
import { SafeVideoEmbedUrlService } from '@features/public/videos/services/safe-video-embed-url.service';
import {
  PARK_VIDEOS_PARKS_PORT,
  PARK_VIDEOS_VIDEOS_PORT,
  ParkVideosParksPort,
  ParkVideosVideosPort
} from './park-videos-data.ports';

interface ParkVideoPageData {
  summary: ParkDetailSummary;
  video: VideoDto;
  videos: VideoDto[];
  videoTags: VideoTagDto[];
}

@Injectable()
export class ParkVideoStateFacade {
  private static readonly NavigationPageSize: number = 100;
  private readonly screenStateStore = new SignalScreenStateStore<ParkVideoPageData>();
  private readonly currentLanguageSignal = signal('en');

  public readonly state = this.screenStateStore.state;
  public readonly data = this.screenStateStore.data;
  public readonly park = computed(() => this.screenStateStore.data()?.summary.park ?? null);
  public readonly rawVideo = computed(() => this.screenStateStore.data()?.video ?? null);
  public readonly video: Signal<PublicVideoWatchViewModel | null> = computed(() => {
    const currentData: ParkVideoPageData | undefined = this.screenStateStore.data();
    if (!currentData) {
      return null;
    }

    return buildPublicVideoWatchView(
      currentData.video,
      currentData.videoTags,
      this.currentLanguageSignal(),
      this.safeVideoEmbedUrlService,
      this.buildVideoLink(currentData.summary, currentData.video)
    );
  });
  public readonly previousVideo: Signal<PublicVideoNavigationItem | null> = computed(() => this.navigation().previous);
  public readonly nextVideo: Signal<PublicVideoNavigationItem | null> = computed(() => this.navigation().next);
  private readonly navigation = computed(() => {
    const currentData: ParkVideoPageData | undefined = this.screenStateStore.data();
    if (!currentData) {
      return { previous: null, next: null };
    }

    return buildPublicVideoNavigation(
      currentData.videos,
      currentData.video.id,
      (video: VideoDto) => this.buildVideoLink(currentData.summary, video)
    );
  });

  constructor(
    @Inject(PARK_VIDEOS_PARKS_PORT) private readonly parksPort: ParkVideosParksPort,
    @Inject(PARK_VIDEOS_VIDEOS_PORT) private readonly videosPort: ParkVideosVideosPort,
    private readonly destroyRef: DestroyRef,
    private readonly ssrHttpStatusService: SsrHttpStatusService,
    private readonly safeVideoEmbedUrlService: SafeVideoEmbedUrlService
  ) {
  }

  setCurrentLanguage(language: string): void {
    this.currentLanguageSignal.set(language || 'en');
  }

  loadParkVideo(parkId: string, videoId: string): void {
    const previousData: ParkVideoPageData | undefined = this.screenStateStore.data();
    this.screenStateStore.setLoading(previousData);

    forkJoin({
      summary: this.parksPort.getParkDetailSummary(parkId, anonymousHttpOptions()),
      video: this.videosPort.getVideoById(videoId, anonymousHttpOptions(), this.currentLanguageSignal()),
      videoPage: this.videosPort.getVideosPage({
        page: 1,
        size: ParkVideoStateFacade.NavigationPageSize,
        ownerType: VideoOwnerType.PARK,
        ownerId: parkId,
        languageCode: this.currentLanguageSignal(),
        sortBy: 'published',
        sortDirection: 'desc'
      }, anonymousHttpOptions()),
      videoTags: this.videosPort.getVideoTags(anonymousHttpOptions())
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (response: { summary: ParkDetailSummary; video: VideoDto; videoPage: PagedResult<VideoDto>; videoTags: VideoTagDto[] }) => {
        if (!isOwnedBy(response.video, VideoOwnerType.PARK, parkId)) {
          this.ssrHttpStatusService.setNotFound();
          this.screenStateStore.setError('videos.watch.errorMessage', previousData);
          return;
        }

        this.screenStateStore.setReady({
          summary: response.summary,
          video: response.video,
          videos: response.videoPage.items,
          videoTags: response.videoTags
        });
      },
      error: (error: unknown) => {
        console.error('Error loading park video page', error);

        if (hasHttpStatus(error, 404)) {
          this.ssrHttpStatusService.setNotFound();
        }

        this.screenStateStore.setError('videos.watch.errorMessage', previousData);
      }
    });
  }

  private buildVideoLink(summary: ParkDetailSummary, video: VideoDto): string[] | null {
    return buildPublicParkVideoRouteCommands({
      language: this.currentLanguageSignal(),
      parkId: summary.park.id,
      parkName: summary.park.name,
      videoId: video.id,
      videoTitle: resolveVideoRouteTitle(video)
    });
  }
}

function resolveVideoRouteTitle(video: VideoDto): string {
  return normalizeOptionalString(video.title)
    ?? normalizeOptionalString(video.titles?.[0]?.value)
    ?? video.id;
}

function isOwnedBy(video: VideoDto, ownerType: VideoOwnerType, ownerId: string): boolean {
  return video.ownerType === ownerType && normalizeOptionalString(video.ownerId) === ownerId;
}

function normalizeOptionalString(value: string | null | undefined): string | null {
  const normalized: string = value?.trim() ?? '';
  return normalized.length > 0 ? normalized : null;
}
