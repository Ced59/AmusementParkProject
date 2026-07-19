import { TestBed } from '@angular/core/testing';
import { Observable, Subject, of, throwError } from 'rxjs';

import { ImageCategory } from '@app/models/images/image-category';
import { ImageDto } from '@app/models/images/image-dto';
import { ImageOwnerType } from '@app/models/images/image-owner-type';
import { ParkItemImageDto } from '@app/models/images/park-item-image-dto';
import { HistoryTimeline } from '@app/models/history/history.models';
import { ParkDistanceResponse } from '@app/models/parks/park-distance';
import { ParkDetailSummary } from '@app/models/parks/park-detail-summary';
import { ParkOpeningHoursCalendar, ParkOpeningHoursDay, ParkOpeningHoursTimeRange } from '@app/models/parks/park-opening-hours';
import { Park } from '@app/models/parks/park';
import { ParkWeatherForecast } from '@app/models/parks/park-weather';
import { ParkItemVideoDto } from '@app/models/videos/park-item-video-dto';
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
  PARK_DETAIL_HISTORY_PORT,
  PARK_DETAIL_IMAGES_PORT,
  PARK_DETAIL_PARKS_PORT,
  ParkDetailHttpOptions,
  PARK_DETAIL_VIDEOS_PORT,
  ParkDetailHistoryPort,
  ParkDetailImagesPort,
  ParkDetailParksPort,
  ParkDetailVideosPort
} from './park-detail-data.ports';
import { ParkDetailStateFacade } from './park-detail-state.facade';

class FakeParksPort implements ParkDetailParksPort {
  public summaryResponse$: Observable<ParkDetailSummary> = of(createSummary());
  public summaryResponses$: Observable<ParkDetailSummary>[] = [];
  public nearestResponse$: Observable<ParkDistanceResponse> = of(createNearbyResponse());
  public weatherResponse$: Observable<ParkWeatherForecast> = of(createWeatherForecast());
  public openingHoursResponse$: Observable<ParkOpeningHoursCalendar> = of(createOpeningHoursCalendar());
  public openingHoursResponses$: Observable<ParkOpeningHoursCalendar>[] = [];
  public readonly summaryCalls: string[] = [];
  public readonly summaryOptions: Array<ParkDetailHttpOptions | undefined> = [];
  public readonly nearestCalls: { sourceParkId: string; limit?: number; maxDistanceKilometers?: number | null }[] = [];
  public readonly nearestOptions: Array<AnonymousHttpOptions | undefined> = [];
  public readonly weatherCalls: { id: string; days?: number }[] = [];
  public readonly weatherOptions: Array<AnonymousHttpOptions | undefined> = [];
  public readonly openingHoursCalls: { id: string; from?: string | null; to?: string | null }[] = [];
  public readonly openingHoursOptions: Array<AnonymousHttpOptions | undefined> = [];

  getParkDetailSummary(id: string, options?: ParkDetailHttpOptions): Observable<ParkDetailSummary> {
    this.summaryCalls.push(id);
    this.summaryOptions.push(options);
    return this.summaryResponses$.shift() ?? this.summaryResponse$;
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

  getParkOpeningHours(id: string, from?: string | null, to?: string | null, options?: AnonymousHttpOptions): Observable<ParkOpeningHoursCalendar> {
    this.openingHoursCalls.push({ id, from, to });
    this.openingHoursOptions.push(options);
    const queuedResponse$: Observable<ParkOpeningHoursCalendar> | undefined = this.openingHoursResponses$.shift();
    if (queuedResponse$) {
      return queuedResponse$;
    }

    return this.openingHoursResponse$;
  }
}

class FakeSsrHttpStatusService {
  public notFoundCallCount = 0;
  public readonly statusCodes: number[] = [];

  setNotFound(): void {
    this.notFoundCallCount += 1;
  }

  setStatus(statusCode: number): void {
    this.statusCodes.push(statusCode);
  }
}

class FakeVideosPort implements ParkDetailVideosPort {
  public videosResponse$: Observable<PagedResult<VideoDto>> = of(createVideosPage(1));
  public itemVideosResponse$: Observable<PagedResult<ParkItemVideoDto>> = of(createImagePage<ParkItemVideoDto>([]));
  public readonly calls: VideoSearchQuery[] = [];
  public readonly itemVideoCalls: { parkId: string; query: VideoSearchQuery }[] = [];
  public readonly options: Array<AnonymousHttpOptions | undefined> = [];

