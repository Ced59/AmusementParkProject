import { HttpClient, HttpContext } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { HomeStatsModel } from '@app/models/home/home-stats.model';
import { HomeFeaturedParkModel } from '@app/models/home/home-featured-park.model';
import { HOME_API_ENDPOINTS } from './home-api-endpoints';

interface HomeHttpOptions {
  context?: HttpContext;
}

@Injectable({
  providedIn: 'root'
})
export class HomeApiService {
  constructor(private readonly http: HttpClient) {
  }

  getHomeStats(options: HomeHttpOptions = {}): Observable<HomeStatsModel> {
    const url: string = `${environment.apiBaseUrl}${HOME_API_ENDPOINTS.getHomeStats}`;
    return this.http.get<HomeStatsModel>(url, options);
  }

  getFeaturedParks(excludedParkIds: readonly string[], limit: number = 3, options: HomeHttpOptions = {}): Observable<HomeFeaturedParkModel[]> {
    const url: string = `${environment.apiBaseUrl}${HOME_API_ENDPOINTS.getFeaturedParks}`;
    const queryParts: string[] = [`limit=${encodeURIComponent(String(limit))}`];

    for (const parkId of excludedParkIds) {
      if (parkId.trim().length > 0) {
        queryParts.push(`excludeIds=${encodeURIComponent(parkId.trim())}`);
      }
    }

    return this.http.get<HomeFeaturedParkModel[]>(`${url}?${queryParts.join('&')}`, options);
  }
}
