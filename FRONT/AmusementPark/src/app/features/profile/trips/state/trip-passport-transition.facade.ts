import { DestroyRef, Inject, Injectable, Signal, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';

import {
  ConfirmTripPassportTransitionResult,
  TripPassportTransition,
  TripPassportTransitionDay
} from '@app/models/trips/trip-passport-transition.models';
import {
  TRIP_PASSPORT_TRANSITION_DATA_PORT,
  TripPassportTransitionDataPort
} from './trip-passport-transition-data.port';

export interface TripPassportConfirmation {
  visitId: string;
  localDate: string;
  revision: number;
}

@Injectable()
export class TripPassportTransitionFacade {
  private tripPlanId: string = '';
  private readonly transitionSignal = signal<TripPassportTransition | null>(null);
  private readonly loadingSignal = signal<boolean>(false);
  private readonly confirmingDateSignal = signal<string | null>(null);
  private readonly feedbackKeySignal = signal<string | null>(null);
  private readonly confirmationSignal = signal<TripPassportConfirmation | null>(null);

  readonly transition: Signal<TripPassportTransition | null> = this.transitionSignal.asReadonly();
  readonly loading: Signal<boolean> = this.loadingSignal.asReadonly();
  readonly confirmingDate: Signal<string | null> = this.confirmingDateSignal.asReadonly();
  readonly feedbackKey: Signal<string | null> = this.feedbackKeySignal.asReadonly();
  readonly confirmation: Signal<TripPassportConfirmation | null> = this.confirmationSignal.asReadonly();

  constructor(
    @Inject(TRIP_PASSPORT_TRANSITION_DATA_PORT)
    private readonly data: TripPassportTransitionDataPort,
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
      next: (transition: TripPassportTransition): void => this.transitionSignal.set(transition),
      error: (): void => {
        this.transitionSignal.set(null);
        this.feedbackKeySignal.set('trips.passportTransition.feedback.loadError');
      }
    });
  }

  confirm(localDate: string, parkItemIds: readonly string[]): void {
    if (!this.tripPlanId || this.confirmingDateSignal()) {
      return;
    }

    const day: TripPassportTransitionDay | undefined = this.transitionSignal()?.days.find(
      (candidate: TripPassportTransitionDay): boolean => candidate.localDate === localDate
    );
    if (!day?.canConfirm) {
      return;
    }

    const selectableIds: Set<string> = new Set(
      day.attractions.map((item): string => item.parkItemId)
    );
    const selectedIds: string[] = Array.from(new Set(parkItemIds)).filter(
      (parkItemId: string): boolean => selectableIds.has(parkItemId)
    );
    if (selectedIds.length !== new Set(parkItemIds).size) {
      return;
    }

    this.confirmingDateSignal.set(localDate);
    this.feedbackKeySignal.set(null);
    this.data.confirm(this.tripPlanId, localDate, { parkItemIds: selectedIds }).pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize((): void => this.confirmingDateSignal.set(null))
    ).subscribe({
      next: (result: ConfirmTripPassportTransitionResult): void => {
        this.transitionSignal.update((transition: TripPassportTransition | null) => transition ? {
          ...transition,
          days: transition.days.map((candidate: TripPassportTransitionDay): TripPassportTransitionDay =>
            candidate.localDate === localDate ? {
              ...candidate,
              canConfirm: false,
              canResume: false,
              existingVisitId: result.visitId,
              existingVisitStatus: 'Draft',
              attractions: []
            } : candidate)
        } : null);
        this.confirmationSignal.update((previous: TripPassportConfirmation | null) => ({
          visitId: result.visitId,
          localDate,
          revision: (previous?.revision ?? 0) + 1
        }));
      },
      error: (): void => this.feedbackKeySignal.set(
        'trips.passportTransition.feedback.confirmError'
      )
    });
  }
}
