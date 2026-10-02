import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { PublicLiveForecast, PublicLiveHistory, PublicLiveTarget } from '@app/models/live-data/public-live.models';
import { provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { environment } from '../../../environments/environment';
import { PublicLiveApiService } from './public-live-api.service';

describe('PublicLiveApiService', () => {
  let service: PublicLiveApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: provideCommonTestDependencies() });
    service = TestBed.inject(PublicLiveApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  it('shares and caches availability for park and item views', () => {
    const values: boolean[] = [];
    service.isPublicReadEnabled().subscribe(value => values.push(value));
    service.isPublicReadEnabled().subscribe(value => values.push(value));
    const request = http.expectOne(`${environment.apiBaseUrl}public/capabilities`);
    request.flush([{ key: 'another-feature', isEnabled: true }, { key: 'live:public-experience', isEnabled: false }]);
    service.isPublicReadEnabled().subscribe(value => values.push(value));
    expect(values).toEqual([false, false, false]);
    http.expectNone(`${environment.apiBaseUrl}public/capabilities`);
  });

  it('retries a failed availability lookup instead of caching the error permanently', () => {
    service.isPublicReadEnabled().subscribe({ error: () => undefined });
    http.expectOne(`${environment.apiBaseUrl}public/capabilities`).flush(null, { status: 503, statusText: 'Unavailable' });
    let enabled = false;
    service.isPublicReadEnabled().subscribe(value => enabled = value);
    http.expectOne(`${environment.apiBaseUrl}public/capabilities`).flush([{ key: 'live:public-experience', isEnabled: true }]);
    expect(enabled).toBe(true);
  });

  it('revalidates with the stored ETag and reuses the cached body on 304', () => {
    const target: PublicLiveTarget = createTarget();
    const received: PublicLiveTarget[] = [];

    service.getParkItem('item/one').subscribe((result: PublicLiveTarget) => received.push(result));
    const firstRequest = http.expectOne(`${environment.apiBaseUrl}public/live/items/item%2Fone`);
    expect(firstRequest.request.headers.has('If-None-Match')).toBe(false);
    firstRequest.flush(target, { headers: { ETag: '"live-v1"' } });

    service.getParkItem('item/one').subscribe((result: PublicLiveTarget) => received.push(result));
    const conditionalRequest = http.expectOne(`${environment.apiBaseUrl}public/live/items/item%2Fone`);
    expect(conditionalRequest.request.headers.get('If-None-Match')).toBe('"live-v1"');
    conditionalRequest.flush(null, { status: 304, statusText: 'Not Modified' });

    expect(received).toEqual([target, target]);
  });

  it('evicts the oldest response when the live cache reaches its bound', () => {
    for (let index: number = 0; index <= 20; index++) {
      service.getParkItem(`item-${index}`).subscribe();
      const request = http.expectOne(`${environment.apiBaseUrl}public/live/items/item-${index}`);
      request.flush(
        { ...createTarget(), targetId: `item-${index}` },
        { headers: { ETag: `"live-${index}"` } }
      );
    }

    service.getParkItem('item-0').subscribe();
    const evictedRequest = http.expectOne(`${environment.apiBaseUrl}public/live/items/item-0`);
    expect(evictedRequest.request.headers.has('If-None-Match')).toBe(false);
    evictedRequest.flush(createTarget());

    service.getParkItem('item-20').subscribe();
    const retainedRequest = http.expectOne(`${environment.apiBaseUrl}public/live/items/item-20`);
    expect(retainedRequest.request.headers.get('If-None-Match')).toBe('"live-20"');
    retainedRequest.flush(null, { status: 304, statusText: 'Not Modified' });
  });

  it('loads hourly item history without exposing raw observations in the URL', () => {
    const history: PublicLiveHistory = createHistory();

    service.getParkItemHistory('item/one').subscribe((result: PublicLiveHistory) => {
      expect(result).toEqual(history);
    });

    const request = http.expectOne(
      `${environment.apiBaseUrl}public/live/items/item%2Fone/history?bucket=hour`
    );
    expect(request.request.method).toBe('GET');
    request.flush(history);
  });

  it('loads a conditional public forecast without technical query parameters', () => {
    const forecast: PublicLiveForecast = createForecast();

    service.getParkItemForecast('item/one').subscribe((result: PublicLiveForecast) => {
      expect(result).toEqual(forecast);
    });

    const request = http.expectOne(
      `${environment.apiBaseUrl}public/live/items/item%2Fone/forecast`
    );
    expect(request.request.method).toBe('GET');
    request.flush(forecast);
  });
});

function createTarget(): PublicLiveTarget {
  return {
    targetId: 'item/one',
    targetType: 'ParkItem',
    displayName: 'Example attraction',
    parkId: 'park-1',
    parkDisplayName: 'Example park',
    availability: 'Current',
    status: 'Open',
    queues: [],
    asOfUtc: '2026-09-29T10:00:00Z',
    observedAtUtc: '2026-09-29T09:59:00Z',
    receivedAtUtc: '2026-09-29T09:59:01Z',
    ageSeconds: 60,
    freshness: 'Fresh',
    expiresAtUtc: '2026-09-29T10:04:00Z',
    source: null,
    confidence: 'High'
  };
}

function createHistory(): PublicLiveHistory {
  return {
    targetId: 'item/one',
    displayName: 'Example attraction',
    parkId: 'park-1',
    parkDisplayName: 'Example park',
    fromUtc: '2026-08-30T10:00:00Z',
    toUtc: '2026-09-29T10:00:00Z',
    timeZoneId: 'Europe/Paris',
    dataStatus: 'Insufficient',
    expectedObservationCount: 100,
    observationCount: 1,
    usableWaitCount: 1,
    daysCovered: 1,
    comparableDays: 1,
    coveragePercent: 1,
    truncatedObservationCount: 0,
    exclusions: {
      duplicateObservations: 0,
      outsideActiveWindow: 0,
      nonOperatingStatus: 0,
      missingStandbyWait: 0,
      total: 0
    },
    hours: [],
    source: {
      id: 'themeparks-wiki',
      displayName: 'ThemeParks.wiki',
      type: 'AuthorizedAggregator',
      attributionText: 'Powered by ThemeParks.wiki',
      attributionUrl: 'https://themeparks.wiki/'
    }
  };
}

function createForecast(): PublicLiveForecast {
  return {
    targetDisplayName: 'Example attraction',
    parkDisplayName: 'Example park',
    timeZoneId: 'Europe/Paris',
    forecastFromUtc: '2026-09-29T13:00:00Z',
    forecastToUtc: '2026-09-29T14:00:00Z',
    calculatedAtUtc: '2026-09-29T12:30:00Z',
    expectedWaitMinutes: 25,
    lowerBoundMinutes: 15,
    upperBoundMinutes: 35,
    trainingDayCount: 12,
    studyVersion: 'live-wait-backtest-v2',
    method: 'rolling-weekday-hour-median-v1',
    intervalMethod: 'rolling-weekday-hour-p10-p90-v1',
    meanAbsoluteErrorMinutes: 4,
    intervalCoveragePercent: 82,
    evaluationFromUtc: '2026-07-01T12:30:00Z',
    evaluationToUtc: '2026-09-29T12:30:00Z',
    evaluationPointCount: 120,
    source: {
      id: 'themeparks-wiki',
      displayName: 'ThemeParks.wiki',
      type: 'AuthorizedAggregator',
      attributionText: 'Powered by ThemeParks.wiki',
      attributionUrl: 'https://themeparks.wiki/'
    }
  };
}
