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
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import { HISTORY_DATA_PORT, HistoryDataPort } from './history-data.ports';
import {
  ResolvedHistoryTimelineRouteData,
  historyTimelineResolver,
} from './history-timeline.resolver';

describe('historyTimelineResolver', () => {
  let historyDataPort: MockedObject<HistoryDataPort>;
  let ssrHttpStatusService: MockedObject<SsrHttpStatusService>;

  beforeEach(() => {
    historyDataPort = {
      getParkTimeline: vi.fn().mockName('HistoryDataPort.getParkTimeline'),
      getParkItemTimeline: vi.fn().mockName('HistoryDataPort.getParkItemTimeline'),
      getStandaloneAttractionTimeline: vi.fn().mockName('HistoryDataPort.getStandaloneAttractionTimeline'),
      getArticle: vi.fn().mockName('HistoryDataPort.getArticle'),
    } as unknown as MockedObject<HistoryDataPort>;
    ssrHttpStatusService = {
      setNotFound: vi.fn().mockName('SsrHttpStatusService.setNotFound'),
      setStatus: vi.fn().mockName('SsrHttpStatusService.setStatus'),
    } as unknown as MockedObject<SsrHttpStatusService>;

    historyDataPort.getArticle.mockReturnValue(of({} as HistoryArticle));

    TestBed.configureTestingModule({
      providers: [
        { provide: HISTORY_DATA_PORT, useValue: historyDataPort },
        { provide: SsrHttpStatusService, useValue: ssrHttpStatusService },
      ],
    });
  });

  it('loads a standalone attraction timeline before route activation', async () => {
    const timeline: HistoryTimeline = createTimeline('StandaloneAttraction');
    historyDataPort.getStandaloneAttractionTimeline.mockReturnValue(of(timeline));

    const resolvedData: ResolvedHistoryTimelineRouteData = await resolveTimeline({ standaloneAttractionId: 'standalone-1' });

    expect(resolvedData).toEqual({ timeline, includeParkItems: false, page: 1 });
    expect(historyDataPort.getStandaloneAttractionTimeline).toHaveBeenCalledWith(
      'standalone-1',
      expect.objectContaining({ context: expect.any(HttpContext) }),
      1,
    );
    expect(historyDataPort.getParkTimeline).not.toHaveBeenCalled();
    expect(historyDataPort.getParkItemTimeline).not.toHaveBeenCalled();
  });

  it('loads a park item timeline before route activation', async () => {
    const timeline: HistoryTimeline = createTimeline('ParkItem');
    historyDataPort.getParkItemTimeline.mockReturnValue(of(timeline));

    const resolvedData: ResolvedHistoryTimelineRouteData =
      await resolveTimeline({ id: 'park-1', itemId: 'item-1' });

    expect(resolvedData).toEqual({
      timeline,
      includeParkItems: false,
      page: 1,
    });
    expect(historyDataPort.getParkItemTimeline).toHaveBeenCalledTimes(1);
    expect(historyDataPort.getParkItemTimeline).toHaveBeenCalledWith(
      'item-1',
      expect.objectContaining({
        context: expect.any(HttpContext),
      }),
      1,
    );
    expect(historyDataPort.getParkTimeline).not.toHaveBeenCalled();
    expect(ssrHttpStatusService.setNotFound).not.toHaveBeenCalled();
  });

  it('falls back to park item events when the park-only timeline is missing', async () => {
    const timeline: HistoryTimeline = createTimeline('Park');
    historyDataPort.getParkTimeline
      .mockReturnValueOnce(
        throwError(() => new HttpErrorResponse({ status: 404 })),
      )
      .mockReturnValueOnce(of(timeline));

    const resolvedData: ResolvedHistoryTimelineRouteData =
      await resolveTimeline({ id: 'park-1' });

    expect(resolvedData).toEqual({ timeline, includeParkItems: true, page: 1 });
    expect(vi.mocked(historyDataPort.getParkTimeline).mock.calls).toEqual([
      [
        'park-1',
        false,
        [],
        expect.objectContaining({ context: expect.any(HttpContext) }),
        1,
      ],
      [
        'park-1',
        true,
        [],
        expect.objectContaining({ context: expect.any(HttpContext) }),
        1,
      ],
    ]);
    expect(ssrHttpStatusService.setNotFound).not.toHaveBeenCalled();
  });

  it('marks transient park item timeline errors as unavailable during SSR', async () => {
    historyDataPort.getParkItemTimeline.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 503 })),
    );

    const resolvedData: ResolvedHistoryTimelineRouteData =
      await resolveTimeline({ id: 'park-1', itemId: 'item-1' });

    expect(resolvedData).toEqual({
      timeline: null,
      includeParkItems: false,
      page: 1,
    });
    expect(ssrHttpStatusService.setNotFound).not.toHaveBeenCalled();
    expect(ssrHttpStatusService.setStatus).toHaveBeenCalledTimes(1);
    expect(ssrHttpStatusService.setStatus).toHaveBeenCalledWith(503);
  });

  it('does not try the park item fallback when the park timeline fails transiently', async () => {
    historyDataPort.getParkTimeline.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 503 })),
    );

    const resolvedData: ResolvedHistoryTimelineRouteData =
      await resolveTimeline({ id: 'park-1' });

    expect(resolvedData).toEqual({
      timeline: null,
      includeParkItems: false,
      page: 1,
    });
    expect(historyDataPort.getParkTimeline).toHaveBeenCalledTimes(1);
    expect(ssrHttpStatusService.setStatus).toHaveBeenCalledTimes(1);
    expect(ssrHttpStatusService.setStatus).toHaveBeenCalledWith(503);
  });

  it('loads the requested timeline page from the route parameter', async () => {
    const timeline: HistoryTimeline = createTimeline('Park');
    historyDataPort.getParkTimeline.mockReturnValue(of(timeline));

    const resolvedData: ResolvedHistoryTimelineRouteData =
      await resolveTimeline({ id: 'park-1', page: '2' });

    expect(resolvedData).toEqual({
      timeline,
      includeParkItems: false,
      page: 2,
    });
    expect(historyDataPort.getParkTimeline).toHaveBeenCalledTimes(1);
    expect(historyDataPort.getParkTimeline).toHaveBeenCalledWith(
      'park-1',
      false,
      [],
      expect.objectContaining({ context: expect.any(HttpContext) }),
      2,
    );
  });

  it('loads included park item events directly when requested by query string', async () => {
    const timeline: HistoryTimeline = createTimeline('Park');
    historyDataPort.getParkTimeline.mockReturnValue(of(timeline));

    const resolvedData: ResolvedHistoryTimelineRouteData =
      await resolveTimeline({ id: 'park-1' }, { includeParkItems: 'true' });

    expect(resolvedData).toEqual({ timeline, includeParkItems: true, page: 1 });
    expect(historyDataPort.getParkTimeline).toHaveBeenCalledTimes(1);
    expect(historyDataPort.getParkTimeline).toHaveBeenCalledWith(
      'park-1',
      true,
      [],
      expect.objectContaining({ context: expect.any(HttpContext) }),
      1,
    );
  });

  it('marks invalid timeline pages as not found during SSR', async () => {
    const resolvedData: ResolvedHistoryTimelineRouteData =
      await resolveTimeline({ id: 'park-1', page: 'zero' });

    expect(resolvedData).toEqual({
      timeline: null,
      includeParkItems: false,
      page: 1,
    });
    expect(historyDataPort.getParkTimeline).not.toHaveBeenCalled();
    expect(ssrHttpStatusService.setNotFound).toHaveBeenCalled();
  });
});

