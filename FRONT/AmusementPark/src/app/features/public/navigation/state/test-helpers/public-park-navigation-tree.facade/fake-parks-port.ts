import { Observable, of } from 'rxjs';

import { ImageCategory } from '@app/models/images/image-category';

import { ImageOwnerType } from '@app/models/images/image-owner-type';

import { ParkDetailSummary } from '@app/models/parks/park-detail-summary';

import { PublicParkNavigationTreeParksApiServicePort } from '../../public-park-navigation-tree-data.ports';

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

export class FakeParksPort implements PublicParkNavigationTreeParksApiServicePort {
  public readonly summaryCalls: string[] = [];

  getParkDetailSummary(id: string): Observable<ParkDetailSummary> {
    this.summaryCalls.push(id);
    return of(createSummary());
  }
}
