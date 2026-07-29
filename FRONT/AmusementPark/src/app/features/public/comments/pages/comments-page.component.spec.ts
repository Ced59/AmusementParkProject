import type { MockedObject } from 'vitest';
import { EventEmitter, Signal, WritableSignal, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, ParamMap, Router, convertToParamMap } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { of } from 'rxjs';

import { CommentThread, UpdateCommentRequest } from '@app/models/comments/comment.models';
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
  readonly threadSignal: WritableSignal<CommentThread | null> = signal<CommentThread | null>(null);
  readonly thread: Signal<CommentThread | null> = this.threadSignal.asReadonly();
  readonly canWrite: Signal<boolean> = signal<boolean>(false).asReadonly();
  readonly canManageSignal: WritableSignal<boolean> = signal<boolean>(false);
  readonly canManage: Signal<boolean> = this.canManageSignal.asReadonly();
  readonly saving: Signal<boolean> = signal<boolean>(false).asReadonly();
  readonly saveErrorKey: Signal<string | null> = signal<string | null>(null).asReadonly();
  readonly editorResetVersion: Signal<number> = signal<number>(0).asReadonly();
  readonly notFoundSignal: WritableSignal<boolean> = signal<boolean>(false);
  readonly notFound: Signal<boolean> = this.notFoundSignal.asReadonly();
  readonly deleteCalls: string[] = [];
  readonly updateCalls: UpdateCommentRequest[] = [];

  initializeAuthorAccess(): void {
  }

  load(_targetType: 'Park' | 'ParkItem', _targetId: string): void {
  }

  create(): void {
  }

  update(request: UpdateCommentRequest): void {
    this.updateCalls.push(request);
  }

  delete(_commentId: string): void {
    this.deleteCalls.push(_commentId);
  }

  clearSaveError(): void {
  }
}

describe('CommentsPageComponent', () => {
  it('lets the comment editor shrink inside the mobile page', () => {
    const styles: string = (
      CommentsPageComponent as unknown as { ɵcmp: { styles: string[] } }
    ).ɵcmp.styles.join('\n');

    expect(styles).toContain('.comment-editor');
    expect(styles).toContain('.comment-editor__header');
    expect(styles).toContain('app-localized-rich-text-editor');
    expect(styles).toContain('min-width: 0');
    expect(styles).toContain('overflow-wrap: anywhere');
  });

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
        { instant: (key: string): string => key } as unknown as TranslateService,
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

  it('requires deletion confirmation and submits an existing comment through edit mode', () => {
    const routeParamMap: ParamMap = convertToParamMap({ lang: 'fr', id: 'park-1' });
    const stateFacade: FakeCommentThreadStateFacade = new FakeCommentThreadStateFacade();
    stateFacade.canManageSignal.set(true);
    const comment = {
      id: 'comment-1',
      targetType: 'Park' as const,
      targetId: 'park-1',
      authorDisplayName: 'Alice',
      authorRole: 'Admin' as const,
      bodies: [{ languageCode: 'fr', value: '<p>Avis</p>' }],
      isOfficial: false,
      canUpdate: true,
      canDelete: true,
      createdAtUtc: '2026-07-01T10:00:00Z',
      updatedAtUtc: '2026-07-01T10:00:00Z'
    };
    stateFacade.threadSignal.set({
      targetType: 'Park',
      targetId: 'park-1',
      targetName: 'Demo Park',
      parkId: 'park-1',
      parkName: 'Demo Park',
      comments: [comment]
    });
    const confirmSpy = vi.spyOn(globalThis, 'confirm').mockReturnValue(false);
    const component: CommentsPageComponent = TestBed.runInInjectionContext(
      (): CommentsPageComponent => new CommentsPageComponent(
        {
          snapshot: { paramMap: routeParamMap },
          paramMap: of(routeParamMap),
          parent: null
        } as unknown as ActivatedRoute,
        { url: '/fr/park/park-1/demo-park/comments' } as Router,
        new FakeTranslationService() as unknown as TranslationService,
        { instant: (key: string): string => key } as unknown as TranslateService,
        {
          applyCommentsSeo: vi.fn(),
          applyNotFoundSeo: vi.fn()
        } as unknown as SeoService,
        stateFacade as unknown as CommentThreadStateFacade
      )
    );
    const managementComponent = component as unknown as {
      deleteComment(value: typeof comment): void;
      startEditing(value: typeof comment): void;
      submit(): void;
    };
    const deleteComment = managementComponent.deleteComment.bind(component);

    deleteComment(comment);

    expect(confirmSpy).toHaveBeenCalledWith('comments.management.deleteConfirm');
    expect(stateFacade.deleteCalls).toEqual([]);

    confirmSpy.mockReturnValue(true);
    deleteComment(comment);

    expect(stateFacade.deleteCalls).toEqual(['comment-1']);

    managementComponent.startEditing.call(component, comment);
    managementComponent.submit.call(component);

    expect(stateFacade.updateCalls).toEqual([{
      id: 'comment-1',
      bodies: [{ languageCode: 'fr', value: '<p>Avis</p>' }],
      isOfficial: false
    }]);
  });
});
