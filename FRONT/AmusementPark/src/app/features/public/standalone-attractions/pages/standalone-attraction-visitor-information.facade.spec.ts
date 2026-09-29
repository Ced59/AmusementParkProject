import { TestBed } from '@angular/core/testing';
import { Observable, Subject } from 'rxjs';

import { ParkOpeningHoursCalendar } from '@app/models/parks/park-opening-hours';
import { ParkPricing } from '@app/models/parks/park-pricing';
import { ParkWeatherForecast } from '@app/models/parks/park-weather';
import { SKIP_AUTHORIZATION_HEADER } from '@core/http/auth/auth-request-policy';
import {
  STANDALONE_ATTRACTION_VISITOR_INFORMATION_PORT,
  StandaloneAttractionVisitorInformationPort
} from './standalone-attraction-visitor-information.ports';
import { StandaloneAttractionVisitorInformationFacade } from './standalone-attraction-visitor-information.facade';

describe('StandaloneAttractionVisitorInformationFacade', () => {
  let facade: StandaloneAttractionVisitorInformationFacade;
  let openingHoursResponses: Map<string, Subject<ParkOpeningHoursCalendar>>;
  let pricingResponses: Map<string, Subject<ParkPricing>>;
  let weatherResponses: Map<string, Subject<ParkWeatherForecast>>;
  let anonymousRequests: boolean[];

  beforeEach(() => {
    openingHoursResponses = new Map<string, Subject<ParkOpeningHoursCalendar>>();
    pricingResponses = new Map<string, Subject<ParkPricing>>();
    weatherResponses = new Map<string, Subject<ParkWeatherForecast>>();
    anonymousRequests = [];
    const port: StandaloneAttractionVisitorInformationPort = {
      getOpeningHours: (id, options): Observable<ParkOpeningHoursCalendar> => {
        anonymousRequests.push(options?.context?.get(SKIP_AUTHORIZATION_HEADER) === true);
        return createResponse(openingHoursResponses, id);
      },
      getPricing: (id, options): Observable<ParkPricing> => {
        anonymousRequests.push(options?.context?.get(SKIP_AUTHORIZATION_HEADER) === true);
        return createResponse(pricingResponses, id);
      },
      getWeather: (id, options): Observable<ParkWeatherForecast> => {
        anonymousRequests.push(options?.context?.get(SKIP_AUTHORIZATION_HEADER) === true);
        return createResponse(weatherResponses, id);
      }
    };

    TestBed.configureTestingModule({
      providers: [
        StandaloneAttractionVisitorInformationFacade,
        { provide: STANDALONE_ATTRACTION_VISITOR_INFORMATION_PORT, useValue: port }
      ]
    });
    facade = TestBed.inject(StandaloneAttractionVisitorInformationFacade);
  });

  it('loads hours, pricing and weather anonymously', () => {
    facade.load('standalone-1');
    openingHoursResponses.get('standalone-1')?.next(createOpeningHours());
    pricingResponses.get('standalone-1')?.next(createPricing());
    weatherResponses.get('standalone-1')?.next(createWeather());

    expect(facade.openingHours()?.timeZoneId).toBe('Europe/Paris');
    expect(facade.pricing()?.currencyCode).toBe('EUR');
    expect(facade.weather()?.days).toHaveLength(1);
    expect(facade.openingHoursState().kind).toBe('ready');
    expect(facade.pricingState().kind).toBe('ready');
    expect(facade.weatherState().kind).toBe('ready');
    expect(anonymousRequests).toEqual([true, true, true]);
  });

  it('ignores visitor information returned for a previous attraction', () => {
    facade.load('standalone-1');
    facade.load('standalone-2');

    openingHoursResponses.get('standalone-1')?.next(createOpeningHours());
    pricingResponses.get('standalone-1')?.next(createPricing());
    weatherResponses.get('standalone-1')?.next(createWeather());

    expect(facade.openingHours()).toBeNull();
    expect(facade.pricing()).toBeNull();
    expect(facade.weather()).toBeNull();
  });
});

function createResponse<T>(responses: Map<string, Subject<T>>, id: string): Observable<T> {
  const response: Subject<T> = new Subject<T>();
  responses.set(id, response);
  return response.asObservable();
}

function createOpeningHours(): ParkOpeningHoursCalendar {
  return {
    parkId: 'standalone-1',
    timeZoneId: 'Europe/Paris',
    updatedAtUtc: '2026-09-30T00:00:00Z',
    fromDate: '2026-09-30',
    toDate: '2026-10-06',
    days: []
  };
}

function createPricing(): ParkPricing {
  return {
    parkId: 'standalone-1',
    currencyCode: 'EUR',
    notes: [],
    admissionOffers: [],
    annualPasses: [],
    parkingOffers: []
  };
}

function createWeather(): ParkWeatherForecast {
  return {
    parkId: 'standalone-1',
    days: [{
      localDate: '2026-09-30',
      dataKind: 'Forecast',
      fetchedAtUtc: '2026-09-30T00:00:00Z'
    }],
    attribution: {
      providerName: 'Open-Meteo',
      providerUrl: 'https://open-meteo.com',
      licenseName: 'CC BY 4.0',
      licenseUrl: 'https://creativecommons.org/licenses/by/4.0/'
    }
  };
}
