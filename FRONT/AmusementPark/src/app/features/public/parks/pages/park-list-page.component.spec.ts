import { DestroyRef, signal, Signal } from '@angular/core';
import {
  ActivatedRoute,
  convertToParamMap,
  ParamMap,
  Router,
} from '@angular/router';
import { BehaviorSubject, Subject } from 'rxjs';

import { ParkListStateFacade } from '../state/park-list-state.facade';
import { ParkListPageComponent } from './park-list-page.component';
import { SeoService } from '@core/seo/seo.service';
import { TranslationService } from '@app/services/translation.service';
import { ScreenState } from '@shared/models/contracts/screen-state.model';
import { ParkMapPointViewModel } from '../models/park-map-point-view.model';
import { ParkCardModel } from '@shared/models/parks/park-card.model';
import { PaginationContract } from '@shared/models/contracts';
import { ParkRegionFilter } from '@shared/models/geo/world-region-filter.model';
import { ParkAudienceClassificationFilter } from '@app/models/parks/park-audience-classification';
import { ParkStatus } from '@app/models/parks/park-status';
import { PublicPlaceDiscoveryScope } from '@shared/models/search/public-search-category-option.model';
import { SearchResultItem } from '@app/models/search/search-result-item';
import { FakeDestroyRef } from './test-helpers/park-list-page.component/fake-destroy-ref';
import { FakeParkListStateFacade } from './test-helpers/park-list-page.component/fake-park-list-state-facade';
import { FakeTranslationService } from './test-helpers/park-list-page.component/fake-translation-service';

describe('ParkListPageComponent', () => {
  it('loads visible map points only once during initial route setup', () => {
    const routeParams$: BehaviorSubject<ParamMap> =
      new BehaviorSubject<ParamMap>(convertToParamMap({ lang: 'fr' }));
    const stateFacade: FakeParkListStateFacade = new FakeParkListStateFacade();
    const component: ParkListPageComponent = createComponent(
      stateFacade,
      routeParams$,
    );

    component.ngOnInit();

    expect(stateFacade.mapLoads).toEqual([{ term: '', region: null, scope: 'parks' }]);
    expect(stateFacade.parkLoads).toEqual([
      { page: 1, size: 9, term: '', region: null },
    ]);
  });

  it('reloads visible map points when the parent language changes after initialization', () => {
    const routeParams$: BehaviorSubject<ParamMap> =
      new BehaviorSubject<ParamMap>(convertToParamMap({ lang: 'fr' }));
    const stateFacade: FakeParkListStateFacade = new FakeParkListStateFacade();
    const component: ParkListPageComponent = createComponent(
      stateFacade,
      routeParams$,
    );

    component.ngOnInit();
    routeParams$.next(convertToParamMap({ lang: 'en' }));

    expect(stateFacade.languages).toEqual(['fr', 'en']);
    expect(stateFacade.mapLoads).toEqual([
      { term: '', region: null, scope: 'parks' },
      { term: '', region: null, scope: 'parks' },
    ]);
  });

  it('runs an explicit search immediately without keeping the pending live search', () => {
    vi.useFakeTimers();

    try {
      const routeParams$: BehaviorSubject<ParamMap> =
        new BehaviorSubject<ParamMap>(convertToParamMap({ lang: 'fr' }));
      const stateFacade: FakeParkListStateFacade = new FakeParkListStateFacade();
      const component: ParkListPageComponent = createComponent(
        stateFacade,
        routeParams$,
      );

      component.ngOnInit();
      stateFacade.mapLoads.length = 0;
      stateFacade.parkLoads.length = 0;

      component.onSearchInput('  Europa-Park  ');
      component.onSearchSubmit();

      expect(stateFacade.mapLoads).toEqual([
        { term: 'Europa-Park', region: null, scope: 'parks' },
      ]);
      expect(stateFacade.parkLoads).toEqual([
        { page: 1, size: 9, term: 'Europa-Park', region: null },
      ]);

      vi.advanceTimersByTime(300);

      expect(stateFacade.mapLoads).toHaveLength(1);
      expect(stateFacade.parkLoads).toHaveLength(1);
    } finally {
      vi.useRealTimers();
    }
  });

  it('keeps mixed discovery results when a park marker is selected', () => {
    const routeParams$: BehaviorSubject<ParamMap> =
      new BehaviorSubject<ParamMap>(convertToParamMap({ lang: 'fr' }));
    const stateFacade: FakeParkListStateFacade = new FakeParkListStateFacade();
    const component: ParkListPageComponent = createComponent(
      stateFacade,
      routeParams$,
    );
    stateFacade.discoveryScopeSignal.set('parksAndStandaloneAttractions');

    component.onMapParkSelected('park-1');

    expect(stateFacade.discoveryMapSelections).toEqual(['park-1']);
    expect(stateFacade.parkMapSelections).toEqual([]);
  });
});

function createComponent(
  stateFacade: FakeParkListStateFacade,
  routeParams$: BehaviorSubject<ParamMap>,
): ParkListPageComponent {
  const route: Pick<ActivatedRoute, 'parent'> = {
    parent: {
      snapshot: {
        paramMap: convertToParamMap({ lang: 'fr' }),
      },
      paramMap: routeParams$.asObservable(),
    } as ActivatedRoute,
  };
  const router: Pick<Router, 'url'> = { url: '/fr/parks' };
  const translationService: FakeTranslationService =
    new FakeTranslationService();
  const seoService: Pick<SeoService, 'applyParkListSeo'> = {
    applyParkListSeo: vi.fn(),
  };

  return new ParkListPageComponent(
    route as ActivatedRoute,
    router as Router,
    stateFacade as unknown as ParkListStateFacade,
    translationService as unknown as TranslationService,
    seoService as SeoService,
    new FakeDestroyRef(),
  );
}
