import { DestroyRef, Inject, Injectable, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { UserRatingListItem, UserRatingStats, UserRatingsPage } from '@app/models/ratings/rating.models';
import { PaginationContract } from '@shared/models/contracts';
import { PROFILE_RATINGS_PORT, ProfileRatingsPort } from './profile-ratings-state-data.ports';

const PROFILE_RATINGS_PAGE_SIZE = 10;

@Injectable()
export class ProfileRatingsStateFacade {
  private readonly loadingSignal = signal<boolean>(false);
  private readonly ratingsSignal = signal<UserRatingListItem[]>([]);
  private readonly statsSignal = signal<UserRatingStats | null>(null);
  private readonly paginationSignal = signal<PaginationContract | null>(null);

  public readonly loading: Signal<boolean> = this.loadingSignal.asReadonly();
  public readonly ratings: Signal<UserRatingListItem[]> = this.ratingsSignal.asReadonly();
  public readonly stats: Signal<UserRatingStats | null> = this.statsSignal.asReadonly();
  public readonly pagination: Signal<PaginationContract | null> = this.paginationSignal.asReadonly();
  public readonly isEmpty: Signal<boolean> = computed(() => !this.loadingSignal() && this.ratingsSignal().length === 0);

  constructor(
    @Inject(PROFILE_RATINGS_PORT) private readonly ratingsApiService: ProfileRatingsPort,
    private readonly destroyRef: DestroyRef
  ) {
  }

  load(page: number = 1): void {
    this.loadingSignal.set(true);

    this.ratingsApiService.getMyRatings(page, PROFILE_RATINGS_PAGE_SIZE).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result: UserRatingsPage): void => {
        this.ratingsSignal.set(result.items);
        this.paginationSignal.set(result.pagination);
        this.loadingSignal.set(false);
      },
      error: (error: unknown): void => {
        console.error('Error loading user ratings', error);
        this.ratingsSignal.set([]);
        this.paginationSignal.set(null);
        this.loadingSignal.set(false);
      }
    });

    this.ratingsApiService.getMyRatingStats().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (stats: UserRatingStats): void => {
        this.statsSignal.set(stats);
      },
      error: (error: unknown): void => {
        console.error('Error loading user rating stats', error);
        this.statsSignal.set(null);
      }
    });
  }
}
