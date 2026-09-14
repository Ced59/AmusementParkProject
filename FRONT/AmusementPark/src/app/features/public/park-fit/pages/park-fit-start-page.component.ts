import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormArray, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

import {
  ParkFitAttractionType,
  ParkFitSearchPark,
  ParkFitSearchRequest,
  ParkFitSearchResponse
} from '@app/models/park-fit/park-fit-search.models';
import { ParkFitGroupProfile } from '@app/models/park-fit/park-fit-group-profile.model';
import { AuthService } from '@app/services/auth/auth.service';
import { SharedService } from '@app/services/shared/shared.service';
import { TranslationService } from '@app/services/translation.service';
import { SeoService } from '@core/seo/seo.service';
import { buildPublicParkRouteCommands } from '@shared/utils/routing/public-detail-route.helpers';
import { resolveLocalizedCountryName } from '@shared/utils/display/country-display.helpers';
import { resolveLanguageFromActivatedRoute } from '@shared/utils/routing/route-language.utils';
import { UiButtonDirective, UiChipComponent, UiKickerComponent, UiSurfaceDirective } from '@ui/primitives';
import { mapParkFitFormToRequest } from '../mappers/park-fit-search-request.mapper';
import {
  ParkFitMemberForm,
  ParkFitPreferenceOption,
  ParkFitSearchForm,
  ParkFitSearchFormValue,
  ParkFitSearchOrigin
} from '../models/park-fit-search-form.models';
import {
  ParkFitBrowserLocationFacade,
  ParkFitBrowserLocationStatus
} from '../state/park-fit-browser-location.facade';
import { ParkFitSearchFacade, ParkFitSearchStatus } from '../state/park-fit-search.facade';
import {
  ParkFitSavedProfilesFacade,
  ParkFitSavedProfilesStatus
} from '../state/park-fit-saved-profiles.facade';

const MAXIMUM_MEMBER_COUNT = 8;

