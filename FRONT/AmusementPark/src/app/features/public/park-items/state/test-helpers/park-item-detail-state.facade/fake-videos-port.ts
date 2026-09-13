import { Observable, of } from 'rxjs';

import { VideoDto } from '@app/models/videos/video-dto';

import { VideoHostingProvider } from '@app/models/videos/video-hosting-provider';

import { VideoOwnerType } from '@app/models/videos/video-owner-type';

import { VideoSearchQuery } from '@app/models/videos/video-search-query';

import { VideoType } from '@app/models/videos/video-type';

import { PagedResult } from '@shared/models/contracts';

import { ParkItemDetailVideosPort } from '../../park-item-detail-data.ports';

function createVideo(): VideoDto {
  return {
    id: 'video-1',
    hostingProvider: VideoHostingProvider.YOUTUBE,
    ownerType: VideoOwnerType.PARK_ITEM,
    ownerId: 'item-1',
    type: VideoType.ON_RIDE,
    originalUrl: 'https://www.youtube.com/watch?v=item',
    canonicalUrl: 'https://www.youtube.com/watch?v=item',
    embedUrl: null,
    externalId: 'item',
    title: 'Item video',
    description: null,
    creatorName: null,
    creatorUrl: null,
    thumbnailUrl: null,
    thumbnailImageId: null,
    durationSeconds: null,
    publishedAtUtc: null,
    languageCodes: ['fr'],
    titles: [],
    descriptions: [],
    tagIds: [],
    externalMetadata: {},
    isPublished: true,
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-01-01T00:00:00Z',
  };
}

function createVideosPage(totalItems: number): PagedResult<VideoDto> {
  return {
    items: totalItems > 0 ? [createVideo()] : [],
    pagination: {
      totalItems,
      totalPages: totalItems > 0 ? 1 : 0,
      currentPage: 1,
      itemsPerPage: 1,
    },
  };
}

export class FakeVideosPort implements ParkItemDetailVideosPort {
  public videosResponse$: Observable<PagedResult<VideoDto>> = of(
    createVideosPage(1),
  );
  public readonly calls: VideoSearchQuery[] = [];

  getVideosPage(
    query: VideoSearchQuery = {},
  ): Observable<PagedResult<VideoDto>> {
    this.calls.push(query);
    return this.videosResponse$;
  }
}
