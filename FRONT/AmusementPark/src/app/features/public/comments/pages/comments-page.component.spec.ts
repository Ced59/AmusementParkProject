import type { MockedObject } from 'vitest';
import { EventEmitter, Signal, WritableSignal, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, ParamMap, Router, convertToParamMap } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { of } from 'rxjs';

import {
  CommentThread,
  CreateCommentRequest,
  UpdateCommentRequest
} from '@app/models/comments/comment.models';
import { TranslationService } from '@app/services/translation.service';
import {
  COMMON_TEST_IMPORTS,
  provideCommonTestDependencies
} from '@app/testing/common-test-providers';
import { registerSupportedAngularLocales } from '@core/i18n/supported-angular-locales';
import { SeoService } from '@core/seo/seo.service';
import { ImagesApiService } from '@data-access/images/images-api.service';
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
  readonly stateSignal: WritableSignal<ScreenState<CommentThread, string>> =
    signal<ScreenState<CommentThread, string>>({ kind: 'loading' });
  readonly state: Signal<ScreenState<CommentThread, string>> = this.stateSignal.asReadonly();
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
  readonly deleteCalls: Array<{ commentId: string; revision: number }> = [];
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

  delete(commentId: string, revision: number): void {
    this.deleteCalls.push({ commentId, revision });
  }

  clearSaveError(): void {
  }
}

class FakeCommentRichTextImagesFacade {
  readonly uploadingSignal: WritableSignal<boolean> = signal<boolean>(false);
  readonly uploading: Signal<boolean> = this.uploadingSignal.asReadonly();
  readonly errorKey: Signal<string | null> = signal<string | null>(null).asReadonly();
  discardCount: number = 0;
  previewUrl: string | null = null;
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
    return this.previewUrl;
  }
}

class FakeImagesApiService {
  readonly buildImageUrlCalls: Array<{ imageId: string; width: number | undefined }> = [];

  buildImageUrl(imageId: string, options: { width?: number } = {}): string {
    this.buildImageUrlCalls.push({ imageId, width: options.width });
    return `/api/images/binary/${imageId}?width=${options.width ?? 0}`;
  }

  resolveImageUrl(imagePathOrUrl: string): string {
    return imagePathOrUrl;
  }

  buildImageSrcSet(): string | null {
    return null;
  }
}

