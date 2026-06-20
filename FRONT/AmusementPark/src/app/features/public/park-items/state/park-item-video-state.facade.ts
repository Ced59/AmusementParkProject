import { DestroyRef, Inject, Injectable, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { forkJoin, of } from 'rxjs';
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
  PARK_ITEM_VIDEOS_ITEMS_PORT,
  PARK_ITEM_VIDEOS_IMAGES_PORT,
  PARK_ITEM_VIDEOS_PARKS_PORT,
  PARK_ITEM_VIDEOS_VIDEOS_PORT,
  ParkItemVideosImagesPort,
  ParkItemVideosItemsPort,
  ParkItemVideosParksPort,
  ParkItemVideosVideosPort
} from './park-item-videos-data.ports';

interface ParkItemVideoPageData {
  item: ParkItem;
  park: Park;
  itemImageId: string | null;
  parkImageId: string | null;
  video: VideoDto;
  videos: VideoDto[];
  videoTags: VideoTagDto[];
}

@Injectable()
export class ParkItemVideoStateFacade {
  private static readonly NavigationPageSize: number = 100;
  private readonly screenStateStore = new SignalScreenStateStore<ParkItemVideoPageData>();
  private readonly currentLanguageSignal = signal('en');

  public readonly state = this.screenStateStore.state;
  public readonly data = this.screenStateStore.data;
  public readonly item = computed(() => this.screenStateStore.data()?.item ?? null);
  public readonly park = computed(() => this.screenStateStore.data()?.park ?? null);
  public readonly itemImageId = computed(() => this.screenStateStore.data()?.itemImageId ?? null);
  public readonly parkImageId = computed(() => this.screenStateStore.data()?.parkImageId ?? null);
  public readonly rawVideo = computed(() => this.screenStateStore.data()?.video ?? null);
  public readonly video: Signal<PublicVideoWatchViewModel | null> = computed(() => {
    const currentData: ParkItemVideoPageData | undefined = this.screenStateStore.data();
    if (!currentData) {
      return null;
    }

    return buildPublicVideoWatchView(
      currentData.video,
      currentData.videoTags,
      this.currentLanguageSignal(),
      this.safeVideoEmbedUrlService,
      this.buildVideoLink(currentData.park, currentData.item, currentData.video)
    );
  });
  public readonly previousVideo: Signal<PublicVideoNavigationItem | null> = computed(() => this.navigation().previous);
  public readonly nextVideo: Signal<PublicVideoNavigationItem | null> = computed(() => this.navigation().next);
  private readonly navigation = computed(() => {
    const currentData: ParkItemVideoPageData | undefined = this.screenStateStore.data();
    if (!currentData) {
      return { previous: null, next: null };
    }

    return buildPublicVideoNavigation(
      currentData.videos,
      currentData.video.id,
      (video: VideoDto) => this.buildVideoLink(currentData.park, currentData.item, video)
    );
  });

  constructor(
    @Inject(PARK_ITEM_VIDEOS_ITEMS_PORT) private readonly itemsPort: ParkItemVideosItemsPort,
    @Inject(PARK_ITEM_VIDEOS_PARKS_PORT) private readonly parksPort: ParkItemVideosParksPort,
    @Inject(PARK_ITEM_VIDEOS_IMAGES_PORT) private readonly imagesPort: ParkItemVideosImagesPort,
    @Inject(PARK_ITEM_VIDEOS_VIDEOS_PORT) private readonly videosPort: ParkItemVideosVideosPort,
    private readonly destroyRef: DestroyRef,
    private readonly ssrHttpStatusService: SsrHttpStatusService,
    private readonly safeVideoEmbedUrlService: SafeVideoEmbedUrlService
  ) {
  }

  setCurrentLanguage(language: string): void {
    this.currentLanguageSignal.set(language || 'en');
  }

  loadItemVideo(itemId: string, videoId: string): void {
    const previousData: ParkItemVideoPageData | undefined = this.screenStateStore.data();
    this.screenStateStore.setLoading(previousData);

    this.itemsPort.getParkItemById(itemId, anonymousHttpOptions()).pipe(
      switchMap((item: ParkItem) => forkJoin({
        item: of(item),
        summary: this.parksPort.getParkDetailSummary(item.parkId, anonymousHttpOptions()),
        itemImages: this.imagesPort.getImages(ImageOwnerType.PARK_ITEM, normalizeOptionalString(item.id) ?? itemId, ImageCategory.PARK_ITEM, 1, 1, anonymousHttpOptions())
          .pipe(catchError(() => of([] as ImageDto[]))),
        video: this.videosPort.getVideoById(videoId, anonymousHttpOptions(), this.currentLanguageSignal()),
        videoPage: this.videosPort.getVideosPage({
          page: 1,
          size: ParkItemVideoStateFacade.NavigationPageSize,
          ownerType: VideoOwnerType.PARK_ITEM,
          ownerId: normalizeOptionalString(item.id) ?? itemId,
          languageCode: this.currentLanguageSignal(),
          sortBy: 'published',
          sortDirection: 'desc'
        }, anonymousHttpOptions()),
        videoTags: this.videosPort.getVideoTags(anonymousHttpOptions())
      })),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: (response: { item: ParkItem; summary: ParkDetailSummary; itemImages: ImageDto[]; video: VideoDto; videoPage: PagedResult<VideoDto>; videoTags: VideoTagDto[] }) => {
        const normalizedItemId: string = normalizeOptionalString(response.item.id) ?? itemId;
        if (!isOwnedBy(response.video, VideoOwnerType.PARK_ITEM, normalizedItemId)) {
          this.ssrHttpStatusService.setNotFound();
          this.screenStateStore.setError('videos.watch.errorMessage', previousData);
          return;
        }

        this.screenStateStore.setReady({
          item: response.item,
          park: response.summary.park,
          itemImageId: resolveItemSocialImageId(response.itemImages),
          parkImageId: response.summary.mainImage?.id ?? null,
          video: response.video,
          videos: response.videoPage.items,
          videoTags: response.videoTags
        });
      },
      error: (error: unknown) => {
        console.error('Error loading park item video page', error);

        if (hasHttpStatus(error, 404)) {
          this.ssrHttpStatusService.setNotFound();
        }

        this.screenStateStore.setError('videos.watch.errorMessage', previousData);
      }
    });
  }

  private buildVideoLink(park: Park, item: ParkItem, video: VideoDto): string[] | null {
    return buildPublicParkItemVideoRouteCommands({
      language: this.currentLanguageSignal(),
      parkId: park.id,
      parkName: park.name,
      itemId: item.id,
      itemName: item.name,
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

function resolveItemSocialImageId(images: ImageDto[]): string | null {
  const image: ImageDto | undefined = images.find((candidate: ImageDto) => {
    return candidate.isPublished !== false && normalizeOptionalString(candidate.id) !== null;
  });

  return normalizeOptionalString(image?.id);
}
