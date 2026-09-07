import { EventEmitter } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { Observable, of } from 'rxjs';

import { SharedVisitRecap } from '@app/models/sharing/share-publication.models';
import { TranslationService } from '@app/services/translation.service';
import { COMMON_TEST_IMPORTS, provideCommonTestDependencies } from '@app/testing/common-test-providers';
import { SeoService } from '@core/seo/seo.service';
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import { SHARED_VISIT_RECAP_PORT, SharedVisitRecapPort } from '../state/shared-visit-recap-state-data.ports';
import { SharedVisitRecapPageComponent } from './shared-visit-recap-page.component';

describe('SharedVisitRecapPageComponent', () => {
  let fixture: ComponentFixture<SharedVisitRecapPageComponent>;
  let applySeo: ReturnType<typeof vi.fn>;

  beforeEach(async () => {
    applySeo = vi.fn();
    const port: SharedVisitRecapPort = {
      getSharedVisit: (_shareId: string): Observable<SharedVisitRecap> => of(createSharedVisit())
    };
    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, SharedVisitRecapPageComponent],
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
        { provide: SHARED_VISIT_RECAP_PORT, useValue: port },
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

  it('renders the frozen public story with names and no technical identifier as visible copy', () => {
    fixture = TestBed.createComponent(SharedVisitRecapPageComponent);
    fixture.detectChanges();

    const host: HTMLElement = fixture.nativeElement as HTMLElement;
    expect(host.textContent).toContain('Denain Évasion');
    expect(host.textContent).toContain('Le Galion');
    expect(host.textContent).toContain('4,5 / 5');
    expect(host.textContent).not.toContain('park-technical-id');
    expect(host.textContent).not.toContain('item-technical-id');
    expect(host.querySelector('.shared-visit__breadcrumb a')).not.toBeNull();
    expect(applySeo).toHaveBeenCalled();
  });
});

function createSharedVisit(): SharedVisitRecap {
  return {
    publishedAtUtc: '2026-09-07T08:00:00Z',
    visitRecap: {
      parkId: 'park-technical-id',
      parkName: 'Denain Évasion',
      date: { year: 2026, month: 7, day: 26, precision: 'Day', isApproximate: false },
      distinctItemCount: 1,
      totalRideCount: 2,
      categories: ['Attraction'],
      parkRating: 4.5,
      topRatedItem: { name: 'Le Galion', rating: 4.5 },
      mostRepeatedItem: { name: 'Le Galion', rideCount: 2 },
      items: [{
        name: 'Le Galion',
        category: 'Attraction',
        rideCount: 2,
        averageRating: 4.5,
        isMissed: false
      }],
      publicCaption: 'Un beau souvenir public.',
      hasHiddenDate: false,
      hasIncompleteRatings: false,
      hasIncompleteItems: false
    }
  };
}
