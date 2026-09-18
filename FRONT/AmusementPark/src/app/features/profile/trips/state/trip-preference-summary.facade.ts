import { HttpErrorResponse } from '@angular/common/http';
import { computed, DestroyRef, Inject, Injectable, Signal, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';

import {
  SetTripItemDecisionRequest,
  TripItemDecisionStatus,
  TripItemPreferenceSummary,
  TripPreferenceCompatibility,
  TripPreferenceSummary
} from '@app/models/trips/trip.models';
import {
  TRIP_PREFERENCE_SUMMARY_DATA_PORT,
  TripPreferenceSummaryDataPort
} from './trip-preference-summary-data.port';

export interface TripDecisionDraft {
  status: TripItemDecisionStatus;
  reason: string;
}

export type TripPreferenceSummaryFilter = 'All' | TripPreferenceCompatibility;

@Injectable()
export class TripPreferenceSummaryFacade {
  private readonly summarySignal = signal<TripPreferenceSummary | null>(null);
  private readonly loadingSignal = signal<boolean>(false);
  private readonly savingItemIdSignal = signal<string | null>(null);
  private readonly feedbackKeySignal = signal<string | null>(null);
  private readonly searchSignal = signal<string>('');
  private readonly filterSignal = signal<TripPreferenceSummaryFilter>('All');
  private readonly draftsSignal = signal<Record<string, TripDecisionDraft>>({});
  private tripPlanId: string = '';

  readonly summary: Signal<TripPreferenceSummary | null> = this.summarySignal.asReadonly();
  readonly loading: Signal<boolean> = this.loadingSignal.asReadonly();
  readonly savingItemId: Signal<string | null> = this.savingItemIdSignal.asReadonly();
  readonly feedbackKey: Signal<string | null> = this.feedbackKeySignal.asReadonly();
  readonly search: Signal<string> = this.searchSignal.asReadonly();
  readonly filter: Signal<TripPreferenceSummaryFilter> = this.filterSignal.asReadonly();
  readonly visibleItems: Signal<TripItemPreferenceSummary[]> = computed((): TripItemPreferenceSummary[] => {
    const query: string = this.searchSignal().trim().toLocaleLowerCase();
    const filter: TripPreferenceSummaryFilter = this.filterSignal();
    return [...(this.summarySignal()?.items ?? [])]
      .filter((item: TripItemPreferenceSummary): boolean =>
        (filter === 'All' || item.compatibility === filter)
        && (!query
          || item.parkItemName.toLocaleLowerCase().includes(query)
          || item.parkName.toLocaleLowerCase().includes(query)))
      .sort((left: TripItemPreferenceSummary, right: TripItemPreferenceSummary): number =>
        compatibilityOrder(left.compatibility) - compatibilityOrder(right.compatibility)
        || left.parkName.localeCompare(right.parkName)
        || left.parkItemName.localeCompare(right.parkItemName));
  });

  constructor(
    @Inject(TRIP_PREFERENCE_SUMMARY_DATA_PORT) private readonly data: TripPreferenceSummaryDataPort,
    private readonly destroyRef: DestroyRef
  ) {
  }

  load(tripPlanId: string): void {
    const normalizedId: string = tripPlanId.trim();
    if (!normalizedId || this.loadingSignal()) {
      return;
    }

    this.tripPlanId = normalizedId;
    this.loadingSignal.set(true);
    this.feedbackKeySignal.set(null);
    this.data.get(normalizedId).pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize((): void => this.loadingSignal.set(false))
    ).subscribe({
      next: (summary: TripPreferenceSummary): void => {
        this.summarySignal.set(summary);
        this.draftsSignal.set({});
      },
      error: (): void => this.feedbackKeySignal.set('trips.preferenceSummary.feedback.loadError')
    });
  }

  setSearch(value: string): void {
    this.searchSignal.set(value);
  }

  setFilter(value: TripPreferenceSummaryFilter): void {
    this.filterSignal.set(value);
  }

  count(compatibility: TripPreferenceCompatibility): number {
    return this.summarySignal()?.items.filter(
      (item: TripItemPreferenceSummary): boolean => item.compatibility === compatibility
    ).length ?? 0;
  }

  draftFor(item: TripItemPreferenceSummary): TripDecisionDraft {
    return this.draftsSignal()[item.parkItemId] ?? {
      status: item.decision?.status ?? 'Review',
      reason: item.decision?.reason ?? ''
    };
  }

  setDecisionStatus(item: TripItemPreferenceSummary, status: TripItemDecisionStatus): void {
    this.updateDraft(item, { ...this.draftFor(item), status });
  }

  setDecisionReason(item: TripItemPreferenceSummary, reason: string): void {
    this.updateDraft(item, { ...this.draftFor(item), reason });
  }

  hasDraft(item: TripItemPreferenceSummary): boolean {
    return !!this.draftsSignal()[item.parkItemId];
  }

  canSave(item: TripItemPreferenceSummary): boolean {
    const draft: TripDecisionDraft = this.draftFor(item);
    const reasonLength: number = draft.reason.trim().length;
    return !!this.summarySignal()?.canDecide
      && this.savingItemIdSignal() === null
      && this.hasDraft(item)
      && reasonLength >= 3
      && reasonLength <= 500;
  }

  saveDecision(item: TripItemPreferenceSummary): void {
    const summary: TripPreferenceSummary | null = this.summarySignal();
    if (!summary || !this.canSave(item)) {
      return;
    }

    const draft: TripDecisionDraft = this.draftFor(item);
    const request: SetTripItemDecisionRequest = {
      expectedPlanVersion: summary.planVersion,
      expectedDecisionVersion: item.decision?.version ?? null,
      status: draft.status,
      reason: draft.reason.trim()
    };
    this.savingItemIdSignal.set(item.parkItemId);
    this.feedbackKeySignal.set(null);
    this.data.setDecision(this.tripPlanId, item.parkItemId, request).pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize((): void => this.savingItemIdSignal.set(null))
    ).subscribe({
      next: (updated: TripPreferenceSummary): void => {
        this.summarySignal.set(updated);
        this.draftsSignal.update((current: Record<string, TripDecisionDraft>): Record<string, TripDecisionDraft> => {
          const next: Record<string, TripDecisionDraft> = { ...current };
          delete next[item.parkItemId];
          return next;
        });
        this.feedbackKeySignal.set('trips.preferenceSummary.feedback.saved');
      },
      error: (error: HttpErrorResponse): void => this.feedbackKeySignal.set(
        error.status === 409
          ? 'trips.preferenceSummary.feedback.conflict'
          : 'trips.preferenceSummary.feedback.saveError'
      )
    });
  }

  private updateDraft(item: TripItemPreferenceSummary, draft: TripDecisionDraft): void {
    const savedStatus: TripItemDecisionStatus = item.decision?.status ?? 'Review';
    const savedReason: string = item.decision?.reason ?? '';
    this.draftsSignal.update((current: Record<string, TripDecisionDraft>): Record<string, TripDecisionDraft> => {
      const next: Record<string, TripDecisionDraft> = { ...current };
      if (draft.status === savedStatus && draft.reason === savedReason) {
        delete next[item.parkItemId];
      } else {
        next[item.parkItemId] = draft;
      }
      return next;
    });
    this.feedbackKeySignal.set(null);
  }
}

function compatibilityOrder(value: TripPreferenceCompatibility): number {
  switch (value) {
    case 'Conflict': return 0;
    case 'Mixed': return 1;
    case 'Unknown': return 2;
    case 'Consensus': return 3;
  }
}
