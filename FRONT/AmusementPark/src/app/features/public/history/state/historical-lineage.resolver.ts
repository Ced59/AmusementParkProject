import { inject } from '@angular/core';
import { ActivatedRouteSnapshot, ResolveFn } from '@angular/router';
import { Observable, catchError, map, of } from 'rxjs';

import { PublicHistoricalLineage } from '@app/models/history/public-park-history.models';
import { anonymousHttpOptions } from '@core/http/auth/anonymous-http-options';
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import { applySsrPublicDataErrorStatus } from '@core/ssr/ssr-public-error-status';
import { HISTORY_DATA_PORT, HistoryDataPort } from './history-data.ports';

export const HISTORICAL_LINEAGE_ROUTE_DATA_KEY = 'historicalLineage';

export const historicalLineageResolver: ResolveFn<PublicHistoricalLineage | null> = (
  route: ActivatedRouteSnapshot
): Observable<PublicHistoricalLineage | null> => {
  const subjectType: string = route.paramMap.get('subjectType')?.trim() ?? '';
  const subjectId: string = route.paramMap.get('subjectId')?.trim() ?? '';
  const contextParkId: string = route.paramMap.get('contextParkId')?.trim() ?? '';
  const ssrStatus: SsrHttpStatusService = inject(SsrHttpStatusService);
  const historyData: HistoryDataPort = inject(HISTORY_DATA_PORT);

  if (contextParkId.length === 0 || subjectType.length === 0 || subjectId.length === 0) {
    ssrStatus.setNotFound();
    return of(null);
  }

  return historyData.getPublicHistoricalLineage(
    subjectType,
    subjectId,
    contextParkId,
    anonymousHttpOptions()
  ).pipe(
    map((lineage: PublicHistoricalLineage): PublicHistoricalLineage => lineage),
    catchError((error: unknown): Observable<null> => {
      applySsrPublicDataErrorStatus(error, ssrStatus);
      return of(null);
    })
  );
};
