import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { HomeStatsModel } from '@app/models/home/home-stats.model';
import { HOME_API_ENDPOINTS } from './home-api-endpoints';

@Injectable({
  providedIn: 'root'
})
export class HomeApiService {
  constructor(private readonly http: HttpClient) {
  }

  getHomeStats(): Observable<HomeStatsModel> {
    const url: string = `${environment.apiBaseUrl}${HOME_API_ENDPOINTS.getHomeStats}`;
    return this.http.get<HomeStatsModel>(url);
  }
}
