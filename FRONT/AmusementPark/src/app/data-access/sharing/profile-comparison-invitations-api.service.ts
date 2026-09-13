import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  ProfileComparisonCategory,
  ProfileComparisonInvitationAcceptance,
  ProfileComparisonInvitationCreation,
  ProfileComparisonInvitationPreview
} from '@app/models/sharing/profile-comparison-invitation.models';
import { environment } from '../../../environments/environment';
import { PROFILE_COMPARISON_INVITATIONS_API_ENDPOINTS } from './profile-comparison-invitations-api-endpoints';

@Injectable({ providedIn: 'root' })
export class ProfileComparisonInvitationsApiService {
  private readonly jsonOptions = {
    headers: new HttpHeaders({ 'Content-Type': 'application/json' })
  };

  constructor(private readonly http: HttpClient) {
  }

  public create(categories: ProfileComparisonCategory[]): Observable<ProfileComparisonInvitationCreation> {
    return this.http.post<ProfileComparisonInvitationCreation>(
      `${environment.apiBaseUrl}${PROFILE_COMPARISON_INVITATIONS_API_ENDPOINTS.create}`,
      { categories },
      this.jsonOptions
    );
  }

  public preview(token: string): Observable<ProfileComparisonInvitationPreview> {
    return this.http.get<ProfileComparisonInvitationPreview>(
      `${environment.apiBaseUrl}${PROFILE_COMPARISON_INVITATIONS_API_ENDPOINTS.preview(token)}`
    );
  }

  public accept(token: string): Observable<ProfileComparisonInvitationAcceptance> {
    return this.http.post<ProfileComparisonInvitationAcceptance>(
      `${environment.apiBaseUrl}${PROFILE_COMPARISON_INVITATIONS_API_ENDPOINTS.accept(token)}`,
      {},
      this.jsonOptions
    );
  }
}
