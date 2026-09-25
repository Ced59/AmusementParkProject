import { DestroyRef, Inject, Injectable, Signal, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';

import { TripActivityEntry, TripActivityPage } from '@app/models/trips/trip-activity.models';
import { TRIP_ACTIVITY_DATA_PORT, TripActivityDataPort } from './trip-activity-data.port';

@Injectable()
export class TripActivityFacade {
  private tripPlanId: string = '';
  private readonly tripTitleSignal = signal<string>('');
  private readonly entriesSignal = signal<TripActivityEntry[]>([]);
  private readonly nextBeforeSequenceSignal = signal<number | null>(null);
  private readonly loadingSignal = signal<boolean>(false);
  private readonly loadingMoreSignal = signal<boolean>(false);
  private readonly errorSignal = signal<boolean>(false);

  readonly tripTitle: Signal<string> = this.tripTitleSignal.asReadonly();
  readonly entries: Signal<TripActivityEntry[]> = this.entriesSignal.asReadonly();
  readonly nextBeforeSequence: Signal<number | null> = this.nextBeforeSequenceSignal.asReadonly();
  readonly loading: Signal<boolean> = this.loadingSignal.asReadonly();
  readonly loadingMore: Signal<boolean> = this.loadingMoreSignal.asReadonly();
  readonly error: Signal<boolean> = this.errorSignal.asReadonly();

  constructor(
    @Inject(TRIP_ACTIVITY_DATA_PORT) private readonly data: TripActivityDataPort,
    private readonly destroyRef: DestroyRef
  ) {
  }

  load(tripPlanId: string): void {
    const normalizedId: string = tripPlanId.trim();
    if (!normalizedId || this.loadingSignal() || this.loadingMoreSignal()) {
      return;
    }

    this.tripPlanId = normalizedId;
    this.loadingSignal.set(true);
    this.errorSignal.set(false);
    this.data.get(normalizedId).pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize((): void => this.loadingSignal.set(false))
    ).subscribe({
      next: (page: TripActivityPage): void => this.replace(page),
      error: (): void => {
        this.entriesSignal.set([]);
        this.nextBeforeSequenceSignal.set(null);
        this.errorSignal.set(true);
      }
    });
  }

  refresh(): void {
    this.load(this.tripPlanId);
  }

  loadMore(): void {
    const cursor: number | null = this.nextBeforeSequenceSignal();
    if (!this.tripPlanId || cursor === null || this.loadingSignal() || this.loadingMoreSignal()) {
      return;
    }

    this.loadingMoreSignal.set(true);
    this.errorSignal.set(false);
    this.data.get(this.tripPlanId, cursor).pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize((): void => this.loadingMoreSignal.set(false))
    ).subscribe({
      next: (page: TripActivityPage): void => {
        const knownSequences: Set<number> = new Set(
          this.entriesSignal().map((entry: TripActivityEntry): number => entry.sequence)
        );
        this.entriesSignal.update((entries: TripActivityEntry[]): TripActivityEntry[] => [
          ...entries,
          ...page.entries.filter((entry: TripActivityEntry): boolean => !knownSequences.has(entry.sequence))
        ]);
        this.nextBeforeSequenceSignal.set(page.nextBeforeSequence);
      },
      error: (): void => this.errorSignal.set(true)
    });
  }

  private replace(page: TripActivityPage): void {
    this.tripTitleSignal.set(page.tripTitle);
    this.entriesSignal.set(page.entries);
    this.nextBeforeSequenceSignal.set(page.nextBeforeSequence);
  }
}
