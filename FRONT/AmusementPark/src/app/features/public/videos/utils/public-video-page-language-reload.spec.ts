import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, ParamMap, Router } from '@angular/router';
import { BehaviorSubject, Subject } from 'rxjs';

import { SeoService } from '@core/seo/seo.service';
import { TranslationService } from '@app/services/translation.service';
import { PublicVideoFilterState } from '../models/public-video-view.model';
import { ParkItemVideoStateFacade } from '../../park-items/state/park-item-video-state.facade';
import { ParkItemVideosStateFacade } from '../../park-items/state/park-item-videos-state.facade';
import { ParkItemVideoPageComponent } from '../../park-items/pages/park-item-video-page.component';
import { ParkItemVideosPageComponent } from '../../park-items/pages/park-item-videos-page.component';
import { ParkVideoPageComponent } from '../../parks/pages/park-video-page.component';
import { ParkVideosPageComponent } from '../../parks/pages/park-videos-page.component';
import { ParkVideoStateFacade } from '../../parks/state/park-video-state.facade';
import { ParkVideosStateFacade } from '../../parks/state/park-videos-state.facade';

class FakeTranslationService {
  readonly languageChanged: Subject<string> = new Subject<string>();

  getCurrentLang(): string {
    return 'fr';
  }
}

class FakeSeoService {
  readonly applyParkVideoSeo = jasmine.createSpy('applyParkVideoSeo');
  readonly applyParkVideosSeo = jasmine.createSpy('applyParkVideosSeo');
  readonly applyParkItemVideoSeo = jasmine.createSpy('applyParkItemVideoSeo');
  readonly applyParkItemVideosSeo = jasmine.createSpy('applyParkItemVideosSeo');
}

class FakeParkVideoStateFacade {
  readonly state = signal({ kind: 'loading' }).asReadonly();
  readonly park = signal(null).asReadonly();
  readonly parkImageId = signal(null).asReadonly();
  readonly video = signal(null).asReadonly();
  readonly rawVideo = signal(null).asReadonly();
  readonly previousVideo = signal(null).asReadonly();
  readonly nextVideo = signal(null).asReadonly();
  readonly languages: string[] = [];
  readonly loads: Array<{ parkId: string; videoId: string }> = [];

  setCurrentLanguage(language: string): void {
    this.languages.push(language);
  }

  loadParkVideo(parkId: string, videoId: string): void {
    this.loads.push({ parkId, videoId });
  }
}

class FakeParkVideosStateFacade {
  readonly state = signal({ kind: 'loading' }).asReadonly();
  readonly park = signal(null).asReadonly();
  readonly parkImageId = signal(null).asReadonly();
  readonly videoCards = signal([]).asReadonly();
  readonly totalVideos = signal(0).asReadonly();
  readonly parkTabVideoCount = signal(0).asReadonly();
  readonly itemTabVideoCount = signal(0).asReadonly();
  readonly showItemTab = signal(false).asReadonly();
  readonly activeTab = signal('park').asReadonly();
  readonly canLoadMore = signal(false).asReadonly();
  readonly loadingMore = signal(false).asReadonly();
  readonly itemVideosLoading = signal(false).asReadonly();
  readonly filters = signal<PublicVideoFilterState>({ type: null, tagId: null, creatorName: '' }).asReadonly();
  readonly typeOptions = signal([]).asReadonly();
  readonly tagOptions = signal([]).asReadonly();
  readonly languages: string[] = [];
  readonly loads: Array<{ parkId: string; filters: PublicVideoFilterState }> = [];
  readonly selectedTabs: string[] = [];

  setCurrentLanguage(language: string): void {
    this.languages.push(language);
  }

  loadParkVideos(parkId: string, filters: PublicVideoFilterState): void {
    this.loads.push({ parkId, filters });
  }

  selectTab(tab: string): void {
    this.selectedTabs.push(tab);
  }

  loadNextPage(): void {
  }
}

class FakeParkItemVideoStateFacade {
  readonly state = signal({ kind: 'loading' }).asReadonly();
  readonly item = signal(null).asReadonly();
  readonly park = signal(null).asReadonly();
  readonly itemImageId = signal(null).asReadonly();
  readonly parkImageId = signal(null).asReadonly();
  readonly video = signal(null).asReadonly();
  readonly rawVideo = signal(null).asReadonly();
  readonly previousVideo = signal(null).asReadonly();
  readonly nextVideo = signal(null).asReadonly();
  readonly languages: string[] = [];
  readonly loads: Array<{ itemId: string; videoId: string }> = [];

  setCurrentLanguage(language: string): void {
    this.languages.push(language);
  }

  loadItemVideo(itemId: string, videoId: string): void {
    this.loads.push({ itemId, videoId });
  }
}

class FakeParkItemVideosStateFacade {
  readonly state = signal({ kind: 'loading' }).asReadonly();
  readonly item = signal(null).asReadonly();
  readonly park = signal(null).asReadonly();
  readonly itemImageId = signal(null).asReadonly();
  readonly parkImageId = signal(null).asReadonly();
  readonly videoCards = signal([]).asReadonly();
  readonly totalVideos = signal(0).asReadonly();
  readonly canLoadMore = signal(false).asReadonly();
  readonly loadingMore = signal(false).asReadonly();
  readonly filters = signal<PublicVideoFilterState>({ type: null, tagId: null, creatorName: '' }).asReadonly();
  readonly typeOptions = signal([]).asReadonly();
  readonly tagOptions = signal([]).asReadonly();
  readonly languages: string[] = [];
  readonly loads: Array<{ itemId: string; filters: PublicVideoFilterState }> = [];

