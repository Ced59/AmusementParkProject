import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  TechnicalStatsSettings,
  TechnicalStatsSnapshot,
  UpdateTechnicalStatsSettingsRequest
} from '@app/models/admin/technical-stats/technical-stats.models';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AdminTechnicalStatsApiService {
  private readonly baseUrl: string = `${environment.apiBaseUrl}admin/technical-stats`;

  constructor(private readonly http: HttpClient) {
  }

  getStats(): Observable<TechnicalStatsSnapshot> {
    return this.http.get<TechnicalStatsSnapshot>(this.baseUrl);
  }

  updateSettings(request: UpdateTechnicalStatsSettingsRequest): Observable<TechnicalStatsSettings> {
    return this.http.put<TechnicalStatsSettings>(`${this.baseUrl}/settings`, request);
  }
}
