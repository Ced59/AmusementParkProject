import { inject } from '@angular/core';
import { ActivatedRouteSnapshot, ResolveFn } from '@angular/router';
import { Observable, catchError, map, of } from 'rxjs';

import { HistoryTimeline } from '@app/models/history/history.models';
import { anonymousHttpOptions } from '@core/http/auth/anonymous-http-options';
import { hasHttpStatus } from '@core/http/http-error-status.helpers';
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import { applySsrPublicDataErrorStatus } from '@core/ssr/ssr-public-error-status';
import { HISTORY_DATA_PORT, HistoryDataPort } from './history-data.ports';

export const HISTORY_TIMELINE_ROUTE_DATA_KEY = 'historyTimeline';

export interface ResolvedHistoryTimelineRouteData {
  readonly timeline: HistoryTimeline | null;
  readonly includeParkItems: boolean;
  readonly page: number;
}

export const historyTimelineResolver: ResolveFn<ResolvedHistoryTimelineRouteData> = (route: ActivatedRouteSnapshot): Observable<ResolvedHistoryTimelineRouteData> => {
  const parkItemId: string = route.paramMap.get('itemId')?.trim() ?? '';
  const parkId: string = route.paramMap.get('id')?.trim() ?? '';
  const page: number | null = resolveTimelinePage(route);
  const requestedIncludeParkItems: boolean = route.queryParamMap.get('includeParkItems') === 'true';
  const ssrHttpStatusService: SsrHttpStatusService = inject(SsrHttpStatusService);
  const historyDataPort: HistoryDataPort = inject(HISTORY_DATA_PORT);

  if (page === null) {
    ssrHttpStatusService.setNotFound();
    return of({ timeline: null, includeParkItems: false, page: 1 });
  }

  if (parkItemId.length > 0) {
    return historyDataPort.getParkItemTimeline(parkItemId, anonymousHttpOptions(), page).pipe(
      map((timeline: HistoryTimeline): ResolvedHistoryTimelineRouteData => ({ timeline, includeParkItems: false, page })),
      catchError((error: unknown): Observable<ResolvedHistoryTimelineRouteData> => {
        applySsrPublicDataErrorStatus(error, ssrHttpStatusService);
        return of({ timeline: null, includeParkItems: false, page });
      })
    );
  }

  if (parkId.length === 0) {
    ssrHttpStatusService.setNotFound();
    return of({ timeline: null, includeParkItems: false, page });
  }

  if (requestedIncludeParkItems) {
    return historyDataPort.getParkTimeline(parkId, true, [], anonymousHttpOptions(), page).pipe(
      map((timeline: HistoryTimeline): ResolvedHistoryTimelineRouteData => ({ timeline, includeParkItems: true, page })),
      catchError((error: unknown): Observable<ResolvedHistoryTimelineRouteData> => {
        applySsrPublicDataErrorStatus(error, ssrHttpStatusService);
        return of({ timeline: null, includeParkItems: true, page });
      })
    );
  }

  return historyDataPort.getParkTimeline(parkId, false, [], anonymousHttpOptions(), page).pipe(
    map((timeline: HistoryTimeline): ResolvedHistoryTimelineRouteData => ({ timeline, includeParkItems: false, page })),
    catchError((error: unknown): Observable<ResolvedHistoryTimelineRouteData> => {
      if (!hasHttpStatus(error, 404)) {
        ssrHttpStatusService.setStatus(503);
        return of({ timeline: null, includeParkItems: false, page });
      }

      return historyDataPort.getParkTimeline(parkId, true, [], anonymousHttpOptions(), page).pipe(
        map((timeline: HistoryTimeline): ResolvedHistoryTimelineRouteData => ({ timeline, includeParkItems: true, page })),
        catchError((fallbackError: unknown): Observable<ResolvedHistoryTimelineRouteData> => {
          applySsrPublicDataErrorStatus(fallbackError, ssrHttpStatusService);
          return of({ timeline: null, includeParkItems: true, page });
        })
      );
    })
  );
};

function resolveTimelinePage(route: ActivatedRouteSnapshot): number | null {
  const rawPage: string | null = route.paramMap.get('page');

  if (!rawPage) {
    return 1;
  }

  const parsedPage: number = Number(rawPage);
  return Number.isInteger(parsedPage) && parsedPage >= 1 ? parsedPage : null;
}
