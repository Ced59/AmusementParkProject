import { Observable, of } from 'rxjs';

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

import { AdminVideosStateVideosApiServicePort } from '../../admin-videos-state-data.ports';

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
    languageCodes: [],
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

export class FakeVideosPort implements AdminVideosStateVideosApiServicePort {
  public pageResponse$: Observable<PagedResult<VideoDto>> = of(
    createPagedResult([createVideo('video-1')]),
  );
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

  updateVideo(id: string, request: VideoWriteRequest): Observable<VideoDto> {
    return of(createVideo(id));
  }

  deleteVideo(id: string): Observable<boolean> {
    return of(true);
  }

  createVideoTag(request: CreateVideoTagRequest): Observable<VideoTagDto> {
    return of(createTag('created-tag'));
  }

  updateVideoTag(
    id: string,
    request: UpdateVideoTagRequest,
  ): Observable<VideoTagDto> {
    return of(createTag(id));
  }
}
