import { TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';

import { CreateVideoTagRequest, UpdateVideoTagRequest } from '@app/models/videos/video-tag-write-request';
import { PagedResult } from '@shared/models/contracts';
import { ResolvedVideoMetadataDto } from '@app/models/videos/resolved-video-metadata-dto';
import { VideoDto } from '@app/models/videos/video-dto';
import { VideoHostingProvider } from '@app/models/videos/video-hosting-provider';
import { VideoOwnerType } from '@app/models/videos/video-owner-type';
import { VideoSearchQuery } from '@app/models/videos/video-search-query';
import { VideoTagDto } from '@app/models/videos/video-tag-dto';
import { VideoType } from '@app/models/videos/video-type';
import { VideoWriteRequest } from '@app/models/videos/video-write-request';
import { createPagedResult } from '@shared/utils/mapping';

import {
  ADMIN_VIDEOS_STATE_VIDEOS_API_SERVICE_PORT,
  AdminVideosStateVideosApiServicePort
} from './admin-videos-state-data.ports';
import { AdminVideosStateFacade } from './admin-videos-state.facade';

class FakeVideosPort implements AdminVideosStateVideosApiServicePort {
  public pageResponse$: Observable<PagedResult<VideoDto>> = of(createPagedResult([createVideo('video-1')]));
  public tagsResponse$: Observable<VideoTagDto[]> = of([createTag('tag-1')]);

  getVideosPage(query?: VideoSearchQuery): Observable<PagedResult<VideoDto>> {
    return this.pageResponse$;
  }

  getVideoTags(): Observable<VideoTagDto[]> {
    return this.tagsResponse$;
  }

  resolveVideoMetadata(videoUrl: string): Observable<ResolvedVideoMetadataDto> {
    return of({
      hostingProvider: VideoHostingProvider.YOUTUBE,
      originalUrl: videoUrl,
      canonicalUrl: videoUrl,
    });
  }

  createVideo(request: VideoWriteRequest): Observable<VideoDto> {
    return of(createVideo('created-video'));
  }

  updateVideo(id: string, request: VideoWriteRequest): Observable<VideoDto> {
    return of(createVideo(id));
  }

  deleteVideo(id: string): Observable<boolean> {
    return of(true);
  }

  createVideoTag(request: CreateVideoTagRequest): Observable<VideoTagDto> {
    return of(createTag('created-tag'));
  }

  updateVideoTag(id: string, request: UpdateVideoTagRequest): Observable<VideoTagDto> {
    return of(createTag(id));
  }
}

function createVideo(id: string): VideoDto {
  return {
    id,
    hostingProvider: VideoHostingProvider.YOUTUBE,
    ownerType: VideoOwnerType.PARK,
    ownerId: 'park-1',
    type: VideoType.ON_RIDE,
    originalUrl: 'https://www.youtube.com/watch?v=abcdefghijk',
    canonicalUrl: 'https://www.youtube.com/watch?v=abcdefghijk',
    embedUrl: 'https://www.youtube.com/embed/abcdefghijk',
    externalId: 'abcdefghijk',
    title: id,
    descriptions: [],
    titles: [],
    tagIds: [],
    externalMetadata: {},
    isPublished: true,
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-01-01T00:00:00Z',
  };
}

function createTag(id: string): VideoTagDto {
  return {
    id,
    slug: id,
    labels: [],
    descriptions: [],
    isActive: true,
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-01-01T00:00:00Z',
  };
}

describe('AdminVideosStateFacade', () => {
  let facade: AdminVideosStateFacade;
  let port: FakeVideosPort;

  beforeEach(() => {
    port = new FakeVideosPort();

    TestBed.configureTestingModule({
      providers: [
        AdminVideosStateFacade,
        { provide: ADMIN_VIDEOS_STATE_VIDEOS_API_SERVICE_PORT, useValue: port },
      ],
    });

    facade = TestBed.inject(AdminVideosStateFacade);
  });

  it('keeps the admin video screen usable when the videos page fails to load', () => {
    port.pageResponse$ = throwError(() => new Error('network'));

    facade.reload();

    expect(facade.state().kind).toBe('ready');
    expect(facade.videos()).toEqual([]);
    expect(facade.tags().map((tag: VideoTagDto) => tag.id)).toEqual(['tag-1']);
    expect(facade.operationErrorKey()).toBeNull();
  });

  it('keeps the admin video screen usable when tags fail to load', () => {
    port.tagsResponse$ = throwError(() => new Error('network'));

    facade.reload();

    expect(facade.state().kind).toBe('ready');
    expect(facade.videos().map((video: VideoDto) => video.id)).toEqual(['video-1']);
    expect(facade.tags()).toEqual([]);
    expect(facade.operationErrorKey()).toBeNull();
  });

  it('keeps current data visible when a write action reports an error', () => {
    facade.reload();

    facade.setError();

    expect(facade.state().kind).toBe('ready');
    expect(facade.videos().map((video: VideoDto) => video.id)).toEqual(['video-1']);
    expect(facade.operationErrorKey()).toBe('common.errorMessage');
  });
});
