import { TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';

import {
  HistoryArticle,
  HistoryTimeline,
} from '@app/models/history/history.models';
import { AnonymousHttpOptions } from '@core/http/auth/anonymous-http-options';
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import { HISTORY_DATA_PORT, HistoryDataPort } from './history-data.ports';
import { HistoryTimelineStateFacade } from './history-timeline-state.facade';
import { FakeHistoryDataPort } from './test-helpers/history-timeline-state.facade/fake-history-data-port';
import { FakeSsrHttpStatusService } from './test-helpers/history-timeline-state.facade/fake-ssr-http-status-service';

function createTimeline(
  entityType: 'Park' | 'ParkItem' | 'StandaloneAttraction' = 'Park',
): HistoryTimeline {
  return {
    entityType,
    park:
      entityType === 'Park'
        ? {
            id: 'park-1',
            name: 'Mirapolis',
            countryCode: 'FR',
            latitude: 49.054,
            longitude: 2.0,
            isVisible: true,
          }
        : null,
    parkItem: null,
    standaloneAttraction:
      entityType === 'StandaloneAttraction'
        ? {
            id: 'standalone-1',
            name: 'Pendolino',
            type: 'RollerCoaster',
            latitude: 46.561236,
            longitude: 13.253481,
            isVisible: true,
            adminReviewStatus: 'Validated',
          }
        : null,
    includedParkItems: [],
    events: [
      {
        event: {
          id: 'auto-event-1',
          key: 'auto-event-1',
          entityType,
          ownerId: entityType === 'Park' ? 'park-1' : entityType === 'ParkItem' ? 'item-1' : 'standalone-1',
          parkId: entityType === 'Park' || entityType === 'ParkItem' ? 'park-1' : null,
          parkItemId: entityType === 'ParkItem' ? 'item-1' : null,
          contextParkId: entityType === 'ParkItem' ? 'park-1' : null,
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
          updatedAtUtc: '2026-01-01T00:00:00Z',
        },
        contextPark: null,
        parkItem: null,
        mainImage: null,
      },
    ],
  };
}

function configureFacade(): {
  facade: HistoryTimelineStateFacade;
  historyDataPort: FakeHistoryDataPort;
  ssrStatusService: FakeSsrHttpStatusService;
} {
  const historyDataPort: FakeHistoryDataPort = new FakeHistoryDataPort();
  const ssrStatusService: FakeSsrHttpStatusService =
    new FakeSsrHttpStatusService();

  TestBed.configureTestingModule({
    providers: [
      HistoryTimelineStateFacade,
      { provide: HISTORY_DATA_PORT, useValue: historyDataPort },
      { provide: SsrHttpStatusService, useValue: ssrStatusService },
    ],
  });

  return {
    facade: TestBed.inject(HistoryTimelineStateFacade),
    historyDataPort,
    ssrStatusService,
  };
}

describe('HistoryTimelineStateFacade', () => {
  beforeEach(() => {
    TestBed.resetTestingModule();
  });

  it('loads a standalone attraction timeline', () => {
    const context = configureFacade();

    context.facade.loadStandaloneAttractionTimeline('standalone-1', 2);

    expect(context.historyDataPort.standaloneTimelineCalls).toEqual([
      { standaloneAttractionId: 'standalone-1', page: 2 },
    ]);
    expect(context.facade.state().kind).toBe('ready');
    expect(context.facade.timeline()?.standaloneAttraction?.name).toBe('Pendolino');
  });

  it('falls back to park item events when the park-only timeline is missing', () => {
    const context = configureFacade();
    context.historyDataPort.parkTimelineResponses$ = [
      throwError(() => ({ status: 404 })),
      of(createTimeline()),
    ];

    context.facade.loadParkTimeline('park-1', false);

    expect(context.historyDataPort.parkTimelineCalls).toEqual([
      { parkId: 'park-1', includeParkItems: false, parkItemIds: [], page: 1 },
      { parkId: 'park-1', includeParkItems: true, parkItemIds: [], page: 1 },
    ]);
    expect(context.facade.state().kind).toBe('ready');
    expect(context.facade.includeParkItems()).toBe(true);
    expect(context.ssrStatusService.notFoundCallCount).toBe(0);
  });

  it('sets SSR unavailable when the timeline lookup fails transiently', () => {
    const context = configureFacade();
    context.historyDataPort.parkTimelineResponses$ = [
      throwError(() => ({ status: 503 })),
    ];

    context.facade.loadParkTimeline('park-1', false);

    expect(context.facade.state().kind).toBe('error');
    expect(context.ssrStatusService.notFoundCallCount).toBe(0);
    expect(context.ssrStatusService.statusCodes).toEqual([503]);
  });

  it('keeps the previous timeline visible when a subsequent lookup fails transiently', () => {
    const context = configureFacade();
    const previousTimeline: HistoryTimeline = createTimeline();
    context.historyDataPort.parkTimelineResponses$ = [
      of(previousTimeline),
      throwError(() => ({ status: 503 })),
    ];

    context.facade.loadParkTimeline('park-1', false, 1);
    context.facade.loadParkTimeline('park-1', true, 2);

    expect(context.facade.state().kind).toBe('ready');
    expect(context.facade.state().data).toBe(previousTimeline);
    expect(context.ssrStatusService.statusCodes).toEqual([503]);
  });

  it('loads the requested page and resets to page one when park item inclusion changes', () => {
    const context = configureFacade();

    context.facade.loadParkTimeline('park-1', false, 2);
    context.facade.setIncludeParkItems(true);

    expect(context.historyDataPort.parkTimelineCalls).toEqual([
      { parkId: 'park-1', includeParkItems: false, parkItemIds: [], page: 2 },
      { parkId: 'park-1', includeParkItems: true, parkItemIds: [], page: 1 },
    ]);
  });
});
