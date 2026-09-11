import { EventEmitter } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { Observable, of } from 'rxjs';

import { SharedYearRecap } from '@app/models/sharing/share-publication.models';
import { TranslationService } from '@app/services/translation.service';
import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { SeoService } from '@core/seo/seo.service';
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import { SHARED_YEAR_RECAP_PORT, SharedYearRecapPort } from '../state/shared-year-recap-state-data.ports';
import { SharedYearRecapPageComponent } from './shared-year-recap-page.component';

describe('SharedYearRecapPageComponent', () => {
  let fixture: ComponentFixture<SharedYearRecapPageComponent>;
  let applySeo: ReturnType<typeof vi.fn>;

  beforeEach(async () => {
    applySeo = vi.fn();
    const port: SharedYearRecapPort = {
      getSharedYear: (_shareId: string): Observable<SharedYearRecap> => of(createSharedYear())
    };
    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, SharedYearRecapPageComponent],
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
        { provide: SHARED_YEAR_RECAP_PORT, useValue: port },
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
        { provide: SsrHttpStatusService, useValue: { setNotFound: vi.fn() } }
      ]
    }).compileComponents();
  });

  it('renders names and evidence without displaying the opaque or technical identifiers', () => {
    fixture = TestBed.createComponent(SharedYearRecapPageComponent);
    fixture.detectChanges();

    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    expect(host.textContent).toContain('2026');
    expect(host.textContent).toContain('Denain Évasion');
    expect(host.textContent).toContain('Le Galion');
    expect(host.textContent).toContain('4,5 / 5');
    expect(host.textContent).not.toContain('opaque-share-id');
    expect(host.textContent).not.toContain('park-technical-id');
    expect(host.textContent).not.toContain('item-technical-id');
    expect(host.querySelector('.shared-year__breadcrumb a')).not.toBeNull();
    expect(applySeo).toHaveBeenCalled();
  });
});

function createSharedYear(): SharedYearRecap {
  return {
    publishedAtUtc: '2026-09-11T08:00:00Z',
    yearRecap: {
      year: 2026,
      parkCount: 1,
      visitCount: 2,
      approximateVisitCount: 0,
      approximateVisitRate: 0,
      totalRideCount: 4,
      distinctItemCount: 2,
      categories: ['Attraction'],
      parkRatings: { ratedCount: 2, eligibleCount: 2, average: 4.5 },
      rideRatings: { ratedCount: 3, eligibleCount: 4, average: 4.5 },
      mostVisitedParks: [{ name: 'Denain Évasion', visitCount: 2, completedRideCount: 4 }],
      mostRepeatedItem: {
        name: 'Le Galion',
        rideCount: 3,
        ratingCount: 2,
        averageRating: 4.5,
        isNowClosed: false
      },
      topRatedItem: null,
      ratingEvolution: null,
      nowClosedItems: [],
      publicCaption: 'Une année mémorable.',
      hasIncompleteCatalog: false,
      calculationVersion: 'passport-year-recap-v1',
      isEmpty: false
    }
  };
}
