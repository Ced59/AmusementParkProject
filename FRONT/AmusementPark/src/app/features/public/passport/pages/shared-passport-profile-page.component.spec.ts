import { EventEmitter } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { Observable, of } from 'rxjs';

import { SharedPassportProfile } from '@app/models/sharing/share-publication.models';
import { TranslationService } from '@app/services/translation.service';
import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { SeoService } from '@core/seo/seo.service';
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import { ImagesApiService } from '@data-access/images/images-api.service';
import {
  SHARED_PASSPORT_PROFILE_PORT,
  SharedPassportProfilePort
} from '../state/shared-passport-profile-state-data.ports';
import { SharedPassportProfilePageComponent } from './shared-passport-profile-page.component';

describe('SharedPassportProfilePageComponent', () => {
  let fixture: ComponentFixture<SharedPassportProfilePageComponent>;
  let applySeo: ReturnType<typeof vi.fn>;

  beforeEach(async () => {
    applySeo = vi.fn();
    const port: SharedPassportProfilePort = {
      getSharedPassportProfile: (_shareId: string): Observable<SharedPassportProfile> =>
        of(createSharedPassport())
    };
    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, SharedPassportProfilePageComponent],
      providers: [
        ...provideCommonTestDependencies(),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: convertToParamMap({ lang: 'fr', shareId: 'opaque-share-id' })
            },
            parent: null
          }
        },
        { provide: SHARED_PASSPORT_PROFILE_PORT, useValue: port },
        {
          provide: TranslationService,
          useValue: {
            getCurrentLang: (): string => 'fr',
            languageChanged: new EventEmitter<string>()
          }
        },
        {
          provide: SeoService,
          useValue: {
            applyRouteDefaults: vi.fn(),
            applySharedVisitRecapSeo: applySeo,
            applyNotFoundSeo: vi.fn()
          }
        },
        { provide: SsrHttpStatusService, useValue: { setNotFound: vi.fn() } },
        { provide: ImagesApiService, useValue: { resolveImageUrl: (): null => null } }
      ]
    }).compileComponents();
  });

  it('uses the exact public passport version for its Open Graph image', () => {
    fixture = TestBed.createComponent(SharedPassportProfilePageComponent);
    fixture.detectChanges();

    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    expect(host.textContent).toContain('Alex');
    expect(host.textContent).toContain('Denain Évasion');
    expect(host.textContent).not.toContain('opaque-share-id');
    expect(applySeo).toHaveBeenCalledWith(
      expect.any(String),
      expect.any(String),
      expect.any(String),
      expect.stringContaining('sharing/social-images/passport/opaque-share-id/v7/t1/fr.png'),
      expect.any(String),
      expect.any(Array)
    );
  });
});

function createSharedPassport(): SharedPassportProfile {
  return {
    publishedAtUtc: '2026-09-13T08:00:00Z',
    publicationVersion: 7,
    passportProfile: {
      displayName: 'Alex',
      visibility: 'Unlisted',
      allowsComparisons: true,
      parkCount: 1,
      visitCount: 2,
      totalRideCount: 4,
      distinctItemCount: 3,
      countries: [],
      years: [],
      parks: [{
        name: 'Denain Évasion',
        countryCode: 'FR',
        visitCount: 2,
        firstVisitYear: 2025,
        lastVisitYear: 2026,
        completedRideCount: 4
      }],
      personalRanking: [],
      missedItems: [],
      hasIncompleteCatalog: false,
      calculationVersion: 'passport-profile-v1',
      isEmpty: false
    }
  };
}
