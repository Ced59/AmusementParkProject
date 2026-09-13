import { inject, InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';

import {
  ProfileComparisonCategory,
  ProfileComparisonInvitationAcceptance,
  ProfileComparisonInvitationCreation,
  ProfileComparisonInvitationPreview
} from '@app/models/sharing/profile-comparison-invitation.models';
import { ProfileComparisonInvitationsApiService } from '@data-access/sharing/profile-comparison-invitations-api.service';

export interface ProfileComparisonInvitationPort {
  create(categories: ProfileComparisonCategory[]): Observable<ProfileComparisonInvitationCreation>;
  preview(token: string): Observable<ProfileComparisonInvitationPreview>;
  accept(token: string): Observable<ProfileComparisonInvitationAcceptance>;
}

export const PROFILE_COMPARISON_INVITATION_PORT =
  new InjectionToken<ProfileComparisonInvitationPort>(
    'PROFILE_COMPARISON_INVITATION_PORT',
    {
      providedIn: 'root',
      factory: (): ProfileComparisonInvitationPort => {
        const api: ProfileComparisonInvitationsApiService =
          inject(ProfileComparisonInvitationsApiService);
        return {
          create: (categories: ProfileComparisonCategory[]) => api.create(categories),
          preview: (token: string) => api.preview(token),
          accept: (token: string) => api.accept(token)
        };
      }
    }
  );
