import { DestroyRef, Inject, Injectable, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { Observable, take } from 'rxjs';

import { RatingSummary, RatingTargetType, UserRating, UserRatingUpsertRequest } from '@app/models/ratings/rating.models';
import { RatingMethodology } from '@app/models/ratings/rating-methodology.models';
import { AuthService } from '@app/services/auth/auth.service';
import { ToastMessageService } from '@app/services/messages/toast-message.service';
import { ModalService } from '@app/services/modal/modal.service';
import { PUBLIC_RATING_RATINGS_PORT, PublicRatingRatingsPort } from './public-rating-state-data.ports';
import { anonymousHttpOptions } from '@core/http/auth/anonymous-http-options';

@Injectable()
export class PublicRatingStateFacade {
  private readonly targetTypeSignal = signal<RatingTargetType | null>(null);
  private readonly targetIdSignal = signal<string | null>(null);
  private readonly summarySignal = signal<RatingSummary | null>(null);
  private readonly userRatingSignal = signal<UserRating | null>(null);
  private readonly savingSignal = signal<boolean>(false);
  private readonly messageKeySignal = signal<string | null>(null);
  private readonly methodologySignal = signal<RatingMethodology | null>(null);
  private methodologyRequestKey: string | null = null;
  private methodologyResolvedKey: string | null = null;
  private methodologyRequestId: number = 0;

  public readonly summary: Signal<RatingSummary | null> = this.summarySignal.asReadonly();
  public readonly userRatingValue: Signal<number | null> = computed(() => this.userRatingSignal()?.value ?? null);
  public readonly saving: Signal<boolean> = this.savingSignal.asReadonly();
  public readonly messageKey: Signal<string | null> = this.messageKeySignal.asReadonly();
  public readonly methodology: Signal<RatingMethodology | null> = this.methodologySignal.asReadonly();

  constructor(
    @Inject(PUBLIC_RATING_RATINGS_PORT) private readonly ratingsApiService: PublicRatingRatingsPort,
    private readonly authService: AuthService,
    private readonly modalService: ModalService,
    private readonly toastMessageService: ToastMessageService,
    private readonly translateService: TranslateService,
    private readonly destroyRef: DestroyRef
  ) {
  }

  configure(targetType: RatingTargetType, targetId: string, summary: RatingSummary | null): void {
    const normalizedTargetId: string = targetId.trim();
    const previousType: RatingTargetType | null = this.targetTypeSignal();
    const previousId: string | null = this.targetIdSignal();

    this.targetTypeSignal.set(targetType);
    this.targetIdSignal.set(normalizedTargetId);
    this.setSummary(summary);
    this.messageKeySignal.set(null);

    if (previousType !== targetType || previousId !== normalizedTargetId) {
      this.userRatingSignal.set(null);
      if (summary?.rank === undefined) {
        this.loadSummary(targetType, normalizedTargetId);
      }
      this.loadUserRatingIfAuthenticated();
    }
  }

  rate(value: number): void {
    const targetType: RatingTargetType | null = this.targetTypeSignal();
    const targetId: string | null = this.targetIdSignal();

    if (!targetType || !targetId || this.savingSignal()) {
      return;
    }

    this.savingSignal.set(true);
    this.authService.ensureValidAccessToken(true).pipe(take(1)).subscribe({
      next: (token: string | null): void => {
        if (!token) {
          this.messageKeySignal.set('ratings.stars.signInMessage');
          this.savingSignal.set(false);
          this.modalService.openModal('loginModal');
          return;
        }

        const request: UserRatingUpsertRequest = {
          targetType,
          targetId,
          value
        };

        this.ratingsApiService.upsertRating(request).pipe(take(1)).subscribe({
          next: (rating: UserRating): void => {
            if (!this.isCurrentTarget(targetType, targetId)
              || rating.targetType !== targetType
              || rating.targetId !== targetId
              || rating.value !== value) {
              this.messageKeySignal.set('ratings.stars.errorMessage');
              this.savingSignal.set(false);
              return;
            }

            this.userRatingSignal.set(rating);
            this.setSummary(rating.summary);
            this.loadSummary(targetType, targetId);
            this.messageKeySignal.set(null);
            this.savingSignal.set(false);
            this.toastMessageService.add(
              'success',
              this.translateService.instant('common.success'),
              this.translateService.instant('ratings.stars.savedToast')
            );
          },
          error: (error: unknown): void => {
            console.error('Error saving rating', error);
            this.messageKeySignal.set('ratings.stars.errorMessage');
            this.savingSignal.set(false);
          }
        });
      },
      error: (error: unknown): void => {
        console.error('Error checking rating session', error);
        this.messageKeySignal.set('ratings.stars.errorMessage');
        this.savingSignal.set(false);
      }
    });
  }

  removeRating(): void {
    const targetType: RatingTargetType | null = this.targetTypeSignal();
    const targetId: string | null = this.targetIdSignal();

    if (!targetType || !targetId || !this.userRatingSignal() || this.savingSignal()) {
      return;
    }

    this.savingSignal.set(true);
    this.authService.ensureValidAccessToken(true).pipe(take(1)).subscribe({
      next: (token: string | null): void => {
        if (!token) {
          this.messageKeySignal.set('ratings.stars.signInMessage');
          this.savingSignal.set(false);
          this.modalService.openModal('loginModal');
          return;
        }

        this.ratingsApiService.deleteMyRating(targetType, targetId).pipe(take(1)).subscribe({
          next: (summary: RatingSummary): void => {
            if (!this.isCurrentTarget(targetType, targetId)
              || summary.targetType !== targetType
              || summary.targetId !== targetId) {
              this.messageKeySignal.set('ratings.stars.removeErrorMessage');
              this.savingSignal.set(false);
              return;
            }

            this.userRatingSignal.set(null);
            this.setSummary(summary);
            this.loadSummary(targetType, targetId);
            this.messageKeySignal.set(null);
            this.savingSignal.set(false);
            this.toastMessageService.add(
              'success',
              this.translateService.instant('common.success'),
              this.translateService.instant('ratings.stars.removedToast')
            );
          },
          error: (error: unknown): void => {
            console.error('Error deleting rating', error);
            this.messageKeySignal.set('ratings.stars.removeErrorMessage');
            this.savingSignal.set(false);
          }
        });
      },
      error: (error: unknown): void => {
        console.error('Error checking rating session', error);
        this.messageKeySignal.set('ratings.stars.removeErrorMessage');
        this.savingSignal.set(false);
      }
    });
  }

  private loadUserRatingIfAuthenticated(): void {
    const targetType: RatingTargetType | null = this.targetTypeSignal();
    const targetId: string | null = this.targetIdSignal();

    if (!targetType || !targetId || !this.authService.isLoggedIn()) {
      return;
    }

    this.ratingsApiService.getMyRating(targetType, targetId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (rating: UserRating | null): void => {
        if (!this.isCurrentTarget(targetType, targetId)) {
          return;
        }

        const isExpectedTarget: boolean = rating === null
          || (rating.targetType === targetType && rating.targetId === targetId);
        this.userRatingSignal.set(isExpectedTarget ? rating : null);
      },
      error: (): void => {
        if (this.isCurrentTarget(targetType, targetId)) {
          this.userRatingSignal.set(null);
        }
      }
    });
  }

  private loadSummary(targetType: RatingTargetType, targetId: string): void {
    this.ratingsApiService.getSummary(targetType, targetId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (summary: RatingSummary): void => {
        if (this.isCurrentTarget(targetType, targetId)
          && summary.targetType === targetType
          && summary.targetId === targetId) {
          this.setSummary(summary);
        }
      },
      error: (error: unknown): void => {
        console.error('Error loading rating rank', error);
      }
    });
  }

  private setSummary(summary: RatingSummary | null): void {
    this.summarySignal.set(summary);
    this.loadMethodology(summary?.methodologyVersion ?? null);
  }

  private loadMethodology(version: string | null): void {
    const normalizedVersion: string | null = normalizeMethodologyVersion(version);
    const requestKey: string = normalizedVersion ?? 'current';
    if (this.methodologyResolvedKey === requestKey || this.methodologyRequestKey === requestKey) {
      return;
    }

    this.methodologySignal.set(null);
    this.methodologyResolvedKey = null;
    const requestId: number = ++this.methodologyRequestId;
    this.methodologyRequestKey = requestKey;
    const request: Observable<RatingMethodology> = normalizedVersion
      ? this.ratingsApiService.getMethodology(normalizedVersion, anonymousHttpOptions())
      : this.ratingsApiService.getCurrentMethodology(anonymousHttpOptions());
    request
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (methodology: RatingMethodology): void => {
          if (requestId !== this.methodologyRequestId) {
            return;
          }

          this.methodologyRequestKey = null;
          if (normalizedVersion !== null && methodology.version !== normalizedVersion) {
            return;
          }

          this.methodologySignal.set(methodology);
          this.methodologyResolvedKey = requestKey;
        },
        error: (error: unknown): void => {
          if (requestId !== this.methodologyRequestId) {
            return;
          }

          console.error('Error loading rating methodology', error);
          this.methodologyRequestKey = null;
        }
      });
  }

  private isCurrentTarget(targetType: RatingTargetType, targetId: string): boolean {
    return this.targetTypeSignal() === targetType && this.targetIdSignal() === targetId;
  }
}

function normalizeMethodologyVersion(value: string | null | undefined): string | null {
  const trimmedValue: string = value?.trim() ?? '';
  return trimmedValue.length > 0 ? trimmedValue : null;
}
