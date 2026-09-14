import { TestBed } from '@angular/core/testing';
import { Observable, of } from 'rxjs';

import { ParkFitGroupProfileDraft } from '@app/models/park-fit/park-fit-group-profile-draft.model';
import { ParkFitGroupProfileExport } from '@app/models/park-fit/park-fit-group-profile-export.model';
import { ParkFitGroupProfile } from '@app/models/park-fit/park-fit-group-profile.model';
import {
  PARK_FIT_GROUP_PROFILE_MANAGEMENT_DATA_PORT,
  ParkFitGroupProfileManagementDataPort
} from './park-fit-group-profile-management-data.port';
import { ParkFitGroupProfileManagementFacade } from './park-fit-group-profile-management.facade';

describe('ParkFitGroupProfileManagementFacade', () => {
  it('creates updates and deletes one private profile with its current version', () => {
    const initial: ParkFitGroupProfile = buildProfile(1, 'Alex');
    const created: ParkFitGroupProfile = { ...initial, profileId: 'profile-2', alias: 'Lina' };
    const updated: ParkFitGroupProfile = { ...created, alias: 'Lina 8 ans', version: 2 };
    const create = vi.fn((): Observable<ParkFitGroupProfile> => of(created));
    const update = vi.fn((): Observable<ParkFitGroupProfile> => of(updated));
    const deletion = vi.fn((): Observable<void> => of(undefined));
    const facade: ParkFitGroupProfileManagementFacade = createFacade({
      listMine: () => of([initial]),
      create,
      update,
      delete: deletion,
      exportMine: (): Observable<ParkFitGroupProfileExport> => of({
        exportedAtUtc: '2026-09-14T14:00:00Z',
        profiles: []
      })
    });
    const draft: ParkFitGroupProfileDraft = {
      alias: 'Lina',
      heightCentimeters: 120,
      ageYears: 8,
      canBeAccompanied: true,
      companionAgeYears: 40
    };

    facade.load();
    facade.create(draft);
    facade.update(created, { ...draft, alias: 'Lina 8 ans' });
    facade.delete(updated);

    expect(create).toHaveBeenCalledWith(draft);
    expect(update).toHaveBeenCalledWith('profile-2', 1, { ...draft, alias: 'Lina 8 ans' });
    expect(deletion).toHaveBeenCalledWith('profile-2', 2);
    expect(facade.profiles()).toEqual([initial]);
  });
});

function createFacade(
  port: ParkFitGroupProfileManagementDataPort
): ParkFitGroupProfileManagementFacade {
  TestBed.resetTestingModule();
  TestBed.configureTestingModule({
    providers: [
      ParkFitGroupProfileManagementFacade,
      { provide: PARK_FIT_GROUP_PROFILE_MANAGEMENT_DATA_PORT, useValue: port }
    ]
  });
  return TestBed.inject(ParkFitGroupProfileManagementFacade);
}

function buildProfile(version: number, alias: string): ParkFitGroupProfile {
  return {
    profileId: 'profile-1',
    alias,
    heightCentimeters: 170,
    ageYears: 30,
    canBeAccompanied: false,
    companionAgeYears: null,
    createdAtUtc: '2026-09-14T14:00:00Z',
    updatedAtUtc: '2026-09-14T14:00:00Z',
    version
  };
}