@Component({
  selector: 'app-park-fit-start-page',
  templateUrl: './park-fit-start-page.component.html',
  styleUrl: './park-fit-start-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [ParkFitSavedProfilesFacade, ParkFitBrowserLocationFacade],
  imports: [
    ReactiveFormsModule,
    RouterLink,
    TranslateModule,
    UiButtonDirective,
    UiChipComponent,
    UiKickerComponent,
    UiSurfaceDirective
  ]
})
export class ParkFitStartPageComponent implements OnInit {
  protected readonly currentLang = signal<string>('en');
  protected readonly status: Signal<ParkFitSearchStatus> = this.facade.status;
  protected readonly response: Signal<ParkFitSearchResponse | null> = this.facade.response;
  protected readonly firstPark: Signal<ParkFitSearchPark | null> = this.facade.firstPark;
  protected readonly visibleParks: Signal<ParkFitSearchPark[]> = this.facade.visibleParks;
  protected readonly errorKey: Signal<string | null> = this.facade.errorKey;
  protected readonly savedProfiles: Signal<ParkFitGroupProfile[]> = this.savedProfilesFacade.profiles;
  protected readonly savedProfilesStatus: Signal<ParkFitSavedProfilesStatus> =
    this.savedProfilesFacade.status;
  protected readonly isAuthenticated = signal<boolean>(false);
  protected readonly locationStatus: Signal<ParkFitBrowserLocationStatus> =
    this.locationFacade.status;
  protected readonly location: Signal<ParkFitSearchOrigin | null> =
    this.locationFacade.position;
  protected readonly locationErrorKey: Signal<string | null> =
    this.locationFacade.errorKey;
  protected readonly parkRoute: Signal<string[] | null> = computed(() => {
    const park: ParkFitSearchPark | null = this.firstPark();
    return park
      ? buildPublicParkRouteCommands({
        language: this.currentLang(),
        parkId: park.parkId,
        parkName: park.parkName
      })
      : null;
  });
  protected readonly preferenceOptions: readonly ParkFitPreferenceOption[] = [
    { value: 'FamilyRide', labelKey: 'parkFit.preferences.types.familyRide', iconClass: 'pi pi-users' },
    { value: 'RollerCoaster', labelKey: 'parkFit.preferences.types.rollerCoaster', iconClass: 'pi pi-bolt' },
    { value: 'DarkRide', labelKey: 'parkFit.preferences.types.darkRide', iconClass: 'pi pi-moon' },
    { value: 'WaterRide', labelKey: 'parkFit.preferences.types.waterRide', iconClass: 'pi pi-sun' },
    { value: 'ThrillRide', labelKey: 'parkFit.preferences.types.thrillRide', iconClass: 'pi pi-bolt' },
    { value: 'Playground', labelKey: 'parkFit.preferences.types.playground', iconClass: 'pi pi-sparkles' }
  ];
  protected readonly form: ParkFitSearchForm = new FormGroup({
    evaluationDate: new FormControl<string>(todayInputValue(), {
      nonNullable: true,
      validators: [Validators.required]
    }),
    members: new FormArray<ParkFitMemberForm>([createMemberForm()]),
    preferredAttractionTypes: new FormControl<ParkFitAttractionType[]>([], { nonNullable: true }),
    preferIndoor: new FormControl<boolean>(false, { nonNullable: true })
  });

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly facade: ParkFitSearchFacade,
    private readonly translationService: TranslationService,
    private readonly translateService: TranslateService,
    private readonly seoService: SeoService,
    private readonly authService: AuthService,
    private readonly sharedService: SharedService,
    private readonly savedProfilesFacade: ParkFitSavedProfilesFacade,
    private readonly locationFacade: ParkFitBrowserLocationFacade,
    private readonly destroyRef: DestroyRef
  ) {
  }

  ngOnInit(): void {
    const language: string = resolveLanguageFromActivatedRoute(
      this.route,
      this.translationService.getCurrentLang() || 'en'
    );
    this.currentLang.set(language);
    this.restoreLastRequest(this.facade.lastRequest());
    this.applySeo();
    this.refreshAuthenticationState();

    this.sharedService.getLoginStatusListener()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((): void => this.refreshAuthenticationState());

    this.translationService.languageChanged
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((currentLanguage: string): void => {
        this.currentLang.set(currentLanguage);
        this.applySeo();
      });
  }

  private refreshAuthenticationState(): void {
    const authenticated: boolean = this.authService.isLoggedIn();
    this.isAuthenticated.set(authenticated);
    if (authenticated) {
      this.savedProfilesFacade.load();
    }
  }

  protected get members(): FormArray<ParkFitMemberForm> {
    return this.form.controls.members;
  }

  protected addMember(): void {
    if (this.members.length >= MAXIMUM_MEMBER_COUNT) {
      return;
    }

    this.members.push(createMemberForm());
  }

  protected removeMember(index: number): void {
    if (this.members.length <= 1 || index < 0 || index >= this.members.length) {
      return;
    }

    this.members.removeAt(index);
  }

  protected addSavedProfile(profile: ParkFitGroupProfile): void {
    if (this.isSavedProfileUsed(profile.profileId) || this.members.length >= MAXIMUM_MEMBER_COUNT) {
      return;
    }

    const member: ParkFitMemberForm = createMemberForm({
      sourceProfileId: profile.profileId,
      sourceAlias: profile.alias,
      heightCentimeters: profile.heightCentimeters,
      ageYears: profile.ageYears,
      canBeAccompanied: profile.canBeAccompanied,
      companionAgeYears: profile.companionAgeYears
    });
    if (this.members.length === 1 && isEmptyMember(this.members.at(0))) {
      this.members.setControl(0, member);
      return;
    }

    this.members.push(member);
  }

  protected isSavedProfileUsed(profileId: string): boolean {
    return this.members.controls.some((member: ParkFitMemberForm): boolean =>
      member.controls.sourceProfileId.value === profileId);
  }

  protected togglePreference(type: ParkFitAttractionType): void {
    const control: FormControl<ParkFitAttractionType[]> = this.form.controls.preferredAttractionTypes;
    const current: ParkFitAttractionType[] = control.value;
    control.setValue(
      current.includes(type)
        ? current.filter((value: ParkFitAttractionType): boolean => value !== type)
        : [...current, type]
    );
  }

  protected hasPreference(type: ParkFitAttractionType): boolean {
    return this.form.controls.preferredAttractionTypes.value.includes(type);
  }

  protected submit(): void {
    if (this.locationStatus() === 'loading') {
      return;
    }

    if (this.form.invalid || this.members.length === 0) {
      this.form.markAllAsTouched();
      return;
    }

    const value: ParkFitSearchFormValue = this.form.getRawValue();
    const origin: ParkFitSearchOrigin | null = this.location();
    this.facade.search(mapParkFitFormToRequest(value, origin));
    if (origin) {
      this.locationFacade.clear();
    }
  }

  protected requestLocation(): void {
    this.locationFacade.request();
  }

  protected clearLocation(): void {
    this.locationFacade.clear();
  }

  protected countryName(countryCode: string | null): string {
    return resolveLocalizedCountryName(countryCode, this.currentLang()) ?? '';
  }

  protected scoreStateKey(scoreState: string): string {
    const knownStates: readonly string[] = ['Available', 'Capped', 'Suspended', 'Excluded'];
    return knownStates.includes(scoreState)
      ? `parkFit.result.states.${scoreState}`
      : 'parkFit.result.states.Unknown';
  }

  private restoreLastRequest(request: ParkFitSearchRequest | null): void {
    if (!request) {
      return;
    }

    this.members.clear();
    for (const member of request.members) {
      this.members.push(createMemberForm({
        sourceProfileId: null,
        sourceAlias: null,
        heightCentimeters: member.heightCentimeters,
        ageYears: member.minimumAgeYears,
        canBeAccompanied: member.canBeAccompanied,
        companionAgeYears: member.companionMinimumAgeYears
      }));
    }

    this.form.patchValue({
      evaluationDate: request.evaluationDate,
      preferredAttractionTypes: [...request.preferredAttractionTypes],
      preferIndoor: request.preferIndoor
    });

  }

  private applySeo(): void {
    this.seoService.applyParkFitSeo(
      this.translateService.instant('parkFit.seo.title'),
      this.translateService.instant('parkFit.seo.description'),
      this.router.url
    );
  }
}

