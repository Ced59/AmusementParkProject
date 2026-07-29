import type { MockedObject } from 'vitest';
import { EventEmitter, Signal, WritableSignal, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, ParamMap, Router, convertToParamMap } from '@angular/router';
import { of } from 'rxjs';

import { CommentThread } from '@app/models/comments/comment.models';
import { TranslationService } from '@app/services/translation.service';
import { SeoService } from '@core/seo/seo.service';
import { ScreenState } from '@shared/models/contracts/screen-state.model';
import { CommentThreadStateFacade } from '../state/comment-thread-state.facade';
import { CommentsPageComponent } from './comments-page.component';

class FakeTranslationService {
  readonly languageChanged: EventEmitter<string> = new EventEmitter<string>();

  getCurrentLang(): string {
    return 'fr';
  }
}

class FakeCommentThreadStateFacade {
  readonly state: Signal<ScreenState<CommentThread, string>> =
    signal<ScreenState<CommentThread, string>>({ kind: 'loading' }).asReadonly();
  readonly thread: Signal<CommentThread | null> = signal<CommentThread | null>(null).asReadonly();
  readonly canWrite: Signal<boolean> = signal<boolean>(false).asReadonly();
  readonly saving: Signal<boolean> = signal<boolean>(false).asReadonly();
  readonly saveErrorKey: Signal<string | null> = signal<string | null>(null).asReadonly();
  readonly createdVersion: Signal<number> = signal<number>(0).asReadonly();
  readonly notFoundSignal: WritableSignal<boolean> = signal<boolean>(false);
  readonly notFound: Signal<boolean> = this.notFoundSignal.asReadonly();

  initializeAuthorAccess(): void {
  }

  load(_targetType: 'Park' | 'ParkItem', _targetId: string): void {
  }

  create(): void {
  }

  clearSaveError(): void {
  }
}

describe('CommentsPageComponent', () => {
  it('applies not-found SEO when the comment target does not exist', () => {
    const routeParamMap: ParamMap = convertToParamMap({
      lang: 'fr',
      id: 'missing-park'
    });
    const route: ActivatedRoute = {
      snapshot: { paramMap: routeParamMap },
      paramMap: of(routeParamMap),
      parent: null
    } as unknown as ActivatedRoute;
    const router: Router = {
      url: '/fr/park/missing-park/parc-introuvable/comments'
    } as Router;
    const translationService: FakeTranslationService = new FakeTranslationService();
    const seoService: MockedObject<SeoService> = {
      applyCommentsSeo: vi.fn().mockName('SeoService.applyCommentsSeo'),
      applyNotFoundSeo: vi.fn().mockName('SeoService.applyNotFoundSeo')
    } as unknown as MockedObject<SeoService>;
    const stateFacade: FakeCommentThreadStateFacade = new FakeCommentThreadStateFacade();
    const component: CommentsPageComponent = TestBed.runInInjectionContext(
      (): CommentsPageComponent => new CommentsPageComponent(
        route,
        router,
        translationService as unknown as TranslationService,
        seoService,
        stateFacade as unknown as CommentThreadStateFacade
      )
    );
    component.ngOnInit();

    stateFacade.notFoundSignal.set(true);
    TestBed.flushEffects();

    expect(seoService.applyNotFoundSeo).toHaveBeenCalledWith(
      'fr',
      '/fr/park/missing-park/parc-introuvable/comments'
    );
  });
});
