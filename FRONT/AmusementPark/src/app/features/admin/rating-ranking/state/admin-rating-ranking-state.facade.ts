import { DestroyRef, Inject, Injectable, Signal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import {
  RatingRankingAdministration,
  RatingRankingPolicyCandidateRequest,
  RatingRankingPolicyImpact,
  RatingRankingRebuildRequestResult
} from '@app/models/admin/ratings/rating-ranking-administration.models';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';
import {
  ADMIN_RATING_RANKING_STATE_PORT,
  AdminRatingRankingStatePort
} from './admin-rating-ranking-state-data.ports';

@Injectable()
export class AdminRatingRankingStateFacade {
  private readonly screenStateStore = new SignalScreenStateStore<RatingRankingAdministration>();
  private readonly previewingSignal = signal<boolean>(false);
  private readonly rebuildingSignal = signal<boolean>(false);
  private readonly impactSignal = signal<RatingRankingPolicyImpact | null>(null);
  private readonly rebuildResultSignal = signal<RatingRankingRebuildRequestResult | null>(null);
  private readonly actionMessageKeySignal = signal<string | null>(null);

  public readonly state = this.screenStateStore.state;
  public readonly loading = this.screenStateStore.isLoading;
  public readonly dashboard: Signal<RatingRankingAdministration | null> = computed(
    () => this.screenStateStore.data() ?? null);
  public readonly previewing = this.previewingSignal.asReadonly();
  public readonly rebuilding = this.rebuildingSignal.asReadonly();
  public readonly impact = this.impactSignal.asReadonly();
  public readonly rebuildResult = this.rebuildResultSignal.asReadonly();
  public readonly actionMessageKey = this.actionMessageKeySignal.asReadonly();

  constructor(
    @Inject(ADMIN_RATING_RANKING_STATE_PORT) private readonly apiService: AdminRatingRankingStatePort,
    private readonly destroyRef: DestroyRef
  ) {
  }

  load(): void {
    const previousData: RatingRankingAdministration | undefined = this.screenStateStore.data();
    this.screenStateStore.setLoading(previousData);
    this.apiService.getDashboard()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (dashboard: RatingRankingAdministration): void => {
          this.screenStateStore.setReady(dashboard);
        },
        error: (error: unknown): void => {
          console.error('Error loading rating ranking administration', error);
          this.screenStateStore.setError('admin.ratingRanking.loadError', previousData);
        }
      });
  }

  preview(request: RatingRankingPolicyCandidateRequest): void {
    this.previewingSignal.set(true);
    this.actionMessageKeySignal.set(null);
    this.apiService.previewImpact(request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (impact: RatingRankingPolicyImpact): void => {
          this.impactSignal.set(impact);
          this.previewingSignal.set(false);
          this.actionMessageKeySignal.set('admin.ratingRanking.preview.success');
        },
        error: (error: unknown): void => {
          console.error('Error previewing rating ranking policy', error);
          this.previewingSignal.set(false);
          this.actionMessageKeySignal.set('admin.ratingRanking.preview.error');
        }
      });
  }

  rebuild(): void {
    this.rebuildingSignal.set(true);
    this.actionMessageKeySignal.set(null);
    this.apiService.rebuild()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result: RatingRankingRebuildRequestResult): void => {
          this.rebuildResultSignal.set(result);
          this.rebuildingSignal.set(false);
          this.actionMessageKeySignal.set('admin.ratingRanking.rebuild.success');
          this.load();
        },
        error: (error: unknown): void => {
          console.error('Error rebuilding rating ranking snapshots', error);
          this.rebuildingSignal.set(false);
          this.actionMessageKeySignal.set('admin.ratingRanking.rebuild.error');
        }
      });
  }
}
