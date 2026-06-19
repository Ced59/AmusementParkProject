import { DestroyRef, Inject, Injectable, Signal, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { RatingRankingItem, RatingTargetType } from '@app/models/ratings/rating.models';
import { anonymousHttpOptions } from '@core/http/auth/anonymous-http-options';
import { RANKINGS_RATINGS_PORT, RankingsRatingsPort } from './rankings-state-data.ports';

const RANKINGS_PAGE_SIZE = 20;

@Injectable()
export class RankingsStateFacade {
  private readonly loadingSignal = signal<boolean>(false);
  private readonly itemsSignal = signal<RatingRankingItem[]>([]);

  public readonly loading: Signal<boolean> = this.loadingSignal.asReadonly();
  public readonly items: Signal<RatingRankingItem[]> = this.itemsSignal.asReadonly();

  constructor(
    @Inject(RANKINGS_RATINGS_PORT) private readonly ratingsApiService: RankingsRatingsPort,
    private readonly destroyRef: DestroyRef
  ) {
  }

  load(targetType: RatingTargetType | null = null, category: string | null = null): void {
    this.loadingSignal.set(true);
    this.ratingsApiService.getRankings(1, RANKINGS_PAGE_SIZE, targetType, category, anonymousHttpOptions()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (page): void => {
        this.itemsSignal.set(page.items);
        this.loadingSignal.set(false);
      },
      error: (error: unknown): void => {
        console.error('Error loading rankings', error);
        this.itemsSignal.set([]);
        this.loadingSignal.set(false);
      }
    });
  }
}
