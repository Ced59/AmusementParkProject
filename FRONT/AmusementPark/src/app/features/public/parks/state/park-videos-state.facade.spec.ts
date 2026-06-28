import { TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';

import { Park } from '@app/models/parks/park';
import { ParkDetailSummary } from '@app/models/parks/park-detail-summary';
import { ParkItemVideoDto } from '@app/models/videos/park-item-video-dto';
import { VideoDto } from '@app/models/videos/video-dto';
import { VideoHostingProvider } from '@app/models/videos/video-hosting-provider';
import { VideoOwnerType } from '@app/models/videos/video-owner-type';
import { VideoSearchQuery } from '@app/models/videos/video-search-query';
import { VideoTagDto } from '@app/models/videos/video-tag-dto';
import { VideoType } from '@app/models/videos/video-type';
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import { PagedResult } from '@shared/models/contracts';
import {
  PARK_VIDEOS_PARKS_PORT,
  PARK_VIDEOS_VIDEOS_PORT,
  ParkVideosParksPort,
  ParkVideosVideosPort
} from './park-videos-data.ports';
import { ParkVideosStateFacade } from './park-videos-state.facade';

class FakeParksPort implements ParkVideosParksPort {
  public response$: Observable<ParkDetailSummary> = of(createSummary());
  public readonly calls: string[] = [];

  getParkDetailSummary(id: string): Observable<ParkDetailSummary> {
    this.calls.push(id);
    return this.response$;
  }
}

class FakeVideosPort implements ParkVideosVideosPort {
  public firstParkPage$: Observable<PagedResult<VideoDto>> = of(createPage([createParkVideo('park-video-1')], 1, 2, 2));
  public nextParkPage$: Observable<PagedResult<VideoDto>> = of(createPage([createParkVideo('park-video-2')], 2, 2, 2));
  public itemProbePage$: Observable<PagedResult<ParkItemVideoDto>> = of(createPage([], 1, 0, 0));
  public firstItemPage$: Observable<PagedResult<ParkItemVideoDto>> = of(createPage([createParkItemVideo('item-video-1')], 1, 2, 2));
  public nextItemPage$: Observable<PagedResult<ParkItemVideoDto>> = of(createPage([createParkItemVideo('item-video-2')], 2, 2, 2));
  public tags$: Observable<VideoTagDto[]> = of([createVideoTag()]);
  public readonly pageCalls: VideoSearchQuery[] = [];
  public readonly itemVideoCalls: { parkId: string; query: VideoSearchQuery }[] = [];
  public tagCallCount: number = 0;

  getVideosPage(query: VideoSearchQuery = {}): Observable<PagedResult<VideoDto>> {
    this.pageCalls.push(query);
    return query.page === 2 ? this.nextParkPage$ : this.firstParkPage$;
  }

  getParkItemVideosByPark(parkId: string, query: VideoSearchQuery = {}): Observable<PagedResult<ParkItemVideoDto>> {
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

class FakeSsrHttpStatusService {
  public notFoundCallCount: number = 0;

  setNotFound(): void {
    this.notFoundCallCount += 1;
  }
}

function createPark(): Park {
  return {
    id: 'park-1',
    name: 'Phantasialand',
    countryCode: 'DE',
    latitude: 50.8,
    longitude: 6.8,
    isVisible: true,
    descriptions: []
  };
}

function createSummary(): ParkDetailSummary {
  return {
    park: createPark(),
    mainImage: null,
    references: {},
    stats: {
      totalItems: 0,
      zoneCount: 0,
      attractionCount: 0,
      restaurantCount: 0,
      showCount: 0,
      shopCount: 0,
      hotelCount: 0,
      countsByCategory: {}
    }
  };
}

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
    updatedAt: '2026-01-01T00:00:00Z'
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
      longitude: null
    },
    video: {
      ...createParkVideo(id),
      ownerType: VideoOwnerType.PARK_ITEM,
      ownerId: 'item-1',
      title: `Item video ${id}`
    }
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
    updatedAt: '2026-01-01T00:00:00Z'
  };
}

function createPage<TItem>(items: TItem[], currentPage: number, totalPages: number, totalItems: number): PagedResult<TItem> {
  return {
    items,
    pagination: {
      currentPage,
      totalPages,
      totalItems,
      itemsPerPage: 24
    }
  };
}

function configureFacade(): {
  facade: ParkVideosStateFacade;
  parksPort: FakeParksPort;
  videosPort: FakeVideosPort;
  ssrStatusService: FakeSsrHttpStatusService;
} {
  const parksPort: FakeParksPort = new FakeParksPort();
  const videosPort: FakeVideosPort = new FakeVideosPort();
  const ssrStatusService: FakeSsrHttpStatusService = new FakeSsrHttpStatusService();

  TestBed.configureTestingModule({
    providers: [
      ParkVideosStateFacade,
      { provide: PARK_VIDEOS_PARKS_PORT, useValue: parksPort },
      { provide: PARK_VIDEOS_VIDEOS_PORT, useValue: videosPort },
      { provide: SsrHttpStatusService, useValue: ssrStatusService }
    ]
  });

  return {
    facade: TestBed.inject(ParkVideosStateFacade),
    parksPort,
    videosPort,
    ssrStatusService
  };
}

