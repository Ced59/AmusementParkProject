import { inject } from '@angular/core';
import { ActivatedRouteSnapshot, ResolveFn } from '@angular/router';
import { Observable, catchError, map, of } from 'rxjs';

import { HistoryTimeline } from '@app/models/history/history.models';
import { anonymousHttpOptions } from '@core/http/auth/anonymous-http-options';
import { hasHttpStatus } from '@core/http/http-error-status.helpers';
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import { HISTORY_DATA_PORT, HistoryDataPort } from './history-data.ports';

export const HISTORY_TIMELINE_ROUTE_DATA_KEY = 'historyTimeline';

export interface ResolvedHistoryTimelineRouteData {
  readonly timeline: HistoryTimeline | null;
  readonly includeParkItems: boolean;
}

export const historyTimelineResolver: ResolveFn<ResolvedHistoryTimelineRouteData> = (route: ActivatedRouteSnapshot): Observable<ResolvedHistoryTimelineRouteData> => {
  const parkItemId: string = route.paramMap.get('itemId')?.trim() ?? '';
  const parkId: string = route.paramMap.get('id')?.trim() ?? '';
  const ssrHttpStatusService: SsrHttpStatusService = inject(SsrHttpStatusService);
  const historyDataPort: HistoryDataPort = inject(HISTORY_DATA_PORT);

  if (parkItemId.length > 0) {
    return historyDataPort.getParkItemTimeline(parkItemId, anonymousHttpOptions()).pipe(
      map((timeline: HistoryTimeline): ResolvedHistoryTimelineRouteData => ({ timeline, includeParkItems: false })),
      catchError((error: unknown): Observable<ResolvedHistoryTimelineRouteData> => {
        if (hasHttpStatus(error, 404)) {
          ssrHttpStatusService.setNotFound();
        }

        return of({ timeline: null, includeParkItems: false });
      })
    );
  }

  if (parkId.length === 0) {
    ssrHttpStatusService.setNotFound();
    return of({ timeline: null, includeParkItems: false });
  }

  return historyDataPort.getParkTimeline(parkId, false, [], anonymousHttpOptions()).pipe(
    map((timeline: HistoryTimeline): ResolvedHistoryTimelineRouteData => ({ timeline, includeParkItems: false })),
    catchError((error: unknown): Observable<ResolvedHistoryTimelineRouteData> => {
      if (!hasHttpStatus(error, 404)) {
        return of({ timeline: null, includeParkItems: false });
      }

      return historyDataPort.getParkTimeline(parkId, true, [], anonymousHttpOptions()).pipe(
        map((timeline: HistoryTimeline): ResolvedHistoryTimelineRouteData => ({ timeline, includeParkItems: true })),
        catchError((fallbackError: unknown): Observable<ResolvedHistoryTimelineRouteData> => {
          if (hasHttpStatus(fallbackError, 404)) {
            ssrHttpStatusService.setNotFound();
          }

          return of({ timeline: null, includeParkItems: true });
        })
      );
    })
  );
};
