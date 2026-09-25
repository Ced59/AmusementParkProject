import { DestroyRef, Inject, Injectable, Signal, computed } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subscription } from 'rxjs';

import { TripPilotMetricsResult } from '@app/models/admin/trip-pilot/trip-pilot-metrics.models';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';
import { ADMIN_TRIP_PILOT_DATA_PORT, AdminTripPilotDataPort } from './admin-trip-pilot-data.port';

@Injectable()
export class AdminTripPilotFacade {
  private readonly store = new SignalScreenStateStore<TripPilotMetricsResult>();
  private generation: number = 0;
  private subscription: Subscription | null = null;

  readonly state = this.store.state;
  readonly metrics: Signal<TripPilotMetricsResult | null> = computed(
    (): TripPilotMetricsResult | null => this.store.data() ?? null
  );

  constructor(
    @Inject(ADMIN_TRIP_PILOT_DATA_PORT) private readonly dataPort: AdminTripPilotDataPort,
    private readonly destroyRef: DestroyRef
  ) {
  }

  load(): void {
    const generation: number = ++this.generation;
    const previous: TripPilotMetricsResult | undefined = this.store.data();
    this.store.setLoading(previous);
    this.subscription?.unsubscribe();
    this.subscription = this.dataPort.getMetrics()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (metrics: TripPilotMetricsResult): void => {
          if (generation === this.generation) {
            this.store.setReady(metrics);
          }
        },
        error: (): void => {
          if (generation === this.generation) {
            this.store.setError('admin.tripPilot.loadError', previous);
          }
        }
      });
  }
}
