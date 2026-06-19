import { HttpClient, HttpContext, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  RatingRankingsPage,
  ParkRatingRanking,
  RatingSummary,
  RatingTargetType,
  UserRating,
  UserRatingListItem,
  UserRatingStats,
  UserRatingUpsertRequest,
  UserRatingsPage
} from '@app/models/ratings/rating.models';
import { PagedCollectionResponse, unwrapPagedCollection } from '@data-access/shared/api-helpers';
import { RATINGS_API_ENDPOINTS } from './ratings-api-endpoints';

interface RatingsHttpOptions {
  context?: HttpContext;
}

@Injectable({
  providedIn: 'root'
})
export class RatingsApiService {
  private readonly jsonHttpOptions = {
    headers: new HttpHeaders({
      'Content-Type': 'application/json'
    })
  };

  constructor(private readonly http: HttpClient) {
  }

  getSummary(targetType: RatingTargetType, targetId: string, options: RatingsHttpOptions = {}): Observable<RatingSummary> {
    const url: string = `${environment.apiBaseUrl}${RATINGS_API_ENDPOINTS.getSummary(targetType, targetId)}`;
    return this.http.get<RatingSummary>(url, options);
  }

  getMyRating(targetType: RatingTargetType, targetId: string): Observable<UserRating | null> {
    const url: string = `${environment.apiBaseUrl}${RATINGS_API_ENDPOINTS.getMyRating(targetType, targetId)}`;
    return this.http.get<UserRating | null>(url);
  }

  upsertRating(request: UserRatingUpsertRequest): Observable<UserRating> {
    const url: string = `${environment.apiBaseUrl}${RATINGS_API_ENDPOINTS.upsert}`;
    return this.http.put<UserRating>(url, request, this.jsonHttpOptions);
  }

  getMyRatings(page: number = 1, size: number = 10, search: string | null = null): Observable<UserRatingsPage> {
    const url: string = `${environment.apiBaseUrl}${RATINGS_API_ENDPOINTS.getMyRatings(page, size, search)}`;
    return this.http.get<PagedCollectionResponse<UserRatingListItem>>(url).pipe(
      map((response: PagedCollectionResponse<UserRatingListItem>) => unwrapPagedCollection<UserRatingListItem>(response))
    );
  }

  getMyRatingStats(): Observable<UserRatingStats> {
    const url: string = `${environment.apiBaseUrl}${RATINGS_API_ENDPOINTS.getMyStats}`;
    return this.http.get<UserRatingStats>(url);
  }

  getRankings(page: number = 1, size: number = 20, category: string | null = null, search: string | null = null, options: RatingsHttpOptions = {}): Observable<RatingRankingsPage> {
    const url: string = `${environment.apiBaseUrl}${RATINGS_API_ENDPOINTS.getRankings(page, size, category, search)}`;
    return this.http.get<PagedCollectionResponse<ParkRatingRanking>>(url, options).pipe(
      map((response: PagedCollectionResponse<ParkRatingRanking>) => unwrapPagedCollection<ParkRatingRanking>(response))
    );
  }
}
