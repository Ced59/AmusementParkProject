import { ParkFitSearchRequest } from '@app/models/park-fit/park-fit-search.models';
import { ParkFitSearchFormValue } from '../models/park-fit-search-form.models';
import { mapParkFitFormToRequest } from './park-fit-search-request.mapper';

describe('mapParkFitFormToRequest', () => {
  it('keeps only structured anonymous criteria and maps exact ages as ranges', () => {
    const formValue: ParkFitSearchFormValue = {
      evaluationDate: '2026-10-10',
      members: [
        {
          sourceProfileId: 'profile-1',
          sourceAlias: 'Enfant',
          heightCentimeters: 121.8,
          ageYears: 8,
          canBeAccompanied: true,
          companionAgeYears: 42
        },
        {
          sourceProfileId: null,
          sourceAlias: null,
          heightCentimeters: null,
          ageYears: null,
          canBeAccompanied: false,
          companionAgeYears: 35
        }
      ],
      preferredAttractionTypes: ['FamilyRide', 'DarkRide'],
      preferIndoor: true
    };

    const request: ParkFitSearchRequest = mapParkFitFormToRequest(formValue);

    expect(request).toEqual({
      evaluationDate: '2026-10-10',
      members: [
        {
          heightCentimeters: 121,
          minimumAgeYears: 8,
          maximumAgeYears: 8,
          canBeAccompanied: true,
          companionMinimumAgeYears: 42,
          companionMaximumAgeYears: 42
        },
        {
          heightCentimeters: null,
          minimumAgeYears: null,
          maximumAgeYears: null,
          canBeAccompanied: false,
          companionMinimumAgeYears: null,
          companionMaximumAgeYears: null
        }
      ],
      preferredAttractionTypes: ['FamilyRide', 'DarkRide'],
      preferIndoor: true,
      countryCode: null,
      originLatitude: null,
      originLongitude: null,
      unknownDataPolicy: 'KeepWithWarning',
      maximumResults: 10
    });
    expect(JSON.stringify(request)).not.toContain('name');
    expect(JSON.stringify(request)).not.toContain('alias');
    expect(JSON.stringify(request)).not.toContain('profile-1');
  });

  it('includes only the explicitly supplied rounded origin coordinates', () => {
    const formValue: ParkFitSearchFormValue = {
      evaluationDate: '2026-10-10',
      members: [{
        sourceProfileId: null,
        sourceAlias: null,
        heightCentimeters: null,
        ageYears: null,
        canBeAccompanied: false,
        companionAgeYears: null
      }],
      preferredAttractionTypes: [],
      preferIndoor: false
    };

    const request: ParkFitSearchRequest = mapParkFitFormToRequest(
      formValue,
      { latitude: 50.6292, longitude: 3.0573 }
    );

    expect(request.originLatitude).toBe(50.6292);
    expect(request.originLongitude).toBe(3.0573);
  });
});
