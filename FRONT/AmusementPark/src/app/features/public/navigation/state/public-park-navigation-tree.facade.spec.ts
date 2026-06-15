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
  PublicParkNavigationTreeParkZonesApiServicePort
} from './public-park-navigation-tree-data.ports';
import { PublicParkNavigationTreeFacade } from './public-park-navigation-tree.facade';

interface PublicParkRouteContextForTest {
  readonly language: string;
  readonly parkId: string;
  readonly parkSlug: string;
  readonly itemId: string | null;
  readonly itemSlug: string | null;
  readonly selectedZoneId: string | null;
  readonly pageKind: 'park-detail' | 'park-items' | 'park-item-detail' | 'park-images' | 'park-map';
}

interface PublicParkNavigationSourceDataForTest {
  readonly park: { name?: string } | null;
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
        { provide: PUBLIC_PARK_NAVIGATION_TREE_PARK_ZONES_API_SERVICE_PORT, useClass: FakeParkZonesPort }
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
      selectedZoneId: null,
      pageKind: 'park-detail'
    }));

    expect(parksPort.summaryCalls).toEqual(['park-1']);
    expect(sourceData.park?.name).toBe('Bellewaerde');
  });
});
