import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { firstValueFrom, Observable, of } from 'rxjs';

import { ImageCategory } from '@app/models/images/image-category';
import { ImageOwnerType } from '@app/models/images/image-owner-type';
import { ParkDetailSummary } from '@app/models/parks/park-detail-summary';
import { ParkItem } from '@app/models/parks/park-item';
import { ParkZone } from '@app/models/parks/park-zone';
import {
  PUBLIC_PARK_NAVIGATION_TREE_PARKS_API_SERVICE_PORT,
  PublicParkNavigationTreeParksApiServicePort,
  PUBLIC_PARK_NAVIGATION_TREE_PARK_ITEMS_API_SERVICE_PORT,
  PublicParkNavigationTreeParkItemsApiServicePort,
  PUBLIC_PARK_NAVIGATION_TREE_PARK_ZONES_API_SERVICE_PORT,
  PublicParkNavigationTreeParkZonesApiServicePort,
  PUBLIC_PARK_NAVIGATION_TREE_VIDEOS_API_SERVICE_PORT,
  PublicParkNavigationTreeVideosApiServicePort
} from './public-park-navigation-tree-data.ports';
import { PublicParkNavigationTreeFacade } from './public-park-navigation-tree.facade';
import { PublicParkNavigationTreeItem } from '../models/public-park-navigation-tree.model';
import { VideoDto } from '@app/models/videos/video-dto';

interface PublicParkRouteContextForTest {
  readonly language: string;
  readonly parkId: string;
  readonly parkSlug: string;
  readonly itemId: string | null;
  readonly itemSlug: string | null;
  readonly zoneId: string | null;
  readonly zoneSlug: string | null;
  readonly selectedZoneId: string | null;
  readonly videoId: string | null;
  readonly videoSlug: string | null;
  readonly pageKind:
    | 'park-detail'
    | 'park-items'
    | 'park-item-detail'
    | 'park-item-images'
    | 'park-images'
    | 'park-map'
    | 'park-video'
    | 'park-item-video';
}

interface PublicParkNavigationSourceDataForTest {
  readonly context: PublicParkRouteContextForTest;
  readonly park: { name?: string } | null;
  readonly item: { name?: string } | null;
  readonly zone: null;
  readonly video: Pick<VideoDto, 'title' | 'titles'> | null;
}

class FakeParksPort implements PublicParkNavigationTreeParksApiServicePort {
  public readonly summaryCalls: string[] = [];

  getParkDetailSummary(id: string): Observable<ParkDetailSummary> {
    this.summaryCalls.push(id);
    return of(createSummary());
  }
}

class FakeParkItemsPort implements PublicParkNavigationTreeParkItemsApiServicePort {
  getParkItemById(): Observable<ParkItem> {
    throw new Error('Park item should not be loaded for park detail navigation.');
  }
}

class FakeParkZonesPort implements PublicParkNavigationTreeParkZonesApiServicePort {
  getParkZoneById(): Observable<ParkZone> {
    throw new Error('Park zone should not be loaded for park detail navigation.');
  }
}

class FakeVideosPort implements PublicParkNavigationTreeVideosApiServicePort {
  getVideoById(): Observable<VideoDto> {
    throw new Error('Video should not be loaded for park detail navigation.');
  }
}

