import { DestroyRef, Inject, Injectable, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable } from 'rxjs';

import {
  SharedUserRankingProfile,
  UserParkItemRatingRanking,
  UserParkItemRatingRankingsPage,
  UserParkRatingRanking,
  UserParkRatingRankingsPage
} from '@app/models/ratings/rating.models';
import { PaginationContract } from '@shared/models/contracts';
import { SHARED_USER_RANKINGS_PORT, SharedUserRankingsPort } from './shared-user-rankings-state-data.ports';

const SHARED_RANKINGS_PAGE_SIZE = 10;

@Injectable()
export class SharedUserRankingsStateFacade {
  private readonly profileSignal = signal<SharedUserRankingProfile | null>(null);
  private readonly parkRankingsSignal = signal<UserParkRatingRanking[]>([]);
  private readonly parkItemRankingsSignal = signal<UserParkItemRatingRanking[]>([]);
  private readonly paginationSignal = signal<PaginationContract | null>(null);
  private readonly loadingSignal = signal<boolean>(false);
  private readonly loadingMoreSignal = signal<boolean>(false);
  private readonly notFoundSignal = signal<boolean>(false);
  private readonly errorSignal = signal<boolean>(false);
  private readonly shareIdSignal = signal<string>('');
  private readonly categorySignal = signal<string | null>(null);
  private readonly typeSignal = signal<string | null>(null);
  private readonly searchSignal = signal<string | null>(null);
  private rankingRequestSequence: number = 0;

  public readonly profile: Signal<SharedUserRankingProfile | null> = this.profileSignal.asReadonly();
  public readonly parkRankings: Signal<UserParkRatingRanking[]> = this.parkRankingsSignal.asReadonly();
  public readonly parkItemRankings: Signal<UserParkItemRatingRanking[]> = this.parkItemRankingsSignal.asReadonly();
  public readonly pagination: Signal<PaginationContract | null> = this.paginationSignal.asReadonly();
  public readonly loading: Signal<boolean> = this.loadingSignal.asReadonly();
  public readonly loadingMore: Signal<boolean> = this.loadingMoreSignal.asReadonly();
  public readonly notFound: Signal<boolean> = this.notFoundSignal.asReadonly();
  public readonly error: Signal<boolean> = this.errorSignal.asReadonly();
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
    @Inject(SHARED_USER_RANKINGS_PORT) private readonly ratingsPort: SharedUserRankingsPort,
    private readonly destroyRef: DestroyRef
  ) {
  }

  loadProfile(
    shareId: string,
    category: string | null = null,
    search: string | null = null,
    type: string | null = null,
  ): void {
    this.shareIdSignal.set(shareId);
    this.loadingSignal.set(true);
    this.errorSignal.set(false);
    this.notFoundSignal.set(false);

    this.ratingsPort.getSharedProfile(shareId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (profile: SharedUserRankingProfile): void => {
        this.profileSignal.set(profile);
        this.loadRankings(1, category, normalizeSearch(search), type);
      },
      error: (error: { status?: number }): void => {
        this.profileSignal.set(null);
        this.loadingSignal.set(false);
        this.notFoundSignal.set(error?.status === 404);
        this.errorSignal.set(error?.status !== 404);
      }
    });
  }

  load(category: string | null, search: string | null, type: string | null): void {
    this.loadingSignal.set(true);
    this.errorSignal.set(false);
    this.loadRankings(1, category, normalizeSearch(search), type);
  }

  loadMore(): void {
    const pagination: PaginationContract | null = this.paginationSignal();
    if (!pagination || !this.hasMore() || this.loadingSignal() || this.loadingMoreSignal()) {
      return;
    }

    this.loadingMoreSignal.set(true);
    const requestSequence: number = this.rankingRequestSequence;
    const requestedCategory: string | null = this.categorySignal();
    this.requestRankings(
      pagination.currentPage + 1,
      requestedCategory,
      this.searchSignal(),
      this.typeSignal()
    ).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (page: UserParkRatingRankingsPage | UserParkItemRatingRankingsPage): void => {
        if (requestSequence !== this.rankingRequestSequence) {
          return;
        }

        if (requestedCategory) {
          this.parkItemRankingsSignal.set([
            ...this.parkItemRankingsSignal(),
            ...(page.items as UserParkItemRatingRanking[])
          ]);
        } else {
          this.parkRankingsSignal.set([
            ...this.parkRankingsSignal(),
            ...(page.items as UserParkRatingRanking[])
          ]);
        }
        this.paginationSignal.set(page.pagination);
        this.loadingMoreSignal.set(false);
      },
      error: (): void => {
        if (requestSequence !== this.rankingRequestSequence) {
          return;
        }

        this.loadingMoreSignal.set(false);
        this.errorSignal.set(true);
      }
    });
  }

  private loadRankings(page: number, category: string | null, search: string | null, type: string | null): void {
    const requestSequence: number = ++this.rankingRequestSequence;
    this.categorySignal.set(category);
    this.searchSignal.set(search);
    this.typeSignal.set(category === 'Attraction' ? type : null);
    this.loadingMoreSignal.set(false);

    this.requestRankings(page, category, search, this.typeSignal())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result: UserParkRatingRankingsPage | UserParkItemRatingRankingsPage): void => {
          if (requestSequence !== this.rankingRequestSequence) {
            return;
          }

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
        error: (error: { status?: number }): void => {
          if (requestSequence !== this.rankingRequestSequence) {
            return;
          }

          this.parkRankingsSignal.set([]);
          this.parkItemRankingsSignal.set([]);
          this.paginationSignal.set(null);
          this.loadingSignal.set(false);
          this.notFoundSignal.set(error?.status === 404);
          this.errorSignal.set(error?.status !== 404);
        }
      });
  }

  private requestRankings(
    page: number,
    category: string | null,
    search: string | null,
    type: string | null
  ): Observable<UserParkRatingRankingsPage | UserParkItemRatingRankingsPage> {
    const shareId: string = this.shareIdSignal();
    return category
      ? this.ratingsPort.getSharedParkItemRankings(
        shareId,
        page,
        SHARED_RANKINGS_PAGE_SIZE,
        category,
        type,
        search
      )
      : this.ratingsPort.getSharedParkRankings(shareId, page, SHARED_RANKINGS_PAGE_SIZE, search);
  }
}

function normalizeSearch(value: string | null | undefined): string | null {
  const normalizedValue: string = value?.trim() ?? '';
  return normalizedValue.length > 0 ? normalizedValue : null;
}
