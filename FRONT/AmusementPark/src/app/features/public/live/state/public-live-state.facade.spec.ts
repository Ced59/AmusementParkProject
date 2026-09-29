import { DOCUMENT } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { Observable, Subject, of, throwError } from 'rxjs';

import { PublicLiveTarget, PublicParkLiveItems } from '@app/models/live-data/public-live.models';
import { SsrRuntimeService } from '@core/ssr/ssr-runtime.service';
import { PUBLIC_LIVE_DATA_PORT, PublicLiveDataPort } from './public-live-data.port';
import { PublicLiveStateFacade } from './public-live-state.facade';

describe('PublicLiveStateFacade', () => {
  afterEach(() => {
    TestBed.resetTestingModule();
    vi.useRealTimers();
    vi.restoreAllMocks();
  });

  it('loads a park and its item states through the live-data port', () => {
    const port: PublicLiveDataPort = createPort();
    const facade: PublicLiveStateFacade = configureFacade(port, true);

    facade.watchPark('park-1');

    expect(port.getPark).toHaveBeenCalledWith('park-1');
    expect(port.getParkItems).toHaveBeenCalledWith('park-1');
    expect(facade.state().kind).toBe('ready');
    expect(facade.state().items.map((item: PublicLiveTarget) => item.targetId)).toEqual(['item-1']);
  });

  it('stops requesting while the tab is hidden and refreshes when it becomes visible', () => {
    const port: PublicLiveDataPort = createPort();
    const documentRef: Document = document;
    const hiddenSpy = vi.spyOn(documentRef, 'hidden', 'get').mockReturnValue(false);
    const facade: PublicLiveStateFacade = configureFacade(port, true);

    facade.watchParkItem('item-1');
    expect(port.getParkItem).toHaveBeenCalledTimes(1);

    hiddenSpy.mockReturnValue(true);
    documentRef.dispatchEvent(new Event('visibilitychange'));
    facade.refresh();
    expect(port.getParkItem).toHaveBeenCalledTimes(1);

    hiddenSpy.mockReturnValue(false);
    documentRef.dispatchEvent(new Event('visibilitychange'));
    expect(port.getParkItem).toHaveBeenCalledTimes(2);
  });

  it('keeps the pilot invisible when public live reads are disabled', () => {
    vi.useFakeTimers();
    const port: PublicLiveDataPort = createPort({
      getParkItem: () => throwError(() => new HttpErrorResponse({ status: 404 }))
    });
    const facade: PublicLiveStateFacade = configureFacade(port, true);

    facade.watchParkItem('item-1');

    expect(facade.state().kind).toBe('disabled');
    vi.advanceTimersByTime(600_000);
    facade.refresh();
    expect(port.getParkItem).toHaveBeenCalledTimes(1);
  });

  it('retries a temporary operational stop and restores live data without a page reload', () => {
    vi.useFakeTimers();
    let requestCount: number = 0;
    const port: PublicLiveDataPort = createPort({
      getParkItem: () => {
        requestCount++;
        return requestCount === 1
          ? throwError(() => new HttpErrorResponse({
            status: 404,
            error: {
              status: 404,
              title: 'Not Found',
              errorCode: 'live-data.public-read.temporarily-suspended'
            }
          }))
          : of(createTarget());
      }
    });
    const facade: PublicLiveStateFacade = configureFacade(port, true);

    facade.watchParkItem('item-1');

    expect(facade.state().kind).toBe('disabled');
    vi.advanceTimersByTime(300_000);

    expect(port.getParkItem).toHaveBeenCalledTimes(2);
    expect(facade.state().kind).toBe('ready');
  });

  it('keeps the offline warning when an in-flight request succeeds after disconnection', () => {
    const response: Subject<PublicLiveTarget> = new Subject<PublicLiveTarget>();
    const onlineSpy = vi.spyOn(navigator, 'onLine', 'get').mockReturnValue(true);
    const port: PublicLiveDataPort = createPort({ getParkItem: () => response.asObservable() });
    const facade: PublicLiveStateFacade = configureFacade(port, true);

    facade.watchParkItem('item-1');
    onlineSpy.mockReturnValue(false);
    window.dispatchEvent(new Event('offline'));
    response.next(createTarget());
    response.complete();

    expect(facade.state().kind).toBe('ready');
    expect(facade.state().isOnline).toBe(false);
  });

  it('expires current operational facts locally while the browser is offline', () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-09-29T10:00:00Z'));
    const onlineSpy = vi.spyOn(navigator, 'onLine', 'get').mockReturnValue(true);
    const target: PublicLiveTarget = createTarget({ expiresAtUtc: '2026-09-29T10:00:30Z' });
    const port: PublicLiveDataPort = createPort({ getParkItem: () => of(target) });
    const facade: PublicLiveStateFacade = configureFacade(port, true);

    facade.watchParkItem('item-1');
    onlineSpy.mockReturnValue(false);
    window.dispatchEvent(new Event('offline'));
    vi.advanceTimersByTime(30_001);

    expect(facade.state().target?.availability).toBe('Expired');
    expect(facade.state().target?.freshness).toBe('Expired');
    expect(facade.state().isOnline).toBe(false);
  });

  it('does not restore expired facts when the refresh triggered at expiry fails', () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-09-29T10:00:00Z'));
    const target: PublicLiveTarget = createTarget({ expiresAtUtc: '2026-09-29T10:00:30Z' });
    let requestCount: number = 0;
    const port: PublicLiveDataPort = createPort({
      getParkItem: () => {
        requestCount++;
        return requestCount === 1
          ? of(target)
          : throwError(() => new HttpErrorResponse({ status: 503 }));
      }
    });
    const facade: PublicLiveStateFacade = configureFacade(port, true);

    facade.watchParkItem('item-1');
    vi.advanceTimersByTime(30_001);

    expect(facade.state().target?.availability).toBe('Expired');
    expect(facade.state().refreshFailed).toBe(true);
  });

  it('advances the displayed observation age locally while offline', () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-09-29T10:00:00Z'));
    const onlineSpy = vi.spyOn(navigator, 'onLine', 'get').mockReturnValue(true);
    const port: PublicLiveDataPort = createPort();
    const facade: PublicLiveStateFacade = configureFacade(port, true);

    facade.watchParkItem('item-1');
    onlineSpy.mockReturnValue(false);
    window.dispatchEvent(new Event('offline'));
    vi.advanceTimersByTime(60_001);

    expect(facade.state().target?.ageSeconds).toBe(120);
    expect(facade.state().isOnline).toBe(false);
  });

  it('does not poll during server-side rendering', () => {
    const port: PublicLiveDataPort = createPort();
    const facade: PublicLiveStateFacade = configureFacade(port, false);

    facade.watchPark('park-1');

    expect(port.getPark).not.toHaveBeenCalled();
    expect(facade.state().kind).toBe('idle');
  });
});

