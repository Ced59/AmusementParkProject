import { DestroyRef, Signal, signal } from '@angular/core';
import { FormArray, FormControl } from '@angular/forms';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { Subject } from 'rxjs';

import {
  ParkFitAttractionType,
  ParkFitSearchPark,
  ParkFitSearchRequest,
  ParkFitSearchResponse
} from '@app/models/park-fit/park-fit-search.models';
import { TranslationService } from '@app/services/translation.service';
import { SeoService } from '@core/seo/seo.service';
import { ParkFitMemberForm, ParkFitSearchForm } from '../models/park-fit-search-form.models';
import { ParkFitSearchFacade, ParkFitSearchStatus } from '../state/park-fit-search.facade';
import { ParkFitBrowserLocationFacade } from '../state/park-fit-browser-location.facade';
import { ParkFitSavedProfilesFacade } from '../state/park-fit-saved-profiles.facade';
import { AuthService } from '@app/services/auth/auth.service';
import { SharedService } from '@app/services/shared/shared.service';
import { ParkFitGroupProfile } from '@app/models/park-fit/park-fit-group-profile.model';
import { ParkFitStartPageComponent } from './park-fit-start-page.component';

interface ParkFitPageTestSurface {
  form: ParkFitSearchForm;
  members: FormArray<ParkFitMemberForm>;
  addMember(): void;
  removeMember(index: number): void;
  addSavedProfile(profile: ParkFitGroupProfile): void;
  isSavedProfileUsed(profileId: string): boolean;
  isAuthenticated: Signal<boolean>;
  togglePreference(type: ParkFitAttractionType): void;
  submit(): void;
}