  setCurrentLanguage(language: string): void {
    this.languages.push(language);
  }

  loadItemVideos(itemId: string, filters: PublicVideoFilterState): void {
    this.loads.push({ itemId, filters });
  }

  loadNextPage(): void {
  }
}

describe('public video page language reloads', () => {
  beforeEach(() => {
    TestBed.resetTestingModule();
  });

  it('reloads a park video watch page when the language changes', () => {
    const routeContext = createRoute({ id: 'park-1', videoId: 'video-1' });
    const translationService = new FakeTranslationService();
    const stateFacade = new FakeParkVideoStateFacade();
    const component = TestBed.runInInjectionContext(() => new ParkVideoPageComponent(
      routeContext.route,
      createRouter('/fr/park/park-1/test/videos/video-1/test'),
      translationService as unknown as TranslationService,
      createSeoService(),
      stateFacade as unknown as ParkVideoStateFacade
    ));

    component.ngOnInit();
    translationService.languageChanged.next('en');

    expect(stateFacade.languages).toEqual(['fr', 'en']);
    expect(stateFacade.loads).toEqual([
      { parkId: 'park-1', videoId: 'video-1' },
      { parkId: 'park-1', videoId: 'video-1' }
    ]);
  });

  it('reloads a park video list page when the language changes', () => {
    const routeContext = createRoute({ id: 'park-1' });
    const translationService = new FakeTranslationService();
    const stateFacade = new FakeParkVideosStateFacade();
    const component = TestBed.runInInjectionContext(() => new ParkVideosPageComponent(
      routeContext.route,
      createRouter('/fr/park/park-1/test/videos'),
      translationService as unknown as TranslationService,
      createSeoService(),
      stateFacade as unknown as ParkVideosStateFacade
    ));

    component.ngOnInit();
    translationService.languageChanged.next('en');

    expect(stateFacade.languages).toEqual(['fr', 'en']);
    expect(stateFacade.loads.map((load) => load.parkId)).toEqual(['park-1', 'park-1']);
  });

  it('reloads a park item video watch page when the language changes', () => {
    const routeContext = createRoute({ itemId: 'item-1', videoId: 'video-1' });
    const translationService = new FakeTranslationService();
    const stateFacade = new FakeParkItemVideoStateFacade();
    const component = TestBed.runInInjectionContext(() => new ParkItemVideoPageComponent(
      routeContext.route,
      createRouter('/fr/park/park-1/test/item/item-1/test/videos/video-1/test'),
      translationService as unknown as TranslationService,
      createSeoService(),
      stateFacade as unknown as ParkItemVideoStateFacade
    ));

    component.ngOnInit();
    translationService.languageChanged.next('en');

    expect(stateFacade.languages).toEqual(['fr', 'en']);
    expect(stateFacade.loads).toEqual([
      { itemId: 'item-1', videoId: 'video-1' },
      { itemId: 'item-1', videoId: 'video-1' }
    ]);
  });

  it('reloads a park item video list page when the language changes', () => {
    const routeContext = createRoute({ itemId: 'item-1' });
    const translationService = new FakeTranslationService();
    const stateFacade = new FakeParkItemVideosStateFacade();
    const component = TestBed.runInInjectionContext(() => new ParkItemVideosPageComponent(
      routeContext.route,
      createRouter('/fr/park/park-1/test/item/item-1/test/videos'),
      translationService as unknown as TranslationService,
      createSeoService(),
      stateFacade as unknown as ParkItemVideosStateFacade
    ));

    component.ngOnInit();
    translationService.languageChanged.next('en');

    expect(stateFacade.languages).toEqual(['fr', 'en']);
    expect(stateFacade.loads.map((load) => load.itemId)).toEqual(['item-1', 'item-1']);
  });
});

function createRoute(
  params: Record<string, string>,
  queryParams: Record<string, string> = {}
): { route: ActivatedRoute; params$: BehaviorSubject<ParamMap>; queryParams$: BehaviorSubject<ParamMap> } {
  const params$: BehaviorSubject<ParamMap> = new BehaviorSubject<ParamMap>(convertToParamMap(params));
  const queryParams$: BehaviorSubject<ParamMap> = new BehaviorSubject<ParamMap>(convertToParamMap(queryParams));

  return {
    route: {
      snapshot: {
        paramMap: convertToParamMap(params)
      },
      parent: {
        snapshot: {
          paramMap: convertToParamMap({ lang: 'fr' })
        }
      },
      paramMap: params$.asObservable(),
      queryParamMap: queryParams$.asObservable()
    } as ActivatedRoute,
    params$,
    queryParams$
  };
}

function createRouter(url: string): Router {
  return {
    url,
    navigate: jasmine.createSpy('navigate')
  } as unknown as Router;
}

function createSeoService(): SeoService {
  return new FakeSeoService() as unknown as SeoService;
}
