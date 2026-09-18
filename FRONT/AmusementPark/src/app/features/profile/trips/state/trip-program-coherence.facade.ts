import { computed, DestroyRef, Inject, Injectable, Signal, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';

import {
  TripProgramCoherence,
  TripProgramCoherenceIssue,
  TripProgramCoherenceSeverity
} from '@app/models/trips/trip.models';
import {
  TRIP_PROGRAM_COHERENCE_DATA_PORT,
  TripProgramCoherenceDataPort
} from './trip-program-coherence-data.port';

@Injectable()
export class TripProgramCoherenceFacade {
  private readonly coherenceSignal = signal<TripProgramCoherence | null>(null);
  private readonly loadingSignal = signal<boolean>(false);
  private readonly errorSignal = signal<boolean>(false);

  readonly coherence: Signal<TripProgramCoherence | null> = this.coherenceSignal.asReadonly();
  readonly loading: Signal<boolean> = this.loadingSignal.asReadonly();
  readonly error: Signal<boolean> = this.errorSignal.asReadonly();
  readonly orderedIssues: Signal<TripProgramCoherenceIssue[]> = computed((): TripProgramCoherenceIssue[] =>
    [...(this.coherenceSignal()?.issues ?? [])].sort(
      (left: TripProgramCoherenceIssue, right: TripProgramCoherenceIssue): number =>
        severityOrder(right.severity) - severityOrder(left.severity)
        || (left.localDate ?? '').localeCompare(right.localDate ?? '')
        || left.code.localeCompare(right.code)
    ));

  constructor(
    @Inject(TRIP_PROGRAM_COHERENCE_DATA_PORT) private readonly data: TripProgramCoherenceDataPort,
    private readonly destroyRef: DestroyRef
  ) {
  }

  load(tripPlanId: string): void {
    const normalizedId: string = tripPlanId.trim();
    if (!normalizedId || this.loadingSignal()) {
      return;
    }

    this.loadingSignal.set(true);
    this.errorSignal.set(false);
    this.data.get(normalizedId).pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize((): void => this.loadingSignal.set(false))
    ).subscribe({
      next: (coherence: TripProgramCoherence): void => this.coherenceSignal.set(coherence),
      error: (): void => {
        this.coherenceSignal.set(null);
        this.errorSignal.set(true);
      }
    });
  }
}

function severityOrder(severity: TripProgramCoherenceSeverity): number {
  switch (severity) {
    case 'Critical': return 3;
    case 'Attention': return 2;
    case 'Information': return 1;
  }
}