  getVideosPage(query: VideoSearchQuery = {}, options?: AnonymousHttpOptions): Observable<PagedResult<VideoDto>> {
    this.calls.push(query);
    this.options.push(options);
    return this.videosResponse$;
  }

  getParkItemVideosByPark(parkId: string, query: VideoSearchQuery = {}, options?: AnonymousHttpOptions): Observable<PagedResult<ParkItemVideoDto>> {
    this.itemVideoCalls.push({ parkId, query });
    this.options.push(options);
    return this.itemVideosResponse$;
  }
}

class FakeImagesPort implements ParkDetailImagesPort {
  public parkImagesResponse$: Observable<PagedResult<ImageDto>> = of(createImagePage<ImageDto>([]));
  public logoImagesResponse$: Observable<PagedResult<ImageDto>> = of(createImagePage<ImageDto>([]));
  public itemImagesResponse$: Observable<PagedResult<ParkItemImageDto>> = of(createImagePage<ParkItemImageDto>([]));
  public readonly pageCalls: { ownerType: ImageOwnerType; ownerId: string; category: ImageCategory; page?: number; size?: number }[] = [];
  public readonly itemImageCalls: { parkId: string; page?: number; size?: number }[] = [];

  getImagesPage(ownerType: ImageOwnerType, ownerId: string, category: ImageCategory, page?: number, size?: number): Observable<PagedResult<ImageDto>> {
    this.pageCalls.push({ ownerType, ownerId, category, page, size });
    return category === ImageCategory.LOGO ? this.logoImagesResponse$ : this.parkImagesResponse$;
  }

  getParkItemImagesByPark(parkId: string, page?: number, size?: number): Observable<PagedResult<ParkItemImageDto>> {
    this.itemImageCalls.push({ parkId, page, size });
    return this.itemImagesResponse$;
  }
}

class FakeHistoryPort implements ParkDetailHistoryPort {
  public timelineResponse$: Observable<HistoryTimeline> = of(createHistoryTimeline(0));
  public timelineResponses$: Observable<HistoryTimeline>[] = [];
  public readonly calls: { parkId: string; includeParkItems?: boolean; parkItemIds?: readonly string[] }[] = [];
  public readonly options: Array<AnonymousHttpOptions | undefined> = [];

