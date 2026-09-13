import { Observable, of } from 'rxjs';

import { Park } from '@app/models/parks/park';

import { ParkItemVideoDto } from '@app/models/videos/park-item-video-dto';

import { VideoDto } from '@app/models/videos/video-dto';

import { VideoHostingProvider } from '@app/models/videos/video-hosting-provider';

import { VideoOwnerType } from '@app/models/videos/video-owner-type';

import { VideoSearchQuery } from '@app/models/videos/video-search-query';

import { VideoTagDto } from '@app/models/videos/video-tag-dto';

import { VideoType } from '@app/models/videos/video-type';

import { PagedResult } from '@shared/models/contracts';

import { ParkVideosVideosPort } from '../../park-videos-data.ports';

function createParkVideo(id: string): VideoDto {
  return {
    id,
    hostingProvider: VideoHostingProvider.YOUTUBE,
    ownerType: VideoOwnerType.PARK,
    ownerId: 'park-1',
    type: VideoType.ON_RIDE,
    originalUrl: 'https://www.youtube.com/watch?v=park',
    canonicalUrl: 'https://www.youtube.com/watch?v=park',
    embedUrl: null,
    externalId: 'park',
    title: `Park video ${id}`,
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
    tagIds: ['tag-1'],
    externalMetadata: {},
    isPublished: true,
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-01-01T00:00:00Z',
  };
}

function createParkItemVideo(id: string): ParkItemVideoDto {
  return {
    item: {
      id: 'item-1',
      parkId: 'park-1',
      name: 'Family Ride',
      category: 'Attraction',
      type: 'FlatRide',
      latitude: null,
      longitude: null,
    },
    video: {
      ...createParkVideo(id),
      ownerType: VideoOwnerType.PARK_ITEM,
      ownerId: 'item-1',
      title: `Item video ${id}`,
    },
  };
}

function createVideoTag(): VideoTagDto {
  return {
    id: 'tag-1',
    slug: 'official',
    labels: [{ languageCode: 'en', value: 'Official' }],
    descriptions: [],
    isActive: true,
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-01-01T00:00:00Z',
  };
}

function createPage<TItem>(
  items: TItem[],
  currentPage: number,
  totalPages: number,
  totalItems: number,
): PagedResult<TItem> {
  return {
    items,
    pagination: {
      currentPage,
      totalPages,
      totalItems,
      itemsPerPage: 24,
    },
  };
}

export class FakeVideosPort implements ParkVideosVideosPort {
  public firstParkPage$: Observable<PagedResult<VideoDto>> = of(
    createPage([createParkVideo('park-video-1')], 1, 2, 2),
  );
  public nextParkPage$: Observable<PagedResult<VideoDto>> = of(
    createPage([createParkVideo('park-video-2')], 2, 2, 2),
  );
  public itemProbePage$: Observable<PagedResult<ParkItemVideoDto>> = of(
    createPage([], 1, 0, 0),
  );
  public firstItemPage$: Observable<PagedResult<ParkItemVideoDto>> = of(
    createPage([createParkItemVideo('item-video-1')], 1, 2, 2),
  );
  public nextItemPage$: Observable<PagedResult<ParkItemVideoDto>> = of(
    createPage([createParkItemVideo('item-video-2')], 2, 2, 2),
  );
  public tags$: Observable<VideoTagDto[]> = of([createVideoTag()]);
  public readonly pageCalls: VideoSearchQuery[] = [];
  public readonly itemVideoCalls: {
    parkId: string;
    query: VideoSearchQuery;
  }[] = [];
  public tagCallCount: number = 0;

  getVideosPage(
    query: VideoSearchQuery = {},
  ): Observable<PagedResult<VideoDto>> {
    this.pageCalls.push(query);
    return query.page === 2 ? this.nextParkPage$ : this.firstParkPage$;
  }

  getParkItemVideosByPark(
    parkId: string,
    query: VideoSearchQuery = {},
  ): Observable<PagedResult<ParkItemVideoDto>> {
    this.itemVideoCalls.push({ parkId, query });
    if (query.size === 1) {
      return this.itemProbePage$;
    }

    return query.page === 2 ? this.nextItemPage$ : this.firstItemPage$;
  }

  getVideoById(): Observable<VideoDto> {
    return of(createParkVideo('video-1'));
  }

  getVideoTags(): Observable<VideoTagDto[]> {
    this.tagCallCount += 1;
    return this.tags$;
  }
}
