import { ParkFitSearchRequest } from '@app/models/park-fit/park-fit-search.models';
import { ParkFitSearchFormValue, ParkFitSearchOrigin } from '../models/park-fit-search-form.models';

export function mapParkFitFormToRequest(
  value: ParkFitSearchFormValue,
  origin: ParkFitSearchOrigin | null = null
): ParkFitSearchRequest {
  return {
    evaluationDate: value.evaluationDate,
    members: value.members.map((member) => ({
      heightCentimeters: normalizeOptionalInteger(member.heightCentimeters),
      minimumAgeYears: normalizeOptionalInteger(member.ageYears),
      maximumAgeYears: normalizeOptionalInteger(member.ageYears),
      canBeAccompanied: member.canBeAccompanied,
      companionMinimumAgeYears: member.canBeAccompanied
        ? normalizeOptionalInteger(member.companionAgeYears)
        : null,
      companionMaximumAgeYears: member.canBeAccompanied
        ? normalizeOptionalInteger(member.companionAgeYears)
        : null
    })),
    preferredAttractionTypes: [...value.preferredAttractionTypes],
    preferIndoor: value.preferIndoor,
    countryCode: null,
    originLatitude: origin?.latitude ?? null,
    originLongitude: origin?.longitude ?? null,
    unknownDataPolicy: 'KeepWithWarning',
    maximumResults: 10
  };
}

function normalizeOptionalInteger(value: number | null): number | null {
  return value === null || !Number.isFinite(value) ? null : Math.trunc(value);
}
