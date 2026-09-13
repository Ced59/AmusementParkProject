import { Observable, of } from 'rxjs';

import { ResolvedVideoMetadataDto } from '@app/models/videos/resolved-video-metadata-dto';

import { VideoDto } from '@app/models/videos/video-dto';

import { VideoHostingProvider } from '@app/models/videos/video-hosting-provider';

import { VideoOwnerType } from '@app/models/videos/video-owner-type';

import { VideoTagDto } from '@app/models/videos/video-tag-dto';

import { VideoType } from '@app/models/videos/video-type';

import { VideoWriteRequest } from '@app/models/videos/video-write-request';

import { AdminVideoCreateVideosApiServicePort } from '../../admin-video-create-state-data.ports';

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

function createMetadata(): ResolvedVideoMetadataDto {
  return {
    hostingProvider: VideoHostingProvider.YOUTUBE,
    originalUrl: 'https://www.youtube.com/watch?v=abcdefghijk',
    canonicalUrl: 'https://www.youtube.com/watch?v=abcdefghijk',
    title: 'Onride test',
  };
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
    title: 'Onride test',
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

export class FakeVideosPort implements AdminVideoCreateVideosApiServicePort {
  public tagsResponse$: Observable<VideoTagDto[]> = of([createTag('official')]);
  public metadataResponse$: Observable<ResolvedVideoMetadataDto> = of(createMetadata());
  public createResponse$: Observable<VideoDto> = of(createVideo('video-1'));
  public lastCreateRequest: VideoWriteRequest | null = null;

  getVideoTags(): Observable<VideoTagDto[]> {
    return this.tagsResponse$;
  }

  resolveVideoMetadata(videoUrl: string): Observable<ResolvedVideoMetadataDto> {
    return this.metadataResponse$;
  }

  createVideo(request: VideoWriteRequest): Observable<VideoDto> {
    this.lastCreateRequest = request;
    return this.createResponse$;
  }
}
