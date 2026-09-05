import { DestroyRef, Inject, Injectable, Signal, computed } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import {
  PassportBetaDailyMetrics,
  PassportBetaMetricsQuery,
  PassportBetaMetricsResult,
  PassportBetaRepeatUsageSignal
} from '@app/models/passport/passport-beta-metrics.models';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';
import {
  ADMIN_PASSPORT_BETA_DATA_PORT,
  AdminPassportBetaDataPort
} from './admin-passport-beta-state-data.ports';

@Injectable()
export class AdminPassportBetaFacade {
  private readonly screenStateStore = new SignalScreenStateStore<PassportBetaMetricsResult>();
  private requestGeneration = 0;

  public readonly state = this.screenStateStore.state;
  public readonly loading = this.screenStateStore.isLoading;
  public readonly metrics: Signal<PassportBetaMetricsResult | null> = computed(
    () => this.screenStateStore.data() ?? null
  );
  public readonly createdVisits: Signal<number> = computed(() => this.metrics()?.createdVisits ?? 0);
  public readonly completedVisits: Signal<number> = computed(() => this.metrics()?.completedVisits ?? 0);
  public readonly usersWithCompletedVisit: Signal<number> = computed(
    () => this.metrics()?.usersWithCompletedVisit ?? 0
  );
  public readonly usersWithSecondCompletedVisit: Signal<number> = computed(
    () => this.metrics()?.usersWithSecondCompletedVisit ?? 0
  );
  public readonly repeatUsageRatePercent: Signal<number> = computed(
    () => this.metrics()?.repeatUsageRatePercent ?? 0
  );
  public readonly repeatUsageSignal: Signal<PassportBetaRepeatUsageSignal> = computed(
    () => this.metrics()?.repeatUsageSignal ?? 'NotObserved'
  );
  public readonly daily: Signal<readonly PassportBetaDailyMetrics[]> = computed(
    () => this.metrics()?.daily ?? []
  );

  constructor(
    @Inject(ADMIN_PASSPORT_BETA_DATA_PORT) private readonly dataPort: AdminPassportBetaDataPort,
    private readonly destroyRef: DestroyRef
  ) {
  }

  load(query: PassportBetaMetricsQuery = {}): void {
    const requestGeneration = ++this.requestGeneration;
    const previousData: PassportBetaMetricsResult | undefined = this.screenStateStore.data();
    this.screenStateStore.setLoading(previousData);

    this.dataPort.getMetrics(query).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (metrics: PassportBetaMetricsResult) => {
        if (requestGeneration !== this.requestGeneration) {
          return;
        }

        this.screenStateStore.setReady(metrics);
      },
      error: (error: unknown) => {
        if (requestGeneration !== this.requestGeneration) {
          return;
        }

        console.error('Error loading passport beta metrics', error);
        this.screenStateStore.setError('admin.passportBeta.loadError', previousData);
      }
    });
  }
}
