import { inject, InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';

import { HistoryApiService } from '@data-access/history/history-api.service';
import { HistoryArticle, HistoryTimeline } from '@app/models/history/history.models';
import { PublicHistoricalLineage, PublicParkHistoricalSnapshot, PublicParkHistoricalTimeline } from '@app/models/history/public-park-history.models';
import { AnonymousHttpOptions } from '@core/http/auth/anonymous-http-options';

export interface HistoryDataPort {
  getPublicHistoricalLineage(subjectType: string, subjectId: string, options?: AnonymousHttpOptions): Observable<PublicHistoricalLineage>;
  getPublicParkTimeline(parkId: string, options?: AnonymousHttpOptions, page?: number, pageSize?: number): Observable<PublicParkHistoricalTimeline>;
  getPublicParkSnapshot(parkId: string, year: number, month?: number | null, day?: number | null, options?: AnonymousHttpOptions): Observable<PublicParkHistoricalSnapshot>;
  getParkTimeline(parkId: string, includeParkItems?: boolean, parkItemIds?: readonly string[], options?: AnonymousHttpOptions, page?: number): Observable<HistoryTimeline>;
  getParkItemTimeline(parkItemId: string, options?: AnonymousHttpOptions, page?: number): Observable<HistoryTimeline>;
  getStandaloneAttractionTimeline(standaloneAttractionId: string, options?: AnonymousHttpOptions, page?: number): Observable<HistoryTimeline>;
  getArticle(eventId: string, options?: AnonymousHttpOptions): Observable<HistoryArticle>;
}

export const HISTORY_DATA_PORT = new InjectionToken<HistoryDataPort>('HISTORY_DATA_PORT', {
  providedIn: 'root',
  factory: () => inject(HistoryApiService)
});
