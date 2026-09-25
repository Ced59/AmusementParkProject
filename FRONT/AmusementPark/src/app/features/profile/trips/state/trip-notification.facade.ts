import { DestroyRef, Inject, Injectable, Signal, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';

import { TripNotificationState } from '@app/models/trips/trip-notification.models';
import { TRIP_NOTIFICATION_DATA_PORT, TripNotificationDataPort } from './trip-notification-data.port';

@Injectable()
export class TripNotificationFacade {
  private tripPlanId: string = '';
  private readonly stateSignal = signal<TripNotificationState | null>(null);
  private readonly loadingSignal = signal<boolean>(false);
  private readonly savingSignal = signal<boolean>(false);
  private readonly errorSignal = signal<boolean>(false);

  readonly state: Signal<TripNotificationState | null> = this.stateSignal.asReadonly();
  readonly loading: Signal<boolean> = this.loadingSignal.asReadonly();
  readonly saving: Signal<boolean> = this.savingSignal.asReadonly();
  readonly error: Signal<boolean> = this.errorSignal.asReadonly();

  constructor(
    @Inject(TRIP_NOTIFICATION_DATA_PORT) private readonly data: TripNotificationDataPort,
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
    this.errorSignal.set(false);
    this.data.get(normalizedId).pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize((): void => this.loadingSignal.set(false))
    ).subscribe({
      next: (state: TripNotificationState): void => this.stateSignal.set(state),
      error: (): void => {
        this.stateSignal.set(null);
        this.errorSignal.set(true);
      }
    });
  }

  toggle(): void {
    const state: TripNotificationState | null = this.stateSignal();
    if (!state || !this.tripPlanId || this.savingSignal()) {
      return;
    }

    this.mutate(() => this.data.setEnabled(this.tripPlanId, {
      enabled: !state.enabled,
      expectedVersion: state.version
    }));
  }

  markRead(): void {
    const state: TripNotificationState | null = this.stateSignal();
    if (!state?.enabled || state.unreadCount === 0 || !this.tripPlanId || this.savingSignal()) {
      return;
    }

    this.mutate(() => this.data.markRead(this.tripPlanId, { expectedVersion: state.version }));
  }

  refresh(): void {
    if (!this.savingSignal()) {
      this.load(this.tripPlanId);
    }
  }

  private mutate(operation: () => ReturnType<TripNotificationDataPort['get']>): void {
    this.savingSignal.set(true);
    this.errorSignal.set(false);
    operation().pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize((): void => this.savingSignal.set(false))
    ).subscribe({
      next: (state: TripNotificationState): void => this.stateSignal.set(state),
      error: (): void => {
        this.errorSignal.set(true);
        this.load(this.tripPlanId);
      }
    });
  }
}