describe('ParkFitStartPageComponent', () => {
  it('builds a bounded anonymous group without adding identity fields', () => {
    const search = vi.fn();
    const component: ParkFitStartPageComponent = createComponent(search);
    const page: ParkFitPageTestSurface = component as unknown as ParkFitPageTestSurface;

    component.ngOnInit();
    for (let index: number = 0; index < 10; index += 1) {
      page.addMember();
    }

    expect(page.members.length).toBe(8);
    const firstMember: ParkFitMemberForm = page.members.at(0);
    firstMember.patchValue({
      heightCentimeters: 121.9,
      ageYears: 8.7,
      canBeAccompanied: false,
      companionAgeYears: 35
    });
    page.form.controls.evaluationDate.setValue('2026-10-10');
    page.togglePreference('FamilyRide');
    page.submit();

    expect(search).toHaveBeenCalledOnce();
    const request: ParkFitSearchRequest = search.mock.calls[0]![0] as ParkFitSearchRequest;
    expect(request.members).toHaveLength(8);
    expect(request.members[0]).toEqual({
      heightCentimeters: 121,
      minimumAgeYears: 8,
      maximumAgeYears: 8,
      canBeAccompanied: false,
      companionMinimumAgeYears: null,
      companionMaximumAgeYears: null
    });
    expect(Object.keys(request.members[0]!)).not.toContain('name');
    expect(request.preferredAttractionTypes).toEqual(['FamilyRide']);
  });

  it('never lets the group become empty', () => {
    const component: ParkFitStartPageComponent = createComponent(vi.fn());
    const page: ParkFitPageTestSurface = component as unknown as ParkFitPageTestSurface;

    page.removeMember(0);

    expect(page.members.length).toBe(1);
  });

  it('consumes an explicitly requested position after one search', () => {
    const search = vi.fn();
    const clear = vi.fn();
    const locationFacade = {
      status: signal('ready').asReadonly(),
      position: signal({ latitude: 50.6292, longitude: 3.0573 }).asReadonly(),
      errorKey: signal(null).asReadonly(),
      request: vi.fn(),
      clear
    } as unknown as ParkFitBrowserLocationFacade;
    const component: ParkFitStartPageComponent = createComponent(
      search,
      null,
      undefined,
      undefined,
      undefined,
      undefined,
      locationFacade
    );
    const page: ParkFitPageTestSurface = component as unknown as ParkFitPageTestSurface;
    page.form.controls.evaluationDate.setValue('2026-10-10');

    page.submit();

    const request: ParkFitSearchRequest = search.mock.calls[0]![0] as ParkFitSearchRequest;
    expect(request.originLatitude).toBe(50.6292);
    expect(request.originLongitude).toBe(3.0573);
    expect(clear).toHaveBeenCalledOnce();
  });

  it('waits for a pending location request before submitting', () => {
    const search = vi.fn();
    const locationFacade = {
      status: signal('loading').asReadonly(),
      position: signal(null).asReadonly(),
      errorKey: signal(null).asReadonly(),
      request: vi.fn(),
      clear: vi.fn()
    } as unknown as ParkFitBrowserLocationFacade;
    const component: ParkFitStartPageComponent = createComponent(
      search,
      null,
      undefined,
      undefined,
      undefined,
      undefined,
      locationFacade
    );
    const page: ParkFitPageTestSurface = component as unknown as ParkFitPageTestSurface;

    page.submit();

    expect(search).not.toHaveBeenCalled();
  });

  it('restores only the in-memory criteria and applies private SEO metadata', () => {
    const lastRequest: ParkFitSearchRequest = buildRequest();
    const seoService: Pick<SeoService, 'applyParkFitSeo'> = {
      applyParkFitSeo: vi.fn()
    };
    const component: ParkFitStartPageComponent = createComponent(vi.fn(), lastRequest, seoService);
    const page: ParkFitPageTestSurface = component as unknown as ParkFitPageTestSurface;

    component.ngOnInit();

    expect(page.form.controls.evaluationDate.value).toBe('2026-10-10');
    expect(page.form.controls.preferredAttractionTypes.value).toEqual(['FamilyRide']);
    expect(page.members.at(0).controls.heightCentimeters.value).toBe(120);
    expect(seoService.applyParkFitSeo).toHaveBeenCalledWith(
      'parkFit.seo.title',
      'parkFit.seo.description',
      '/fr/park-fit'
    );
  });

  it('injects one saved private profile without sending its alias or identifier', () => {
    const search = vi.fn();
    const component: ParkFitStartPageComponent = createComponent(search);
    const page: ParkFitPageTestSurface = component as unknown as ParkFitPageTestSurface;
    const profile: ParkFitGroupProfile = {
      profileId: 'profile-1',
      alias: 'Lina',
      heightCentimeters: 121,
      ageYears: 8,
      canBeAccompanied: true,
      companionAgeYears: 40,
      createdAtUtc: '2026-09-14T14:00:00Z',
      updatedAtUtc: '2026-09-14T14:00:00Z',
      version: 1
    };

    page.addSavedProfile(profile);
    page.form.controls.evaluationDate.setValue('2026-10-10');
    page.submit();

    expect(page.members.length).toBe(1);
    expect(page.isSavedProfileUsed('profile-1')).toBe(true);
    const request: ParkFitSearchRequest = search.mock.calls[0]![0] as ParkFitSearchRequest;
    expect(request.members[0]?.heightCentimeters).toBe(121);
    expect(JSON.stringify(request)).not.toContain('Lina');
    expect(JSON.stringify(request)).not.toContain('profile-1');
  });

  it('loads saved profiles immediately after an in-page login', () => {
    let authenticated: boolean = false;
    const loginStatus: Subject<void> = new Subject<void>();
    const load = vi.fn();
    const savedProfilesFacade = {
      profiles: signal<ParkFitGroupProfile[]>([]).asReadonly(),
      status: signal('idle').asReadonly(),
      load
    } as unknown as ParkFitSavedProfilesFacade;
    const component: ParkFitStartPageComponent = createComponent(
      vi.fn(),
      null,
      { applyParkFitSeo: vi.fn() },
      (): boolean => authenticated,
      loginStatus,
      savedProfilesFacade
    );
    const page: ParkFitPageTestSurface = component as unknown as ParkFitPageTestSurface;

    component.ngOnInit();
    expect(page.isAuthenticated()).toBe(false);
    expect(load).not.toHaveBeenCalled();

    authenticated = true;
    loginStatus.next();

    expect(page.isAuthenticated()).toBe(true);
    expect(load).toHaveBeenCalledOnce();
  });
});

