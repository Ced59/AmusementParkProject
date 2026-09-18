import { DestroyRef, Inject, Injectable, Signal, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';

import { TripDateProposal, TripPlan, TripPlanWriteRequest } from '@app/models/trips/trip.models';
import { areTripDateInputsValid, buildConfirmedTripDates } from './trip-date-proposal.helpers';
import {
  TRIP_OPERATION_ID_PORT,
  TRIP_PLANS_DATA_PORT,
  TripOperationIdPort,
  TripPlansDataPort
} from './trip-state-data.ports';

export type TripListStatus = 'idle' | 'loading' | 'ready' | 'error';

@Injectable()
export class TripListStateFacade {
  private readonly tripsSignal = signal<TripPlan[]>([]);
  private readonly statusSignal = signal<TripListStatus>('idle');
  private readonly creatingSignal = signal<boolean>(false);
  private readonly createErrorSignal = signal<boolean>(false);
  private readonly createdTripIdSignal = signal<string | null>(null);
  private pendingCreateFingerprint: string | null = null;
  private pendingCreateIdempotencyKey: string | null = null;

  readonly trips: Signal<TripPlan[]> = this.tripsSignal.asReadonly();
  readonly status: Signal<TripListStatus> = this.statusSignal.asReadonly();
  readonly creating: Signal<boolean> = this.creatingSignal.asReadonly();
  readonly createError: Signal<boolean> = this.createErrorSignal.asReadonly();
  readonly createdTripId: Signal<string | null> = this.createdTripIdSignal.asReadonly();

  constructor(
    @Inject(TRIP_PLANS_DATA_PORT) private readonly plans: TripPlansDataPort,
    @Inject(TRIP_OPERATION_ID_PORT) private readonly operationIds: TripOperationIdPort,
    private readonly destroyRef: DestroyRef
  ) {
  }

  load(): void {
    if (this.statusSignal() === 'loading') {
      return;
    }

    this.statusSignal.set('loading');
    this.plans.listMine()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (trips: TripPlan[]): void => {
          this.tripsSignal.set(trips);
          this.statusSignal.set('ready');
        },
        error: (): void => this.statusSignal.set('error')
      });
  }

  create(title: string, startDate: string, endDate: string, destinationTimeZoneId: string): void {
    const normalizedTitle: string = title.trim();
    const normalizedTimeZoneId: string = destinationTimeZoneId.trim();
    if (!normalizedTitle
      || this.creatingSignal()
      || !areTripDateInputsValid(startDate, endDate)) {
      return;
    }
    const dateProposal: TripDateProposal = buildConfirmedTripDates(startDate, endDate);
    if (dateProposal.kind !== 'None' && !normalizedTimeZoneId) {
      return;
    }

    const request: TripPlanWriteRequest = {
      title: normalizedTitle,
      dateProposal,
      destinationTimeZoneId: dateProposal.kind === 'None' ? null : normalizedTimeZoneId
    };
    const fingerprint: string = JSON.stringify(request);
    if (this.pendingCreateFingerprint !== fingerprint || !this.pendingCreateIdempotencyKey) {
      this.pendingCreateFingerprint = fingerprint;
      this.pendingCreateIdempotencyKey = this.operationIds.create();
    }

    this.creatingSignal.set(true);
    this.createErrorSignal.set(false);
    this.plans.create(request, this.pendingCreateIdempotencyKey)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize((): void => this.creatingSignal.set(false))
      )
      .subscribe({
        next: (trip: TripPlan): void => {
          this.tripsSignal.update((trips: TripPlan[]): TripPlan[] => [trip, ...trips]);
          this.createdTripIdSignal.set(trip.tripPlanId);
          this.pendingCreateFingerprint = null;
          this.pendingCreateIdempotencyKey = null;
        },
        error: (): void => this.createErrorSignal.set(true)
      });
  }

  clearCreatedTrip(): void {
    this.createdTripIdSignal.set(null);
  }
}
