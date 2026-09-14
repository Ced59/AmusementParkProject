import { FormControl, FormGroup } from '@angular/forms';

export interface ParkFitGroupProfileFormControls {
  alias: FormControl<string>;
  heightCentimeters: FormControl<number | null>;
  ageYears: FormControl<number | null>;
  canBeAccompanied: FormControl<boolean>;
  companionAgeYears: FormControl<number | null>;
}

export type ParkFitGroupProfileForm = FormGroup<ParkFitGroupProfileFormControls>;
