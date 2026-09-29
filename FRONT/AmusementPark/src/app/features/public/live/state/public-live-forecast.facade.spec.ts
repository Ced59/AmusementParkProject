import { TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';

import {
  PublicLiveForecast,
  PublicLiveHistory,
  PublicLiveTarget,
  PublicParkLiveItems
} from '@app/models/live-data/public-live.models';
import { SsrRuntimeService } from '@core/ssr/ssr-runtime.service';
import { PUBLIC_LIVE_DATA_PORT, PublicLiveDataPort } from './public-live-data.port';
import { PublicLiveForecastFacade } from './public-live-forecast.facade';

describe('PublicLiveForecastFacade', () => {
  afterEach(() => {
    TestBed.resetTestingModule();
    vi.useRealTimers();
  });

  it('publishes a verified forecast returned by the port', () => {
    const forecast: PublicLiveForecast = createForecast();
    const port: PublicLiveDataPort = createPort(() => of(forecast));
    const facade: PublicLiveForecastFacade = configureFacade(port);

    facade.load('item-1');

    expect(port.getParkItemForecast).toHaveBeenCalledWith('item-1');
    expect(facade.state()).toEqual({ kind: 'ready', forecast });
  });

  it('keeps an unavailable or rejected forecast out of the page', () => {
    const port: PublicLiveDataPort = createPort(() => throwError(() => new Error('unavailable')));
    const facade: PublicLiveForecastFacade = configureFacade(port);

    facade.load('item-1');

    expect(facade.state()).toEqual({ kind: 'unavailable', forecast: null });
  });

  it('does not run the forecast backtest during SSR', () => {
    const port: PublicLiveDataPort = createPort(() => of(createForecast()));
    const facade: PublicLiveForecastFacade = configureFacade(port, false);

    facade.load('item-1');

    expect(port.getParkItemForecast).not.toHaveBeenCalled();
    expect(facade.state().kind).toBe('idle');
  });

  it('renews the forecast when its covered interval ends', () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-09-29T13:59:00Z'));
    const port: PublicLiveDataPort = createPort(() => of(createForecast()));
    const facade: PublicLiveForecastFacade = configureFacade(port);

    facade.load('item-1');
    vi.advanceTimersByTime(60_000);

    expect(port.getParkItemForecast).toHaveBeenCalledTimes(2);
  });

  it('retries an unavailable forecast after the bounded retry window', () => {
    vi.useFakeTimers();
    const port: PublicLiveDataPort = createPort(() => throwError(() => new Error('unavailable')));
    const facade: PublicLiveForecastFacade = configureFacade(port);

    facade.load('item-1');
    vi.advanceTimersByTime(15 * 60 * 1000);

    expect(port.getParkItemForecast).toHaveBeenCalledTimes(2);
  });

  it('avoids a tight retry loop when an intermediary returns an expired forecast', () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-09-29T14:00:00Z'));
    const port: PublicLiveDataPort = createPort(() => of(createForecast()));
    const facade: PublicLiveForecastFacade = configureFacade(port);

    facade.load('item-1');
    vi.advanceTimersByTime(29_999);

    expect(port.getParkItemForecast).toHaveBeenCalledTimes(1);

    vi.advanceTimersByTime(1);

    expect(port.getParkItemForecast).toHaveBeenCalledTimes(2);
  });

  it('refreshes the current item on demand', () => {
    const port: PublicLiveDataPort = createPort(() => of(createForecast()));
    const facade: PublicLiveForecastFacade = configureFacade(port);

    facade.load('item-1');
    facade.refresh();

    expect(port.getParkItemForecast).toHaveBeenCalledTimes(2);
  });
});

function configureFacade(
  port: PublicLiveDataPort,
  isBrowser: boolean = true
): PublicLiveForecastFacade {
  TestBed.configureTestingModule({
    providers: [
      PublicLiveForecastFacade,
      { provide: PUBLIC_LIVE_DATA_PORT, useValue: port },
      { provide: SsrRuntimeService, useValue: { isBrowserRuntime: (): boolean => isBrowser } }
    ]
  });
  return TestBed.inject(PublicLiveForecastFacade);
}

function createPort(
  getForecast: () => Observable<PublicLiveForecast>
): PublicLiveDataPort {
  return {
    getPark: vi.fn(() => of({} as PublicLiveTarget)),
    getParkItem: vi.fn(() => of({} as PublicLiveTarget)),
    getParkItems: vi.fn(() => of({} as PublicParkLiveItems)),
    getParkItemHistory: vi.fn(() => of({} as PublicLiveHistory)),
    getParkItemForecast: vi.fn(getForecast)
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
