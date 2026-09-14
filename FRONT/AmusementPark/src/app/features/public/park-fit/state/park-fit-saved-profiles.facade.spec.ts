import { TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';

import { ParkFitGroupProfile } from '@app/models/park-fit/park-fit-group-profile.model';
import {
  PARK_FIT_SAVED_PROFILES_DATA_PORT,
  ParkFitSavedProfilesDataPort
} from './park-fit-saved-profiles-data.port';
import { ParkFitSavedProfilesFacade } from './park-fit-saved-profiles.facade';

describe('ParkFitSavedProfilesFacade', () => {
  it('loads private profiles only when the public page explicitly requests them', () => {
    const profile: ParkFitGroupProfile = buildProfile();
    const listMine = vi.fn((): Observable<ParkFitGroupProfile[]> => of([profile]));
    const facade: ParkFitSavedProfilesFacade = createFacade({ listMine });

    expect(listMine).not.toHaveBeenCalled();
    facade.load();

    expect(listMine).toHaveBeenCalledOnce();
    expect(facade.status()).toBe('success');
    expect(facade.profiles()).toEqual([profile]);
  });

  it('does not retain stale profiles after a private load failure', () => {
    const facade: ParkFitSavedProfilesFacade = createFacade({
      listMine: () => throwError(() => new Error('offline'))
    });

    facade.load();

    expect(facade.status()).toBe('error');
    expect(facade.profiles()).toEqual([]);
  });
});

function createFacade(port: ParkFitSavedProfilesDataPort): ParkFitSavedProfilesFacade {
  TestBed.resetTestingModule();
  TestBed.configureTestingModule({
    providers: [
      ParkFitSavedProfilesFacade,
      { provide: PARK_FIT_SAVED_PROFILES_DATA_PORT, useValue: port }
    ]
  });
  return TestBed.inject(ParkFitSavedProfilesFacade);
}

function buildProfile(): ParkFitGroupProfile {
  return {
    profileId: 'profile-1',
    alias: 'Alex',
    heightCentimeters: 170,
    ageYears: 30,
    canBeAccompanied: false,
    companionAgeYears: null,
    createdAtUtc: '2026-09-14T14:00:00Z',
    updatedAtUtc: '2026-09-14T14:00:00Z',
    version: 1
  };
}
