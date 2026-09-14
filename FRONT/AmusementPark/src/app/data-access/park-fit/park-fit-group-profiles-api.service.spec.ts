import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { ParkFitGroupProfileDraft } from '@app/models/park-fit/park-fit-group-profile-draft.model';
import { ParkFitGroupProfile } from '@app/models/park-fit/park-fit-group-profile.model';
import { provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { environment } from '../../../environments/environment';
import { ParkFitGroupProfilesApiService } from './park-fit-group-profiles-api.service';

describe('ParkFitGroupProfilesApiService', () => {
  let service: ParkFitGroupProfilesApiService;
  let httpTestingController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: provideCommonTestDependencies() });
    service = TestBed.inject(ParkFitGroupProfilesApiService);
    httpTestingController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTestingController.verify());

  it('uses authenticated owner-scoped endpoints and fences update and delete versions', () => {
    const draft: ParkFitGroupProfileDraft = {
      alias: 'Alex',
      heightCentimeters: 170,
      ageYears: 30,
      canBeAccompanied: false,
      companionAgeYears: null
    };
    const profile: ParkFitGroupProfile = buildProfile();

    service.listMine().subscribe();
    const list = httpTestingController.expectOne(
      `${environment.apiBaseUrl}me/park-fit/group-profiles`
    );
    expect(list.request.method).toBe('GET');
    list.flush([profile]);

    service.create(draft).subscribe();
    const create = httpTestingController.expectOne(
      `${environment.apiBaseUrl}me/park-fit/group-profiles`
    );
    expect(create.request.method).toBe('POST');
    expect(create.request.body).toEqual(draft);
    create.flush(profile);

    service.update('profile/1', 3, draft).subscribe();
    const update = httpTestingController.expectOne(
      `${environment.apiBaseUrl}me/park-fit/group-profiles/profile%2F1`
    );
    expect(update.request.method).toBe('PUT');
    expect(update.request.body).toEqual({ ...draft, expectedVersion: 3 });
    update.flush({ ...profile, version: 4 });

    service.delete('profile/1', 4).subscribe();
    const deletion = httpTestingController.expectOne((request) =>
      request.url === `${environment.apiBaseUrl}me/park-fit/group-profiles/profile%2F1`
      && request.params.get('expectedVersion') === '4'
    );
    expect(deletion.request.method).toBe('DELETE');
    deletion.flush(null);
  });

  it('loads a human-readable export from the private endpoint', () => {
    service.exportMine().subscribe((value): void => {
      expect(value.profiles[0]?.alias).toBe('Alex');
      expect(JSON.stringify(value)).not.toContain('profileId');
    });

    const request = httpTestingController.expectOne(
      `${environment.apiBaseUrl}me/park-fit/group-profiles/export`
    );
    request.flush({
      exportedAtUtc: '2026-09-14T14:00:00Z',
      profiles: [{
        alias: 'Alex',
        heightCentimeters: 170,
        ageYears: 30,
        canBeAccompanied: false,
        companionAgeYears: null
      }]
    });
  });
});

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
    version: 3
  };
}
