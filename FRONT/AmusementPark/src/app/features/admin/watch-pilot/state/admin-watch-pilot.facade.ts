import { DestroyRef, Inject, Injectable, Signal, computed } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subscription } from 'rxjs';

import {
  WatchPilotMetricsQuery,
  WatchPilotMetricsResult
} from '@app/models/admin/watch-pilot/watch-pilot-metrics.models';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';
import {
  ADMIN_WATCH_PILOT_DATA_PORT,
  AdminWatchPilotDataPort
} from './admin-watch-pilot-data.port';

@Injectable()
export class AdminWatchPilotFacade {
  private readonly store = new SignalScreenStateStore<WatchPilotMetricsResult>();
  private generation: number = 0;
  private subscription: Subscription | null = null;

  readonly state = this.store.state;
  readonly metrics: Signal<WatchPilotMetricsResult | null> = computed(
    (): WatchPilotMetricsResult | null => this.store.data() ?? null
  );

  constructor(
    @Inject(ADMIN_WATCH_PILOT_DATA_PORT) private readonly dataPort: AdminWatchPilotDataPort,
    private readonly destroyRef: DestroyRef
  ) {
  }

  load(query: WatchPilotMetricsQuery = {}): void {
    const generation: number = ++this.generation;
    const previous: WatchPilotMetricsResult | undefined = this.store.data();
    this.store.setLoading(previous);
    this.subscription?.unsubscribe();
    this.subscription = this.dataPort.getMetrics(query)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (metrics: WatchPilotMetricsResult): void => {
          if (generation === this.generation) {
            this.store.setReady(metrics);
          }
        },
        error: (): void => {
          if (generation === this.generation) {
            this.store.setError('admin.watchPilot.loadError', previous);
          }
        }
      });
  }
}
