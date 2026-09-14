import { TestBed } from '@angular/core/testing';
import { Observable, of } from 'rxjs';

import {
  ProfileComparisonCategory,
  ProfileComparisonInvitationAcceptance,
  ProfileComparisonInvitationCreation,
  ProfileComparisonInvitationPreview
} from '@app/models/sharing/profile-comparison-invitation.models';
import { SHARE_PRODUCT_ANALYTICS_PORT } from '@core/analytics/share-product-analytics.port';
import { ShareProductEvent } from '@core/analytics/share-product-event.model';
import {
  PROFILE_COMPARISON_INVITATION_PORT,
  ProfileComparisonInvitationPort
} from './profile-comparison-invitation-data.ports';
import { ProfileComparisonInvitationCreatorStateFacade } from './profile-comparison-invitation-creator-state.facade';

describe('ProfileComparisonInvitationCreatorStateFacade', () => {
  it('keeps only available categories and exposes the opaque invitation', () => {
    const analyticsEvents: ShareProductEvent[] = [];
    const requests: ProfileComparisonCategory[][] = [];
    const creation: ProfileComparisonInvitationCreation = {
      token: 'opaque-token',
      expiresAtUtc: '2026-09-20T12:00:00Z',
      categories: ['VisitedParks']
    };
    const port: ProfileComparisonInvitationPort = {
      create: (categories: ProfileComparisonCategory[]): Observable<ProfileComparisonInvitationCreation> => {
        requests.push(categories);
        return of(creation);
      },
      preview: (): Observable<ProfileComparisonInvitationPreview> => {
        throw new Error('Unexpected preview.');
      },
      accept: (): Observable<ProfileComparisonInvitationAcceptance> => {
        throw new Error('Unexpected acceptance.');
      }
    };
    TestBed.configureTestingModule({ providers: [
      ProfileComparisonInvitationCreatorStateFacade,
      { provide: PROFILE_COMPARISON_INVITATION_PORT, useValue: port },
      {
        provide: SHARE_PRODUCT_ANALYTICS_PORT,
        useValue: {
          track: (event: ShareProductEvent): void => {
            analyticsEvents.push(event);
          }
        }
      }
    ] });
    const facade: ProfileComparisonInvitationCreatorStateFacade =
      TestBed.inject(ProfileComparisonInvitationCreatorStateFacade);

    facade.initialize(['VisitedParks', 'YearlyActivity']);
    facade.toggle('YearlyActivity');
    facade.create();

    expect(requests).toEqual([['VisitedParks']]);
    expect(facade.invitation()).toEqual(creation);
    expect(facade.loading()).toBe(false);
    expect(analyticsEvents).toEqual([
      { type: 'share_activation_started', recapType: 'profile-comparison' }
    ]);
  });
});
