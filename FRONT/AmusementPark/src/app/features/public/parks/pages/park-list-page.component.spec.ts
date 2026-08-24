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

class FakeDestroyRef implements DestroyRef {
  readonly destroyed = false;

  onDestroy(callback: () => void): () => void {
    void callback;
    return (): void => undefined;
  }
}

class FakeParkListStateFacade {
  readonly state: Signal<ScreenState<unknown, string>> = signal<
    ScreenState<unknown, string>
  >({ kind: 'ready', data: { parks: [], pagination: null } }).asReadonly();
  readonly mapState: Signal<ScreenState<ParkMapPointViewModel[], string>> =
    signal<ScreenState<ParkMapPointViewModel[], string>>({
      kind: 'ready',
      data: [],
    }).asReadonly();
  readonly parks: Signal<ParkCardModel[]> = signal<ParkCardModel[]>(
    [],
  ).asReadonly();
  readonly displayedParks: Signal<ParkCardModel[]> = signal<ParkCardModel[]>(
    [],
  ).asReadonly();
  readonly searchResults: Signal<SearchResultItem[]> = signal<SearchResultItem[]>([]).asReadonly();
  readonly pagination: Signal<PaginationContract | null> =
    signal<PaginationContract | null>(null).asReadonly();
  readonly visibleMapPoints: Signal<ParkMapPointViewModel[]> = signal<
    ParkMapPointViewModel[]
  >([]).asReadonly();
  readonly visibleCountryCount: Signal<number> = signal(0).asReadonly();
  readonly selectedParkId: Signal<string | null> = signal<string | null>(
    null,
  ).asReadonly();
  readonly selectedParkCard: Signal<ParkCardModel | null> =
    signal<ParkCardModel | null>(null).asReadonly();
  readonly selectedRegion: Signal<ParkRegionFilter | null> =
    signal<ParkRegionFilter | null>(null).asReadonly();
  readonly selectedStatus: Signal<ParkStatus | null> = signal<ParkStatus | null>('Operating').asReadonly();
  readonly selectedAudienceClassificationFilter: Signal<ParkAudienceClassificationFilter | null> = signal<ParkAudienceClassificationFilter | null>(null).asReadonly();
  readonly discoveryScopeSignal = signal<PublicPlaceDiscoveryScope>('parks');
  readonly discoveryScope: Signal<PublicPlaceDiscoveryScope> = this.discoveryScopeSignal.asReadonly();
  readonly currentPage: Signal<number> = signal(1).asReadonly();
  readonly pageSize: Signal<number> = signal(9).asReadonly();
  readonly mapLoads: Array<{
    term: string;
    region: ParkRegionFilter | null;
    scope: PublicPlaceDiscoveryScope;
  }> = [];
  readonly parkLoads: Array<{
    page: number;
    size: number;
    term: string;
    region: ParkRegionFilter | null;
  }> = [];
  readonly languages: string[] = [];
  readonly parkMapSelections: Array<string | null> = [];
  readonly discoveryMapSelections: Array<string | null> = [];

  setCurrentLanguage(language: string): void {
    this.languages.push(language);
  }

  loadVisibleMapPoints(
    term: string = '',
    region: ParkRegionFilter | null = null,
    scope: PublicPlaceDiscoveryScope = 'parks',
  ): void {
    this.mapLoads.push({ term, region, scope });
  }

  loadParks(
    page: number,
    size: number,
    term: string,
    region: ParkRegionFilter | null,
  ): void {
    this.parkLoads.push({ page, size, term, region });
  }

  clearSelectedPark(): void {}

  setSelectedRegion(): void {}

  setStatus(): void {}

  setAudienceClassificationFilter(): void {}

  setDiscoveryScope(): void {}

  loadDiscoveryResults(): void {}

  selectParkFromMap(parkId: string | null): void {
    this.parkMapSelections.push(parkId);
  }

  selectDiscoveryPointFromMap(pointId: string | null): void {
    this.discoveryMapSelections.push(pointId);
  }

  selectParkFromCard(): void {}
}

class FakeTranslationService {
  readonly languageChanged: Subject<string> = new Subject<string>();

  getCurrentLang(): string {
    return 'fr';
  }
}

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
