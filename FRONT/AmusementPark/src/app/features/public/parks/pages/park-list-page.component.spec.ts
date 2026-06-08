import { DestroyRef, signal, Signal } from '@angular/core';
import { ActivatedRoute, convertToParamMap, ParamMap, Router } from '@angular/router';
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

class FakeDestroyRef implements DestroyRef {
  readonly destroyed = false;

  onDestroy(callback: () => void): () => void {
    void callback;
    return (): void => undefined;
  }
}

class FakeParkListStateFacade {
  readonly state: Signal<ScreenState<unknown, string>> = signal<ScreenState<unknown, string>>({ kind: 'ready', data: { parks: [], pagination: null } }).asReadonly();
  readonly mapState: Signal<ScreenState<ParkMapPointViewModel[], string>> = signal<ScreenState<ParkMapPointViewModel[], string>>({ kind: 'ready', data: [] }).asReadonly();
  readonly parks: Signal<ParkCardModel[]> = signal<ParkCardModel[]>([]).asReadonly();
  readonly displayedParks: Signal<ParkCardModel[]> = signal<ParkCardModel[]>([]).asReadonly();
  readonly pagination: Signal<PaginationContract | null> = signal<PaginationContract | null>(null).asReadonly();
  readonly visibleMapPoints: Signal<ParkMapPointViewModel[]> = signal<ParkMapPointViewModel[]>([]).asReadonly();
  readonly visibleCountryCount: Signal<number> = signal(0).asReadonly();
  readonly selectedParkId: Signal<string | null> = signal<string | null>(null).asReadonly();
  readonly selectedParkCard: Signal<ParkCardModel | null> = signal<ParkCardModel | null>(null).asReadonly();
  readonly selectedRegion: Signal<ParkRegionFilter | null> = signal<ParkRegionFilter | null>(null).asReadonly();
  readonly pageSize: Signal<number> = signal(9).asReadonly();
  readonly mapLoads: Array<{ term: string; region: ParkRegionFilter | null }> = [];
  readonly parkLoads: Array<{ page: number; size: number; term: string; region: ParkRegionFilter | null }> = [];
  readonly languages: string[] = [];

  setCurrentLanguage(language: string): void {
    this.languages.push(language);
  }

  loadVisibleMapPoints(term: string = '', region: ParkRegionFilter | null = null): void {
    this.mapLoads.push({ term, region });
  }

  loadParks(page: number, size: number, term: string, region: ParkRegionFilter | null): void {
    this.parkLoads.push({ page, size, term, region });
  }

  clearSelectedPark(): void {
  }

  setSelectedRegion(): void {
  }

  selectParkFromMap(): void {
  }

  selectParkFromCard(): void {
  }
}

class FakeTranslationService {
  readonly languageChanged: Subject<string> = new Subject<string>();

  getCurrentLang(): string {
    return 'fr';
  }
}

describe('ParkListPageComponent', () => {
  it('loads visible map points only once during initial route setup', () => {
    const routeParams$: BehaviorSubject<ParamMap> = new BehaviorSubject<ParamMap>(convertToParamMap({ lang: 'fr' }));
    const stateFacade: FakeParkListStateFacade = new FakeParkListStateFacade();
    const component: ParkListPageComponent = createComponent(stateFacade, routeParams$);

    component.ngOnInit();

    expect(stateFacade.mapLoads).toEqual([{ term: '', region: null }]);
    expect(stateFacade.parkLoads).toEqual([{ page: 1, size: 9, term: '', region: null }]);
  });

  it('reloads visible map points when the parent language changes after initialization', () => {
    const routeParams$: BehaviorSubject<ParamMap> = new BehaviorSubject<ParamMap>(convertToParamMap({ lang: 'fr' }));
    const stateFacade: FakeParkListStateFacade = new FakeParkListStateFacade();
    const component: ParkListPageComponent = createComponent(stateFacade, routeParams$);

    component.ngOnInit();
    routeParams$.next(convertToParamMap({ lang: 'en' }));

    expect(stateFacade.languages).toEqual(['fr', 'en']);
    expect(stateFacade.mapLoads).toEqual([
      { term: '', region: null },
      { term: '', region: null }
    ]);
  });
});

function createComponent(
  stateFacade: FakeParkListStateFacade,
  routeParams$: BehaviorSubject<ParamMap>
): ParkListPageComponent {
  const route: Pick<ActivatedRoute, 'parent'> = {
    parent: {
      snapshot: {
        paramMap: convertToParamMap({ lang: 'fr' })
      },
      paramMap: routeParams$.asObservable()
    } as ActivatedRoute
  };
  const router: Pick<Router, 'url'> = { url: '/fr/parks' };
  const translationService: FakeTranslationService = new FakeTranslationService();
  const seoService: Pick<SeoService, 'applyParkListSeo'> = {
    applyParkListSeo: jasmine.createSpy('applyParkListSeo')
  };

  return new ParkListPageComponent(
    route as ActivatedRoute,
    router as Router,
    stateFacade as unknown as ParkListStateFacade,
    translationService as unknown as TranslationService,
    seoService as SeoService,
    new FakeDestroyRef()
  );
}