async function resolveTimeline(
  params: Record<string, string>,
  queryParams: Record<string, string> = {},
): Promise<ResolvedHistoryTimelineRouteData> {
  const result: Observable<ResolvedHistoryTimelineRouteData> =
    TestBed.runInInjectionContext(
      (): Observable<ResolvedHistoryTimelineRouteData> => {
        return historyTimelineResolver(
          createRoute(params, queryParams),
          {} as RouterStateSnapshot,
        ) as Observable<ResolvedHistoryTimelineRouteData>;
      },
    );

  return firstValueFrom(result);
}

function createRoute(
  params: Record<string, string>,
  queryParams: Record<string, string>,
): ActivatedRouteSnapshot {
  return {
    paramMap: convertToParamMap(params),
    queryParamMap: convertToParamMap(queryParams),
  } as ActivatedRouteSnapshot;
}

function createTimeline(entityType: 'Park' | 'ParkItem' | 'StandaloneAttraction'): HistoryTimeline {
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
    parkItem:
      entityType === 'ParkItem'
        ? {
            id: 'item-1',
            parkId: 'park-1',
            name: 'Le Nitro',
            category: 'Attraction',
            type: 'RollerCoaster',
            latitude: 50.8,
            longitude: 6.8,
            isVisible: true,
          }
        : null,
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
    events: [],
  };
}
