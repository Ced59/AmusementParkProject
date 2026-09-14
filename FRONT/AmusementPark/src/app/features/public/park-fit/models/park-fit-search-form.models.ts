import { FormArray, FormControl, FormGroup } from '@angular/forms';

import { ParkFitAttractionType } from '@app/models/park-fit/park-fit-search.models';

export interface ParkFitMemberFormControls {
  heightCentimeters: FormControl<number | null>;
  ageYears: FormControl<number | null>;
  canBeAccompanied: FormControl<boolean>;
  companionAgeYears: FormControl<number | null>;
}

export type ParkFitMemberForm = FormGroup<ParkFitMemberFormControls>;

export interface ParkFitSearchFormControls {
  evaluationDate: FormControl<string>;
  members: FormArray<ParkFitMemberForm>;
  preferredAttractionTypes: FormControl<ParkFitAttractionType[]>;
  preferIndoor: FormControl<boolean>;
}

export type ParkFitSearchForm = FormGroup<ParkFitSearchFormControls>;

export interface ParkFitSearchFormMemberValue {
  heightCentimeters: number | null;
  ageYears: number | null;
  canBeAccompanied: boolean;
  companionAgeYears: number | null;
}

export interface ParkFitSearchFormValue {
  evaluationDate: string;
  members: ParkFitSearchFormMemberValue[];
  preferredAttractionTypes: ParkFitAttractionType[];
  preferIndoor: boolean;
}

export interface ParkFitPreferenceOption {
  value: ParkFitAttractionType;
  labelKey: string;
  iconClass: string;
}
