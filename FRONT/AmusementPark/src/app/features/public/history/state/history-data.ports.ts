import { inject, InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';

import { HistoryApiService } from '@data-access/history/history-api.service';
import { HistoryArticle, HistoryTimeline } from '@app/models/history/history.models';
import { AnonymousHttpOptions } from '@core/http/auth/anonymous-http-options';

export interface HistoryDataPort {
  getParkTimeline(parkId: string, includeParkItems?: boolean, parkItemIds?: readonly string[], options?: AnonymousHttpOptions): Observable<HistoryTimeline>;
  getParkItemTimeline(parkItemId: string, options?: AnonymousHttpOptions): Observable<HistoryTimeline>;
  getArticle(eventId: string, options?: AnonymousHttpOptions): Observable<HistoryArticle>;
}

export const HISTORY_DATA_PORT = new InjectionToken<HistoryDataPort>('HISTORY_DATA_PORT', {
  providedIn: 'root',
  factory: () => inject(HistoryApiService)
});
