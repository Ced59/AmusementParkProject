import { DestroyRef, Inject, Injectable, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { UserRatingListItem, UserRatingStats, UserRatingsPage } from '@app/models/ratings/rating.models';
import { PaginationContract } from '@shared/models/contracts';
import { PROFILE_RATINGS_PORT, ProfileRatingsPort } from './profile-ratings-state-data.ports';

const PROFILE_RATINGS_PAGE_SIZE = 10;

@Injectable()
export class ProfileRatingsStateFacade {
  private readonly loadingSignal = signal<boolean>(false);
  private readonly loadingMoreSignal = signal<boolean>(false);
  private readonly ratingsSignal = signal<UserRatingListItem[]>([]);
  private readonly statsSignal = signal<UserRatingStats | null>(null);
  private readonly paginationSignal = signal<PaginationContract | null>(null);
  private readonly searchSignal = signal<string | null>(null);

  public readonly loading: Signal<boolean> = this.loadingSignal.asReadonly();
  public readonly loadingMore: Signal<boolean> = this.loadingMoreSignal.asReadonly();
  public readonly ratings: Signal<UserRatingListItem[]> = this.ratingsSignal.asReadonly();
  public readonly stats: Signal<UserRatingStats | null> = this.statsSignal.asReadonly();
  public readonly pagination: Signal<PaginationContract | null> = this.paginationSignal.asReadonly();
  public readonly hasMore: Signal<boolean> = computed(() => {
    const pagination: PaginationContract | null = this.paginationSignal();
    return Boolean(pagination && pagination.currentPage < pagination.totalPages && !this.searchSignal());
  });
  public readonly isEmpty: Signal<boolean> = computed(() => !this.loadingSignal() && this.ratingsSignal().length === 0);

  constructor(
    @Inject(PROFILE_RATINGS_PORT) private readonly ratingsApiService: ProfileRatingsPort,
    private readonly destroyRef: DestroyRef
  ) {
  }

  load(page: number = 1, search: string | null = null): void {
    this.searchSignal.set(normalizeSearch(search));
    this.loadingSignal.set(true);
    this.loadingMoreSignal.set(false);

    this.ratingsApiService.getMyRatings(page, PROFILE_RATINGS_PAGE_SIZE, this.searchSignal()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
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

  loadMore(): void {
    const pagination: PaginationContract | null = this.paginationSignal();
    if (!pagination || this.searchSignal() || pagination.currentPage >= pagination.totalPages || this.loadingSignal() || this.loadingMoreSignal()) {
      return;
    }

    this.loadingMoreSignal.set(true);
    this.ratingsApiService.getMyRatings(pagination.currentPage + 1, PROFILE_RATINGS_PAGE_SIZE, null).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result: UserRatingsPage): void => {
        this.ratingsSignal.set([...this.ratingsSignal(), ...result.items]);
        this.paginationSignal.set(result.pagination);
        this.loadingMoreSignal.set(false);
      },
      error: (error: unknown): void => {
        console.error('Error loading more user ratings', error);
        this.loadingMoreSignal.set(false);
      }
    });
  }
}

function normalizeSearch(value: string | null | undefined): string | null {
  const trimmedValue: string = value?.trim() ?? '';
  return trimmedValue.length > 0 ? trimmedValue : null;
}
