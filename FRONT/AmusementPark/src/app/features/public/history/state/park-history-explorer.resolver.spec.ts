import type { MockedObject } from 'vitest';
import { HttpContext, HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, RouterStateSnapshot, convertToParamMap } from '@angular/router';
import { Observable, firstValueFrom, of, throwError } from 'rxjs';

import { PublicParkHistoricalSnapshot, PublicParkHistoricalTimeline } from '@app/models/history/public-park-history.models';
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import { HISTORY_DATA_PORT, HistoryDataPort } from './history-data.ports';
import {
  ResolvedParkHistoricalSnapshotRouteData,
  ResolvedParkHistoryTimelineRouteData,
  parkHistoricalSnapshotResolver,
  parkHistoryTimelineResolver
} from './park-history-explorer.resolver';

describe('park history explorer resolvers', () => {
  let historyDataPort: MockedObject<HistoryDataPort>;
  let ssrStatusService: MockedObject<SsrHttpStatusService>;

  beforeEach(() => {
    historyDataPort = {
      getPublicParkTimeline: vi.fn().mockName('HistoryDataPort.getPublicParkTimeline'),
      getPublicParkSnapshot: vi.fn().mockName('HistoryDataPort.getPublicParkSnapshot')
    } as unknown as MockedObject<HistoryDataPort>;
    ssrStatusService = {
      setNotFound: vi.fn().mockName('SsrHttpStatusService.setNotFound'),
      setStatus: vi.fn().mockName('SsrHttpStatusService.setStatus')
    } as unknown as MockedObject<SsrHttpStatusService>;

    TestBed.configureTestingModule({
      providers: [
        { provide: HISTORY_DATA_PORT, useValue: historyDataPort },
        { provide: SsrHttpStatusService, useValue: ssrStatusService }
      ]
    });
  });

  it('loads the requested canonical timeline page anonymously', async () => {
    const timeline: PublicParkHistoricalTimeline = createTimeline();
    historyDataPort.getPublicParkTimeline.mockReturnValue(of(timeline));

    const result: ResolvedParkHistoryTimelineRouteData = await resolveTimeline({ id: 'park-1', page: '2' });

    expect(result).toEqual({ timeline, page: 2 });
    expect(historyDataPort.getPublicParkTimeline).toHaveBeenCalledWith(
      'park-1',
      expect.objectContaining({ context: expect.any(HttpContext) }),
      2,
      25
    );
  });

  it('loads an annual snapshot from optional date parameters without an unrelated timeline page', async () => {
    const snapshot: PublicParkHistoricalSnapshot = createSnapshot();
    historyDataPort.getPublicParkSnapshot.mockReturnValue(of(snapshot));

    const result: ResolvedParkHistoricalSnapshotRouteData = await resolveSnapshot(
      { id: 'park-1', year: '1998' },
      { month: '7', day: '12' }
    );

    expect(result).toEqual({ snapshot, year: 1998, month: 7, day: 12 });
    expect(historyDataPort.getPublicParkSnapshot).toHaveBeenCalledWith(
      'park-1',
      1998,
      7,
      12,
      expect.objectContaining({ context: expect.any(HttpContext) })
    );
    expect(historyDataPort.getPublicParkTimeline).not.toHaveBeenCalled();
  });

  it('rejects invalid snapshot dates before calling the API', async () => {
    const result: ResolvedParkHistoricalSnapshotRouteData = await resolveSnapshot(
      { id: 'park-1', year: '98' },
      { day: '12' }
    );

    expect(result.snapshot).toBeNull();
    expect(historyDataPort.getPublicParkSnapshot).not.toHaveBeenCalled();
    expect(ssrStatusService.setNotFound).toHaveBeenCalledTimes(1);
  });

  it('publishes a transient snapshot failure as an SSR 503', async () => {
    historyDataPort.getPublicParkSnapshot.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 503 }))
    );
    const result: ResolvedParkHistoricalSnapshotRouteData = await resolveSnapshot({ id: 'park-1', year: '1998' });

    expect(result.snapshot).toBeNull();
    expect(ssrStatusService.setStatus).toHaveBeenCalledWith(503);
  });
});

async function resolveTimeline(params: Record<string, string>): Promise<ResolvedParkHistoryTimelineRouteData> {
  const result: Observable<ResolvedParkHistoryTimelineRouteData> = TestBed.runInInjectionContext(
    () => parkHistoryTimelineResolver(createRoute(params), {} as RouterStateSnapshot) as Observable<ResolvedParkHistoryTimelineRouteData>
  );
  return firstValueFrom(result);
}

async function resolveSnapshot(
  params: Record<string, string>,
  queryParams: Record<string, string> = {}
): Promise<ResolvedParkHistoricalSnapshotRouteData> {
  const result: Observable<ResolvedParkHistoricalSnapshotRouteData> = TestBed.runInInjectionContext(
    () => parkHistoricalSnapshotResolver(createRoute(params, queryParams), {} as RouterStateSnapshot) as Observable<ResolvedParkHistoricalSnapshotRouteData>
  );
  return firstValueFrom(result);
}

function createRoute(params: Record<string, string>, queryParams: Record<string, string> = {}): ActivatedRouteSnapshot {
  return {
    paramMap: convertToParamMap(params),
    queryParamMap: convertToParamMap(queryParams)
  } as ActivatedRouteSnapshot;
}

function createTimeline(): PublicParkHistoricalTimeline {
  return {
    parkId: 'park-1',
    parkName: 'Example Park',
    events: [],
    pagination: { currentPage: 1, itemsPerPage: 25, totalItems: 0, totalPages: 1 }
  };
}

function createSnapshot(): PublicParkHistoricalSnapshot {
  return {
    parkId: 'park-1',
    parkName: 'Example Park',
    requestedInstant: { year: 1998, precision: 'Year' },
    subjects: [],
    coverage: {
      totalSubjectCount: 0,
      reliablePeriodSubjectCount: 0,
      partialPeriodSubjectCount: 0,
      undatedSubjectCount: 0,
      name: { documentedSubjectCount: 0, applicableSubjectCount: 0, percentage: 0, isComplete: false },
      zone: { documentedSubjectCount: 0, applicableSubjectCount: 0, percentage: 0, isComplete: false },
      status: 'Partial'
    },
    ambiguities: [],
    methodologyVersion: '1.0'
  };
}
