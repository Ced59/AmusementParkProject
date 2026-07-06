import { TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';

import { HistoryArticle, HistoryTimeline } from '@app/models/history/history.models';
import { AnonymousHttpOptions } from '@core/http/auth/anonymous-http-options';
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import { HISTORY_DATA_PORT, HistoryDataPort } from './history-data.ports';
import { HistoryTimelineStateFacade } from './history-timeline-state.facade';

class FakeHistoryDataPort implements HistoryDataPort {
  public parkTimelineResponses$: Observable<HistoryTimeline>[] = [of(createTimeline())];
  public readonly parkTimelineCalls: { parkId: string; includeParkItems?: boolean; parkItemIds?: readonly string[] }[] = [];

  getParkTimeline(
    parkId: string,
    includeParkItems?: boolean,
    parkItemIds?: readonly string[],
    _options?: AnonymousHttpOptions
  ): Observable<HistoryTimeline> {
    this.parkTimelineCalls.push({ parkId, includeParkItems, parkItemIds });
    return this.parkTimelineResponses$.shift() ?? of(createTimeline());
  }

  getParkItemTimeline(_parkItemId: string, _options?: AnonymousHttpOptions): Observable<HistoryTimeline> {
    return of(createTimeline('ParkItem'));
  }

  getArticle(_eventId: string, _options?: AnonymousHttpOptions): Observable<HistoryArticle> {
    throw new Error('Not implemented in this spec');
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

function createTimeline(entityType: 'Park' | 'ParkItem' = 'Park'): HistoryTimeline {
  return {
    entityType,
    park: entityType === 'Park' ? {
      id: 'park-1',
      name: 'Mirapolis',
      countryCode: 'FR',
      latitude: 49.054,
      longitude: 2.0,
      isVisible: true
    } : null,
    parkItem: null,
    includedParkItems: [],
    events: [{
      event: {
        id: 'auto-event-1',
        key: 'auto-event-1',
        entityType,
        ownerId: entityType === 'Park' ? 'park-1' : 'item-1',
        parkId: 'park-1',
        parkItemId: entityType === 'ParkItem' ? 'item-1' : null,
        contextParkId: 'park-1',
        year: 1988,
        month: null,
        day: null,
        datePrecision: 'Year',
        eventType: 'Opening',
        isMajor: false,
        isVisible: true,
        slug: null,
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
    }]
  };
}

function configureFacade(): {
  facade: HistoryTimelineStateFacade;
  historyDataPort: FakeHistoryDataPort;
  ssrStatusService: FakeSsrHttpStatusService;
} {
  const historyDataPort: FakeHistoryDataPort = new FakeHistoryDataPort();
  const ssrStatusService: FakeSsrHttpStatusService = new FakeSsrHttpStatusService();

  TestBed.configureTestingModule({
    providers: [
      HistoryTimelineStateFacade,
      { provide: HISTORY_DATA_PORT, useValue: historyDataPort },
      { provide: SsrHttpStatusService, useValue: ssrStatusService }
    ]
  });

  return {
    facade: TestBed.inject(HistoryTimelineStateFacade),
    historyDataPort,
    ssrStatusService
  };
}

describe('HistoryTimelineStateFacade', () => {
  beforeEach(() => {
    TestBed.resetTestingModule();
  });

  it('falls back to park item events when the park-only timeline is missing', () => {
    const context = configureFacade();
    context.historyDataPort.parkTimelineResponses$ = [
      throwError(() => ({ status: 404 })),
      of(createTimeline())
    ];

    context.facade.loadParkTimeline('park-1', false);

    expect(context.historyDataPort.parkTimelineCalls).toEqual([
      { parkId: 'park-1', includeParkItems: false, parkItemIds: [] },
      { parkId: 'park-1', includeParkItems: true, parkItemIds: [] }
    ]);
    expect(context.facade.state().kind).toBe('ready');
    expect(context.facade.includeParkItems()).toBeTrue();
    expect(context.ssrStatusService.notFoundCallCount).toBe(0);
  });

  it('sets SSR unavailable when the timeline lookup fails transiently', () => {
    const context = configureFacade();
    context.historyDataPort.parkTimelineResponses$ = [
      throwError(() => ({ status: 503 }))
    ];

    context.facade.loadParkTimeline('park-1', false);

    expect(context.facade.state().kind).toBe('error');
    expect(context.ssrStatusService.notFoundCallCount).toBe(0);
    expect(context.ssrStatusService.statusCodes).toEqual([503]);
  });
});
