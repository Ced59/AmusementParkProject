import { Injectable, Signal, computed } from '@angular/core';

import {
  PublicHistoricalSubjectComparison,
  PublicParkHistoricalComparison
} from '@app/models/history/public-park-history.models';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';

@Injectable()
export class ParkHistoryComparisonStateFacade {
  private readonly store = new SignalScreenStateStore<PublicParkHistoricalComparison>();

  readonly state = this.store.state;
  readonly comparison = this.store.data;
  readonly presentAtBoth: Signal<PublicHistoricalSubjectComparison[]> = computed(() =>
    this.byPresence('PresentAtBoth')
  );
  readonly opened: Signal<PublicHistoricalSubjectComparison[]> = computed(() =>
    this.byPresence('Opened')
  );
  readonly closed: Signal<PublicHistoricalSubjectComparison[]> = computed(() =>
    this.byPresence('Closed')
  );
  readonly uncertain: Signal<PublicHistoricalSubjectComparison[]> = computed(() =>
    this.byPresence('Uncertain')
  );
  readonly renamed: Signal<PublicHistoricalSubjectComparison[]> = computed(() =>
    (this.comparison()?.subjects ?? []).filter(
      (subject: PublicHistoricalSubjectComparison): boolean => subject.isRenamed
    )
  );
  readonly moved: Signal<PublicHistoricalSubjectComparison[]> = computed(() =>
    (this.comparison()?.subjects ?? []).filter(
      (subject: PublicHistoricalSubjectComparison): boolean => subject.isMoved
    )
  );
  readonly changed: Signal<PublicHistoricalSubjectComparison[]> = computed(() =>
    (this.comparison()?.subjects ?? []).filter(
      (subject: PublicHistoricalSubjectComparison): boolean => subject.isRenamed || subject.isMoved
    )
  );
  readonly stable: Signal<PublicHistoricalSubjectComparison[]> = computed(() =>
    this.presentAtBoth().filter(
      (subject: PublicHistoricalSubjectComparison): boolean => !subject.isRenamed && !subject.isMoved
    )
  );

  setResolvedComparison(comparison: PublicParkHistoricalComparison | null): void {
    if (comparison) {
      this.store.setReady(comparison);
      return;
    }

    this.store.setError('history.comparison.errorMessage');
  }

  private byPresence(presenceChange: string): PublicHistoricalSubjectComparison[] {
    return (this.comparison()?.subjects ?? []).filter(
      (subject: PublicHistoricalSubjectComparison): boolean => subject.presenceChange === presenceChange
    );
  }
}
