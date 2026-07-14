import { HttpContext, HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, RouterStateSnapshot, convertToParamMap } from '@angular/router';
import { Observable, firstValueFrom, of, throwError } from 'rxjs';

import { HistoryArticle, HistoryTimeline } from '@app/models/history/history.models';
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import { HISTORY_DATA_PORT, HistoryDataPort } from './history-data.ports';
import { ResolvedHistoryTimelineRouteData, historyTimelineResolver } from './history-timeline.resolver';

describe('historyTimelineResolver', () => {
  let historyDataPort: jasmine.SpyObj<HistoryDataPort>;
  let ssrHttpStatusService: jasmine.SpyObj<SsrHttpStatusService>;

  beforeEach(() => {
    historyDataPort = jasmine.createSpyObj<HistoryDataPort>('HistoryDataPort', [
      'getParkTimeline',
      'getParkItemTimeline',
      'getArticle'
    ]);
    ssrHttpStatusService = jasmine.createSpyObj<SsrHttpStatusService>('SsrHttpStatusService', ['setNotFound', 'setStatus']);

    historyDataPort.getArticle.and.returnValue(of({} as HistoryArticle));

    TestBed.configureTestingModule({
      providers: [
        { provide: HISTORY_DATA_PORT, useValue: historyDataPort },
        { provide: SsrHttpStatusService, useValue: ssrHttpStatusService }
      ]
    });
  });

  it('loads a park item timeline before route activation', async () => {
    const timeline: HistoryTimeline = createTimeline('ParkItem');
    historyDataPort.getParkItemTimeline.and.returnValue(of(timeline));

    const resolvedData: ResolvedHistoryTimelineRouteData = await resolveTimeline({ id: 'park-1', itemId: 'item-1' });

    expect(resolvedData).toEqual({ timeline, includeParkItems: false, page: 1 });
    expect(historyDataPort.getParkItemTimeline).toHaveBeenCalledOnceWith('item-1', jasmine.objectContaining({
      context: jasmine.any(HttpContext)
    }), 1);
    expect(historyDataPort.getParkTimeline).not.toHaveBeenCalled();
    expect(ssrHttpStatusService.setNotFound).not.toHaveBeenCalled();
  });

  it('falls back to park item events when the park-only timeline is missing', async () => {
    const timeline: HistoryTimeline = createTimeline('Park');
    historyDataPort.getParkTimeline.and.returnValues(
      throwError(() => new HttpErrorResponse({ status: 404 })),
      of(timeline)
    );

    const resolvedData: ResolvedHistoryTimelineRouteData = await resolveTimeline({ id: 'park-1' });

    expect(resolvedData).toEqual({ timeline, includeParkItems: true, page: 1 });
    expect(historyDataPort.getParkTimeline.calls.allArgs()).toEqual([
      ['park-1', false, [], jasmine.objectContaining({ context: jasmine.any(HttpContext) }), 1],
      ['park-1', true, [], jasmine.objectContaining({ context: jasmine.any(HttpContext) }), 1]
    ]);
    expect(ssrHttpStatusService.setNotFound).not.toHaveBeenCalled();
  });

  it('marks transient park item timeline errors as unavailable during SSR', async () => {
    historyDataPort.getParkItemTimeline.and.returnValue(throwError(() => new HttpErrorResponse({ status: 503 })));

    const resolvedData: ResolvedHistoryTimelineRouteData = await resolveTimeline({ id: 'park-1', itemId: 'item-1' });

    expect(resolvedData).toEqual({ timeline: null, includeParkItems: false, page: 1 });
    expect(ssrHttpStatusService.setNotFound).not.toHaveBeenCalled();
    expect(ssrHttpStatusService.setStatus).toHaveBeenCalledOnceWith(503);
  });

  it('does not try the park item fallback when the park timeline fails transiently', async () => {
    historyDataPort.getParkTimeline.and.returnValue(throwError(() => new HttpErrorResponse({ status: 503 })));

    const resolvedData: ResolvedHistoryTimelineRouteData = await resolveTimeline({ id: 'park-1' });

    expect(resolvedData).toEqual({ timeline: null, includeParkItems: false, page: 1 });
    expect(historyDataPort.getParkTimeline).toHaveBeenCalledTimes(1);
    expect(ssrHttpStatusService.setStatus).toHaveBeenCalledOnceWith(503);
  });

  it('loads the requested timeline page from the route parameter', async () => {
    const timeline: HistoryTimeline = createTimeline('Park');
    historyDataPort.getParkTimeline.and.returnValue(of(timeline));

    const resolvedData: ResolvedHistoryTimelineRouteData = await resolveTimeline({ id: 'park-1', page: '2' });

    expect(resolvedData).toEqual({ timeline, includeParkItems: false, page: 2 });
    expect(historyDataPort.getParkTimeline).toHaveBeenCalledOnceWith(
      'park-1',
      false,
      [],
      jasmine.objectContaining({ context: jasmine.any(HttpContext) }),
      2
    );
  });

  it('loads included park item events directly when requested by query string', async () => {
    const timeline: HistoryTimeline = createTimeline('Park');
    historyDataPort.getParkTimeline.and.returnValue(of(timeline));

    const resolvedData: ResolvedHistoryTimelineRouteData = await resolveTimeline({ id: 'park-1' }, { includeParkItems: 'true' });

    expect(resolvedData).toEqual({ timeline, includeParkItems: true, page: 1 });
    expect(historyDataPort.getParkTimeline).toHaveBeenCalledOnceWith(
      'park-1',
      true,
      [],
      jasmine.objectContaining({ context: jasmine.any(HttpContext) }),
      1
    );
  });

  it('marks invalid timeline pages as not found during SSR', async () => {
    const resolvedData: ResolvedHistoryTimelineRouteData = await resolveTimeline({ id: 'park-1', page: 'zero' });

    expect(resolvedData).toEqual({ timeline: null, includeParkItems: false, page: 1 });
    expect(historyDataPort.getParkTimeline).not.toHaveBeenCalled();
    expect(ssrHttpStatusService.setNotFound).toHaveBeenCalled();
  });
});

async function resolveTimeline(params: Record<string, string>, queryParams: Record<string, string> = {}): Promise<ResolvedHistoryTimelineRouteData> {
  const result: Observable<ResolvedHistoryTimelineRouteData> = TestBed.runInInjectionContext((): Observable<ResolvedHistoryTimelineRouteData> => {
    return historyTimelineResolver(createRoute(params, queryParams), {} as RouterStateSnapshot) as Observable<ResolvedHistoryTimelineRouteData>;
  });

  return firstValueFrom(result);
}

function createRoute(params: Record<string, string>, queryParams: Record<string, string>): ActivatedRouteSnapshot {
  return {
    paramMap: convertToParamMap(params),
    queryParamMap: convertToParamMap(queryParams)
  } as ActivatedRouteSnapshot;
}

function createTimeline(entityType: 'Park' | 'ParkItem'): HistoryTimeline {
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
    parkItem: entityType === 'ParkItem' ? {
      id: 'item-1',
      parkId: 'park-1',
      name: 'Le Nitro',
      category: 'Attraction',
      type: 'RollerCoaster',
      latitude: 50.8,
      longitude: 6.8,
      isVisible: true
    } : null,
    includedParkItems: [],
    events: []
  };
}
