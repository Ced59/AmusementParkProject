import { DestroyRef, Inject, Injectable, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable } from 'rxjs';

import {
  ParkItemRatingRanking,
  ParkItemRatingRankingsPage,
  ParkRatingRanking,
  RatingRankingsPage
} from '@app/models/ratings/rating.models';
import { anonymousHttpOptions } from '@core/http/auth/anonymous-http-options';
import { PaginationContract } from '@shared/models/contracts';
import { RANKINGS_RATINGS_PORT, RankingsRatingsPort } from './rankings-state-data.ports';

const RANKINGS_PAGE_SIZE = 20;

@Injectable()
export class RankingsStateFacade {
  private readonly loadingSignal = signal<boolean>(false);
  private readonly loadingMoreSignal = signal<boolean>(false);
  private readonly itemsSignal = signal<ParkRatingRanking[]>([]);
  private readonly parkItemsSignal = signal<ParkItemRatingRanking[]>([]);
  private readonly paginationSignal = signal<PaginationContract | null>(null);
  private readonly categorySignal = signal<string | null>(null);
  private readonly parkItemTypeSignal = signal<string | null>(null);
  private readonly searchSignal = signal<string | null>(null);

  public readonly loading: Signal<boolean> = this.loadingSignal.asReadonly();
  public readonly loadingMore: Signal<boolean> = this.loadingMoreSignal.asReadonly();
  public readonly items: Signal<ParkRatingRanking[]> = this.itemsSignal.asReadonly();
  public readonly parkItems: Signal<ParkItemRatingRanking[]> = this.parkItemsSignal.asReadonly();
  public readonly pagination: Signal<PaginationContract | null> = this.paginationSignal.asReadonly();
  public readonly hasMore: Signal<boolean> = computed(() => {
    const pagination: PaginationContract | null = this.paginationSignal();
    return Boolean(pagination && pagination.currentPage < pagination.totalPages);
  });

  constructor(
    @Inject(RANKINGS_RATINGS_PORT) private readonly ratingsApiService: RankingsRatingsPort,
    private readonly destroyRef: DestroyRef
  ) {
  }

  load(category: string | null = null, search: string | null = null, parkItemType: string | null = null): void {
    this.categorySignal.set(category);
    this.parkItemTypeSignal.set(category === 'Attraction' ? parkItemType : null);
    this.searchSignal.set(normalizeSearch(search));
    this.loadingSignal.set(true);
    this.loadingMoreSignal.set(false);
    const request: Observable<RatingRankingsPage | ParkItemRatingRankingsPage> = category
      ? this.ratingsApiService.getParkItemRankings(
        1,
        RANKINGS_PAGE_SIZE,
        category,
        this.parkItemTypeSignal(),
        this.searchSignal(),
        anonymousHttpOptions()
      )
      : this.ratingsApiService.getRankings(1, RANKINGS_PAGE_SIZE, null, this.searchSignal(), anonymousHttpOptions());
    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (page: RatingRankingsPage | ParkItemRatingRankingsPage): void => {
        if (category) {
          this.itemsSignal.set([]);
          this.parkItemsSignal.set(page.items as ParkItemRatingRanking[]);
        } else {
          this.itemsSignal.set(page.items as ParkRatingRanking[]);
          this.parkItemsSignal.set([]);
        }
        this.paginationSignal.set(page.pagination);
        this.loadingSignal.set(false);
      },
      error: (error: unknown): void => {
        console.error('Error loading rankings', error);
        this.itemsSignal.set([]);
        this.parkItemsSignal.set([]);
        this.paginationSignal.set(null);
        this.loadingSignal.set(false);
      }
    });
  }

  loadMore(): void {
    const pagination: PaginationContract | null = this.paginationSignal();
    if (!pagination || pagination.currentPage >= pagination.totalPages || this.loadingSignal() || this.loadingMoreSignal()) {
      return;
    }

    this.loadingMoreSignal.set(true);
    const category: string | null = this.categorySignal();
    const request: Observable<RatingRankingsPage | ParkItemRatingRankingsPage> = category
      ? this.ratingsApiService.getParkItemRankings(
        pagination.currentPage + 1,
        RANKINGS_PAGE_SIZE,
        category,
        this.parkItemTypeSignal(),
        this.searchSignal(),
        anonymousHttpOptions()
      )
      : this.ratingsApiService.getRankings(
        pagination.currentPage + 1,
        RANKINGS_PAGE_SIZE,
        null,
        this.searchSignal(),
        anonymousHttpOptions()
      );
    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (page: RatingRankingsPage | ParkItemRatingRankingsPage): void => {
        if (category) {
          this.parkItemsSignal.set([
            ...this.parkItemsSignal(),
            ...(page.items as ParkItemRatingRanking[])
          ]);
        } else {
          this.itemsSignal.set([
            ...this.itemsSignal(),
            ...(page.items as ParkRatingRanking[])
          ]);
        }
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
