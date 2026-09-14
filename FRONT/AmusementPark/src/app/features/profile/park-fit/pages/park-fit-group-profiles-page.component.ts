import { ChangeDetectionStrategy, Component, OnInit, Signal, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';

import { ParkFitGroupProfileDraft } from '@app/models/park-fit/park-fit-group-profile-draft.model';
import { ParkFitGroupProfile } from '@app/models/park-fit/park-fit-group-profile.model';
import { TranslationService } from '@app/services/translation.service';
import { UiButtonDirective, UiKickerComponent, UiSurfaceDirective } from '@ui/primitives';
import { ParkFitGroupProfileForm } from '../models/park-fit-group-profile-form.models';
import {
  ParkFitGroupProfileManagementFacade,
  ParkFitGroupProfileManagementStatus
} from '../state/park-fit-group-profile-management.facade';

@Component({
  selector: 'app-park-fit-group-profiles-page',
  templateUrl: './park-fit-group-profiles-page.component.html',
  styleUrl: './park-fit-group-profiles-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [ParkFitGroupProfileManagementFacade],
  imports: [
    ReactiveFormsModule,
    RouterLink,
    TranslateModule,
    UiButtonDirective,
    UiKickerComponent,
    UiSurfaceDirective
  ]
})
export class ParkFitGroupProfilesPageComponent implements OnInit {
  protected readonly profiles: Signal<ParkFitGroupProfile[]> = this.facade.profiles;
  protected readonly status: Signal<ParkFitGroupProfileManagementStatus> = this.facade.status;
  protected readonly saving: Signal<boolean> = this.facade.saving;
  protected readonly deletingId: Signal<string | null> = this.facade.deletingId;
  protected readonly exporting: Signal<boolean> = this.facade.exporting;
  protected readonly errorKey: Signal<string | null> = this.facade.errorKey;
  protected readonly editingId = signal<string | null>(null);
  protected readonly confirmingDeleteId = signal<string | null>(null);
  protected readonly currentLang: string;
  protected readonly form: ParkFitGroupProfileForm = createForm();

  constructor(
    protected readonly facade: ParkFitGroupProfileManagementFacade,
    translationService: TranslationService
  ) {
    this.currentLang = translationService.getCurrentLang() || 'en';
  }

  ngOnInit(): void {
    this.facade.load();
  }

  protected submit(): void {
    if (this.form.invalid || this.saving()) {
      this.form.markAllAsTouched();
      return;
    }

    const draft: ParkFitGroupProfileDraft = normalizeDraft(
      this.form.getRawValue()
    );
    const profile: ParkFitGroupProfile | undefined = this.profiles().find(
      (candidate: ParkFitGroupProfile): boolean =>
        candidate.profileId === this.editingId()
    );
    if (profile) {
      this.facade.update(profile, draft);
      return;
    }

    this.facade.create(draft);
  }

  protected edit(profile: ParkFitGroupProfile): void {
    this.confirmingDeleteId.set(null);
    this.editingId.set(profile.profileId);
    this.form.reset({
      alias: profile.alias,
      heightCentimeters: profile.heightCentimeters,
      ageYears: profile.ageYears,
      canBeAccompanied: profile.canBeAccompanied,
      companionAgeYears: profile.companionAgeYears
    });
  }

  protected newProfile(): void {
    this.editingId.set(null);
    this.confirmingDeleteId.set(null);
    this.form.reset({
      alias: '',
      heightCentimeters: null,
      ageYears: null,
      canBeAccompanied: false,
      companionAgeYears: null
    });
  }

  protected requestDelete(profile: ParkFitGroupProfile): void {
    if (this.confirmingDeleteId() !== profile.profileId) {
      this.confirmingDeleteId.set(profile.profileId);
      return;
    }

    this.confirmingDeleteId.set(null);
    if (this.editingId() === profile.profileId) {
      this.newProfile();
    }
    this.facade.delete(profile);
  }

  protected cancelDelete(): void {
    this.confirmingDeleteId.set(null);
  }

  protected factCount(profile: ParkFitGroupProfile): number {
    return [
      profile.heightCentimeters,
      profile.ageYears,
      profile.companionAgeYears
    ].filter((value: number | null): boolean => value !== null).length + 1;
  }
}

function createForm(): ParkFitGroupProfileForm {
  return new FormGroup({
    alias: new FormControl<string>('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(60)]
    }),
    heightCentimeters: new FormControl<number | null>(null, [
      Validators.min(40),
      Validators.max(260)
    ]),
    ageYears: new FormControl<number | null>(null, [
      Validators.min(0),
      Validators.max(130)
    ]),
    canBeAccompanied: new FormControl<boolean>(false, { nonNullable: true }),
    companionAgeYears: new FormControl<number | null>(null, [
      Validators.min(0),
      Validators.max(130)
    ])
  });
}

function normalizeDraft(value: ParkFitGroupProfileDraft): ParkFitGroupProfileDraft {
  const canBeAccompanied: boolean = value.canBeAccompanied;
  return {
    alias: value.alias.trim(),
    heightCentimeters: normalizeOptionalInteger(value.heightCentimeters),
    ageYears: normalizeOptionalInteger(value.ageYears),
    canBeAccompanied,
    companionAgeYears: canBeAccompanied
      ? normalizeOptionalInteger(value.companionAgeYears)
      : null
  };
}

function normalizeOptionalInteger(value: number | null): number | null {
  return value === null || !Number.isFinite(value) ? null : Math.trunc(value);
}