function createMemberForm(initial?: {
  sourceProfileId: string | null;
  sourceAlias: string | null;
  heightCentimeters: number | null;
  ageYears: number | null;
  canBeAccompanied: boolean;
  companionAgeYears: number | null;
}): ParkFitMemberForm {
  return new FormGroup({
    sourceProfileId: new FormControl<string | null>(initial?.sourceProfileId ?? null),
    sourceAlias: new FormControl<string | null>(initial?.sourceAlias ?? null),
    heightCentimeters: new FormControl<number | null>(initial?.heightCentimeters ?? null, [
      Validators.min(40),
      Validators.max(260)
    ]),
    ageYears: new FormControl<number | null>(initial?.ageYears ?? null, [
      Validators.min(0),
      Validators.max(130)
    ]),
    canBeAccompanied: new FormControl<boolean>(initial?.canBeAccompanied ?? false, { nonNullable: true }),
    companionAgeYears: new FormControl<number | null>(initial?.companionAgeYears ?? null, [
      Validators.min(0),
      Validators.max(130)
    ])
  });
}

function isEmptyMember(member: ParkFitMemberForm): boolean {
  const value = member.getRawValue();
  return value.sourceProfileId === null
    && value.heightCentimeters === null
    && value.ageYears === null
    && !value.canBeAccompanied
    && value.companionAgeYears === null;
}

function todayInputValue(): string {
  const today: Date = new Date();
  const year: string = String(today.getFullYear()).padStart(4, '0');
  const month: string = String(today.getMonth() + 1).padStart(2, '0');
  const day: string = String(today.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}