  getParkTimeline(
    parkId: string,
    includeParkItems: boolean = false,
    parkItemIds: readonly string[] = [],
    options?: AnonymousHttpOptions
  ): Observable<HistoryTimeline> {
    this.calls.push({ parkId, includeParkItems, parkItemIds });
    this.options.push(options);
    const queuedResponse$: Observable<HistoryTimeline> | undefined = this.timelineResponses$.shift();
    if (queuedResponse$) {
      return queuedResponse$;
    }

    return this.timelineResponse$;
  }
}

function createPark(hasLogo: boolean = true, status: Park['status'] = 'Operating'): Park {
  return {
    id: 'park-1',
    name: 'Bellewaerde',
    status,
    countryCode: 'BE',
    latitude: 50.845,
    longitude: 2.945,
    isVisible: true,
    founderId: 'founder-1',
    operatorId: 'operator-1',
    currentLogoImageId: hasLogo ? 'logo-1' : null,
    descriptions: [
      { languageCode: 'en', value: '<p>Belgian park.</p>' }
    ]
  };
}

function createSummary(
  totalItems: number = 3,
  hasMainImage: boolean = true,
  hasLogo: boolean = true,
  status: Park['status'] = 'Operating'
): ParkDetailSummary {
  return {
    park: createPark(hasLogo, status),
    mainImage: hasMainImage ? {
      id: 'main-image-1',
      category: ImageCategory.PARK,
      ownerType: ImageOwnerType.PARK,
      ownerId: 'park-1',
      path: 'parks/main.jpg',
      description: 'Main park image',
      isCurrent: true,
      isWatermarked: false,
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
  return createImagePage(totalItems > 0 ? [createVideo()] : [], totalItems);
}

function createImagePage<TItem>(items: TItem[], totalItems: number = items.length): PagedResult<TItem> {
  return {
    items,
    pagination: {
      totalItems,
      totalPages: totalItems > 0 ? 1 : 0,
      currentPage: 1,
      itemsPerPage: 1
    }
  };
}

function createHistoryTimeline(totalEvents: number): HistoryTimeline {
  return {
    entityType: 'Park',
    park: createPark(),
    parkItem: null,
    includedParkItems: [],
    events: totalEvents > 0 ? [{
      event: {
        id: 'history-event-1',
        key: 'opening',
        entityType: 'Park',
        ownerId: 'park-1',
        parkId: 'park-1',
        parkItemId: null,
        contextParkId: null,
        year: 1954,
        month: null,
        day: null,
        datePrecision: 'Year',
        eventType: 'Opening',
        isMajor: false,
        isVisible: true,
        slug: 'opening',
        titles: [],
        summaries: [],
        mainImageId: null,
        previousName: null,
        newName: null,
        previousLogoImageId: null,
        newLogoImageId: null,
        previousOperatorId: null,
        newOperatorId: null,
        locationLabel: null,
        relatedParkIds: [],
        relatedParkItemIds: [],
        sources: [],
        article: null,
        createdAtUtc: '2026-01-01T00:00:00Z',
        updatedAtUtc: '2026-01-01T00:00:00Z'
      },
      contextPark: null,
      parkItem: null,
      mainImage: null
    }] : []
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

function createOpeningHoursCalendar(days: ParkOpeningHoursDay[] = [createOpenOpeningHoursDay(formatUtcDate(addUtcDays(new Date(), 1)))]): ParkOpeningHoursCalendar {
  return {
    parkId: 'park-1',
    timeZoneId: 'UTC',
    updatedAtUtc: '2026-06-19T00:00:00Z',
    firstDate: '2026-01-01',
    lastDate: '2026-12-31',
    fromDate: '2026-06-18',
    toDate: '2026-06-20',
    days
  };
}

function createClosedOpeningHoursDay(localDate: string): ParkOpeningHoursDay {
  return {
    localDate,
    isClosed: true,
    isDefined: true,
    sourceKind: 'Rule',
    labels: [],
    reasons: [],
    timeRanges: []
  };
}

function createOpenOpeningHoursDay(localDate: string): ParkOpeningHoursDay {
  return {
    localDate,
    isClosed: false,
    isDefined: true,
    sourceKind: 'Rule',
    labels: [],
    reasons: [],
    timeRanges: [createOpeningHoursRange('10:00', '18:00')]
  };
}

function createOpeningHoursRange(opensAt: string, closesAt: string): ParkOpeningHoursTimeRange {
  return {
    opensAt,
    closesAt,
    closesNextDay: false,
    lastAdmissionAt: null,
    lastAdmissionNextDay: false
  };
}

function addUtcDays(date: Date, days: number): Date {
  const nextDate: Date = new Date(date);
  nextDate.setUTCDate(nextDate.getUTCDate() + days);
  return nextDate;
}

function formatUtcDate(date: Date): string {
  return date.toISOString().slice(0, 10);
}

function configureFacade(): {
  facade: ParkDetailStateFacade;
  parksPort: FakeParksPort;
  videosPort: FakeVideosPort;
  imagesPort: FakeImagesPort;
  historyPort: FakeHistoryPort;
  ssrStatusService: FakeSsrHttpStatusService;
} {
  const parksPort: FakeParksPort = new FakeParksPort();
  const videosPort: FakeVideosPort = new FakeVideosPort();
  const imagesPort: FakeImagesPort = new FakeImagesPort();
  const historyPort: FakeHistoryPort = new FakeHistoryPort();
  const ssrStatusService: FakeSsrHttpStatusService = new FakeSsrHttpStatusService();

  TestBed.configureTestingModule({
    providers: [
      ParkDetailStateFacade,
      { provide: PARK_DETAIL_PARKS_PORT, useValue: parksPort },
      { provide: PARK_DETAIL_VIDEOS_PORT, useValue: videosPort },
      { provide: PARK_DETAIL_IMAGES_PORT, useValue: imagesPort },
      { provide: PARK_DETAIL_HISTORY_PORT, useValue: historyPort },
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
    imagesPort,
    historyPort,
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
    expect(context.facade.summary()?.entries.find((entry) => entry.labelKey === 'publicCounts.place')?.count).toBe(3);
    expect(context.parksPort.summaryCalls).toEqual(['park-1']);
    expect(context.videosPort.calls).toEqual([{
      page: 1,
      size: 1,
      ownerType: VideoOwnerType.PARK,
      ownerId: 'park-1'
    }]);
    expect(context.videosPort.itemVideoCalls).toEqual([{ parkId: 'park-1', query: { page: 1, size: 1 } }]);
    expect(context.parksPort.nearestCalls).toEqual([{ sourceParkId: 'park-1', limit: 4, maxDistanceKilometers: null }]);
    expect(context.parksPort.weatherCalls).toEqual([{ id: 'park-1', days: 7 }]);
    expect(context.parksPort.openingHoursCalls.length).toBe(1);
    expect(context.parksPort.openingHoursCalls[0].id).toBe('park-1');
    expect(context.historyPort.calls).toEqual([{ parkId: 'park-1', includeParkItems: false, parkItemIds: [] }]);
    expect(context.facade.nearbyState().kind).toBe('ready');
    expect(context.facade.nearbyParks().map((park) => park.id)).toEqual(['near-1']);
    expect(context.facade.weatherState().kind).toBe('ready');
    expect(context.facade.weather()?.days.length).toBe(1);
    expect(context.facade.nearbyParks()[0].shortDescription?.length).toBeLessThanOrEqual(140);
  });

  it('reloads the park detail summary with all items for definitively closed parks', () => {
    const context = configureFacade();
    context.parksPort.summaryResponses$ = [
      of(createSummary(0, true, true, 'ClosedDefinitively')),
      of(createSummary(3, true, true, 'ClosedDefinitively'))
    ];

    context.facade.loadPark('park-1');

    expect(context.facade.park()?.stats[0].value).toBe(3);
    expect(context.parksPort.summaryCalls).toEqual(['park-1', 'park-1']);
    expect(context.parksPort.summaryOptions[0]?.closedFilter).toBeUndefined();
    expect(context.parksPort.summaryOptions[1]?.closedFilter).toBe('all');
  });

  it('clears stale secondary park data while another park is loading', () => {
    const context = configureFacade();
    context.facade.loadPark('park-1');

    expect(context.facade.nearbyParks().map((park) => park.id)).toEqual(['near-1']);
    expect(context.facade.weather()?.parkId).toBe('park-1');
    expect(context.facade.openingHours()?.parkId).toBe('park-1');

    context.parksPort.summaryResponse$ = new Subject<ParkDetailSummary>();

    context.facade.loadPark('park-2');

    expect(context.facade.nearbyState().kind).toBe('loading');
    expect(context.facade.nearbyParks()).toEqual([]);
    expect(context.facade.weatherState().kind).toBe('loading');
    expect(context.facade.weather()).toBeNull();
    expect(context.facade.openingHoursState().kind).toBe('loading');
    expect(context.facade.openingHours()).toBeNull();
  });

  it('hides the videos link when the park has no published videos', () => {
    const context = configureFacade();
    context.videosPort.videosResponse$ = of(createVideosPage(0));

    context.facade.setCurrentLanguage('fr');
    context.facade.loadPark('park-1');

    expect(context.facade.park()?.videosLink).toBeNull();
  });

  it('exposes the videos link when only park items have videos', () => {
    const context = configureFacade();
    context.videosPort.videosResponse$ = of(createVideosPage(0));
    context.videosPort.itemVideosResponse$ = of(createImagePage<ParkItemVideoDto>([
      {
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
          ...createVideo(),
          id: 'item-video-1',
          ownerType: VideoOwnerType.PARK_ITEM,
          ownerId: 'item-1'
        }
      }
    ]));

    context.facade.setCurrentLanguage('fr');
    context.facade.loadPark('park-1');

    expect(context.facade.park()?.videosLink).toEqual(['/', 'fr', 'park', 'park-1', 'bellewaerde', 'videos']);
  });

  it('keeps public park detail API reads anonymous by default', () => {
    const context = configureFacade();

    context.facade.loadPark('park-1');

    const capturedOptions: Array<AnonymousHttpOptions | undefined> = [
      ...context.parksPort.summaryOptions,
      ...context.parksPort.nearestOptions,
      ...context.parksPort.weatherOptions,
      ...context.parksPort.openingHoursOptions,
      ...context.videosPort.options,
      ...context.historyPort.options
    ];

    expect(capturedOptions.length).toBe(7);
    expect(capturedOptions.every((options: AnonymousHttpOptions | undefined) => options?.context.get(SKIP_AUTHORIZATION_HEADER) === true)).toBeTrue();
  });

  it('exposes the history link when the park timeline has events', () => {
    const context = configureFacade();
    context.historyPort.timelineResponse$ = of(createHistoryTimeline(1));

    context.facade.setCurrentLanguage('fr');
    context.facade.loadPark('park-1');

    expect(context.facade.park()?.historyLink).toEqual(['/', 'fr', 'park', 'park-1', 'bellewaerde', 'history']);
  });

  it('falls back to item timelines when the park-only history check returns not found', () => {
    const context = configureFacade();
    context.historyPort.timelineResponses$ = [
      throwError(() => ({ status: 404 })),
      of(createHistoryTimeline(1))
    ];

    context.facade.setCurrentLanguage('fr');
    context.facade.loadPark('park-1');

    expect(context.historyPort.calls).toEqual([
      { parkId: 'park-1', includeParkItems: false, parkItemIds: [] },
      { parkId: 'park-1', includeParkItems: true, parkItemIds: [] }
    ]);
    expect(context.facade.park()?.historyLink).toEqual(['/', 'fr', 'park', 'park-1', 'bellewaerde', 'history']);
  });

  it('loads a bounded future opening window when the preview has no upcoming opening', () => {
    const context = configureFacade();
    const today: string = formatUtcDate(new Date());
    const futureDate: string = formatUtcDate(addUtcDays(new Date(), 45));
    context.parksPort.openingHoursResponses$ = [
      of(createOpeningHoursCalendar([createClosedOpeningHoursDay(today)])),
      of(createOpeningHoursCalendar([createOpenOpeningHoursDay(futureDate)]))
    ];

    context.facade.loadPark('park-1');

    expect(context.parksPort.openingHoursCalls.length).toBe(2);
    expect(context.facade.openingHoursState().kind).toBe('ready');
    expect(context.facade.openingHours()?.days.map((day: ParkOpeningHoursDay) => day.localDate)).toEqual([today, futureDate]);
  });

  it('keeps the logo fallback and exposes the images link when no main photo exists', () => {
    const context = configureFacade();
    context.parksPort.summaryResponse$ = of(createSummary(3, false));

    context.facade.setCurrentLanguage('fr');
    context.facade.loadPark('park-1');

    expect(context.facade.park()?.heroImageId).toBe('logo-1');
    expect(context.facade.park()?.primaryPhoto).toBeNull();
    expect(context.facade.park()?.imagesLink).toEqual(['/', 'fr', 'park', 'park-1', 'bellewaerde', 'images']);
    expect(context.imagesPort.pageCalls).toEqual([]);
    expect(context.imagesPort.itemImageCalls).toEqual([]);
  });

  it('exposes the images link when only park items have photos', () => {
    const context = configureFacade();
    context.parksPort.summaryResponse$ = of(createSummary(3, false, false));
    context.imagesPort.itemImagesResponse$ = of(createImagePage<ParkItemImageDto>([
      {
        item: {
          id: 'item-1',
          parkId: 'park-1',
          name: 'Family Ride',
          category: 'Attraction',
          type: 'FlatRide',
          latitude: null,
          longitude: null
        },
        image: {
          id: 'item-image-1',
          category: ImageCategory.PARK_ITEM,
          ownerType: ImageOwnerType.PARK_ITEM,
          ownerId: 'item-1',
          path: 'items/item-image-1',
          description: 'Item image',
          isCurrent: false,
          isWatermarked: false,
          isPublished: true,
          width: 1200,
          height: 800,
          sizeInBytes: 1000,
          altTexts: [],
          captions: [],
          credits: [],
          tagIds: [],
          createdAt: '2026-01-01T00:00:00Z',
          updatedAt: '2026-01-01T00:00:00Z'
        }
      }
    ]));

    context.facade.setCurrentLanguage('fr');
    context.facade.loadPark('park-1');

    expect(context.facade.park()?.heroImageId).toBeNull();
    expect(context.facade.park()?.primaryPhoto).toBeNull();
    expect(context.facade.park()?.imagesLink).toEqual(['/', 'fr', 'park', 'park-1', 'bellewaerde', 'images']);
    expect(context.imagesPort.pageCalls).toEqual([
      { ownerType: ImageOwnerType.PARK, ownerId: 'park-1', category: ImageCategory.PARK, page: 1, size: 1 },
      { ownerType: ImageOwnerType.PARK, ownerId: 'park-1', category: ImageCategory.LOGO, page: 1, size: 1 }
    ]);
    expect(context.imagesPort.itemImageCalls).toEqual([{ parkId: 'park-1', page: 1, size: 1 }]);
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

  it('sets the SSR 503 status when the summary lookup fails transiently', () => {
    spyOn(console, 'error');
    const context = configureFacade();
    context.parksPort.summaryResponse$ = throwError(() => ({ status: 503 }));

    context.facade.loadPark('park-1');

    expect(context.facade.state().kind).toBe('error');
    expect(context.ssrStatusService.notFoundCallCount).toBe(0);
    expect(context.ssrStatusService.statusCodes).toEqual([503]);
  });
});
