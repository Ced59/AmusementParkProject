import type { MockedObject } from 'vitest';
import { EventEmitter, Signal, WritableSignal, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, ParamMap, Router, convertToParamMap } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { of } from 'rxjs';

import {
  CommentThread,
  CreateCommentRequest,
  UpdateCommentRequest
} from '@app/models/comments/comment.models';
import { TranslationService } from '@app/services/translation.service';
import { SeoService } from '@core/seo/seo.service';
import { ScreenState } from '@shared/models/contracts/screen-state.model';
import {
  CommentEditorResetReason,
  CommentThreadStateFacade
} from '../state/comment-thread-state.facade';
import { CommentRichTextImagesFacade } from '../state/comment-rich-text-images.facade';
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
  readonly canWriteSignal: WritableSignal<boolean> = signal<boolean>(false);
  readonly canWrite: Signal<boolean> = this.canWriteSignal.asReadonly();
  readonly canManageSignal: WritableSignal<boolean> = signal<boolean>(false);
  readonly canManage: Signal<boolean> = this.canManageSignal.asReadonly();
  readonly savingSignal: WritableSignal<boolean> = signal<boolean>(false);
  readonly saving: Signal<boolean> = this.savingSignal.asReadonly();
  readonly saveErrorKey: Signal<string | null> = signal<string | null>(null).asReadonly();
  readonly editorResetVersionSignal: WritableSignal<number> = signal<number>(0);
  readonly editorResetVersion: Signal<number> = this.editorResetVersionSignal.asReadonly();
  readonly editorResetReasonSignal: WritableSignal<CommentEditorResetReason | null> =
    signal<CommentEditorResetReason | null>(null);
  readonly editorResetReason: Signal<CommentEditorResetReason | null> =
    this.editorResetReasonSignal.asReadonly();
  readonly notFoundSignal: WritableSignal<boolean> = signal<boolean>(false);
  readonly notFound: Signal<boolean> = this.notFoundSignal.asReadonly();
  readonly deleteCalls: string[] = [];
  readonly createCalls: CreateCommentRequest[] = [];
  readonly updateCalls: UpdateCommentRequest[] = [];

  initializeAuthorAccess(): void {
  }

  load(_targetType: 'Park' | 'ParkItem', _targetId: string): void {
  }

  create(request: CreateCommentRequest): void {
    this.createCalls.push(request);
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

class FakeCommentRichTextImagesFacade {
  readonly uploadingSignal: WritableSignal<boolean> = signal<boolean>(false);
  readonly uploading: Signal<boolean> = this.uploadingSignal.asReadonly();
  readonly errorKey: Signal<string | null> = signal<string | null>(null).asReadonly();
  discardCount: number = 0;
  readonly committedImageIdSnapshots: string[][] = [];

  uploadImage(): Promise<{ id: string }> {
    return Promise.resolve({ id: '0123456789abcdef0123456789abcdef' });
  }

  discardDraftImages(): void {
    this.discardCount += 1;
  }

  markDraftImagesCommitted(imageIds: ReadonlySet<string>): void {
    this.committedImageIdSnapshots.push(Array.from(imageIds));
  }

  clearError(): void {
  }

  resolvePreviewUrl(): string | null {
    return null;
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
    expect(styles).toContain('img.rich-text__image--left');
    expect(styles).toContain('img.rich-text__image--right');
    expect(styles).toContain('float: none');
    expect(styles).toContain('overflow: hidden');
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
    const imagesFacade: FakeCommentRichTextImagesFacade = new FakeCommentRichTextImagesFacade();
    const component: CommentsPageComponent = TestBed.runInInjectionContext(
      (): CommentsPageComponent => new CommentsPageComponent(
        route,
        router,
        translationService as unknown as TranslationService,
        { instant: (key: string): string => key } as unknown as TranslateService,
        seoService,
        stateFacade as unknown as CommentThreadStateFacade,
        imagesFacade as unknown as CommentRichTextImagesFacade
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

  it('lets an owner without publication permission manage an existing comment', () => {
    const routeParamMap: ParamMap = convertToParamMap({ lang: 'fr', id: 'park-1' });
    const stateFacade: FakeCommentThreadStateFacade = new FakeCommentThreadStateFacade();
    const imagesFacade: FakeCommentRichTextImagesFacade = new FakeCommentRichTextImagesFacade();
    stateFacade.canManageSignal.set(true);
    const comment = {
      id: 'comment-1',
      targetType: 'Park' as const,
      targetId: 'park-1',
      authorDisplayName: 'Alice',
      authorAvatarUrl: '/images/avatar-1',
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
        stateFacade as unknown as CommentThreadStateFacade,
        imagesFacade as unknown as CommentRichTextImagesFacade
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

    expect(stateFacade.canWrite()).toBe(false);
    expect(stateFacade.updateCalls).toEqual([{
      id: 'comment-1',
      bodies: [{ languageCode: 'fr', value: '<p>Avis</p>' }],
      isOfficial: false
    }]);
  });

  it('snapshots the strict union of submitted image ids and commits only that snapshot on success', () => {
    const routeParamMap: ParamMap = convertToParamMap({ lang: 'fr', id: 'park-1' });
    const stateFacade: FakeCommentThreadStateFacade = new FakeCommentThreadStateFacade();
    const imagesFacade: FakeCommentRichTextImagesFacade = new FakeCommentRichTextImagesFacade();
    const firstImageId: string = '0123456789abcdef0123456789abcdef';
    const secondImageId: string = 'abcdef0123456789abcdef0123456789';
    stateFacade.canWriteSignal.set(true);
    stateFacade.threadSignal.set({
      targetType: 'Park',
      targetId: 'park-1',
      targetName: 'Demo Park',
      parkId: 'park-1',
      parkName: 'Demo Park',
      comments: []
    });
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
        { applyCommentsSeo: vi.fn(), applyNotFoundSeo: vi.fn() } as unknown as SeoService,
        stateFacade as unknown as CommentThreadStateFacade,
        imagesFacade as unknown as CommentRichTextImagesFacade
      )
    );
    const testable = component as unknown as {
      editorForm: {
        controls: {
          bodies: { setValue(value: Array<{ languageCode: string; value: string }>): void };
        };
      };
      submit(): void;
    };
    testable.editorForm.controls.bodies.setValue([
      {
        languageCode: 'fr',
        value: `<p>Avis</p><img src="/images/${firstImageId}"><img src="/images/${firstImageId}">`
      },
      {
        languageCode: 'en',
        value: `<img class="rich-text__image" src='/images/${secondImageId}'>`
      },
      {
        languageCode: 'es',
        value: `<img src="/images/${secondImageId.toUpperCase()}"><img src="https://cdn.test/images/${secondImageId}">`
      }
    ]);

    testable.submit.call(component);
    expect(stateFacade.createCalls).toHaveLength(1);
    expect(imagesFacade.committedImageIdSnapshots).toEqual([]);

    stateFacade.editorResetReasonSignal.set('saved');
    stateFacade.editorResetVersionSignal.set(1);
    TestBed.flushEffects();

    expect(imagesFacade.committedImageIdSnapshots).toEqual([[firstImageId, secondImageId]]);
  });

  it('rejects an image-only comment globally and blocks submit while an image upload runs', () => {
    const routeParamMap: ParamMap = convertToParamMap({ lang: 'fr', id: 'park-1' });
    const stateFacade: FakeCommentThreadStateFacade = new FakeCommentThreadStateFacade();
    const imagesFacade: FakeCommentRichTextImagesFacade = new FakeCommentRichTextImagesFacade();
    stateFacade.canWriteSignal.set(true);
    stateFacade.threadSignal.set({
      targetType: 'Park',
      targetId: 'park-1',
      targetName: 'Demo Park',
      parkId: 'park-1',
      parkName: 'Demo Park',
      comments: []
    });
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
        { applyCommentsSeo: vi.fn(), applyNotFoundSeo: vi.fn() } as unknown as SeoService,
        stateFacade as unknown as CommentThreadStateFacade,
        imagesFacade as unknown as CommentRichTextImagesFacade
      )
    );
    const testable = component as unknown as {
      editorForm: {
        controls: {
          bodies: { setValue(value: Array<{ languageCode: string; value: string }>): void };
        };
      };
      submit(): void;
    };
    testable.editorForm.controls.bodies.setValue([{
      languageCode: 'fr',
      value: '<img src="/images/0123456789abcdef0123456789abcdef">'
    }]);
    testable.submit.call(component);
    expect(stateFacade.createCalls).toEqual([]);

    testable.editorForm.controls.bodies.setValue([{
      languageCode: 'fr',
      value: '<p>Avis texte</p>'
    }]);
    imagesFacade.uploadingSignal.set(true);
    testable.submit.call(component);
    expect(stateFacade.createCalls).toEqual([]);
  });
});
