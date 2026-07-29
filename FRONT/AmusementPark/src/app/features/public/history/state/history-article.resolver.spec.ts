import type { MockedObject } from 'vitest';
import { HttpContext, HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import {
  ActivatedRouteSnapshot,
  RouterStateSnapshot,
  convertToParamMap,
} from '@angular/router';
import { Observable, firstValueFrom, of, throwError } from 'rxjs';

import {
  HistoryArticle,
  HistoryTimeline,
} from '@app/models/history/history.models';
import { AnonymousHttpOptions } from '@core/http/auth/anonymous-http-options';
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import { HISTORY_DATA_PORT, HistoryDataPort } from './history-data.ports';
import { historyArticleResolver } from './history-article.resolver';

describe('historyArticleResolver', () => {
  let historyDataPort: MockedObject<HistoryDataPort>;
  let ssrHttpStatusService: MockedObject<SsrHttpStatusService>;

  beforeEach(() => {
    historyDataPort = {
      getParkTimeline: vi.fn().mockName('HistoryDataPort.getParkTimeline'),
      getParkItemTimeline: vi
        .fn()
        .mockName('HistoryDataPort.getParkItemTimeline'),
      getArticle: vi.fn().mockName('HistoryDataPort.getArticle'),
    } as unknown as MockedObject<HistoryDataPort>;
    ssrHttpStatusService = {
      setNotFound: vi.fn().mockName('SsrHttpStatusService.setNotFound'),
      setStatus: vi.fn().mockName('SsrHttpStatusService.setStatus'),
    } as unknown as MockedObject<SsrHttpStatusService>;

    historyDataPort.getParkTimeline.mockReturnValue(of({} as HistoryTimeline));
    historyDataPort.getParkItemTimeline.mockReturnValue(
      of({} as HistoryTimeline),
    );

    TestBed.configureTestingModule({
      providers: [
        { provide: HISTORY_DATA_PORT, useValue: historyDataPort },
        { provide: SsrHttpStatusService, useValue: ssrHttpStatusService },
      ],
    });
  });

  it('loads the history article before route activation', async () => {
    const article: HistoryArticle = createArticle();
    historyDataPort.getArticle.mockReturnValue(of(article));

    const resolvedArticle: HistoryArticle | null =
      await resolveArticle('event-1');

    expect(resolvedArticle).toBe(article);
    expect(historyDataPort.getArticle).toHaveBeenCalledTimes(1);
    expect(historyDataPort.getArticle).toHaveBeenCalledWith(
      'event-1',
      expect.objectContaining({
        context: expect.any(HttpContext),
      }),
    );
    expect(ssrHttpStatusService.setNotFound).not.toHaveBeenCalled();
  });

  it('marks missing articles as not found during SSR', async () => {
    historyDataPort.getArticle.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 404 })),
    );

    const resolvedArticle: HistoryArticle | null =
      await resolveArticle('missing-event');

    expect(resolvedArticle).toBeNull();
    expect(ssrHttpStatusService.setNotFound).toHaveBeenCalled();
  });

  it('marks transient article load errors as unavailable during SSR', async () => {
    historyDataPort.getArticle.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 503 })),
    );

    const resolvedArticle: HistoryArticle | null =
      await resolveArticle('event-1');

    expect(resolvedArticle).toBeNull();
    expect(ssrHttpStatusService.setNotFound).not.toHaveBeenCalled();
    expect(ssrHttpStatusService.setStatus).toHaveBeenCalledTimes(1);
    expect(ssrHttpStatusService.setStatus).toHaveBeenCalledWith(503);
  });

  it('does not call the API when the route has no event id', async () => {
    const resolvedArticle: HistoryArticle | null = await resolveArticle(null);

    expect(resolvedArticle).toBeNull();
    expect(historyDataPort.getArticle).not.toHaveBeenCalled();
    expect(ssrHttpStatusService.setNotFound).toHaveBeenCalled();
  });
});

async function resolveArticle(
  eventId: string | null,
): Promise<HistoryArticle | null> {
  const result: Observable<HistoryArticle | null> =
    TestBed.runInInjectionContext((): Observable<HistoryArticle | null> => {
      return historyArticleResolver(
        createRoute(eventId),
        {} as RouterStateSnapshot,
      ) as Observable<HistoryArticle | null>;
    });

  return firstValueFrom(result);
}

function createRoute(eventId: string | null): ActivatedRouteSnapshot {
  return {
    paramMap: convertToParamMap(eventId ? { eventId } : {}),
  } as ActivatedRouteSnapshot;
}

function createArticle(): HistoryArticle {
  return {
    event: {
      id: 'event-1',
      key: 'event-1',
      entityType: 'ParkItem',
      ownerId: 'item-1',
      parkId: 'park-1',
      parkItemId: 'item-1',
      contextParkId: 'park-1',
      year: 2026,
      month: 7,
      day: 4,
      datePrecision: 'Day',
      eventType: 'Incident',
      isMajor: true,
      isVisible: true,
      slug: 'incident',
      titles: [],
      summaries: [],
      mainImageId: 'image-1',
      relatedParkIds: [],
      relatedParkItemIds: [],
      sources: [],
      article: {
        slug: 'incident',
        titles: [{ languageCode: 'fr', value: 'Incident sur Le Nitro' }],
        subtitles: [],
        summaries: [],
        mainImageId: 'image-1',
        blocks: [],
        sources: [],
        isPublished: true,
      },
      createdAtUtc: '2026-07-04T00:00:00Z',
      updatedAtUtc: '2026-07-04T00:00:00Z',
    },
    park: null,
    parkItem: null,
    contextPark: null,
    mainImage: null,
  };
}
