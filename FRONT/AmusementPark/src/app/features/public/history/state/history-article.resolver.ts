import { inject } from '@angular/core';
import { ActivatedRouteSnapshot, ResolveFn } from '@angular/router';
import { Observable, catchError, of } from 'rxjs';

import { HistoryArticle } from '@app/models/history/history.models';
import { anonymousHttpOptions } from '@core/http/auth/anonymous-http-options';
import { hasHttpStatus } from '@core/http/http-error-status.helpers';
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import { HISTORY_DATA_PORT, HistoryDataPort } from './history-data.ports';

export const HISTORY_ARTICLE_ROUTE_DATA_KEY = 'historyArticle';

export const historyArticleResolver: ResolveFn<HistoryArticle | null> = (route: ActivatedRouteSnapshot): Observable<HistoryArticle | null> => {
  const eventId: string = route.paramMap.get('eventId')?.trim() ?? '';
  const ssrHttpStatusService: SsrHttpStatusService = inject(SsrHttpStatusService);

  if (eventId.length === 0) {
    ssrHttpStatusService.setNotFound();
    return of(null);
  }

  const historyDataPort: HistoryDataPort = inject(HISTORY_DATA_PORT);

  return historyDataPort.getArticle(eventId, anonymousHttpOptions()).pipe(
    catchError((error: unknown): Observable<null> => {
      if (hasHttpStatus(error, 404)) {
        ssrHttpStatusService.setNotFound();
      }

      return of(null);
    })
  );
};
