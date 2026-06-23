import { DestroyRef, Inject, Injectable, Signal, computed } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { TechnicalStatsSnapshot } from '@app/models/admin/technical-stats/technical-stats.models';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';
import { ADMIN_TECHNICAL_STATS_DATA_PORT, AdminTechnicalStatsDataPort } from './admin-technical-stats-state-data.ports';

@Injectable()
export class AdminTechnicalStatsFacade {
  private readonly screenStateStore = new SignalScreenStateStore<TechnicalStatsSnapshot>();

  public readonly state = this.screenStateStore.state;
  public readonly loading = this.screenStateStore.isLoading;
  public readonly stats: Signal<TechnicalStatsSnapshot | null> = computed(() => this.screenStateStore.data() ?? null);
  public readonly hitRatePercent: Signal<number> = computed(() => this.stats()?.cache.hitRatePercent ?? 0);
  public readonly robotHitRatePercent: Signal<number> = computed(() => this.stats()?.cache.robotHitRatePercent ?? 0);

  constructor(
    @Inject(ADMIN_TECHNICAL_STATS_DATA_PORT) private readonly dataPort: AdminTechnicalStatsDataPort,
    private readonly destroyRef: DestroyRef
  ) {
  }

  load(): void {
    const previousData: TechnicalStatsSnapshot | undefined = this.screenStateStore.data();
    this.screenStateStore.setLoading(previousData);

    this.dataPort.getStats().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (stats: TechnicalStatsSnapshot) => {
        this.screenStateStore.setReady(stats);
      },
      error: (error: unknown) => {
        console.error('Error loading technical stats', error);
        this.screenStateStore.setError('technical-stats.load-error', previousData);
      }
    });
  }
}
