import { Observable, of } from 'rxjs';

import { ImageCategory } from '@app/models/images/image-category';

import { ImageOwnerType } from '@app/models/images/image-owner-type';

import { ParkDistanceResponse } from '@app/models/parks/park-distance';

import { ParkDetailSummary } from '@app/models/parks/park-detail-summary';

import { ParkOpeningHoursCalendar, ParkOpeningHoursDay, ParkOpeningHoursTimeRange } from '@app/models/parks/park-opening-hours';

import { Park } from '@app/models/parks/park';

import { ParkWeatherForecast } from '@app/models/parks/park-weather';

import { AnonymousHttpOptions } from '@core/http/auth/anonymous-http-options';

import { ParkDetailHttpOptions, ParkDetailParksPort } from '../../park-detail-data.ports';

function createPark(
  hasLogo: boolean = true,
  status: Park['status'] = 'Operating',
): Park {
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
    descriptions: [{ languageCode: 'en', value: '<p>Belgian park.</p>' }],
  };
}

function createSummary(
  totalItems: number = 3,
  hasMainImage: boolean = true,
  hasLogo: boolean = true,
  status: Park['status'] = 'Operating',
): ParkDetailSummary {
  return {
    park: createPark(hasLogo, status),
    mainImage: hasMainImage
      ? {
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
          updatedAt: '2026-01-01T00:00:00Z',
        }
      : null,
    references: {
      founderName: 'Founder',
      operatorName: 'Operator',
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
        Restaurant: totalItems > 0 ? 1 : 0,
      },
    },
  };
}

function createNearbyPark(
  id: string,
  name: string,
  distanceLatitude: number,
): Park {
  return {
    id,
    name,
    countryCode: 'BE',
    latitude: distanceLatitude,
    longitude: 3.1,
    isVisible: true,
    descriptions: [
      { languageCode: 'en', value: 'Nearby park description '.repeat(12) },
    ],
  };
}

function createNearbyResponse(): ParkDistanceResponse {
  return {
    source: {
      id: 'park-1',
      name: 'Bellewaerde',
      countryCode: 'BE',
      latitude: 50.845,
      longitude: 2.945,
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
        park: createNearbyPark('near-1', 'Nearby One', 50.9),
      },
    ],
    missingTargetParkIds: [],
    unavailableTargetParkIds: [],
  };
}

function createWeatherForecast(): ParkWeatherForecast {
  return {
    parkId: 'park-1',
    attribution: {
      providerName: 'Open-Meteo',
      providerUrl: 'https://open-meteo.com/',
      licenseName: 'CC BY 4.0',
      licenseUrl: 'https://creativecommons.org/licenses/by/4.0/',
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
        fetchedAtUtc: '2026-06-19T00:00:00Z',
      },
    ],
  };
}

function createOpeningHoursCalendar(
  days: ParkOpeningHoursDay[] = [
    createOpenOpeningHoursDay(formatUtcDate(addUtcDays(new Date(), 1))),
  ],
): ParkOpeningHoursCalendar {
  return {
    parkId: 'park-1',
    timeZoneId: 'UTC',
    updatedAtUtc: '2026-06-19T00:00:00Z',
    firstDate: '2026-01-01',
    lastDate: '2026-12-31',
    fromDate: '2026-06-18',
    toDate: '2026-06-20',
    days,
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
    timeRanges: [createOpeningHoursRange('10:00', '18:00')],
  };
}

function createOpeningHoursRange(
  opensAt: string,
  closesAt: string,
): ParkOpeningHoursTimeRange {
  return {
    opensAt,
    closesAt,
    closesNextDay: false,
    lastAdmissionAt: null,
    lastAdmissionNextDay: false,
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

export class FakeParksPort implements ParkDetailParksPort {
  public summaryResponse$: Observable<ParkDetailSummary> = of(createSummary());
  public summaryResponses$: Observable<ParkDetailSummary>[] = [];
  public nearestResponse$: Observable<ParkDistanceResponse> = of(
    createNearbyResponse(),
  );
  public weatherResponse$: Observable<ParkWeatherForecast> = of(
    createWeatherForecast(),
  );
  public openingHoursResponse$: Observable<ParkOpeningHoursCalendar> = of(
    createOpeningHoursCalendar(),
  );
  public openingHoursResponses$: Observable<ParkOpeningHoursCalendar>[] = [];
  public readonly summaryCalls: string[] = [];
  public readonly summaryOptions: Array<ParkDetailHttpOptions | undefined> = [];
  public readonly nearestCalls: {
    sourceParkId: string;
    limit?: number;
    maxDistanceKilometers?: number | null;
  }[] = [];
  public readonly nearestOptions: Array<AnonymousHttpOptions | undefined> = [];
  public readonly weatherCalls: {
    id: string;
    days?: number;
  }[] = [];
  public readonly weatherOptions: Array<AnonymousHttpOptions | undefined> = [];
  public readonly openingHoursCalls: {
    id: string;
    from?: string | null;
    to?: string | null;
  }[] = [];
  public readonly openingHoursOptions: Array<AnonymousHttpOptions | undefined> =
    [];

  getParkDetailSummary(
    id: string,
    options?: ParkDetailHttpOptions,
  ): Observable<ParkDetailSummary> {
    this.summaryCalls.push(id);
    this.summaryOptions.push(options);
    return this.summaryResponses$.shift() ?? this.summaryResponse$;
  }

  getNearestParks(
    sourceParkId: string,
    limit?: number,
    maxDistanceKilometers?: number | null,
    options?: AnonymousHttpOptions,
  ): Observable<ParkDistanceResponse> {
    this.nearestCalls.push({ sourceParkId, limit, maxDistanceKilometers });
    this.nearestOptions.push(options);
    return this.nearestResponse$;
  }

  getParkWeather(
    id: string,
    days?: number,
    options?: AnonymousHttpOptions,
  ): Observable<ParkWeatherForecast> {
    this.weatherCalls.push({ id, days });
    this.weatherOptions.push(options);
    return this.weatherResponse$;
  }

  getParkOpeningHours(
    id: string,
    from?: string | null,
    to?: string | null,
    options?: AnonymousHttpOptions,
  ): Observable<ParkOpeningHoursCalendar> {
    this.openingHoursCalls.push({ id, from, to });
    this.openingHoursOptions.push(options);
    const queuedResponse$: Observable<ParkOpeningHoursCalendar> | undefined =
      this.openingHoursResponses$.shift();
    if (queuedResponse$) {
      return queuedResponse$;
    }

    return this.openingHoursResponse$;
  }
}
