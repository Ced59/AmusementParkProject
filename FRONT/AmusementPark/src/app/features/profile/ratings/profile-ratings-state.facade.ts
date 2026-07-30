import { DestroyRef, Inject, Injectable, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { Observable } from 'rxjs';

import {
  UserParkItemRatingRanking,
  UserParkItemRatingRankingsPage,
  UserParkRatingRanking,
  UserParkRatingRankingsPage,
  UserRating,
  UserRatingListItem,
  UserRatingStats,
  UserRatingUpsertRequest
} from '@app/models/ratings/rating.models';
import { ToastMessageService } from '@app/services/messages/toast-message.service';
import { PaginationContract } from '@shared/models/contracts';
import { PROFILE_RATINGS_PORT, ProfileRatingsPort } from './profile-ratings-state-data.ports';

const PROFILE_RATINGS_PAGE_SIZE = 10;

@Injectable()
export class ProfileRatingsStateFacade {
  private readonly loadingSignal = signal<boolean>(false);
  private readonly loadingMoreSignal = signal<boolean>(false);
  private readonly parkRankingsSignal = signal<UserParkRatingRanking[]>([]);
  private readonly parkItemRankingsSignal = signal<UserParkItemRatingRanking[]>([]);
  private readonly statsSignal = signal<UserRatingStats | null>(null);
  private readonly paginationSignal = signal<PaginationContract | null>(null);
  private readonly categorySignal = signal<string | null>(null);
  private readonly parkItemTypeSignal = signal<string | null>(null);
  private readonly searchSignal = signal<string | null>(null);
  private readonly savingRatingIdsSignal = signal<ReadonlySet<string>>(new Set<string>());

  public readonly loading: Signal<boolean> = this.loadingSignal.asReadonly();
  public readonly loadingMore: Signal<boolean> = this.loadingMoreSignal.asReadonly();
  public readonly parkRankings: Signal<UserParkRatingRanking[]> = this.parkRankingsSignal.asReadonly();
  public readonly parkItemRankings: Signal<UserParkItemRatingRanking[]> = this.parkItemRankingsSignal.asReadonly();
  public readonly stats: Signal<UserRatingStats | null> = this.statsSignal.asReadonly();
  public readonly pagination: Signal<PaginationContract | null> = this.paginationSignal.asReadonly();
  public readonly savingRatingIds: Signal<ReadonlySet<string>> = this.savingRatingIdsSignal.asReadonly();
  public readonly hasMore: Signal<boolean> = computed(() => {
    const pagination: PaginationContract | null = this.paginationSignal();
    return Boolean(pagination && pagination.currentPage < pagination.totalPages);
  });
  public readonly isEmpty: Signal<boolean> = computed(() => {
    return !this.loadingSignal()
      && this.parkRankingsSignal().length === 0
      && this.parkItemRankingsSignal().length === 0;
  });

  constructor(
    @Inject(PROFILE_RATINGS_PORT) private readonly ratingsApiService: ProfileRatingsPort,
    private readonly toastMessageService: ToastMessageService,
    private readonly translateService: TranslateService,
    private readonly destroyRef: DestroyRef
  ) {
  }

  load(
    page: number = 1,
    category: string | null = null,
    search: string | null = null,
    parkItemType: string | null = null
  ): void {
    this.categorySignal.set(category);
    this.parkItemTypeSignal.set(category === 'Attraction' ? parkItemType : null);
    this.searchSignal.set(normalizeSearch(search));
    this.loadingSignal.set(true);
    this.loadingMoreSignal.set(false);

    this.loadRankings(page, category, this.parkItemTypeSignal(), this.searchSignal()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result: UserParkRatingRankingsPage | UserParkItemRatingRankingsPage): void => {
        if (category) {
          this.parkRankingsSignal.set([]);
          this.parkItemRankingsSignal.set(result.items as UserParkItemRatingRanking[]);
        } else {
          this.parkRankingsSignal.set(result.items as UserParkRatingRanking[]);
          this.parkItemRankingsSignal.set([]);
        }
        this.paginationSignal.set(result.pagination);
        this.loadingSignal.set(false);
      },
      error: (error: unknown): void => {
        console.error('Error loading user rankings', error);
        this.parkRankingsSignal.set([]);
        this.parkItemRankingsSignal.set([]);
        this.paginationSignal.set(null);
        this.loadingSignal.set(false);
      }
    });

    this.refreshStats();
  }

  loadMore(): void {
    const pagination: PaginationContract | null = this.paginationSignal();
    if (!pagination || pagination.currentPage >= pagination.totalPages || this.loadingSignal() || this.loadingMoreSignal()) {
      return;
    }

    const category: string | null = this.categorySignal();
    this.loadingMoreSignal.set(true);
    this.loadRankings(
      pagination.currentPage + 1,
      category,
      this.parkItemTypeSignal(),
      this.searchSignal()
    ).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (result: UserParkRatingRankingsPage | UserParkItemRatingRankingsPage): void => {
        if (category) {
          this.parkItemRankingsSignal.set([
            ...this.parkItemRankingsSignal(),
            ...(result.items as UserParkItemRatingRanking[])
          ]);
        } else {
          this.parkRankingsSignal.set([
            ...this.parkRankingsSignal(),
            ...(result.items as UserParkRatingRanking[])
          ]);
        }
        this.paginationSignal.set(result.pagination);
        this.loadingMoreSignal.set(false);
      },
      error: (error: unknown): void => {
        console.error('Error loading more user rankings', error);
        this.loadingMoreSignal.set(false);
      }
    });
  }

  updateRating(ratingId: string, value: number): void {
    const existingRating: UserRatingListItem | null = this.findRating(ratingId);
    if (!existingRating || this.savingRatingIdsSignal().has(ratingId)) {
      return;
    }

    this.setRatingSaving(ratingId, true);
    const request: UserRatingUpsertRequest = {
      targetType: existingRating.targetType,
      targetId: existingRating.targetId,
      value
    };

    this.ratingsApiService.upsertRating(request).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (rating: UserRating): void => {
        if (rating.id !== ratingId || rating.targetType !== existingRating.targetType || rating.targetId !== existingRating.targetId) {
          this.showSaveError();
          this.setRatingSaving(ratingId, false);
          return;
        }

        this.setRatingSaving(ratingId, false);
        this.load(1, this.categorySignal(), this.searchSignal(), this.parkItemTypeSignal());
        this.toastMessageService.add(
          'success',
          this.translateService.instant('common.success'),
          this.translateService.instant('ratings.stars.savedToast')
        );
      },
      error: (error: unknown): void => {
        console.error('Error updating user rating', error);
        this.showSaveError();
        this.setRatingSaving(ratingId, false);
      }
    });
  }

  private loadRankings(
    page: number,
    category: string | null,
    parkItemType: string | null,
    search: string | null
  ): Observable<UserParkRatingRankingsPage | UserParkItemRatingRankingsPage> {
    return category
      ? this.ratingsApiService.getMyParkItemRankings(
        page,
        PROFILE_RATINGS_PAGE_SIZE,
        category,
        parkItemType,
        search
      )
      : this.ratingsApiService.getMyParkRankings(page, PROFILE_RATINGS_PAGE_SIZE, search);
  }

  private findRating(ratingId: string): UserRatingListItem | null {
    for (const ranking of this.parkRankingsSignal()) {
      if (ranking.parkRating?.id === ratingId) {
        return ranking.parkRating;
      }

      for (const category of ranking.categories) {
        const match: UserRatingListItem | undefined = category.items.find(
          (rating: UserRatingListItem): boolean => rating.id === ratingId
        );
        if (match) {
          return match;
        }
      }
    }

    return this.parkItemRankingsSignal().find(
      (ranking: UserParkItemRatingRanking): boolean => ranking.rating.id === ratingId
    )?.rating ?? null;
  }

  private refreshStats(): void {
    this.ratingsApiService.getMyRatingStats().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (stats: UserRatingStats): void => {
        this.statsSignal.set(stats);
      },
      error: (error: unknown): void => {
        console.error('Error refreshing user rating stats', error);
        this.statsSignal.set(null);
      }
    });
  }

  private setRatingSaving(ratingId: string, saving: boolean): void {
    const nextSavingRatingIds = new Set<string>(this.savingRatingIdsSignal());
    if (saving) {
      nextSavingRatingIds.add(ratingId);
    } else {
      nextSavingRatingIds.delete(ratingId);
    }

    this.savingRatingIdsSignal.set(nextSavingRatingIds);
  }

  private showSaveError(): void {
    this.toastMessageService.add(
      'error',
      this.translateService.instant('common.error'),
      this.translateService.instant('ratings.stars.errorMessage')
    );
  }
}

function normalizeSearch(value: string | null | undefined): string | null {
  const trimmedValue: string = value?.trim() ?? '';
  return trimmedValue.length > 0 ? trimmedValue : null;
}
