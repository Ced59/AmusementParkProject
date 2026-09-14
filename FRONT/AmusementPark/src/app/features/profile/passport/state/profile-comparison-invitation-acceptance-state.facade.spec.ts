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
import { ProfileComparisonInvitationAcceptanceStateFacade } from './profile-comparison-invitation-acceptance-state.facade';

describe('ProfileComparisonInvitationAcceptanceStateFacade', () => {
  it('loads the consent preview before accepting the same token', () => {
    const analyticsEvents: ShareProductEvent[] = [];
    const calls: string[] = [];
    const preview: ProfileComparisonInvitationPreview = {
      status: 'Ready',
      creatorDisplayName: 'Camille',
      inviteeDisplayName: 'Alex',
      expiresAtUtc: '2026-09-20T12:00:00Z',
      acceptedAtUtc: null,
      categories: ['VisitedParks'],
      canAccept: true
    };
    const acceptance: ProfileComparisonInvitationAcceptance = {
      shareId: 'opaque-comparison',
      acceptedAtUtc: '2026-09-13T12:00:00Z',
      categories: ['VisitedParks']
    };
    const port: ProfileComparisonInvitationPort = {
      create: (_categories: ProfileComparisonCategory[]): Observable<ProfileComparisonInvitationCreation> => {
        throw new Error('Unexpected creation.');
      },
      preview: (token: string): Observable<ProfileComparisonInvitationPreview> => {
        calls.push(`preview:${token}`);
        return of(preview);
      },
      accept: (token: string): Observable<ProfileComparisonInvitationAcceptance> => {
        calls.push(`accept:${token}`);
        return of(acceptance);
      }
    };
    TestBed.configureTestingModule({ providers: [
      ProfileComparisonInvitationAcceptanceStateFacade,
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
    const facade: ProfileComparisonInvitationAcceptanceStateFacade =
      TestBed.inject(ProfileComparisonInvitationAcceptanceStateFacade);

    facade.load(' opaque-token ');
    facade.accept();

    expect(calls).toEqual(['preview:opaque-token', 'accept:opaque-token']);
    expect(facade.acceptance()).toEqual(acceptance);
    expect(facade.accepting()).toBe(false);
    expect(analyticsEvents).toEqual([
      { type: 'share_published', recapType: 'profile-comparison' }
    ]);
  });
});
