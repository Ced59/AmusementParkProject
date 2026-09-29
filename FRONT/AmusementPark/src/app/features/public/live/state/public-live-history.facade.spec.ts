import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';

import { PublicLiveForecast, PublicLiveHistory, PublicLiveTarget, PublicParkLiveItems } from '@app/models/live-data/public-live.models';
import { SsrRuntimeService } from '@core/ssr/ssr-runtime.service';
import { PUBLIC_LIVE_DATA_PORT, PublicLiveDataPort } from './public-live-data.port';
import { PublicLiveHistoryFacade } from './public-live-history.facade';

describe('PublicLiveHistoryFacade', () => {
  afterEach(() => TestBed.resetTestingModule());

  it('loads descriptive history through the port', () => {
    const history: PublicLiveHistory = createHistory();
    const port: PublicLiveDataPort = createPort(() => of(history));
    const facade: PublicLiveHistoryFacade = configureFacade(port);

    facade.load('item-1');

    expect(port.getParkItemHistory).toHaveBeenCalledWith('item-1');
    expect(facade.state()).toEqual({ kind: 'ready', history });
  });

  it('keeps a disabled endpoint out of the visible page', () => {
    const port: PublicLiveDataPort = createPort(() => throwError(() => new HttpErrorResponse({ status: 404 })));
    const facade: PublicLiveHistoryFacade = configureFacade(port);

    facade.load('item-1');

    expect(facade.state()).toEqual({ kind: 'disabled', history: null });
  });

  it('retries the last requested item after a transient failure', () => {
    const history: PublicLiveHistory = createHistory();
    const responses: Observable<PublicLiveHistory>[] = [
      throwError(() => new HttpErrorResponse({ status: 503 })),
      of(history)
    ];
    const port: PublicLiveDataPort = createPort(() => responses.shift()!);
    const facade: PublicLiveHistoryFacade = configureFacade(port);

    facade.load('item-1');
    expect(facade.state().kind).toBe('error');
    facade.retry();

    expect(port.getParkItemHistory).toHaveBeenCalledTimes(2);
    expect(facade.state()).toEqual({ kind: 'ready', history });
  });

  it('does not perform the historical aggregation during SSR', () => {
    const port: PublicLiveDataPort = createPort(() => of(createHistory()));
    const facade: PublicLiveHistoryFacade = configureFacade(port, false);

    facade.load('item-1');

    expect(port.getParkItemHistory).not.toHaveBeenCalled();
    expect(facade.state().kind).toBe('idle');
  });
});

function configureFacade(port: PublicLiveDataPort, isBrowser: boolean = true): PublicLiveHistoryFacade {
  TestBed.configureTestingModule({
    providers: [
      PublicLiveHistoryFacade,
      { provide: PUBLIC_LIVE_DATA_PORT, useValue: port },
      { provide: SsrRuntimeService, useValue: { isBrowserRuntime: (): boolean => isBrowser } }
    ]
  });
  return TestBed.inject(PublicLiveHistoryFacade);
}

function createPort(
  getHistory: () => Observable<PublicLiveHistory>
): PublicLiveDataPort {
  return {
    getPark: vi.fn(() => of({} as PublicLiveTarget)),
    getParkItem: vi.fn(() => of({} as PublicLiveTarget)),
    getParkItems: vi.fn(() => of({} as PublicParkLiveItems)),
    getParkItemHistory: vi.fn(getHistory),
    getParkItemForecast: vi.fn(() => of({} as PublicLiveForecast))
  };
}

function createHistory(): PublicLiveHistory {
  return {
    targetId: 'item-1',
    displayName: 'Example attraction',
    parkId: 'park-1',
    parkDisplayName: 'Example park',
    fromUtc: '2026-08-30T10:00:00Z',
    toUtc: '2026-09-29T10:00:00Z',
    timeZoneId: 'UTC',
    dataStatus: 'Insufficient',
    expectedObservationCount: 10,
    observationCount: 1,
    usableWaitCount: 1,
    daysCovered: 1,
    comparableDays: 1,
    coveragePercent: 10,
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
      id: 'source',
      displayName: 'Source',
      type: 'AuthorizedAggregator',
      attributionText: 'Source attribution',
      attributionUrl: 'https://example.com/'
    }
  };
}