function createComponent(
  search: ReturnType<typeof vi.fn>,
  lastRequest: ParkFitSearchRequest | null = null,
  seoService: Pick<SeoService, 'applyParkFitSeo'> = { applyParkFitSeo: vi.fn() },
  isLoggedIn: () => boolean = (): boolean => false,
  loginStatus: Subject<void> = new Subject<void>(),
  savedProfilesFacade: ParkFitSavedProfilesFacade = {
    profiles: signal<ParkFitGroupProfile[]>([]).asReadonly(),
    status: signal('idle').asReadonly(),
    load: vi.fn()
  } as unknown as ParkFitSavedProfilesFacade,
  locationFacade: ParkFitBrowserLocationFacade = {
    status: signal('idle').asReadonly(),
    position: signal(null).asReadonly(),
    errorKey: signal(null).asReadonly(),
    request: vi.fn(),
    clear: vi.fn()
  } as unknown as ParkFitBrowserLocationFacade
): ParkFitStartPageComponent {
  const status: Signal<ParkFitSearchStatus> = signal<ParkFitSearchStatus>('idle').asReadonly();
  const response: Signal<ParkFitSearchResponse | null> = signal<ParkFitSearchResponse | null>(null).asReadonly();
  const park: Signal<ParkFitSearchPark | null> = signal<ParkFitSearchPark | null>(null).asReadonly();
  const visibleParks: Signal<ParkFitSearchPark[]> = signal<ParkFitSearchPark[]>([]).asReadonly();
  const errorKey: Signal<string | null> = signal<string | null>(null).asReadonly();
  const lastRequestSignal: Signal<ParkFitSearchRequest | null> = signal(lastRequest).asReadonly();
  const facade = {
    status,
    response,
    firstPark: park,
    visibleParks,
    errorKey,
    lastRequest: lastRequestSignal,
    search
  };
  const route = {
    snapshot: { paramMap: convertToParamMap({}) },
    parent: {
      snapshot: { paramMap: convertToParamMap({ lang: 'fr' }) },
      parent: null
    }
  };
  const translationService = {
    getCurrentLang: (): string => 'fr',
    languageChanged: new Subject<string>()
  };
  const translateService = {
    instant: (key: string): string => key
  };
  const destroyRef: DestroyRef = {
    destroyed: false,
    onDestroy: (): (() => void) => (): void => undefined
  };
  return new ParkFitStartPageComponent(
    route as ActivatedRoute,
    { url: '/fr/park-fit' } as Router,
    facade as unknown as ParkFitSearchFacade,
    translationService as unknown as TranslationService,
    translateService as TranslateService,
    seoService as SeoService,
    { isLoggedIn } as AuthService,
    { getLoginStatusListener: (): Subject<void> => loginStatus } as unknown as SharedService,
    savedProfilesFacade,
    locationFacade,
    destroyRef
  );
}

function buildRequest(): ParkFitSearchRequest {
  return {
    evaluationDate: '2026-10-10',
    members: [{
      heightCentimeters: 120,
      minimumAgeYears: 8,
      maximumAgeYears: 8,
      canBeAccompanied: false,
      companionMinimumAgeYears: null,
      companionMaximumAgeYears: null
    }],
    preferredAttractionTypes: ['FamilyRide'],
    preferIndoor: true,
    countryCode: null,
    originLatitude: null,
    originLongitude: null,
    unknownDataPolicy: 'KeepWithWarning',
    maximumResults: 10
  };
}