interface PortOverrides {
  readonly getParkItem?: (itemId: string) => Observable<PublicLiveTarget>;
}

function configureFacade(port: PublicLiveDataPort, isBrowser: boolean): PublicLiveStateFacade {
  TestBed.configureTestingModule({
    providers: [
      PublicLiveStateFacade,
      { provide: PUBLIC_LIVE_DATA_PORT, useValue: port },
      {
        provide: SsrRuntimeService,
        useValue: {
          isBrowserRuntime: (): boolean => isBrowser
        }
      },
      { provide: DOCUMENT, useValue: document }
    ]
  });
  return TestBed.inject(PublicLiveStateFacade);
}

function createPort(overrides: PortOverrides = {}): PublicLiveDataPort {
  const target: PublicLiveTarget = createTarget();
  const parkTarget: PublicLiveTarget = { ...target, targetId: 'park-1', targetType: 'Park' };
  return {
    getPark: vi.fn(() => of(parkTarget)),
    getParkItem: vi.fn(overrides.getParkItem ?? (() => of(target))),
    getParkItems: vi.fn(() => of({
      parkId: 'park-1',
      parkDisplayName: 'Example park',
      asOfUtc: target.asOfUtc,
      items: [target]
    } satisfies PublicParkLiveItems))
  };
}

interface TargetOverrides {
  readonly expiresAtUtc?: string | null;
}

function createTarget(overrides: TargetOverrides = {}): PublicLiveTarget {
  return {
    targetId: 'item-1',
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
    expiresAtUtc: overrides.expiresAtUtc === undefined
      ? '2026-09-29T10:04:00Z'
      : overrides.expiresAtUtc,
    source: null,
    confidence: 'High'
  };
}