describe('ParkVideosStateFacade', () => {
  beforeEach(() => {
    TestBed.resetTestingModule();
  });

  it('loads park videos and probes park item videos without loading that tab', () => {
    const context = configureFacade();
    context.videosPort.itemProbePage$ = of(createPage([createParkItemVideo('probe')], 1, 2, 2));

    context.facade.setCurrentLanguage('fr');
    context.facade.loadParkVideos('park-1', { type: VideoType.ON_RIDE, tagId: 'tag-1', creatorName: 'creator' });

    expect(context.facade.state().kind).toBe('ready');
    expect(context.facade.activeTab()).toBe('park');
    expect(context.facade.totalVideos()).toBe(4);
    expect(context.facade.parkTabVideoCount()).toBe(2);
    expect(context.facade.itemTabVideoCount()).toBe(2);
    expect(context.facade.showItemTab()).toBeTrue();
    expect(context.facade.videoCards()[0]?.detailLink).toEqual(['/', 'fr', 'park', 'park-1', 'phantasialand', 'videos', 'park-video-1', 'park-video-park-video-1']);
    expect(context.videosPort.pageCalls).toEqual([{
      page: 1,
      size: 24,
      ownerType: VideoOwnerType.PARK,
      ownerId: 'park-1',
      type: VideoType.ON_RIDE,
      tagId: 'tag-1',
      creatorName: 'creator',
      languageCode: 'fr',
      sortBy: 'published',
      sortDirection: 'desc'
    }]);
    expect(context.videosPort.itemVideoCalls).toEqual([{
      parkId: 'park-1',
      query: {
        page: 1,
        size: 1,
        type: VideoType.ON_RIDE,
        tagId: 'tag-1',
        creatorName: 'creator',
        languageCode: 'fr',
        sortBy: 'published',
        sortDirection: 'desc'
      }
    }]);
  });

  it('loads park item videos only when their tab is selected', () => {
    const context = configureFacade();
    context.videosPort.itemProbePage$ = of(createPage([createParkItemVideo('probe')], 1, 2, 2));

    context.facade.setCurrentLanguage('fr');
    context.facade.loadParkVideos('park-1', { type: null, tagId: null, creatorName: '' });
    context.facade.selectTab('items');

    expect(context.facade.activeTab()).toBe('items');
    expect(context.facade.videoCards().map((video) => video.id)).toEqual(['item-video-1']);
    expect(context.facade.videoCards()[0]?.detailLink).toEqual([
      '/',
      'fr',
      'park',
      'park-1',
      'phantasialand',
      'item',
      'item-1',
      'family-ride',
      'videos',
      'item-video-1',
      'item-video-item-video-1'
    ]);
    expect(context.facade.canLoadMore()).toBeTrue();

    context.facade.loadNextPage();

    expect(context.facade.videoCards().map((video) => video.id)).toEqual(['item-video-1', 'item-video-2']);
    expect(context.facade.canLoadMore()).toBeFalse();
    expect(context.videosPort.itemVideoCalls).toEqual([
      { parkId: 'park-1', query: { page: 1, size: 1, type: null, tagId: null, creatorName: '', languageCode: 'fr', sortBy: 'published', sortDirection: 'desc' } },
      { parkId: 'park-1', query: { page: 1, size: 24, type: null, tagId: null, creatorName: '', languageCode: 'fr', sortBy: 'published', sortDirection: 'desc' } },
      { parkId: 'park-1', query: { page: 2, size: 24, type: null, tagId: null, creatorName: '', languageCode: 'fr', sortBy: 'published', sortDirection: 'desc' } }
    ]);
  });

  it('selects the park item tab when the park has no direct videos', () => {
    const context = configureFacade();
    context.videosPort.firstParkPage$ = of(createPage([], 1, 0, 0));
    context.videosPort.itemProbePage$ = of(createPage([createParkItemVideo('probe')], 1, 2, 2));

    context.facade.setCurrentLanguage('fr');
    context.facade.loadParkVideos('park-1', { type: null, tagId: null, creatorName: '' });

    expect(context.facade.activeTab()).toBe('items');
    expect(context.facade.videoCards().map((video) => video.id)).toEqual(['item-video-1']);
    expect(context.videosPort.itemVideoCalls).toEqual([
      { parkId: 'park-1', query: { page: 1, size: 1, type: null, tagId: null, creatorName: '', languageCode: 'fr', sortBy: 'published', sortDirection: 'desc' } },
      { parkId: 'park-1', query: { page: 1, size: 24, type: null, tagId: null, creatorName: '', languageCode: 'fr', sortBy: 'published', sortDirection: 'desc' } }
    ]);
  });

  it('sets SSR not found when the summary lookup returns 404', () => {
    const context = configureFacade();
    context.parksPort.response$ = throwError(() => ({ status: 404 }));

    context.facade.loadParkVideos('missing-park', { type: null, tagId: null, creatorName: '' });

    expect(context.facade.state().kind).toBe('error');
    expect(context.ssrStatusService.notFoundCallCount).toBe(1);
  });
});
