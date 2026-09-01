import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { Observable, Subject, of } from 'rxjs';

import { RatingMethodology } from '@app/models/ratings/rating-methodology.models';
import { AnonymousHttpOptions } from '@core/http/auth/anonymous-http-options';
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import { RATING_METHODOLOGY_PORT, RatingMethodologyPort } from './rating-methodology-state-data.ports';
import { RatingMethodologyStateFacade } from './rating-methodology-state.facade';

class FakeRatingMethodologyPort implements RatingMethodologyPort {
  readonly missingResponse = new Subject<RatingMethodology>();
  requestedVersion: string | null = null;

  getCurrentMethodology(_options?: AnonymousHttpOptions): Observable<RatingMethodology> {
    return of(createMethodology());
  }

  getMethodology(version: string, _options?: AnonymousHttpOptions): Observable<RatingMethodology> {
    this.requestedVersion = version;
    return this.missingResponse.asObservable();
  }

  getMethodologyHistory(_options?: AnonymousHttpOptions): Observable<RatingMethodology[]> {
    return of([createMethodology()]);
  }
}

describe('RatingMethodologyStateFacade', () => {
  let facade: RatingMethodologyStateFacade;
  let port: FakeRatingMethodologyPort;
  const ssrStatus = { setNotFound: vi.fn(), setStatus: vi.fn() };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        RatingMethodologyStateFacade,
        { provide: RATING_METHODOLOGY_PORT, useClass: FakeRatingMethodologyPort },
        { provide: SsrHttpStatusService, useValue: ssrStatus }
      ]
    });
    facade = TestBed.inject(RatingMethodologyStateFacade);
    port = TestBed.inject(RATING_METHODOLOGY_PORT) as FakeRatingMethodologyPort;
    vi.clearAllMocks();
  });

  it('loads the authoritative current methodology with its history', () => {
    facade.load(null);

    expect(facade.loading()).toBe(false);
    expect(facade.methodology()?.version).toBe('ratings-2026-01');
    expect(facade.history()).toHaveLength(1);
    expect(facade.error()).toBe(false);
  });

  it('maps an unknown historical version to a public SSR 404', () => {
    facade.load('ratings-missing');
    port.missingResponse.error(new HttpErrorResponse({ status: 404 }));

    expect(port.requestedVersion).toBe('ratings-missing');
    expect(facade.notFound()).toBe(true);
    expect(facade.error()).toBe(true);
    expect(ssrStatus.setNotFound).toHaveBeenCalledTimes(1);
  });

  it('ignores a stale historical response after loading the current version', () => {
    facade.load('ratings-old');
    facade.load(null);

    port.missingResponse.error(new HttpErrorResponse({ status: 404 }));

    expect(facade.methodology()?.version).toBe('ratings-2026-01');
    expect(facade.loading()).toBe(false);
    expect(facade.error()).toBe(false);
    expect(facade.notFound()).toBe(false);
    expect(ssrStatus.setNotFound).not.toHaveBeenCalled();
  });
});

function createMethodology(): RatingMethodology {
  return {
    version: 'ratings-2026-01', effectiveDate: '2026-08-31', isCurrent: true, previousVersion: null,
    ratingScale: { minimum: 0.5, maximum: 5, step: 0.5 },
    bayesian: { priorMean: 3.5, priorWeight: 10 },
    parkComposition: { directRatingWeight: 0.7, itemRatingWeight: 0.3, balancesItemCategoriesEqually: true, minimumEligibleItems: 5, minimumItemsPerCategory: 2, minimumCategories: 2 },
    evidenceThresholds: { provisional: 3, eligible: 10, established: 30, strong: 100 },
    publicationRules: { minimumEligibleEntries: 3, scoreTieEpsilon: 0.0001, rankingConvention: 'competition' }
  };
}
