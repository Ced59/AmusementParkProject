import { HttpClient, HttpContext, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  RatingRankingsPage,
  ParkRatingRanking,
  ParkItemRatingRanking,
  ParkItemRatingRankingsPage,
  RatingSummary,
  RatingTargetType,
  UserRating,
  UserRatingListItem,
  UserRatingStats,
  UserRatingUpsertRequest,
  UserRatingsPage,
  UserParkItemRatingRanking,
  UserParkItemRatingRankingsPage,
  UserParkRatingRanking,
  UserParkRatingRankingsPage,
  UserRankingShareSettings,
  UserRankingShareVisibilityRequest,
  SharedUserRankingProfile
} from '@app/models/ratings/rating.models';
import { PagedCollectionResponse, unwrapPagedCollection } from '@data-access/shared/api-helpers';
import { RATINGS_API_ENDPOINTS } from './ratings-api-endpoints';

interface RatingsHttpOptions {
  context?: HttpContext;
  transferCache?: boolean;
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

  deleteMyRating(targetType: RatingTargetType, targetId: string): Observable<RatingSummary> {
    const url: string = `${environment.apiBaseUrl}${RATINGS_API_ENDPOINTS.deleteMyRating(targetType, targetId)}`;
    return this.http.delete<RatingSummary>(url);
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

  getMyShareSettings(): Observable<UserRankingShareSettings> {
    const url: string = `${environment.apiBaseUrl}${RATINGS_API_ENDPOINTS.getMyShareSettings}`;
    return this.http.get<UserRankingShareSettings>(url);
  }

  setMyShareVisibility(isPublic: boolean): Observable<UserRankingShareSettings> {
    const url: string = `${environment.apiBaseUrl}${RATINGS_API_ENDPOINTS.setMyShareVisibility}`;
    const request: UserRankingShareVisibilityRequest = { isPublic };
    return this.http.put<UserRankingShareSettings>(url, request, this.jsonHttpOptions);
  }

  getMyParkRankings(page: number = 1, size: number = 10, search: string | null = null): Observable<UserParkRatingRankingsPage> {
    const url: string = `${environment.apiBaseUrl}${RATINGS_API_ENDPOINTS.getMyParkRankings(page, size, search)}`;
    return this.http.get<PagedCollectionResponse<UserParkRatingRanking>>(url).pipe(
      map((response: PagedCollectionResponse<UserParkRatingRanking>) => unwrapPagedCollection<UserParkRatingRanking>(response))
    );
  }

  getMyParkItemRankings(
    page: number,
    size: number,
    category: string,
    type: string | null = null,
    search: string | null = null
  ): Observable<UserParkItemRatingRankingsPage> {
    const url: string = `${environment.apiBaseUrl}${RATINGS_API_ENDPOINTS.getMyParkItemRankings(page, size, category, type, search)}`;
    return this.http.get<PagedCollectionResponse<UserParkItemRatingRanking>>(url).pipe(
      map((response: PagedCollectionResponse<UserParkItemRatingRanking>) => unwrapPagedCollection<UserParkItemRatingRanking>(response))
    );
  }

  getRankings(page: number = 1, size: number = 20, category: string | null = null, search: string | null = null, options: RatingsHttpOptions = {}): Observable<RatingRankingsPage> {
    const url: string = `${environment.apiBaseUrl}${RATINGS_API_ENDPOINTS.getRankings(page, size, category, search)}`;
    return this.http.get<PagedCollectionResponse<ParkRatingRanking>>(url, options).pipe(
      map((response: PagedCollectionResponse<ParkRatingRanking>) => unwrapPagedCollection<ParkRatingRanking>(response))
    );
  }

  getParkItemRankings(
    page: number,
    size: number,
    category: string,
    type: string | null = null,
    search: string | null = null,
    options: RatingsHttpOptions = {}
  ): Observable<ParkItemRatingRankingsPage> {
    const url: string = `${environment.apiBaseUrl}${RATINGS_API_ENDPOINTS.getParkItemRankings(page, size, category, type, search)}`;
    return this.http.get<PagedCollectionResponse<ParkItemRatingRanking>>(url, options).pipe(
      map((response: PagedCollectionResponse<ParkItemRatingRanking>) => unwrapPagedCollection<ParkItemRatingRanking>(response))
    );
  }

  getSharedProfile(shareId: string, options: RatingsHttpOptions = {}): Observable<SharedUserRankingProfile> {
    const url: string = `${environment.apiBaseUrl}${RATINGS_API_ENDPOINTS.getSharedProfile(shareId)}`;
    return this.http.get<SharedUserRankingProfile>(url, {
      ...options,
      transferCache: false
    });
  }

  getSharedParkRankings(
    shareId: string,
    page: number = 1,
    size: number = 10,
    search: string | null = null,
    options: RatingsHttpOptions = {}
  ): Observable<UserParkRatingRankingsPage> {
    const url: string = `${environment.apiBaseUrl}${RATINGS_API_ENDPOINTS.getSharedParkRankings(shareId, page, size, search)}`;
    return this.http.get<PagedCollectionResponse<UserParkRatingRanking>>(url, options).pipe(
      map((response: PagedCollectionResponse<UserParkRatingRanking>) => unwrapPagedCollection<UserParkRatingRanking>(response))
    );
  }

  getSharedParkItemRankings(
    shareId: string,
    page: number,
    size: number,
    category: string,
    type: string | null = null,
    search: string | null = null,
    options: RatingsHttpOptions = {}
  ): Observable<UserParkItemRatingRankingsPage> {
    const url: string = `${environment.apiBaseUrl}${RATINGS_API_ENDPOINTS.getSharedParkItemRankings(shareId, page, size, category, type, search)}`;
    return this.http.get<PagedCollectionResponse<UserParkItemRatingRanking>>(url, options).pipe(
      map((response: PagedCollectionResponse<UserParkItemRatingRanking>) => unwrapPagedCollection<UserParkItemRatingRanking>(response))
    );
  }
}
