import { DestroyRef, Inject, Injectable, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { RatingSummary, RatingTargetType, UserRating, UserRatingUpsertRequest } from '@app/models/ratings/rating.models';
import { AuthService } from '@app/services/auth/auth.service';
import { ModalService } from '@app/services/modal/modal.service';
import { PUBLIC_RATING_RATINGS_PORT, PublicRatingRatingsPort } from './public-rating-state-data.ports';

@Injectable()
export class PublicRatingStateFacade {
  private readonly targetTypeSignal = signal<RatingTargetType | null>(null);
  private readonly targetIdSignal = signal<string | null>(null);
  private readonly summarySignal = signal<RatingSummary | null>(null);
  private readonly userRatingSignal = signal<UserRating | null>(null);
  private readonly savingSignal = signal<boolean>(false);
  private readonly messageKeySignal = signal<string | null>(null);

  public readonly summary: Signal<RatingSummary | null> = this.summarySignal.asReadonly();
  public readonly userRatingValue: Signal<number | null> = computed(() => this.userRatingSignal()?.value ?? null);
  public readonly saving: Signal<boolean> = this.savingSignal.asReadonly();
  public readonly messageKey: Signal<string | null> = this.messageKeySignal.asReadonly();

  constructor(
    @Inject(PUBLIC_RATING_RATINGS_PORT) private readonly ratingsApiService: PublicRatingRatingsPort,
    private readonly authService: AuthService,
    private readonly modalService: ModalService,
    private readonly destroyRef: DestroyRef
  ) {
  }

  configure(targetType: RatingTargetType, targetId: string, summary: RatingSummary | null): void {
    const normalizedTargetId: string = targetId.trim();
    const previousType: RatingTargetType | null = this.targetTypeSignal();
    const previousId: string | null = this.targetIdSignal();

    this.targetTypeSignal.set(targetType);
    this.targetIdSignal.set(normalizedTargetId);
    this.summarySignal.set(summary);
    this.messageKeySignal.set(null);

    if (previousType !== targetType || previousId !== normalizedTargetId) {
      this.userRatingSignal.set(null);
      this.loadUserRatingIfAuthenticated();
    }
  }

  rate(value: number): void {
    const targetType: RatingTargetType | null = this.targetTypeSignal();
    const targetId: string | null = this.targetIdSignal();

    if (!targetType || !targetId || this.savingSignal()) {
      return;
    }

    this.authService.ensureValidAccessToken(true).pipe(takeUntilDestroyed(this.destroyRef)).subscribe((token: string | null): void => {
      if (!token) {
        this.messageKeySignal.set('ratings.stars.signInMessage');
        this.modalService.openModal('loginModal');
        return;
      }

      const request: UserRatingUpsertRequest = {
        targetType,
        targetId,
        value
      };

      this.savingSignal.set(true);
      this.ratingsApiService.upsertRating(request).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: (rating: UserRating): void => {
          this.userRatingSignal.set(rating);
          this.summarySignal.set(rating.summary);
          this.messageKeySignal.set('ratings.stars.savedMessage');
          this.savingSignal.set(false);
        },
        error: (error: unknown): void => {
          console.error('Error saving rating', error);
          this.messageKeySignal.set('ratings.stars.errorMessage');
          this.savingSignal.set(false);
        }
      });
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
        this.userRatingSignal.set(rating);
      },
      error: (): void => {
        this.userRatingSignal.set(null);
      }
    });
  }
}
