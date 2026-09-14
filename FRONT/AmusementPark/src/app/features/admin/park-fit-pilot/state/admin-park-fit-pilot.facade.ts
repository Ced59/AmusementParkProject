import { DestroyRef, Inject, Injectable, Signal, computed } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subscription } from 'rxjs';

import {
  ParkFitPilotDailyMetrics,
  ParkFitPilotMetricsQuery,
  ParkFitPilotMetricsResult,
  ParkFitPilotSignal
} from '@app/models/admin/park-fit/park-fit-pilot-metrics.models';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';
import {
  ADMIN_PARK_FIT_PILOT_STATE_DATA_PORT,
  AdminParkFitPilotStateDataPort
} from './admin-park-fit-pilot-state-data.port';

@Injectable()
export class AdminParkFitPilotFacade {
  private readonly screenStateStore = new SignalScreenStateStore<ParkFitPilotMetricsResult>();
  private requestGeneration: number = 0;
  private metricsSubscription: Subscription | null = null;

  public readonly state = this.screenStateStore.state;
  public readonly loading = this.screenStateStore.isLoading;
  public readonly metrics: Signal<ParkFitPilotMetricsResult | null> = computed(
    (): ParkFitPilotMetricsResult | null => this.screenStateStore.data() ?? null
  );
  public readonly signal: Signal<ParkFitPilotSignal> = computed(
    (): ParkFitPilotSignal => this.metrics()?.health.signal ?? 'AwaitingObservations'
  );
  public readonly daily: Signal<readonly ParkFitPilotDailyMetrics[]> = computed(
    (): readonly ParkFitPilotDailyMetrics[] => this.metrics()?.daily ?? []
  );

  constructor(
    @Inject(ADMIN_PARK_FIT_PILOT_STATE_DATA_PORT)
    private readonly dataPort: AdminParkFitPilotStateDataPort,
    private readonly destroyRef: DestroyRef
  ) {
  }

  load(query: ParkFitPilotMetricsQuery = {}): void {
    const requestGeneration: number = ++this.requestGeneration;
    const previousData: ParkFitPilotMetricsResult | undefined = this.screenStateStore.data();
    this.screenStateStore.setLoading(previousData);
    this.metricsSubscription?.unsubscribe();
    this.metricsSubscription = this.dataPort.getMetrics(query)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (metrics: ParkFitPilotMetricsResult): void => {
          if (requestGeneration === this.requestGeneration) {
            this.screenStateStore.setReady(metrics);
          }
        },
        error: (error: unknown): void => {
          if (requestGeneration !== this.requestGeneration) {
            return;
          }

          console.error('Error loading Park Fit pilot metrics', error);
          this.screenStateStore.setError('admin.parkFitPilot.loadError', previousData);
        }
      });
  }
}
