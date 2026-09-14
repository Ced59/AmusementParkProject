import { TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';

import { SharedPassportProfile } from '@app/models/sharing/share-publication.models';
import { SHARE_PRODUCT_ANALYTICS_PORT } from '@core/analytics/share-product-analytics.port';
import { ShareProductEvent } from '@core/analytics/share-product-event.model';
import { SHARED_PASSPORT_PROFILE_PORT, SharedPassportProfilePort } from './shared-passport-profile-state-data.ports';
import { SharedPassportProfileStateFacade } from './shared-passport-profile-state.facade';

describe('SharedPassportProfileStateFacade', () => {
  it('loads a frozen public passport from its opaque link', () => {
    const analyticsEvents: ShareProductEvent[] = [];
    const port: SharedPassportProfilePort = {
      getSharedPassportProfile: (): Observable<SharedPassportProfile> => of(createProfile())
    };
    TestBed.configureTestingModule({ providers: [
      SharedPassportProfileStateFacade,
      { provide: SHARED_PASSPORT_PROFILE_PORT, useValue: port },
      {
        provide: SHARE_PRODUCT_ANALYTICS_PORT,
        useValue: {
          track: (event: ShareProductEvent): void => {
            analyticsEvents.push(event);
          }
        }
      }
    ] });
    const facade: SharedPassportProfileStateFacade = TestBed.inject(SharedPassportProfileStateFacade);

    facade.load('opaque-share-id');

    expect(facade.profile()?.passportProfile.displayName).toBe('Alex');
    expect(facade.notFound()).toBe(false);
    facade.trackPassportCta();
    expect(analyticsEvents).toEqual([
      { type: 'share_opened', recapType: 'passport-profile' },
      { type: 'share_cta_passport_started', recapType: 'passport-profile' }
    ]);
  });

  it('maps a revoked link to not found', () => {
    const analyticsEvents: ShareProductEvent[] = [];
    const port: SharedPassportProfilePort = {
      getSharedPassportProfile: (): Observable<SharedPassportProfile> => throwError(() => ({ status: 404 }))
    };
    TestBed.configureTestingModule({ providers: [
      SharedPassportProfileStateFacade,
      { provide: SHARED_PASSPORT_PROFILE_PORT, useValue: port },
      {
        provide: SHARE_PRODUCT_ANALYTICS_PORT,
        useValue: {
          track: (event: ShareProductEvent): void => {
            analyticsEvents.push(event);
          }
        }
      }
    ] });
    const facade: SharedPassportProfileStateFacade = TestBed.inject(SharedPassportProfileStateFacade);

    facade.load('revoked-share-id');

    expect(facade.profile()).toBeNull();
    expect(facade.notFound()).toBe(true);
    expect(facade.error()).toBe(false);
    expect(analyticsEvents).toEqual([]);
  });
});

function createProfile(): SharedPassportProfile {
  return {
    publishedAtUtc: '2026-09-12T00:00:00Z',
    publicationVersion: 3,
    passportProfile: {
      displayName: 'Alex', visibility: 'Unlisted', allowsComparisons: true,
      countries: [], years: [], parks: [], personalRanking: [], missedItems: [],
      hasIncompleteCatalog: false, calculationVersion: 'passport-profile-v1', isEmpty: false
    }
  };
}
