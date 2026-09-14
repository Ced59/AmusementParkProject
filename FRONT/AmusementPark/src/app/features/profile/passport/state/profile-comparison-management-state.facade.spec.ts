import { TestBed } from '@angular/core/testing';
import { Observable, of } from 'rxjs';

import {
  ProfileComparisonRevocation,
  ProfileComparisonSummary,
} from '@app/models/sharing/profile-comparison.models';
import { SHARE_PRODUCT_ANALYTICS_PORT } from '@core/analytics/share-product-analytics.port';
import { ShareProductEvent } from '@core/analytics/share-product-event.model';
import {
  PROFILE_COMPARISON_MANAGEMENT_PORT,
  ProfileComparisonManagementPort,
} from './profile-comparison-management-data.ports';
import { ProfileComparisonManagementStateFacade } from './profile-comparison-management-state.facade';

describe('ProfileComparisonManagementStateFacade', () => {
  it('removes a comparison from the visible list after bilateral revocation', () => {
    const analyticsEvents: ShareProductEvent[] = [];
    const comparison: ProfileComparisonSummary = {
      shareId: 'opaque-share',
      otherDisplayName: 'Alex',
      createdAtUtc: '2026-09-13T12:00:00Z',
      categories: ['VisitedParks'],
      isModerationSuspended: false,
    };
    const port: ProfileComparisonManagementPort = {
      listMine: (): Observable<ProfileComparisonSummary[]> => of([comparison]),
      revoke: (shareId: string): Observable<ProfileComparisonRevocation> => {
        expect(shareId).toBe('opaque-share');
        return of({ revokedAtUtc: '2026-09-13T12:05:00Z' });
      },
    };
    TestBed.configureTestingModule({
      providers: [
        ProfileComparisonManagementStateFacade,
        { provide: PROFILE_COMPARISON_MANAGEMENT_PORT, useValue: port },
        {
          provide: SHARE_PRODUCT_ANALYTICS_PORT,
          useValue: {
            track: (event: ShareProductEvent): void => {
              analyticsEvents.push(event);
            }
          }
        },
      ],
    });
    const facade: ProfileComparisonManagementStateFacade = TestBed.inject(
      ProfileComparisonManagementStateFacade,
    );

    facade.load();
    facade.revoke('opaque-share');

    expect(facade.comparisons()).toEqual([]);
    expect(facade.revokingShareId()).toBeNull();
    expect(analyticsEvents).toEqual([
      { type: 'share_revoked', recapType: 'profile-comparison' }
    ]);
  });
});
