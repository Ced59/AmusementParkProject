import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  ProfileComparisonRevocation,
  ProfileComparisonSummary,
  SharedProfileComparison,
} from '@app/models/sharing/profile-comparison.models';
import { environment } from '../../../environments/environment';
import { PROFILE_COMPARISONS_API_ENDPOINTS } from './profile-comparisons-api-endpoints';

@Injectable({ providedIn: 'root' })
export class ProfileComparisonsApiService {
  constructor(private readonly http: HttpClient) {}

  public getShared(shareId: string): Observable<SharedProfileComparison> {
    return this.http.get<SharedProfileComparison>(
      `${environment.apiBaseUrl}${PROFILE_COMPARISONS_API_ENDPOINTS.shared(shareId)}`,
    );
  }

  public listMine(): Observable<ProfileComparisonSummary[]> {
    return this.http.get<ProfileComparisonSummary[]>(
      `${environment.apiBaseUrl}${PROFILE_COMPARISONS_API_ENDPOINTS.mine}`,
    );
  }

  public revoke(shareId: string): Observable<ProfileComparisonRevocation> {
    return this.http.delete<ProfileComparisonRevocation>(
      `${environment.apiBaseUrl}${PROFILE_COMPARISONS_API_ENDPOINTS.revoke(shareId)}`,
    );
  }
}
