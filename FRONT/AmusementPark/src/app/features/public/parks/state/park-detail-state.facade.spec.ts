import { TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';

import { ImageCategory } from '@app/models/images/image-category';
import { ImageOwnerType } from '@app/models/images/image-owner-type';
import { ParkDistanceResponse } from '@app/models/parks/park-distance';
import { ParkDetailSummary } from '@app/models/parks/park-detail-summary';
import { Park } from '@app/models/parks/park';
import { ParkWeatherForecast } from '@app/models/parks/park-weather';
import { VideoDto } from '@app/models/videos/video-dto';
import { VideoHostingProvider } from '@app/models/videos/video-hosting-provider';
import { VideoOwnerType } from '@app/models/videos/video-owner-type';
import { VideoSearchQuery } from '@app/models/videos/video-search-query';
import { VideoType } from '@app/models/videos/video-type';
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import { PagedResult } from '@shared/models/contracts';
import { CountryDisplayService } from '@shared/services/countries/country-display.service';
import { AnonymousHttpOptions } from '@core/http/auth/anonymous-http-options';
import { SKIP_AUTHORIZATION_HEADER } from '@core/http/auth/auth-request-policy';
import {
  PARK_DETAIL_PARKS_PORT,
  PARK_DETAIL_VIDEOS_PORT,
  ParkDetailParksPort,
  ParkDetailVideosPort
} from './park-detail-data.ports';
import { ParkDetailStateFacade } from './park-detail-state.facade';

class FakeParksPort implements ParkDetailParksPort {
  public summaryResponse$: Observable<ParkDetailSummary> = of(createSummary());
  public nearestResponse$: Observable<ParkDistanceResponse> = of(createNearbyResponse());
  public weatherResponse$: Observable<ParkWeatherForecast> = of(createWeatherForecast());
  public readonly summaryCalls: string[] = [];
  public readonly summaryOptions: Array<AnonymousHttpOptions | undefined> = [];
  public readonly nearestCalls: { sourceParkId: string; limit?: number; maxDistanceKilometers?: number | null }[] = [];
  public readonly nearestOptions: Array<AnonymousHttpOptions | undefined> = [];
  public readonly weatherCalls: { id: string; days?: number }[] = [];
  public readonly weatherOptions: Array<AnonymousHttpOptions | undefined> = [];

  getParkDetailSummary(id: string, options?: AnonymousHttpOptions): Observable<ParkDetailSummary> {
    this.summaryCalls.push(id);
    this.summaryOptions.push(options);
    return this.summaryResponse$;
  }

  getNearestParks(sourceParkId: string, limit?: number, maxDistanceKilometers?: number | null, options?: AnonymousHttpOptions): Observable<ParkDistanceResponse> {
    this.nearestCalls.push({ sourceParkId, limit, maxDistanceKilometers });
    this.nearestOptions.push(options);
    return this.nearestResponse$;
  }

  getParkWeather(id: string, days?: number, options?: AnonymousHttpOptions): Observable<ParkWeatherForecast> {
    this.weatherCalls.push({ id, days });
    this.weatherOptions.push(options);
    return this.weatherResponse$;
  }
}

class FakeSsrHttpStatusService {
  public notFoundCallCount = 0;

  setNotFound(): void {
    this.notFoundCallCount += 1;
  }
}

class FakeVideosPort implements ParkDetailVideosPort {
  public videosResponse$: Observable<PagedResult<VideoDto>> = of(createVideosPage(1));
  public readonly calls: VideoSearchQuery[] = [];
  public readonly options: Array<AnonymousHttpOptions | undefined> = [];

  getVideosPage(query: VideoSearchQuery = {}, options?: AnonymousHttpOptions): Observable<PagedResult<VideoDto>> {
    this.calls.push(query);
    this.options.push(options);
    return this.videosResponse$;
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
    updatedAt: '2026-01-01T00:00:00Z'
  };
}

function createVideosPage(totalItems: number): PagedResult<VideoDto> {
  return {
    items: totalItems > 0 ? [createVideo()] : [],
    pagination: {
      totalItems,
      totalPages: totalItems > 0 ? 1 : 0,
      currentPage: 1,
      itemsPerPage: 1
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

function createWeatherForecast(): ParkWeatherForecast {
  return {
    parkId: 'park-1',
    attribution: {
      providerName: 'Open-Meteo',
      providerUrl: 'https://open-meteo.com/',
      licenseName: 'CC BY 4.0',
      licenseUrl: 'https://creativecommons.org/licenses/by/4.0/'
    },
    days: [
      {
        localDate: '2026-06-19',
        dataKind: 'Forecast',
        weatherCode: 1,
        temperatureMinCelsius: 12,
        temperatureMaxCelsius: 21,
        apparentTemperatureMinCelsius: 11,
        apparentTemperatureMaxCelsius: 22,
        precipitationProbabilityMaxPercent: 20,
        precipitationSumMillimeters: 0,
        windSpeedMaxKilometersPerHour: 18,
        windGustsMaxKilometersPerHour: 28,
        timeZone: 'Europe/Paris',
        fetchedAtUtc: '2026-06-19T00:00:00Z'
      }
    ]
  };
}

function configureFacade(): {
  facade: ParkDetailStateFacade;
  parksPort: FakeParksPort;
  videosPort: FakeVideosPort;
  ssrStatusService: FakeSsrHttpStatusService;
} {
  const parksPort: FakeParksPort = new FakeParksPort();
  const videosPort: FakeVideosPort = new FakeVideosPort();
  const ssrStatusService: FakeSsrHttpStatusService = new FakeSsrHttpStatusService();

  TestBed.configureTestingModule({
    providers: [
      ParkDetailStateFacade,
      { provide: PARK_DETAIL_PARKS_PORT, useValue: parksPort },
      { provide: PARK_DETAIL_VIDEOS_PORT, useValue: videosPort },
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
    videosPort,
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
    expect(context.facade.park()?.videosLink).toEqual(['/', 'fr', 'park', 'park-1', 'bellewaerde', 'videos']);
    expect(context.facade.park()?.founderName).toBe('Founder');
    expect(context.facade.park()?.operatorName).toBe('Operator');
    expect(context.facade.park()?.stats[0].value).toBe(3);
    expect(context.facade.summary()?.entries.find((entry) => entry.labelKey === 'parkVisitor.summary.totalItems')?.count).toBe(3);
    expect(context.parksPort.summaryCalls).toEqual(['park-1']);
    expect(context.videosPort.calls).toEqual([{
      page: 1,
      size: 1,
      ownerType: VideoOwnerType.PARK,
      ownerId: 'park-1'
    }]);
    expect(context.parksPort.nearestCalls).toEqual([{ sourceParkId: 'park-1', limit: 4, maxDistanceKilometers: null }]);
    expect(context.parksPort.weatherCalls).toEqual([{ id: 'park-1', days: 7 }]);
    expect(context.facade.nearbyState().kind).toBe('ready');
    expect(context.facade.nearbyParks().map((park) => park.id)).toEqual(['near-1']);
    expect(context.facade.weatherState().kind).toBe('ready');
    expect(context.facade.weather()?.days.length).toBe(1);
    expect(context.facade.nearbyParks()[0].shortDescription?.length).toBeLessThanOrEqual(140);
  });

  it('hides the videos link when the park has no published videos', () => {
    const context = configureFacade();
    context.videosPort.videosResponse$ = of(createVideosPage(0));

    context.facade.setCurrentLanguage('fr');
    context.facade.loadPark('park-1');

    expect(context.facade.park()?.videosLink).toBeNull();
  });

  it('keeps public park detail API reads anonymous by default', () => {
    const context = configureFacade();

    context.facade.loadPark('park-1');

    const capturedOptions: Array<AnonymousHttpOptions | undefined> = [
      ...context.parksPort.summaryOptions,
      ...context.parksPort.nearestOptions,
      ...context.parksPort.weatherOptions,
      ...context.videosPort.options
    ];

    expect(capturedOptions.length).toBe(4);
    expect(capturedOptions.every((options: AnonymousHttpOptions | undefined) => options?.context.get(SKIP_AUTHORIZATION_HEADER) === true)).toBeTrue();
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
