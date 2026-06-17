import { DestroyRef, Inject, Injectable, Signal, computed } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable, forkJoin } from 'rxjs';

import { CreateVideoTagRequest, UpdateVideoTagRequest } from '@app/models/videos/video-tag-write-request';
import { PagedResult, DEFAULT_PAGINATION, PaginationContract } from '@shared/models/contracts';
import { ResolvedVideoMetadataDto } from '@app/models/videos/resolved-video-metadata-dto';
import { VideoDto } from '@app/models/videos/video-dto';
import { VideoSearchQuery } from '@app/models/videos/video-search-query';
import { VideoTagDto } from '@app/models/videos/video-tag-dto';
import { VideoWriteRequest } from '@app/models/videos/video-write-request';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';

import {
  ADMIN_VIDEOS_STATE_VIDEOS_API_SERVICE_PORT,
  AdminVideosStateVideosApiServicePort
} from './admin-videos-state-data.ports';

interface AdminVideosViewModel {
  videos: VideoDto[];
  tags: VideoTagDto[];
  selectedVideo: VideoDto | null;
  pagination: PaginationContract;
  query: VideoSearchQuery;
}

const DEFAULT_VIDEO_QUERY: VideoSearchQuery = {
  page: 1,
  size: 24,
  search: null,
  hostingProvider: null,
  ownerType: null,
  ownerId: null,
  type: null,
  tagId: null,
  creatorName: null,
  isPublished: null,
  sortBy: 'created',
  sortDirection: 'desc',
};

@Injectable()
export class AdminVideosStateFacade {
  private readonly screenStateStore = new SignalScreenStateStore<AdminVideosViewModel>();

  public readonly state = this.screenStateStore.state;
  public readonly videos: Signal<VideoDto[]> = computed(() => this.screenStateStore.data()?.videos ?? []);
  public readonly tags: Signal<VideoTagDto[]> = computed(() => this.screenStateStore.data()?.tags ?? []);
  public readonly selectedVideo: Signal<VideoDto | null> = computed(() => this.screenStateStore.data()?.selectedVideo ?? null);
  public readonly pagination: Signal<PaginationContract> = computed(() => this.screenStateStore.data()?.pagination ?? DEFAULT_PAGINATION);
  public readonly query: Signal<VideoSearchQuery> = computed(() => this.screenStateStore.data()?.query ?? DEFAULT_VIDEO_QUERY);

  constructor(
    @Inject(ADMIN_VIDEOS_STATE_VIDEOS_API_SERVICE_PORT) private readonly videosApiService: AdminVideosStateVideosApiServicePort,
    private readonly destroyRef: DestroyRef
  ) {
  }

  reload(): void {
    const previousData: AdminVideosViewModel | undefined = this.screenStateStore.data();
    const query: VideoSearchQuery = previousData?.query ?? DEFAULT_VIDEO_QUERY;
    this.screenStateStore.setLoading(previousData);

    forkJoin({
      page: this.videosApiService.getVideosPage(query),
      tags: this.videosApiService.getVideoTags(),
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: ({ page, tags }: { page: PagedResult<VideoDto>; tags: VideoTagDto[] }) => {
        this.screenStateStore.setReady({
          videos: page.items,
          tags,
          selectedVideo: this.resolveSelectedVideo(page.items, previousData?.selectedVideo?.id ?? null),
          pagination: page.pagination,
          query,
        });
      },
      error: (error: unknown) => {
        console.error('Error loading admin video data', error);
        this.screenStateStore.setError('common.errorMessage', previousData);
      },
    });
  }

  updateQuery(patch: Partial<VideoSearchQuery>, resetPage: boolean = true): void {
    const currentData: AdminVideosViewModel | undefined = this.screenStateStore.data();
    const currentQuery: VideoSearchQuery = currentData?.query ?? DEFAULT_VIDEO_QUERY;
    const query: VideoSearchQuery = {
      ...currentQuery,
      ...patch,
      page: resetPage ? 1 : (patch.page ?? currentQuery.page),
    };

    this.screenStateStore.setReady({
      videos: currentData?.videos ?? [],
      tags: currentData?.tags ?? [],
      selectedVideo: currentData?.selectedVideo ?? null,
      pagination: currentData?.pagination ?? DEFAULT_PAGINATION,
      query,
    });
  }

