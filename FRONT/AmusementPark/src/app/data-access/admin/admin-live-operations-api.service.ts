import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  LiveOperationalScope,
  LiveOperationsDashboard,
  LiveQualityReplay,
  UpdateLiveOperationalControlRequest
} from '@app/models/admin/live-data/live-operations.models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AdminLiveOperationsApiService {
  private readonly endpoint: string = `${environment.apiBaseUrl}admin/live/operations`;

  constructor(private readonly http: HttpClient) {
  }

  getDashboard(): Observable<LiveOperationsDashboard> {
    return this.http.get<LiveOperationsDashboard>(this.endpoint);
  }

  updateControl(
    request: UpdateLiveOperationalControlRequest
  ): Observable<LiveOperationalScope> {
    return this.http.put<LiveOperationalScope>(`${this.endpoint}/controls`, request);
  }

  replayQuarantine(maximumCount: number): Observable<LiveQualityReplay> {
    return this.http.post<LiveQualityReplay>(
      `${environment.apiBaseUrl}admin/live/quality/quarantine/replay`,
      { maximumCount }
    );
  }
}