describe('CommentsPageComponent', () => {
  it('renders a ready French comment with its localized date instead of the loading state', async () => {
    registerSupportedAngularLocales();
    const routeParamMap: ParamMap = convertToParamMap({ lang: 'fr', id: 'park-1' });
    const route: ActivatedRoute = {
      snapshot: { paramMap: routeParamMap },
      paramMap: of(routeParamMap),
      parent: null
    } as unknown as ActivatedRoute;
    const stateFacade: FakeCommentThreadStateFacade = new FakeCommentThreadStateFacade();
    const imagesFacade: FakeCommentRichTextImagesFacade = new FakeCommentRichTextImagesFacade();
    const imagesApiService: FakeImagesApiService = new FakeImagesApiService();
    const thread: CommentThread = {
      targetType: 'Park',
      targetId: 'park-1',
      targetName: 'Parc Démo',
      parkId: 'park-1',
      parkName: 'Parc Démo',
      comments: [{
        id: 'comment-1',
        targetType: 'Park',
        targetId: 'park-1',
        authorDisplayName: 'Admin01',
        authorAvatarUrl: null,
        authorRole: 'Admin',
        bodies: [{ languageCode: 'fr', value: '<p>Test commentaire en français</p>' }],
        isOfficial: true,
        canUpdate: false,
        canDelete: false,
        revision: 1,
        createdAtUtc: '2026-07-01T10:00:00Z',
        updatedAtUtc: '2026-07-01T10:00:00Z'
      }, {
        id: 'comment-2',
        targetType: 'Park',
        targetId: 'park-1',
        authorDisplayName: 'Moderateur02',
        authorAvatarUrl: null,
        authorRole: 'Moderator',
        bodies: [{ languageCode: 'es', value: '<p>Comentario en español</p>' }],
        isOfficial: false,
        canUpdate: false,
        canDelete: false,
        revision: 1,
        createdAtUtc: '2026-07-02T11:00:00Z',
        updatedAtUtc: '2026-07-02T11:00:00Z'
      }]
    };
    stateFacade.threadSignal.set(thread);
    stateFacade.stateSignal.set({ kind: 'ready', data: thread });

    await TestBed.configureTestingModule({
      imports: [...COMMON_TEST_IMPORTS, CommentsPageComponent],
      providers: [
        ...provideCommonTestDependencies(),
        { provide: ActivatedRoute, useValue: route },
        { provide: TranslationService, useClass: FakeTranslationService },
        {
          provide: SeoService,
          useValue: {
            applyCommentsSeo: vi.fn(),
            applyNotFoundSeo: vi.fn()
          }
        },
        { provide: ImagesApiService, useValue: imagesApiService }
      ]
    })
      .overrideComponent(CommentsPageComponent, {
        set: {
          providers: [
            { provide: CommentThreadStateFacade, useValue: stateFacade },
            { provide: CommentRichTextImagesFacade, useValue: imagesFacade }
          ]
        }
      })
      .compileComponents();

    const translateService: TranslateService = TestBed.inject(TranslateService);
    translateService.setTranslation('fr', {
      comments: {
        subtitle: 'Commentaires publiés',
        officialBadge: 'Avis officiel',
        roles: {
          Admin: 'Administrateur',
          Moderator: 'Modérateur'
        },
        view: {
          currentCount: {
            one: '{{count}} commentaire en {{language}}',
            other: '{{count}} commentaires en {{language}}'
          },
          allCount: {
            one: '{{count}} commentaire toutes langues',
            other: '{{count}} commentaires toutes langues'
          },
          allLanguages: {
            one: 'Voir le commentaire de toutes les langues',
            other: 'Voir les {{count}} commentaires de toutes les langues'
          },
          currentOnly: 'Voir uniquement les commentaires en {{language}}'
        }
      }
    });
    translateService.use('fr');

    const fixture: ComponentFixture<CommentsPageComponent> =
      TestBed.createComponent(CommentsPageComponent);
    fixture.detectChanges();

    const element: HTMLElement = fixture.nativeElement as HTMLElement;
    const commentBody: HTMLElement | null = element.querySelector('.comment-card__body');
    const publishedAt: HTMLTimeElement | null = element.querySelector('time');

    expect(element.querySelector('.app-page-state--loading')).toBeNull();
    expect(commentBody?.textContent).toContain('Test commentaire en français');
    expect(publishedAt?.textContent).toContain('1 juillet 2026');
    expect(element.querySelectorAll('.comment-card')).toHaveLength(1);
    expect(element.textContent).not.toContain('Comentario en español');

    const languageToggle: HTMLButtonElement | null =
      element.querySelector('.comments-language-filter button');
    expect(languageToggle?.textContent).toContain('Voir les 2 commentaires de toutes les langues');
    languageToggle?.click();
    fixture.detectChanges();

    expect(element.querySelectorAll('.comment-card')).toHaveLength(2);
    expect(element.textContent).toContain('Comentario en español');
    expect(element.textContent).toContain('Español');

    const routingTranslationService: FakeTranslationService =
      TestBed.inject(TranslationService) as unknown as FakeTranslationService;
    routingTranslationService.languageChanged.emit('de');
    fixture.detectChanges();

    expect(element.querySelectorAll('.comment-card')).toHaveLength(0);
    expect(element.querySelector('.comments-empty')).not.toBeNull();
    expect(element.querySelector('.comments-language-filter button')?.textContent)
      .toContain('Voir les 2 commentaires de toutes les langues');
  });

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
    expect(styles).toContain('white-space: normal');
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
    const imagesFacade: FakeCommentRichTextImagesFacade = new FakeCommentRichTextImagesFacade();
    const imagesApiService: FakeImagesApiService = new FakeImagesApiService();
    const component: CommentsPageComponent = TestBed.runInInjectionContext(
      (): CommentsPageComponent => new CommentsPageComponent(
        route,
        router,
        translationService as unknown as TranslationService,
        { instant: (key: string): string => key } as unknown as TranslateService,
        seoService,
        stateFacade as unknown as CommentThreadStateFacade,
        imagesFacade as unknown as CommentRichTextImagesFacade,
        imagesApiService as unknown as ImagesApiService
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
      revision: 4,
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
        imagesFacade as unknown as CommentRichTextImagesFacade,
        new FakeImagesApiService() as unknown as ImagesApiService
      )
    );
    const managementComponent = component as unknown as {
      deleteComment(value: typeof comment): void;
      startEditing(value: typeof comment): void;
      submit(): void;
    };
    TestBed.flushEffects();
    const deleteComment = managementComponent.deleteComment.bind(component);

    deleteComment(comment);

    expect(confirmSpy).toHaveBeenCalledWith('comments.management.deleteConfirm');
    expect(stateFacade.deleteCalls).toEqual([]);

    confirmSpy.mockReturnValue(true);
    deleteComment(comment);

    expect(stateFacade.deleteCalls).toEqual([{ commentId: 'comment-1', revision: 4 }]);

    managementComponent.startEditing.call(component, comment);
    managementComponent.submit.call(component);

    expect(stateFacade.canWrite()).toBe(false);
    expect(stateFacade.updateCalls).toEqual([{
      id: 'comment-1',
      bodies: [{ languageCode: 'fr', value: '<p>Avis</p>' }],
      isOfficial: false,
      revision: 4
    }]);

    const preservedDraft = [{ languageCode: 'fr', value: '<p>Mon brouillon local</p>' }];
    const testableForm = component as unknown as {
      editorForm: {
        controls: {
          bodies: { setValue(value: Array<{ languageCode: string; value: string }>): void };
        };
      };
    };
    testableForm.editorForm.controls.bodies.setValue(preservedDraft);
    stateFacade.threadSignal.set({
      targetType: 'Park',
      targetId: 'park-1',
      targetName: 'Demo Park',
      parkId: 'park-1',
      parkName: 'Demo Park',
      comments: [{
        ...comment,
        bodies: [{ languageCode: 'fr', value: '<p>Version distante</p>' }],
        revision: 5
      }]
    });
    TestBed.flushEffects();

    managementComponent.submit.call(component);

    expect(stateFacade.updateCalls[1]).toEqual({
      id: 'comment-1',
      bodies: preservedDraft,
      isOfficial: false,
      revision: 5
    });
  });

  it('snapshots the strict union of submitted image ids and commits only that snapshot on success', () => {
    const routeParamMap: ParamMap = convertToParamMap({ lang: 'fr', id: 'park-1' });
    const stateFacade: FakeCommentThreadStateFacade = new FakeCommentThreadStateFacade();
    const imagesFacade: FakeCommentRichTextImagesFacade = new FakeCommentRichTextImagesFacade();
    const imagesApiService: FakeImagesApiService = new FakeImagesApiService();
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
        imagesFacade as unknown as CommentRichTextImagesFacade,
        imagesApiService as unknown as ImagesApiService
      )
    );
    const testable = component as unknown as {
      editorForm: {
        controls: {
          bodies: { setValue(value: Array<{ languageCode: string; value: string }>): void };
        };
      };
      submit(): void;
      resolveCommentImagePreview(imageId: string): string;
    };
    expect(testable.resolveCommentImagePreview(firstImageId)).toBe(
      `/api/images/binary/${firstImageId}?width=1280`
    );
    expect(imagesApiService.buildImageUrlCalls).toEqual([
      { imageId: firstImageId, width: 1280 }
    ]);
    imagesFacade.previewUrl = 'blob:comment-draft';
    expect(testable.resolveCommentImagePreview(secondImageId)).toBe('blob:comment-draft');
    expect(imagesApiService.buildImageUrlCalls).toHaveLength(1);

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
    expect(stateFacade.createCalls[0]?.bodies).toEqual([
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
    expect(JSON.stringify(stateFacade.createCalls[0]?.bodies)).not.toContain(
      '/api/images/binary/'
    );
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
        imagesFacade as unknown as CommentRichTextImagesFacade,
        new FakeImagesApiService() as unknown as ImagesApiService
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
