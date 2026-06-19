import { DestroyRef, Inject, Injectable, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { ParkRatingRanking } from '@app/models/ratings/rating.models';
import { anonymousHttpOptions } from '@core/http/auth/anonymous-http-options';
import { PaginationContract } from '@shared/models/contracts';
import { RANKINGS_RATINGS_PORT, RankingsRatingsPort } from './rankings-state-data.ports';

const RANKINGS_PAGE_SIZE = 20;

@Injectable()
export class RankingsStateFacade {
  private readonly loadingSignal = signal<boolean>(false);
  private readonly loadingMoreSignal = signal<boolean>(false);
  private readonly itemsSignal = signal<ParkRatingRanking[]>([]);
  private readonly paginationSignal = signal<PaginationContract | null>(null);
  private readonly categorySignal = signal<string | null>(null);
  private readonly searchSignal = signal<string | null>(null);

  public readonly loading: Signal<boolean> = this.loadingSignal.asReadonly();
  public readonly loadingMore: Signal<boolean> = this.loadingMoreSignal.asReadonly();
  public readonly items: Signal<ParkRatingRanking[]> = this.itemsSignal.asReadonly();
  public readonly pagination: Signal<PaginationContract | null> = this.paginationSignal.asReadonly();
  public readonly hasMore: Signal<boolean> = computed(() => {
    const pagination: PaginationContract | null = this.paginationSignal();
    return Boolean(pagination && pagination.currentPage < pagination.totalPages && !this.searchSignal());
  });

  constructor(
    @Inject(RANKINGS_RATINGS_PORT) private readonly ratingsApiService: RankingsRatingsPort,
    private readonly destroyRef: DestroyRef
  ) {
  }

  load(category: string | null = null, search: string | null = null): void {
    this.categorySignal.set(category);
    this.searchSignal.set(normalizeSearch(search));
    this.loadingSignal.set(true);
    this.loadingMoreSignal.set(false);
    this.ratingsApiService.getRankings(1, RANKINGS_PAGE_SIZE, category, this.searchSignal(), anonymousHttpOptions()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (page): void => {
        this.itemsSignal.set(page.items);
        this.paginationSignal.set(page.pagination);
        this.loadingSignal.set(false);
      },
      error: (error: unknown): void => {
        console.error('Error loading rankings', error);
        this.itemsSignal.set([]);
        this.paginationSignal.set(null);
        this.loadingSignal.set(false);
      }
    });
  }

  loadMore(): void {
    const pagination: PaginationContract | null = this.paginationSignal();
    if (!pagination || this.searchSignal() || pagination.currentPage >= pagination.totalPages || this.loadingSignal() || this.loadingMoreSignal()) {
      return;
    }

    this.loadingMoreSignal.set(true);
    this.ratingsApiService.getRankings(
      pagination.currentPage + 1,
      RANKINGS_PAGE_SIZE,
      this.categorySignal(),
      null,
      anonymousHttpOptions()
    ).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (page): void => {
        this.itemsSignal.set([...this.itemsSignal(), ...page.items]);
        this.paginationSignal.set(page.pagination);
        this.loadingMoreSignal.set(false);
      },
      error: (error: unknown): void => {
        console.error('Error loading more rankings', error);
        this.loadingMoreSignal.set(false);
      }
    });
  }
}

function normalizeSearch(value: string | null | undefined): string | null {
  const trimmedValue: string = value?.trim() ?? '';
  return trimmedValue.length > 0 ? trimmedValue : null;
}
