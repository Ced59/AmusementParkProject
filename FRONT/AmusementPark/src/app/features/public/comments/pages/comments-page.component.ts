import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, Signal, computed, effect, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, ParamMap, Router, RouterLink } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

import {
  CommentTargetType,
  CommentThread,
  CreateCommentRequest,
  PublicComment,
  UpdateCommentRequest
} from '@app/models/comments/comment.models';
import { ManagedRichTextImage } from '@app/models/comments/comment-image.models';
import { LocalizedItem } from '@app/models/shared/localized-item';
import { TranslationService } from '@app/services/translation.service';
import { SeoService } from '@core/seo/seo.service';
import { ImagesApiService } from '@data-access/images/images-api.service';
import { LocalizedRichTextEditorComponent } from '@shared/components/localized-rich-text-editor/localized-rich-text-editor.component';
import { ImageDisplayComponent } from '@shared/components/image-display/image-display.component';
import { PageStateComponent } from '@shared/components/page-state/page-state.component';
import { LANGUAGES, LanguageOption } from '@shared/models/localization';
import { LocalizedPluralPipe, SafeCommentRichHtmlPipe } from '@shared/pipes';
import {
  extractManagedCommentImageIdsFromHtml
} from '@shared/utils/comments/managed-comment-image.helpers';
import {
  findExactLocalizedText,
  findLocalizedTextWithLanguage,
  isRichTextEmpty
} from '@shared/utils/localization';
import {
  buildPublicParkCommentsRouteCommands,
  buildPublicParkItemCommentsRouteCommands,
  buildPublicParkItemRouteCommands,
  buildPublicParkRouteCommands,
  buildPublicRoutePath
} from '@shared/utils/routing/public-detail-route.helpers';
import { resolveLanguageFromActivatedRoute } from '@shared/utils/routing/route-language.utils';
import { UiButtonDirective, UiChipComponent, UiKickerComponent, UiSurfaceDirective } from '@ui/primitives';
import {
  CommentEditorResetReason,
  CommentThreadStateFacade
} from '../state/comment-thread-state.facade';
import { CommentRichTextImagesFacade } from '../state/comment-rich-text-images.facade';

interface CommentEditorForm {
  readonly bodies: FormControl<LocalizedItem<string>[]>;
  readonly isOfficial: FormControl<boolean>;
}

interface DisplayedComment {
  readonly comment: PublicComment;
  readonly body: string;
  readonly languageCode: string;
  readonly languageLabel: string;
}

