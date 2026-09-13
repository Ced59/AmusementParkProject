import { Observable, of } from 'rxjs';

import { Park } from '@app/models/parks/park';

import { ParkItemVideoDto } from '@app/models/videos/park-item-video-dto';

import { VideoDto } from '@app/models/videos/video-dto';

import { VideoHostingProvider } from '@app/models/videos/video-hosting-provider';

import { VideoOwnerType } from '@app/models/videos/video-owner-type';

import { VideoSearchQuery } from '@app/models/videos/video-search-query';

import { VideoType } from '@app/models/videos/video-type';

import { PagedResult } from '@shared/models/contracts';

import { AnonymousHttpOptions } from '@core/http/auth/anonymous-http-options';

import { ParkDetailVideosPort } from '../../park-detail-data.ports';

function createVideo(): VideoDto {
  return {
    id: 'video-1',
    hostingProvider: VideoHostingProvider.YOUTUBE,
    ownerType: VideoOwnerType.PARK,
    ownerId: 'park-1',
    type: VideoType.ON_RIDE,
    originalUrl: 'https://www.youtube.com/watch?v=park',
    canonicalUrl: 'https://www.youtube.com/watch?v=park',
    embedUrl: null,
    externalId: 'park',
    title: 'Park video',
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
  return createImagePage(totalItems > 0 ? [createVideo()] : [], totalItems);
}

function createImagePage<TItem>(
  items: TItem[],
  totalItems: number = items.length,
): PagedResult<TItem> {
  return {
    items,
    pagination: {
      totalItems,
      totalPages: totalItems > 0 ? 1 : 0,
      currentPage: 1,
      itemsPerPage: 1,
    },
  };
}

export class FakeVideosPort implements ParkDetailVideosPort {
  public videosResponse$: Observable<PagedResult<VideoDto>> = of(
    createVideosPage(1),
  );
  public itemVideosResponse$: Observable<PagedResult<ParkItemVideoDto>> = of(
    createImagePage<ParkItemVideoDto>([]),
  );
  public readonly calls: VideoSearchQuery[] = [];
  public readonly itemVideoCalls: {
    parkId: string;
    query: VideoSearchQuery;
  }[] = [];
  public readonly options: Array<AnonymousHttpOptions | undefined> = [];

  getVideosPage(
    query: VideoSearchQuery = {},
    options?: AnonymousHttpOptions,
  ): Observable<PagedResult<VideoDto>> {
    this.calls.push(query);
    this.options.push(options);
    return this.videosResponse$;
  }

  getParkItemVideosByPark(
    parkId: string,
    query: VideoSearchQuery = {},
    options?: AnonymousHttpOptions,
  ): Observable<PagedResult<ParkItemVideoDto>> {
    this.itemVideoCalls.push({ parkId, query });
    this.options.push(options);
    return this.itemVideosResponse$;
  }
}