  applyQuery(): void {
    this.reload();
  }

  clearFilters(): void {
    const currentData: AdminVideosViewModel | undefined = this.screenStateStore.data();
    this.screenStateStore.setReady({
      videos: currentData?.videos ?? [],
      tags: currentData?.tags ?? [],
      selectedVideo: currentData?.selectedVideo ?? null,
      pagination: currentData?.pagination ?? DEFAULT_PAGINATION,
      query: DEFAULT_VIDEO_QUERY,
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

  selectVideo(video: VideoDto): void {
    const currentData: AdminVideosViewModel | undefined = this.screenStateStore.data();

    if (!currentData) {
      return;
    }

    this.screenStateStore.setReady({
      ...currentData,
      selectedVideo: this.cloneVideo(video),
    });
  }

  updateSelectedVideo(patch: Partial<VideoDto>): void {
    const currentData: AdminVideosViewModel | undefined = this.screenStateStore.data();

    if (!currentData?.selectedVideo) {
      return;
    }

    this.screenStateStore.setReady({
      ...currentData,
      selectedVideo: {
        ...currentData.selectedVideo,
        ...patch,
      },
    });
  }

  toggleTag(tagId: string, checked: boolean): void {
    const currentData: AdminVideosViewModel | undefined = this.screenStateStore.data();

    if (!currentData?.selectedVideo) {
      return;
    }

    const currentTags: Set<string> = new Set(currentData.selectedVideo.tagIds ?? []);

    if (checked) {
      currentTags.add(tagId);
    } else {
      currentTags.delete(tagId);
    }

    this.screenStateStore.setReady({
      ...currentData,
      selectedVideo: {
        ...currentData.selectedVideo,
        tagIds: Array.from(currentTags),
      },
    });
  }

  resolveVideoMetadata(videoUrl: string): Observable<ResolvedVideoMetadataDto> {
    return this.videosApiService.resolveVideoMetadata(videoUrl);
  }

  createVideo(request: VideoWriteRequest): Observable<VideoDto> {
    return this.videosApiService.createVideo(request);
  }

  updateVideo(id: string, request: VideoWriteRequest): Observable<VideoDto> {
    return this.videosApiService.updateVideo(id, request);
  }

  deleteVideo(id: string): Observable<boolean> {
    return this.videosApiService.deleteVideo(id);
  }

  createTag(request: CreateVideoTagRequest): Observable<VideoTagDto> {
    return this.videosApiService.createVideoTag(request);
  }

  updateTag(id: string, request: UpdateVideoTagRequest): Observable<VideoTagDto> {
    return this.videosApiService.updateVideoTag(id, request);
  }

  setError(): void {
    this.screenStateStore.setError('common.errorMessage', this.screenStateStore.data());
  }

  private resolveSelectedVideo(videos: VideoDto[], previousSelectionId: string | null): VideoDto | null {
    if (previousSelectionId) {
      const refreshedSelection: VideoDto | undefined = videos.find((video: VideoDto) => video.id === previousSelectionId);

      if (refreshedSelection) {
        return this.cloneVideo(refreshedSelection);
      }
    }

    if (videos[0]) {
      return this.cloneVideo(videos[0]);
    }

    return null;
  }

  private cloneVideo(video: VideoDto): VideoDto {
    const clonedVideo: VideoDto = JSON.parse(JSON.stringify(video)) as VideoDto;
    clonedVideo.tagIds = clonedVideo.tagIds ?? [];
    clonedVideo.titles = clonedVideo.titles ?? [];
    clonedVideo.descriptions = clonedVideo.descriptions ?? [];
    clonedVideo.externalMetadata = clonedVideo.externalMetadata ?? {};

    return clonedVideo;
  }
}
