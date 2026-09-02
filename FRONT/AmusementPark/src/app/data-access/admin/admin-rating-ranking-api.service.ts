import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  RatingRankingAdministration,
  RatingRankingPolicyCandidateRequest,
  RatingRankingPolicyImpact,
  RatingRankingRebuildRequestResult
} from '@app/models/admin/ratings/rating-ranking-administration.models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AdminRatingRankingApiService {
  private readonly baseUrl: string = `${environment.apiBaseUrl}admin/ratings/ranking-management`;

  constructor(private readonly http: HttpClient) {
  }

  getDashboard(): Observable<RatingRankingAdministration> {
    return this.http.get<RatingRankingAdministration>(this.baseUrl);
  }

  previewImpact(request: RatingRankingPolicyCandidateRequest): Observable<RatingRankingPolicyImpact> {
    return this.http.post<RatingRankingPolicyImpact>(`${this.baseUrl}/preview`, request);
  }

  rebuild(): Observable<RatingRankingRebuildRequestResult> {
    return this.http.post<RatingRankingRebuildRequestResult>(`${this.baseUrl}/rebuild`, { confirmed: true });
  }
}