function createSummary(): ParkDetailSummary {
  return {
    park: {
      id: 'park-1',
      name: 'Bellewaerde',
      countryCode: 'BE',
      latitude: 50.845,
      longitude: 2.945,
      isVisible: true,
      descriptions: []
    },
    mainImage: {
      id: 'image-1',
      category: ImageCategory.PARK,
      ownerType: ImageOwnerType.PARK,
      ownerId: 'park-1',
      path: 'park/image-1',
      description: 'Bellewaerde',
      isCurrent: true,
      isWatermarked: false,
      isPublished: true,
      width: 1200,
      height: 800,
      sizeInBytes: 1000,
      originalFileName: 'bellewaerde.jpg',
      contentType: 'image/jpeg',
      geoLocation: null,
      altTexts: [],
      captions: [],
      credits: [],
      tagIds: [],
      createdAt: '2026-01-01T00:00:00Z',
      updatedAt: '2026-01-01T00:00:00Z'
    },
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

describe('PublicParkNavigationTreeFacade', () => {
  beforeEach(() => {
    TestBed.resetTestingModule();
  });

  it('loads the optimized park detail summary for park detail navigation', async () => {
    const parksPort: FakeParksPort = new FakeParksPort();

    TestBed.configureTestingModule({
      providers: [
        PublicParkNavigationTreeFacade,
        provideRouter([]),
        { provide: PUBLIC_PARK_NAVIGATION_TREE_PARKS_API_SERVICE_PORT, useValue: parksPort },
        { provide: PUBLIC_PARK_NAVIGATION_TREE_PARK_ITEMS_API_SERVICE_PORT, useClass: FakeParkItemsPort },
        { provide: PUBLIC_PARK_NAVIGATION_TREE_PARK_ZONES_API_SERVICE_PORT, useClass: FakeParkZonesPort },
        { provide: PUBLIC_PARK_NAVIGATION_TREE_VIDEOS_API_SERVICE_PORT, useClass: FakeVideosPort }
      ]
    });

    const facade: PublicParkNavigationTreeFacade = TestBed.inject(PublicParkNavigationTreeFacade);
    TestBed.inject(Router);
    const loadSourceData = (facade as unknown as {
      loadSourceData(context: PublicParkRouteContextForTest): Observable<PublicParkNavigationSourceDataForTest>;
    }).loadSourceData.bind(facade);

    const sourceData: PublicParkNavigationSourceDataForTest = await firstValueFrom(loadSourceData({
      language: 'fr',
      parkId: 'park-1',
      parkSlug: 'bellewaerde',
      itemId: null,
      itemSlug: null,
      zoneId: null,
      zoneSlug: null,
      selectedZoneId: null,
      videoId: null,
      videoSlug: null,
      pageKind: 'park-detail'
    }));

    expect(parksPort.summaryCalls).toEqual(['park-1']);
    expect(sourceData.park?.name).toBe('Bellewaerde');
  });

  it('uses natural localized park places labels in public navigation', () => {
    TestBed.configureTestingModule({
      providers: [
        PublicParkNavigationTreeFacade,
        provideRouter([]),
        { provide: PUBLIC_PARK_NAVIGATION_TREE_PARKS_API_SERVICE_PORT, useClass: FakeParksPort },
        { provide: PUBLIC_PARK_NAVIGATION_TREE_PARK_ITEMS_API_SERVICE_PORT, useClass: FakeParkItemsPort },
        { provide: PUBLIC_PARK_NAVIGATION_TREE_PARK_ZONES_API_SERVICE_PORT, useClass: FakeParkZonesPort },
        { provide: PUBLIC_PARK_NAVIGATION_TREE_VIDEOS_API_SERVICE_PORT, useClass: FakeVideosPort }
      ]
    });

    const facade: PublicParkNavigationTreeFacade = TestBed.inject(PublicParkNavigationTreeFacade);
    const resolveParkItemsListLabel = (facade as unknown as {
      resolveParkItemsListLabel(language: string, parkLabel: string): string;
    }).resolveParkItemsListLabel.bind(facade);

    expect(resolveParkItemsListLabel('fr', 'Bellewaerde')).toBe('Lieux de Bellewaerde');
    expect(resolveParkItemsListLabel('en', 'Bellewaerde')).toBe('Places at Bellewaerde');
  });

  it('builds contextual park video breadcrumbs with the localized video title', () => {
    TestBed.configureTestingModule({
      providers: [
        PublicParkNavigationTreeFacade,
        provideRouter([]),
        { provide: PUBLIC_PARK_NAVIGATION_TREE_PARKS_API_SERVICE_PORT, useClass: FakeParksPort },
        { provide: PUBLIC_PARK_NAVIGATION_TREE_PARK_ITEMS_API_SERVICE_PORT, useClass: FakeParkItemsPort },
        { provide: PUBLIC_PARK_NAVIGATION_TREE_PARK_ZONES_API_SERVICE_PORT, useClass: FakeParkZonesPort },
        { provide: PUBLIC_PARK_NAVIGATION_TREE_VIDEOS_API_SERVICE_PORT, useClass: FakeVideosPort }
      ]
    });

    const facade: PublicParkNavigationTreeFacade = TestBed.inject(PublicParkNavigationTreeFacade);
    const buildTreeItems = (facade as unknown as {
      buildTreeItems(sourceData: PublicParkNavigationSourceDataForTest): PublicParkNavigationTreeItem[];
    }).buildTreeItems.bind(facade);

    const items: PublicParkNavigationTreeItem[] = buildTreeItems({
      context: {
        language: 'fr',
        parkId: 'park-1',
        parkSlug: 'bellewaerde',
        itemId: null,
        itemSlug: null,
        zoneId: null,
        zoneSlug: null,
        selectedZoneId: null,
        videoId: 'video-1',
        videoSlug: 'provider-title',
        pageKind: 'park-video'
      },
      park: { name: 'Bellewaerde' },
      item: null,
      zone: null,
      video: {
        title: 'Provider title',
        titles: [{ languageCode: 'fr', value: 'Belle vidéo' }]
      }
    });

    expect(items.map((item: PublicParkNavigationTreeItem): string => item.label)).toEqual([
      'Liste des parcs',
      'Bellewaerde',
      'Vidéos de Bellewaerde',
      'Belle vidéo'
    ]);
  });
});