@Component({
  selector: 'app-comments-page',
  templateUrl: './comments-page.component.html',
  styleUrls: ['./comments-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [CommentThreadStateFacade, CommentRichTextImagesFacade],
  imports: [
    DatePipe,
    ImageDisplayComponent,
    LocalizedPluralPipe,
    LocalizedRichTextEditorComponent,
    PageStateComponent,
    ReactiveFormsModule,
    RouterLink,
    SafeCommentRichHtmlPipe,
    TranslateModule,
    UiButtonDirective,
    UiChipComponent,
    UiKickerComponent,
    UiSurfaceDirective
  ]
})
export class CommentsPageComponent implements OnInit {
  protected readonly state = this.stateFacade.state;
  protected readonly thread = this.stateFacade.thread;
  protected readonly canWrite = this.stateFacade.canWrite;
  protected readonly canManage = this.stateFacade.canManage;
  protected readonly saving = this.stateFacade.saving;
  protected readonly uploadingImages = this.commentImagesFacade.uploading;
  protected readonly imageErrorKey = this.commentImagesFacade.errorKey;
  protected readonly saveErrorKey = this.stateFacade.saveErrorKey;
  protected readonly notFound = this.stateFacade.notFound;
  protected readonly currentLanguage = signal<string>('en');
  protected readonly showAllLanguages = signal<boolean>(false);
  protected readonly currentLanguageLabel: Signal<string> = computed(
    (): string => this.resolveLanguageLabel(this.currentLanguage())
  );
  protected readonly currentLanguageComments: Signal<readonly DisplayedComment[]> = computed(
    (): readonly DisplayedComment[] => this.buildDisplayedComments(true)
  );
  protected readonly allLanguageComments: Signal<readonly DisplayedComment[]> = computed(
    (): readonly DisplayedComment[] => this.buildDisplayedComments(false)
  );
  protected readonly displayedComments: Signal<readonly DisplayedComment[]> = computed(
    (): readonly DisplayedComment[] =>
      this.showAllLanguages() ? this.allLanguageComments() : this.currentLanguageComments()
  );
  protected readonly hasOtherLanguageComments: Signal<boolean> = computed(
    (): boolean => this.allLanguageComments().length > this.currentLanguageComments().length
  );
  protected readonly avatarResponsiveWidths: readonly number[] = [48, 96];
  protected readonly editingCommentId = signal<string | null>(null);
  protected readonly isEditing = computed(() => this.editingCommentId() !== null);
  protected readonly editorForm = new FormGroup<CommentEditorForm>({
    bodies: new FormControl<LocalizedItem<string>[]>([], { nonNullable: true }),
    isOfficial: new FormControl<boolean>(false, { nonNullable: true })
  });
  protected readonly uploadCommentImage = (file: File): Promise<ManagedRichTextImage> =>
    this.commentImagesFacade.uploadImage(file);
  protected readonly resolveCommentImagePreview = (imageId: string): string =>
    this.commentImagesFacade.resolvePreviewUrl(imageId)
    ?? this.imagesApiService.buildImageUrl(imageId, { width: 1280 });

  protected readonly homeLink: Signal<string[]> = computed(() => ['/', this.currentLanguage(), 'home']);
  protected readonly parksLink: Signal<string[]> = computed(() => ['/', this.currentLanguage(), 'parks']);
  protected readonly parkLink: Signal<string[] | null> = computed(() => {
    const currentThread: CommentThread | null = this.thread();
    return currentThread
      ? buildPublicParkRouteCommands({
        language: this.currentLanguage(),
        parkId: currentThread.parkId,
        parkName: currentThread.parkName
      })
      : null;
  });
  protected readonly targetLink: Signal<string[] | null> = computed(() => {
    const currentThread: CommentThread | null = this.thread();
    if (!currentThread) {
      return null;
    }

    if (currentThread.targetType === 'Park') {
      return this.parkLink();
    }

    return buildPublicParkItemRouteCommands({
      language: this.currentLanguage(),
      parkId: currentThread.parkId,
      parkName: currentThread.parkName,
      itemId: currentThread.targetId,
      itemName: currentThread.targetName
    });
  });

  private readonly destroyRef: DestroyRef = inject(DestroyRef);
  private lastEditorResetVersion: number = 0;
  private pendingSubmissionImageIds: ReadonlySet<string> | null = null;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly translationService: TranslationService,
    private readonly translateService: TranslateService,
    private readonly seoService: SeoService,
    private readonly stateFacade: CommentThreadStateFacade,
    private readonly commentImagesFacade: CommentRichTextImagesFacade,
    private readonly imagesApiService: ImagesApiService
  ) {
    effect((): void => {
      const currentThread: CommentThread | null = this.thread();
      if (!currentThread) {
        return;
      }

      this.seoService.applyCommentsSeo(
        currentThread,
        this.currentLanguage(),
        this.router.url,
        this.resolveCanonicalPath(currentThread)
      );
    });

    effect((): void => {
      if (this.notFound()) {
        this.seoService.applyNotFoundSeo(this.currentLanguage(), this.router.url);
      }
    });

    effect((): void => {
      const resetVersion: number = this.stateFacade.editorResetVersion();
      if (resetVersion > this.lastEditorResetVersion) {
        const resetReason: CommentEditorResetReason | null =
          this.stateFacade.editorResetReason();
        if (resetReason === 'saved') {
          this.commentImagesFacade.markDraftImagesCommitted(
            this.pendingSubmissionImageIds ?? new Set<string>()
          );
        } else {
          this.commentImagesFacade.discardDraftImages();
        }
        this.pendingSubmissionImageIds = null;
      }
      this.lastEditorResetVersion = resetVersion;
      this.resetEditor();
    });
  }

  ngOnInit(): void {
    const initialLanguage: string = resolveLanguageFromActivatedRoute(
      this.route,
      this.translationService.getCurrentLang() || 'en'
    );
    this.currentLanguage.set(initialLanguage);
    this.stateFacade.initializeAuthorAccess();

    this.translationService.languageChanged
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((language: string): void => {
        this.currentLanguage.set(language);
        this.showAllLanguages.set(false);
      });

    this.route.paramMap
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((params: ParamMap): void => {
        const itemId: string | null = params.get('itemId');
        const parkId: string | null = params.get('id');
        const targetType: CommentTargetType = itemId ? 'ParkItem' : 'Park';
        const targetId: string | null = itemId ?? parkId;

        if (targetId) {
          this.showAllLanguages.set(false);
          this.pendingSubmissionImageIds = null;
          this.commentImagesFacade.discardDraftImages();
          this.resetEditor();
          this.stateFacade.load(targetType, targetId);
        }
      });
  }

  protected showCommentsFromAllLanguages(): void {
    this.showAllLanguages.set(true);
  }

  protected showCommentsFromCurrentLanguage(): void {
    this.showAllLanguages.set(false);
  }

  protected submit(): void {
    const currentThread: CommentThread | null = this.thread();
    const bodies: LocalizedItem<string>[] = this.editorForm.controls.bodies.value;
    if (!currentThread
      || !this.hasGlobalCommentText(bodies)
      || this.saving()
      || this.uploadingImages()) {
      this.editorForm.controls.bodies.markAsTouched();
      return;
    }

    this.pendingSubmissionImageIds = this.captureManagedImageIds(bodies);
    const editingId: string | null = this.editingCommentId();
    if (editingId) {
      const editingComment: PublicComment | undefined = currentThread.comments.find(
        (comment: PublicComment) => comment.id === editingId
      );
      if (!editingComment) {
        this.pendingSubmissionImageIds = null;
        return;
      }

      const request: UpdateCommentRequest = {
        id: editingId,
        bodies,
        isOfficial: this.editorForm.controls.isOfficial.value,
        revision: editingComment.revision
      };
      this.stateFacade.update(request);
      return;
    }

    const createRequest: CreateCommentRequest = {
      targetType: currentThread.targetType,
      targetId: currentThread.targetId,
      bodies,
      isOfficial: this.editorForm.controls.isOfficial.value
    };
    this.stateFacade.create(createRequest);
  }

  protected startEditing(comment: PublicComment): void {
    if (!this.canUpdateComment(comment) || this.saving() || this.uploadingImages()) {
      return;
    }

    this.pendingSubmissionImageIds = null;
    this.commentImagesFacade.discardDraftImages();
    this.editingCommentId.set(comment.id);
    this.editorForm.reset({
      bodies: comment.bodies.map((body: LocalizedItem<string>) => ({ ...body })),
      isOfficial: comment.isOfficial
    });
    this.clearSaveError();
  }

  protected cancelEditing(): void {
    if (!this.saving() && !this.uploadingImages()) {
      this.pendingSubmissionImageIds = null;
      this.commentImagesFacade.discardDraftImages();
      this.resetEditor();
    }
  }

  protected deleteComment(comment: PublicComment): void {
    if (!this.canDeleteComment(comment) || this.saving() || this.uploadingImages()) {
      return;
    }

    const confirmed: boolean = confirm(
      this.translateService.instant('comments.management.deleteConfirm')
    );
    if (!confirmed) {
      return;
    }

    this.stateFacade.delete(comment.id, comment.revision);
  }

  protected canUpdateComment(comment: PublicComment): boolean {
    return this.canManage() && comment.canUpdate;
  }

  protected canDeleteComment(comment: PublicComment): boolean {
    return this.canManage() && comment.canDelete;
  }

  protected clearSaveError(): void {
    this.stateFacade.clearSaveError();
    this.commentImagesFacade.clearError();
  }

  protected hasGlobalCommentText(bodies: readonly LocalizedItem<string>[]): boolean {
    return bodies.some((body: LocalizedItem<string>) => !isRichTextEmpty(body.value));
  }

  private resetEditor(): void {
    this.editingCommentId.set(null);
    this.editorForm.reset({
      bodies: [],
      isOfficial: false
    });
  }

  private captureManagedImageIds(
    bodies: readonly LocalizedItem<string>[]
  ): ReadonlySet<string> {
    const imageIds: Set<string> = new Set<string>();
    for (const body of bodies) {
      for (const imageId of extractManagedCommentImageIdsFromHtml(body.value)) {
        imageIds.add(imageId);
      }
    }
    return imageIds;
  }

  private buildDisplayedComments(exactLanguageOnly: boolean): readonly DisplayedComment[] {
    const currentThread: CommentThread | null = this.thread();
    if (!currentThread) {
      return [];
    }

    return currentThread.comments.flatMap(
      (comment: PublicComment): DisplayedComment[] => {
        const body: LocalizedItem<string> | undefined = exactLanguageOnly
          ? findExactLocalizedText(comment.bodies, this.currentLanguage())
          : findLocalizedTextWithLanguage(comment.bodies, this.currentLanguage());
        if (!body) {
          return [];
        }

        return [{
          comment,
          body: body.value,
          languageCode: body.languageCode.trim().toLowerCase(),
          languageLabel: this.resolveLanguageLabel(body.languageCode)
        }];
      }
    );
  }

  private resolveLanguageLabel(languageCode: string): string {
    const normalizedLanguageCode: string = languageCode.trim().toLowerCase();
    return LANGUAGES.find(
      (language: LanguageOption): boolean => language.value === normalizedLanguageCode
    )?.label ?? normalizedLanguageCode.toUpperCase();
  }

  private resolveCanonicalPath(thread: CommentThread): string | null {
    if (thread.targetType === 'Park') {
      return buildPublicRoutePath(buildPublicParkCommentsRouteCommands({
        language: this.currentLanguage(),
        parkId: thread.parkId,
        parkName: thread.parkName
      }));
    }

    return buildPublicRoutePath(buildPublicParkItemCommentsRouteCommands({
      language: this.currentLanguage(),
      parkId: thread.parkId,
      parkName: thread.parkName,
      itemId: thread.targetId,
      itemName: thread.targetName
    }));
  }
}
