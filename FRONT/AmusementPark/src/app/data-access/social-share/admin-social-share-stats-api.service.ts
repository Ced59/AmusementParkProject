import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  SocialShareStatsQuery,
  SocialShareStatsResult
} from '@app/models/social-share/social-share.models';

@Injectable({
  providedIn: 'root'
})
export class AdminSocialShareStatsApiService {
  private readonly baseUrl: string = `${environment.apiBaseUrl}admin/social-share/stats`;

  constructor(private readonly http: HttpClient) {
  }

  getStats(query: SocialShareStatsQuery = {}): Observable<SocialShareStatsResult> {
    let params: HttpParams = new HttpParams();

    params = this.setOptionalParam(params, 'fromUtc', query.fromUtc);
    params = this.setOptionalParam(params, 'toUtc', query.toUtc);

    return this.http.get<SocialShareStatsResult>(this.baseUrl, { params });
  }

  private setOptionalParam(params: HttpParams, key: string, value: string | null | undefined): HttpParams {
    if (!value || value.trim().length === 0) {
      return params;
    }

    return params.set(key, value.trim());
  }
}
