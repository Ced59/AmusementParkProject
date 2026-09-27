import { inject } from '@angular/core';
import { ActivatedRouteSnapshot, ResolveFn } from '@angular/router';
import { Observable, catchError, map, of } from 'rxjs';

import { PublicParkHistoricalComparison, PublicParkHistoricalSnapshot, PublicParkHistoricalTimeline } from '@app/models/history/public-park-history.models';
import { anonymousHttpOptions } from '@core/http/auth/anonymous-http-options';
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import { applySsrPublicDataErrorStatus } from '@core/ssr/ssr-public-error-status';
import { isValidHistoricalSnapshotDate } from '../utils/history-snapshot-date-selection';
import { HISTORY_DATA_PORT, HistoryDataPort } from './history-data.ports';

export const PARK_HISTORY_EXPLORER_ROUTE_DATA_KEY = 'parkHistoryExplorer';

export interface ResolvedParkHistoryTimelineRouteData {
  readonly timeline: PublicParkHistoricalTimeline | null;
  readonly page: number;
}

export interface ResolvedParkHistoricalSnapshotRouteData {
  readonly snapshot: PublicParkHistoricalSnapshot | null;
  readonly year: number;
  readonly month: number | null;
  readonly day: number | null;
}

export interface ResolvedParkHistoricalComparisonRouteData {
  readonly comparison: PublicParkHistoricalComparison | null;
  readonly fromYear: number;
  readonly toYear: number;
}

export const parkHistoryTimelineResolver: ResolveFn<ResolvedParkHistoryTimelineRouteData> = (
  route: ActivatedRouteSnapshot
): Observable<ResolvedParkHistoryTimelineRouteData> => {
  const parkId: string = route.paramMap.get('id')?.trim() ?? '';
  const page: number | null = resolvePositiveInteger(route.paramMap.get('page'), 1);
  const ssrStatus: SsrHttpStatusService = inject(SsrHttpStatusService);
  const historyData: HistoryDataPort = inject(HISTORY_DATA_PORT);

  if (parkId.length === 0 || page === null) {
    ssrStatus.setNotFound();
    return of({ timeline: null, page: 1 });
  }

  return historyData.getPublicParkTimeline(parkId, anonymousHttpOptions(), page, 25).pipe(
    map((timeline: PublicParkHistoricalTimeline): ResolvedParkHistoryTimelineRouteData => {
      const lastPage: number = Math.max(1, timeline.pagination.totalPages);
      if (page > lastPage) {
        ssrStatus.setNotFound();
        return { timeline: null, page };
      }

      return { timeline, page };
    }),
    catchError((error: unknown): Observable<ResolvedParkHistoryTimelineRouteData> => {
      applySsrPublicDataErrorStatus(error, ssrStatus);
      return of({ timeline: null, page });
    })
  );
};

export const parkHistoricalSnapshotResolver: ResolveFn<ResolvedParkHistoricalSnapshotRouteData> = (
  route: ActivatedRouteSnapshot
): Observable<ResolvedParkHistoricalSnapshotRouteData> => {
  const parkId: string = route.paramMap.get('id')?.trim() ?? '';
  const year: number | null = resolveFourDigitYear(route.paramMap.get('year'));
  const month: number | null | undefined = resolveOptionalBoundedInteger(route.queryParamMap.get('month'), 1, 12);
  const day: number | null | undefined = resolveOptionalBoundedInteger(route.queryParamMap.get('day'), 1, 31);
  const ssrStatus: SsrHttpStatusService = inject(SsrHttpStatusService);
  const historyData: HistoryDataPort = inject(HISTORY_DATA_PORT);

  if (
    parkId.length === 0
    || year === null
    || month === undefined
    || day === undefined
    || !isValidHistoricalSnapshotDate(year, month, day)
  ) {
    ssrStatus.setNotFound();
    return of({ snapshot: null, year: year ?? 1, month: month ?? null, day: day ?? null });
  }

  return historyData.getPublicParkSnapshot(parkId, year, month, day, anonymousHttpOptions()).pipe(
    map((snapshot: PublicParkHistoricalSnapshot): ResolvedParkHistoricalSnapshotRouteData => ({
      snapshot,
      year,
      month,
      day
    })),
    catchError((error: unknown): Observable<ResolvedParkHistoricalSnapshotRouteData> => {
      applySsrPublicDataErrorStatus(error, ssrStatus);
      return of({ snapshot: null, year, month, day });
    })
  );
};

export const parkHistoricalComparisonResolver: ResolveFn<ResolvedParkHistoricalComparisonRouteData> = (
  route: ActivatedRouteSnapshot
): Observable<ResolvedParkHistoricalComparisonRouteData> => {
  const parkId: string = route.paramMap.get('id')?.trim() ?? '';
  const fromYear: number | null = resolveFourDigitYear(route.paramMap.get('fromYear'));
  const toYear: number | null = resolveFourDigitYear(route.paramMap.get('toYear'));
  const ssrStatus: SsrHttpStatusService = inject(SsrHttpStatusService);
  const historyData: HistoryDataPort = inject(HISTORY_DATA_PORT);

  if (parkId.length === 0 || fromYear === null || toYear === null || fromYear >= toYear) {
    ssrStatus.setNotFound();
    return of({ comparison: null, fromYear: fromYear ?? 1, toYear: toYear ?? 1 });
  }

  return historyData.getPublicParkComparison(parkId, fromYear, toYear, anonymousHttpOptions()).pipe(
    map((comparison: PublicParkHistoricalComparison): ResolvedParkHistoricalComparisonRouteData => ({
      comparison,
      fromYear,
      toYear
    })),
    catchError((error: unknown): Observable<ResolvedParkHistoricalComparisonRouteData> => {
      applySsrPublicDataErrorStatus(error, ssrStatus);
      return of({ comparison: null, fromYear, toYear });
    })
  );
};

function resolvePositiveInteger(rawValue: string | null, fallback: number): number | null {
  if (rawValue === null || rawValue.length === 0) {
    return fallback;
  }

  const value: number = Number(rawValue);
  return Number.isInteger(value) && value >= 1 ? value : null;
}

function resolveFourDigitYear(rawValue: string | null): number | null {
  if (!rawValue || !/^\d{4}$/.test(rawValue)) {
    return null;
  }

  const year: number = Number(rawValue);
  return year >= 1000 && year <= 9999 ? year : null;
}

function resolveOptionalBoundedInteger(rawValue: string | null, minimum: number, maximum: number): number | null | undefined {
  if (rawValue === null || rawValue.length === 0) {
    return null;
  }

  const value: number = Number(rawValue);
  return Number.isInteger(value) && value >= minimum && value <= maximum ? value : undefined;
}
