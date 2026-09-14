import { DestroyRef, Signal, signal } from '@angular/core';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { Subject } from 'rxjs';

import { TranslationService } from '@app/services/translation.service';
import { SeoService } from '@core/seo/seo.service';
import { ParkFitComparisonSelection } from '../models/park-fit-comparison.models';
import { ParkFitSearchFacade } from '../state/park-fit-search.facade';
import { ParkFitComparisonPageComponent } from './park-fit-comparison-page.component';

interface ComparisonPageTestSurface {
  homeRoute(): string[];
  resultsRoute(): string[];
  toggleDifferences(): void;
  showOnlyDifferences(): boolean;
}

describe('ParkFitComparisonPageComponent', () => {
  it('keeps the comparison private and exposes contextual routes', () => {
    const seoService: Pick<SeoService, 'applyParkFitComparisonSeo'> = { applyParkFitComparisonSeo: vi.fn() };
    const component: ParkFitComparisonPageComponent = createComponent(seoService);
    const page: ComparisonPageTestSurface = component as unknown as ComparisonPageTestSurface;

    component.ngOnInit();

    expect(page.homeRoute()).toEqual(['/', 'fr', 'home']);
    expect(page.resultsRoute()).toEqual(['/', 'fr', 'park-fit', 'results']);
    expect(seoService.applyParkFitComparisonSeo).toHaveBeenCalledWith(
      'parkFit.comparison.seo.title',
      'parkFit.comparison.seo.description',
      '/fr/park-fit/compare',
      'fr',
      'parkFit.results.breadcrumb.home',
      'parkFit.results.breadcrumb.parkFit',
      'parkFit.results.breadcrumb.current',
      'parkFit.comparison.breadcrumb.current'
    );
  });

  it('can reduce the matrix to rows whose values differ', () => {
    const component: ParkFitComparisonPageComponent = createComponent();
    const page: ComparisonPageTestSurface = component as unknown as ComparisonPageTestSurface;

    component.ngOnInit();
    page.toggleDifferences();

    expect(page.showOnlyDifferences()).toBe(true);
  });
});

function createComponent(
  seoService: Pick<SeoService, 'applyParkFitComparisonSeo'> = { applyParkFitComparisonSeo: vi.fn() }
): ParkFitComparisonPageComponent {
  const comparisonSelections: Signal<ParkFitComparisonSelection[]> = signal([]).asReadonly();
  const route = {
    snapshot: { paramMap: convertToParamMap({}) },
    parent: { snapshot: { paramMap: convertToParamMap({ lang: 'fr' }) }, parent: null }
  };
  const translationService = {
    getCurrentLang: (): string => 'fr',
    languageChanged: new Subject<string>()
  };
  const translateService = { instant: (key: string): string => key };
  const destroyRef: DestroyRef = { destroyed: false, onDestroy: (): (() => void) => (): void => undefined };

  return new ParkFitComparisonPageComponent(
    route as ActivatedRoute,
    { url: '/fr/park-fit/compare' } as Router,
    { comparisonSelections } as unknown as ParkFitSearchFacade,
    translationService as unknown as TranslationService,
    translateService as TranslateService,
    seoService as SeoService,
    destroyRef
  );
}
