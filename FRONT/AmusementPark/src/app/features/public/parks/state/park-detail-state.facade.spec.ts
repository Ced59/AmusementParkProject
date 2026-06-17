import { TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';

import { ImageCategory } from '@app/models/images/image-category';
import { ImageOwnerType } from '@app/models/images/image-owner-type';
import { ParkDistanceResponse } from '@app/models/parks/park-distance';
import { ParkDetailSummary } from '@app/models/parks/park-detail-summary';
import { Park } from '@app/models/parks/park';
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import { CountryDisplayService } from '@shared/services/countries/country-display.service';
import {
  PARK_DETAIL_PARKS_PORT,
  ParkDetailParksPort
} from './park-detail-data.ports';
import { ParkDetailStateFacade } from './park-detail-state.facade';

class FakeParksPort implements ParkDetailParksPort {
  public summaryResponse$: Observable<ParkDetailSummary> = of(createSummary());
  public nearestResponse$: Observable<ParkDistanceResponse> = of(createNearbyResponse());
  public readonly summaryCalls: string[] = [];
  public readonly nearestCalls: { sourceParkId: string; limit?: number; maxDistanceKilometers?: number | null }[] = [];

  getParkDetailSummary(id: string): Observable<ParkDetailSummary> {
    this.summaryCalls.push(id);
    return this.summaryResponse$;
  }

  getNearestParks(sourceParkId: string, limit?: number, maxDistanceKilometers?: number | null): Observable<ParkDistanceResponse> {
    this.nearestCalls.push({ sourceParkId, limit, maxDistanceKilometers });
    return this.nearestResponse$;
  }
}

class FakeSsrHttpStatusService {
  public notFoundCallCount = 0;

  setNotFound(): void {
    this.notFoundCallCount += 1;
  }
}

function createPark(): Park {
  return {
    id: 'park-1',
    name: 'Bellewaerde',
    countryCode: 'BE',
    latitude: 50.845,
    longitude: 2.945,
    isVisible: true,
    founderId: 'founder-1',
    operatorId: 'operator-1',
    currentLogoImageId: 'logo-1',
    descriptions: [
      { languageCode: 'en', value: '<p>Belgian park.</p>' }
    ]
  };
}

function createSummary(totalItems: number = 3, hasMainImage: boolean = true): ParkDetailSummary {
  return {
    park: createPark(),
    mainImage: hasMainImage ? {
      id: 'main-image-1',
      category: ImageCategory.PARK,
      ownerType: ImageOwnerType.PARK,
      ownerId: 'park-1',
      path: 'parks/main.jpg',
      description: 'Main park image',
      isCurrent: true,
      isPublished: true,
      width: 1200,
      height: 800,
      sizeInBytes: 1000,
      originalFileName: 'main.jpg',
      contentType: 'image/jpeg',
      geoLocation: null,
      altTexts: [],
      captions: [],
      credits: [],
      tagIds: [],
      createdAt: '2026-01-01T00:00:00Z',
      updatedAt: '2026-01-01T00:00:00Z'
    } : null,
    references: {
      founderName: 'Founder',
      operatorName: 'Operator'
    },
    stats: {
      totalItems,
      zoneCount: totalItems > 0 ? 1 : 0,
      attractionCount: Math.max(totalItems - 1, 0),
      restaurantCount: totalItems > 0 ? 1 : 0,
      showCount: 0,
      shopCount: 0,
      hotelCount: 0,
      countsByCategory: {
        Attraction: Math.max(totalItems - 1, 0),
        Restaurant: totalItems > 0 ? 1 : 0
      }
    }
  };
}

function createNearbyPark(id: string, name: string, distanceLatitude: number): Park {
  return {
    id,
    name,
    countryCode: 'BE',
    latitude: distanceLatitude,
    longitude: 3.1,
    isVisible: true,
    descriptions: [
      { languageCode: 'en', value: 'Nearby park description '.repeat(12) }
    ]
  };
}

function createNearbyResponse(): ParkDistanceResponse {
  return {
    source: {
      id: 'park-1',
      name: 'Bellewaerde',
      countryCode: 'BE',
      latitude: 50.845,
      longitude: 2.945
    },
    distanceUnit: 'km',
    calculationKind: 'nearest',
    targets: [
      {
        proximityRank: 1,
        distanceKilometers: 18.4,
        distanceMeters: 18400,
        distanceUnit: 'km',
        estimatedTravelDurationMinutes: 22,
        park: createNearbyPark('near-1', 'Nearby One', 50.9)
      }
    ],
    missingTargetParkIds: [],
    unavailableTargetParkIds: []
  };
}

function configureFacade(): {
  facade: ParkDetailStateFacade;
  parksPort: FakeParksPort;
  ssrStatusService: FakeSsrHttpStatusService;
} {
  const parksPort: FakeParksPort = new FakeParksPort();
  const ssrStatusService: FakeSsrHttpStatusService = new FakeSsrHttpStatusService();

  TestBed.configureTestingModule({
    providers: [
      ParkDetailStateFacade,
      { provide: PARK_DETAIL_PARKS_PORT, useValue: parksPort },
      {
        provide: CountryDisplayService,
        useValue: {
          resolveLocalizedCountryName: (): string => 'Belgique'
        }
      },
      { provide: SsrHttpStatusService, useValue: ssrStatusService }
    ]
  });

  return {
    facade: TestBed.inject(ParkDetailStateFacade),
    parksPort,
    ssrStatusService
  };
}

describe('ParkDetailStateFacade', () => {
  beforeEach(() => {
    TestBed.resetTestingModule();
  });

  it('loads the optimized park detail summary and nearby parks', () => {
    const context = configureFacade();
    context.parksPort.summaryResponse$ = of(createSummary(3));

    context.facade.setCurrentLanguage('fr');
    context.facade.loadPark('park-1');

    expect(context.facade.state().kind).toBe('ready');
    expect(context.facade.park()?.name).toBe('Bellewaerde');
    expect(context.facade.park()?.countryName).toBe('Belgique');
    expect(context.facade.park()?.heroImageId).toBe('main-image-1');
    expect(context.facade.park()?.primaryPhoto?.imageId).toBe('main-image-1');
    expect(context.facade.park()?.imagesLink).toEqual(['/', 'fr', 'park', 'park-1', 'bellewaerde', 'images']);
    expect(context.facade.park()?.founderName).toBe('Founder');
    expect(context.facade.park()?.operatorName).toBe('Operator');
    expect(context.facade.park()?.stats[0].value).toBe(3);
    expect(context.facade.summary()?.entries.find((entry) => entry.labelKey === 'parkVisitor.summary.totalItems')?.count).toBe(3);
    expect(context.parksPort.summaryCalls).toEqual(['park-1']);
    expect(context.parksPort.nearestCalls).toEqual([{ sourceParkId: 'park-1', limit: 4, maxDistanceKilometers: null }]);
    expect(context.facade.nearbyState().kind).toBe('ready');
    expect(context.facade.nearbyParks().map((park) => park.id)).toEqual(['near-1']);
    expect(context.facade.nearbyParks()[0].shortDescription?.length).toBeLessThanOrEqual(140);
  });

  it('keeps the logo fallback but hides the images link when no main photo exists', () => {
    const context = configureFacade();
    context.parksPort.summaryResponse$ = of(createSummary(3, false));

    context.facade.setCurrentLanguage('fr');
    context.facade.loadPark('park-1');

    expect(context.facade.park()?.heroImageId).toBe('logo-1');
    expect(context.facade.park()?.primaryPhoto).toBeNull();
    expect(context.facade.park()?.imagesLink).toBeNull();
  });

  it('sets the SSR 404 status and preserves the previous data when the summary is missing', () => {
    spyOn(console, 'error');
    const context = configureFacade();
    context.facade.loadPark('park-1');
    context.parksPort.summaryResponse$ = throwError(() => ({ status: 404 }));

    context.facade.loadPark('missing-park');

    expect(context.facade.state().kind).toBe('error');
    expect(context.facade.state().error).toBe('parks.detail.errorMessage');
    expect(context.facade.park()?.id).toBe('park-1');
    expect(context.ssrStatusService.notFoundCallCount).toBe(1);
  });
});
