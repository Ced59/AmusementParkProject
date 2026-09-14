import { DestroyRef, Signal, signal } from '@angular/core';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { Subject } from 'rxjs';

import { ParkFitSearchPark, ParkFitSearchResponse } from '@app/models/park-fit/park-fit-search.models';
import { TranslationService } from '@app/services/translation.service';
import { SeoService } from '@core/seo/seo.service';
import { ParkFitSearchFacade } from '../state/park-fit-search.facade';
import { ParkFitResultsPageComponent } from './park-fit-results-page.component';

interface ParkFitResultsTestSurface {
  criteriaRoute(): string[];
  homeRoute(): string[];
  countryName(countryCode: string | null): string;
  sourceUrl(source: ParkFitSearchPark['criticalSources'][number]): string | null;
  scoreReasonKey(value: string): string;
}

describe('ParkFitResultsPageComponent', () => {
  it('keeps the result page private and exposes visitor-facing routes', () => {
    const seoService: Pick<SeoService, 'applyParkFitResultsSeo'> = { applyParkFitResultsSeo: vi.fn() };
    const component: ParkFitResultsPageComponent = createComponent(buildResponse(), seoService);
    const page: ParkFitResultsTestSurface = component as unknown as ParkFitResultsTestSurface;

    component.ngOnInit();

    expect(page.criteriaRoute()).toEqual(['/', 'fr', 'park-fit']);
    expect(page.homeRoute()).toEqual(['/', 'fr', 'home']);
    expect(page.countryName('FR')).toBeTruthy();
    expect(seoService.applyParkFitResultsSeo).toHaveBeenCalledWith(
      'parkFit.results.seo.title',
      'parkFit.results.seo.description',
      '/fr/park-fit/results',
      'fr',
      'parkFit.results.breadcrumb.home',
      'parkFit.results.breadcrumb.parkFit',
      'parkFit.results.breadcrumb.current'
    );
  });

  it('does not turn an internal source reference or an unknown reason into public text', () => {
    const component: ParkFitResultsPageComponent = createComponent(buildResponse());
    const page: ParkFitResultsTestSurface = component as unknown as ParkFitResultsTestSurface;
    const park: ParkFitSearchPark = buildResponse().parks[0]!;

    component.ngOnInit();

    expect(page.sourceUrl(park.criticalSources[0]!)).toBeNull();
    expect(page.scoreReasonKey('FutureInternalReason')).toBe('parkFit.results.scoreReasons.Unknown');
  });
});

function createComponent(
  responseValue: ParkFitSearchResponse | null,
  seoService: Pick<SeoService, 'applyParkFitResultsSeo'> = { applyParkFitResultsSeo: vi.fn() }
): ParkFitResultsPageComponent {
  const response: Signal<ParkFitSearchResponse | null> = signal(responseValue).asReadonly();
  const visibleParks: Signal<ParkFitSearchPark[]> = signal(responseValue?.parks ?? []).asReadonly();
  const comparisonParks: Signal<ParkFitSearchPark[]> = signal([]).asReadonly();
  const canCompare: Signal<boolean> = signal(false).asReadonly();
  const comparisonLimitReached: Signal<boolean> = signal(false).asReadonly();
  const facade = { response, visibleParks, comparisonParks, canCompare, comparisonLimitReached, toggleComparisonPark: vi.fn() };
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
  const translateService = { instant: (key: string): string => key };
  const destroyRef: DestroyRef = {
    destroyed: false,
    onDestroy: (): (() => void) => (): void => undefined
  };

  return new ParkFitResultsPageComponent(
    route as ActivatedRoute,
    { url: '/fr/park-fit/results' } as Router,
    facade as unknown as ParkFitSearchFacade,
    translationService as unknown as TranslationService,
    translateService as TranslateService,
    seoService as SeoService,
    destroyRef
  );
}

function buildResponse(): ParkFitSearchResponse {
  return {
    methodVersion: 'park-fit-2026-01',
    evaluationDate: '2026-10-10',
    evaluatedAtUtc: '2026-09-14T10:00:00Z',
    totalCandidateCount: 1,
    inspectedCandidateCount: 1,
    qualityEligibleCandidateCount: 1,
    qualityRejectedCandidateCount: 0,
    candidatePoolTruncated: false,
    qualityStatusCounts: {},
    qualityIssueCounts: {},
    parks: [{
      parkId: 'park-1',
      parkName: 'Parc Démo',
      countryCode: 'FR',
      parkType: 'ThemePark',
      scoreState: 'Available',
      comparativeScore: 82,
      rawKnownScore: 82,
      coveragePercent: 90,
      knownWeightPercent: 90,
      scoreCeilingPercent: null,
      confidence: 'High',
      dateAvailabilityState: 'Available',
      unknownCount: 1,
      everyoneTogetherAttractionCount: 12,
      splitRequiredAttractionCount: 1,
      partialAttractionCount: 0,
      noCompatibleMemberAttractionCount: 1,
      unknownAttractionCount: 1,
      dataQualityStatus: 'EligibleForFitComparison',
      dataQualityCoveragePercent: 95,
      lastVerifiedAtUtc: '2026-09-01T00:00:00Z',
      reasons: ['ScoreAvailable'],
      components: [],
      memberSummaries: [{
        memberNumber: 1,
        compatibleAloneAttractionCount: 12,
        compatibleWithCompanionAttractionCount: 0,
        incompatibleAttractionCount: 1,
        unknownAttractionCount: 1,
        notApplicableAttractionCount: 0
      }],
      criticalSources: [{
        kind: 'Official',
        url: null,
        reference: 'internal-reference-never-rendered',
        languageCode: 'fr',
        collectedAtUtc: null,
        verifiedAtUtc: '2026-09-01T00:00:00Z',
        confidence: 'High',
        summaries: [{ languageCode: 'fr', value: 'Condition officielle.' }]
      }]
    }]
  };
}
