import { DestroyRef, Inject, Injectable, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';

import { UserRating, UserRatingListItem, UserRatingStats, UserRatingUpsertRequest, UserRatingsPage } from '@app/models/ratings/rating.models';
import { ToastMessageService } from '@app/services/messages/toast-message.service';
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
  private readonly savingRatingIdsSignal = signal<ReadonlySet<string>>(new Set<string>());

  public readonly loading: Signal<boolean> = this.loadingSignal.asReadonly();
  public readonly loadingMore: Signal<boolean> = this.loadingMoreSignal.asReadonly();
  public readonly ratings: Signal<UserRatingListItem[]> = this.ratingsSignal.asReadonly();
  public readonly stats: Signal<UserRatingStats | null> = this.statsSignal.asReadonly();
  public readonly pagination: Signal<PaginationContract | null> = this.paginationSignal.asReadonly();
  public readonly savingRatingIds: Signal<ReadonlySet<string>> = this.savingRatingIdsSignal.asReadonly();
  public readonly hasMore: Signal<boolean> = computed(() => {
    const pagination: PaginationContract | null = this.paginationSignal();
    return Boolean(pagination && pagination.currentPage < pagination.totalPages && !this.searchSignal());
  });
  public readonly isEmpty: Signal<boolean> = computed(() => !this.loadingSignal() && this.ratingsSignal().length === 0);

  constructor(
    @Inject(PROFILE_RATINGS_PORT) private readonly ratingsApiService: ProfileRatingsPort,
    private readonly toastMessageService: ToastMessageService,
    private readonly translateService: TranslateService,
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

  updateRating(ratingId: string, value: number): void {
    const existingRating: UserRatingListItem | undefined = this.ratingsSignal().find((rating: UserRatingListItem): boolean => rating.id === ratingId);
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

        this.ratingsSignal.set(this.ratingsSignal().map((currentRating: UserRatingListItem): UserRatingListItem => {
          if (currentRating.id !== ratingId) {
            return currentRating;
          }

          return {
            ...currentRating,
            parkItemCategory: rating.parkItemCategory,
            parkItemType: rating.parkItemType,
            value: rating.value,
            updatedAtUtc: rating.updatedAtUtc,
            summary: rating.summary
          };
        }));
        this.refreshStats();
        this.setRatingSaving(ratingId, false);
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

  private refreshStats(): void {
    this.ratingsApiService.getMyRatingStats().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (stats: UserRatingStats): void => {
        this.statsSignal.set(stats);
      },
      error: (error: unknown): void => {
        console.error('Error refreshing user rating stats', error);
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
