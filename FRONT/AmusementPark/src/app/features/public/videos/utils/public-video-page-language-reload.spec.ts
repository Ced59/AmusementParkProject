import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import {
  ActivatedRoute,
  convertToParamMap,
  ParamMap,
  Router,
} from '@angular/router';
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
import { FakeTranslationService } from './test-helpers/public-video-page-language-reload/fake-translation-service';
import { FakeSeoService } from './test-helpers/public-video-page-language-reload/fake-seo-service';
import { FakeParkVideoStateFacade } from './test-helpers/public-video-page-language-reload/fake-park-video-state-facade';
import { FakeParkVideosStateFacade } from './test-helpers/public-video-page-language-reload/fake-park-videos-state-facade';
import { FakeParkItemVideoStateFacade } from './test-helpers/public-video-page-language-reload/fake-park-item-video-state-facade';
import { FakeParkItemVideosStateFacade } from './test-helpers/public-video-page-language-reload/fake-park-item-videos-state-facade';

describe('public video page language reloads', () => {
  beforeEach(() => {
    TestBed.resetTestingModule();
  });

  it('reloads a park video watch page when the language changes', () => {
    const routeContext = createRoute({ id: 'park-1', videoId: 'video-1' });
    const translationService = new FakeTranslationService();
    const stateFacade = new FakeParkVideoStateFacade();
    const component = TestBed.runInInjectionContext(
      () =>
        new ParkVideoPageComponent(
          routeContext.route,
          createRouter('/fr/park/park-1/test/videos/video-1/test'),
          translationService as unknown as TranslationService,
          createSeoService(),
          stateFacade as unknown as ParkVideoStateFacade,
        ),
    );

    component.ngOnInit();
    translationService.languageChanged.next('en');

    expect(stateFacade.languages).toEqual(['fr', 'en']);
    expect(stateFacade.loads).toEqual([
      { parkId: 'park-1', videoId: 'video-1' },
      { parkId: 'park-1', videoId: 'video-1' },
    ]);
  });

  it('reloads a park video list page when the language changes', () => {
    const routeContext = createRoute({ id: 'park-1' });
    const translationService = new FakeTranslationService();
    const stateFacade = new FakeParkVideosStateFacade();
    const component = TestBed.runInInjectionContext(
      () =>
        new ParkVideosPageComponent(
          routeContext.route,
          createRouter('/fr/park/park-1/test/videos'),
          translationService as unknown as TranslationService,
          createSeoService(),
          stateFacade as unknown as ParkVideosStateFacade,
        ),
    );

    component.ngOnInit();
    translationService.languageChanged.next('en');

    expect(stateFacade.languages).toEqual(['fr', 'en']);
    expect(stateFacade.loads.map((load) => load.parkId)).toEqual([
      'park-1',
      'park-1',
    ]);
  });

  it('reloads a park item video watch page when the language changes', () => {
    const routeContext = createRoute({ itemId: 'item-1', videoId: 'video-1' });
    const translationService = new FakeTranslationService();
    const stateFacade = new FakeParkItemVideoStateFacade();
    const component = TestBed.runInInjectionContext(
      () =>
        new ParkItemVideoPageComponent(
          routeContext.route,
          createRouter(
            '/fr/park/park-1/test/item/item-1/test/videos/video-1/test',
          ),
          translationService as unknown as TranslationService,
          createSeoService(),
          stateFacade as unknown as ParkItemVideoStateFacade,
        ),
    );

    component.ngOnInit();
    translationService.languageChanged.next('en');

    expect(stateFacade.languages).toEqual(['fr', 'en']);
    expect(stateFacade.loads).toEqual([
      { itemId: 'item-1', videoId: 'video-1' },
      { itemId: 'item-1', videoId: 'video-1' },
    ]);
  });

  it('reloads a park item video list page when the language changes', () => {
    const routeContext = createRoute({ itemId: 'item-1' });
    const translationService = new FakeTranslationService();
    const stateFacade = new FakeParkItemVideosStateFacade();
    const component = TestBed.runInInjectionContext(
      () =>
        new ParkItemVideosPageComponent(
          routeContext.route,
          createRouter('/fr/park/park-1/test/item/item-1/test/videos'),
          translationService as unknown as TranslationService,
          createSeoService(),
          stateFacade as unknown as ParkItemVideosStateFacade,
        ),
    );

    component.ngOnInit();
    translationService.languageChanged.next('en');

    expect(stateFacade.languages).toEqual(['fr', 'en']);
    expect(stateFacade.loads.map((load) => load.itemId)).toEqual([
      'item-1',
      'item-1',
    ]);
  });
});

function createRoute(
  params: Record<string, string>,
  queryParams: Record<string, string> = {},
): {
  route: ActivatedRoute;
  params$: BehaviorSubject<ParamMap>;
  queryParams$: BehaviorSubject<ParamMap>;
} {
  const params$: BehaviorSubject<ParamMap> = new BehaviorSubject<ParamMap>(
    convertToParamMap(params),
  );
  const queryParams$: BehaviorSubject<ParamMap> = new BehaviorSubject<ParamMap>(
    convertToParamMap(queryParams),
  );

  return {
    route: {
      snapshot: {
        paramMap: convertToParamMap(params),
      },
      parent: {
        snapshot: {
          paramMap: convertToParamMap({ lang: 'fr' }),
        },
      },
      paramMap: params$.asObservable(),
      queryParamMap: queryParams$.asObservable(),
    } as ActivatedRoute,
    params$,
    queryParams$,
  };
}

function createRouter(url: string): Router {
  return {
    url,
    navigate: vi.fn(),
  } as unknown as Router;
}

function createSeoService(): SeoService {
  return new FakeSeoService() as unknown as SeoService;
}
