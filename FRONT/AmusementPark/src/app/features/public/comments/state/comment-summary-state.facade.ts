import { DestroyRef, Inject, Injectable, Signal, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { take } from 'rxjs';

import { CommentSummary, CommentTargetType } from '@app/models/comments/comment.models';
import { AuthService } from '@app/services/auth/auth.service';
import { COMMENT_DATA_PORT, CommentDataPort } from './comment-data.ports';

@Injectable()
export class CommentSummaryStateFacade {
  private readonly summarySignal = signal<CommentSummary | null>(null);
  private readonly canWriteSignal = signal<boolean>(false);

  readonly summary: Signal<CommentSummary | null> = this.summarySignal.asReadonly();
  readonly canWrite: Signal<boolean> = this.canWriteSignal.asReadonly();

  private currentTargetKey: string | null = null;

  constructor(
    @Inject(COMMENT_DATA_PORT) private readonly commentDataPort: CommentDataPort,
    private readonly authService: AuthService,
    private readonly destroyRef: DestroyRef
  ) {
  }

  load(targetType: CommentTargetType, targetId: string, languageCode: string): void {
    const normalizedTargetId: string = targetId.trim();
    const normalizedLanguageCode: string = languageCode.trim().toLowerCase();
    const targetKey: string = `${targetType}:${normalizedTargetId}:${normalizedLanguageCode}`;
    if (!normalizedTargetId || !normalizedLanguageCode || this.currentTargetKey === targetKey) {
      return;
    }

    this.currentTargetKey = targetKey;
    this.summarySignal.set(null);
    this.commentDataPort.getSummary(targetType, normalizedTargetId, normalizedLanguageCode)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (summary: CommentSummary): void => {
          if (this.currentTargetKey === targetKey
            && summary.targetType === targetType
            && summary.targetId === normalizedTargetId
            && summary.languageCode === normalizedLanguageCode) {
            this.summarySignal.set(summary);
          }
        },
        error: (): void => {
          if (this.currentTargetKey === targetKey) {
            this.summarySignal.set(null);
          }
        }
      });
  }

  initializeAuthorAccess(): void {
    this.authService.ensureValidAccessToken(true)
      .pipe(take(1), takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (token: string | null): void => {
          this.canWriteSignal.set(
            !!token
            && (this.authService.hasRole('ADMIN') || this.authService.hasRole('MODERATOR'))
          );
        },
        error: (): void => {
          this.canWriteSignal.set(false);
        }
      });
  }
}
