import { TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';

import { SharedPassportProfile } from '@app/models/sharing/share-publication.models';
import { SHARED_PASSPORT_PROFILE_PORT, SharedPassportProfilePort } from './shared-passport-profile-state-data.ports';
import { SharedPassportProfileStateFacade } from './shared-passport-profile-state.facade';

describe('SharedPassportProfileStateFacade', () => {
  it('loads a frozen public passport from its opaque link', () => {
    const port: SharedPassportProfilePort = {
      getSharedPassportProfile: (): Observable<SharedPassportProfile> => of(createProfile())
    };
    TestBed.configureTestingModule({ providers: [SharedPassportProfileStateFacade, { provide: SHARED_PASSPORT_PROFILE_PORT, useValue: port }] });
    const facade: SharedPassportProfileStateFacade = TestBed.inject(SharedPassportProfileStateFacade);

    facade.load('opaque-share-id');

    expect(facade.profile()?.passportProfile.displayName).toBe('Alex');
    expect(facade.notFound()).toBe(false);
  });

  it('maps a revoked link to not found', () => {
    const port: SharedPassportProfilePort = {
      getSharedPassportProfile: (): Observable<SharedPassportProfile> => throwError(() => ({ status: 404 }))
    };
    TestBed.configureTestingModule({ providers: [SharedPassportProfileStateFacade, { provide: SHARED_PASSPORT_PROFILE_PORT, useValue: port }] });
    const facade: SharedPassportProfileStateFacade = TestBed.inject(SharedPassportProfileStateFacade);

    facade.load('revoked-share-id');

    expect(facade.profile()).toBeNull();
    expect(facade.notFound()).toBe(true);
    expect(facade.error()).toBe(false);
  });
});

function createProfile(): SharedPassportProfile {
  return {
    publishedAtUtc: '2026-09-12T00:00:00Z',
    passportProfile: {
      displayName: 'Alex', visibility: 'Unlisted', allowsComparisons: true,
      countries: [], years: [], parks: [], personalRanking: [], missedItems: [],
      hasIncompleteCatalog: false, calculationVersion: 'passport-profile-v1', isEmpty: false
    }
  };
}
