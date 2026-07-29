import { DestroyRef, Inject, Injectable, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { take } from 'rxjs';

import {
  CommentTargetType,
  CommentThread,
  CreateCommentRequest,
  PublicComment
} from '@app/models/comments/comment.models';
import { AuthService } from '@app/services/auth/auth.service';
import { ToastMessageService } from '@app/services/messages/toast-message.service';
import { hasHttpStatus } from '@core/http/http-error-status.helpers';
import { SsrHttpStatusService } from '@core/ssr/ssr-http-status.service';
import { applySsrPublicDataErrorStatus } from '@core/ssr/ssr-public-error-status';
import { ScreenState } from '@shared/models/contracts/screen-state.model';
import { TranslateService } from '@ngx-translate/core';
import { COMMENT_DATA_PORT, CommentDataPort } from './comment-data.ports';

@Injectable()
export class CommentThreadStateFacade {
  private readonly stateSignal = signal<ScreenState<CommentThread, string>>({ kind: 'loading' });
  private readonly canWriteSignal = signal<boolean>(false);
  private readonly savingSignal = signal<boolean>(false);
  private readonly saveErrorKeySignal = signal<string | null>(null);
  private readonly createdVersionSignal = signal<number>(0);
  private readonly notFoundSignal = signal<boolean>(false);

  readonly state: Signal<ScreenState<CommentThread, string>> = this.stateSignal.asReadonly();
  readonly thread: Signal<CommentThread | null> = computed(() => this.stateSignal().data ?? null);
  readonly canWrite: Signal<boolean> = this.canWriteSignal.asReadonly();
  readonly saving: Signal<boolean> = this.savingSignal.asReadonly();
  readonly saveErrorKey: Signal<string | null> = this.saveErrorKeySignal.asReadonly();
  readonly createdVersion: Signal<number> = this.createdVersionSignal.asReadonly();
  readonly notFound: Signal<boolean> = this.notFoundSignal.asReadonly();

  private currentTargetKey: string | null = null;

  constructor(
    @Inject(COMMENT_DATA_PORT) private readonly commentDataPort: CommentDataPort,
    private readonly authService: AuthService,
    private readonly toastMessageService: ToastMessageService,
    private readonly translateService: TranslateService,
    private readonly destroyRef: DestroyRef,
    private readonly ssrHttpStatusService: SsrHttpStatusService
  ) {
  }

  initializeAuthorAccess(): void {
    this.authService.ensureValidAccessToken(true)
      .pipe(take(1), takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (token: string | null): void => {
          this.canWriteSignal.set(!!token && this.hasStaffRole());
        },
        error: (): void => {
          this.canWriteSignal.set(false);
        }
      });
  }

  load(targetType: CommentTargetType, targetId: string): void {
    const normalizedTargetId: string = targetId.trim();
    const targetKey: string = `${targetType}:${normalizedTargetId}`;
    if (!normalizedTargetId || this.currentTargetKey === targetKey) {
      return;
    }

    this.currentTargetKey = targetKey;
    this.stateSignal.set({ kind: 'loading' });
    this.saveErrorKeySignal.set(null);
    this.notFoundSignal.set(false);

    this.commentDataPort.getThread(targetType, normalizedTargetId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (thread: CommentThread): void => {
          if (this.currentTargetKey !== targetKey
            || thread.targetType !== targetType
            || thread.targetId !== normalizedTargetId) {
            return;
          }

          this.stateSignal.set({
            kind: 'ready',
            data: {
              ...thread,
              comments: sortComments(thread.comments)
            }
          });
        },
        error: (error: unknown): void => {
          if (this.currentTargetKey === targetKey) {
            applySsrPublicDataErrorStatus(error, this.ssrHttpStatusService);
            this.notFoundSignal.set(hasHttpStatus(error, 404));
            this.stateSignal.set({ kind: 'error', error: 'comments.errors.load' });
          }
        }
      });
  }

  create(request: CreateCommentRequest): void {
    const thread: CommentThread | null = this.thread();
    if (!thread
      || !this.canWriteSignal()
      || this.savingSignal()
      || request.targetType !== thread.targetType
      || request.targetId !== thread.targetId) {
      return;
    }

    this.savingSignal.set(true);
    this.saveErrorKeySignal.set(null);
    this.authService.ensureValidAccessToken(true)
      .pipe(take(1), takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (token: string | null): void => {
          if (!token || !this.hasStaffRole()) {
            this.canWriteSignal.set(false);
            this.savingSignal.set(false);
            this.saveErrorKeySignal.set('comments.errors.forbidden');
            return;
          }

          this.commentDataPort.createComment(request)
            .pipe(take(1), takeUntilDestroyed(this.destroyRef))
            .subscribe({
              next: (comment: PublicComment): void => {
                const currentThread: CommentThread | null = this.thread();
                if (!currentThread
                  || comment.targetType !== currentThread.targetType
                  || comment.targetId !== currentThread.targetId) {
                  this.savingSignal.set(false);
                  this.saveErrorKeySignal.set('comments.errors.save');
                  return;
                }

                this.stateSignal.set({
                  kind: 'ready',
                  data: {
                    ...currentThread,
                    comments: sortComments([comment, ...currentThread.comments])
                  }
                });
                this.savingSignal.set(false);
                this.createdVersionSignal.update((value: number) => value + 1);
                this.toastMessageService.add(
                  'success',
                  this.translateService.instant('common.success'),
                  this.translateService.instant('comments.editor.saved')
                );
              },
              error: (): void => {
                this.savingSignal.set(false);
                this.saveErrorKeySignal.set('comments.errors.save');
              }
            });
        },
        error: (): void => {
          this.savingSignal.set(false);
          this.saveErrorKeySignal.set('comments.errors.save');
        }
      });
  }

  clearSaveError(): void {
    this.saveErrorKeySignal.set(null);
  }

  private hasStaffRole(): boolean {
    return this.authService.hasRole('ADMIN') || this.authService.hasRole('MODERATOR');
  }
}

function sortComments(comments: PublicComment[]): PublicComment[] {
  return [...comments].sort((left: PublicComment, right: PublicComment): number => {
    if (left.isOfficial !== right.isOfficial) {
      return left.isOfficial ? -1 : 1;
    }

    return Date.parse(right.createdAtUtc) - Date.parse(left.createdAtUtc);
  });
}
