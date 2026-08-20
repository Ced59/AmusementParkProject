import { DestroyRef, Inject, Injectable, Signal, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { anonymousHttpOptions } from '@core/http/auth/anonymous-http-options';
import { HomeStatsModel } from '@app/models/home/home-stats.model';
import { ABOUT_STATE_HOME_STATS_PORT, AboutStateHomeStatsPort } from './about-state-data.ports';

@Injectable()
export class AboutStateFacade {
  private readonly visibleParkCountSignal = signal<number | null>(null);

  public readonly visibleParkCount: Signal<number | null> = this.visibleParkCountSignal.asReadonly();

  constructor(
    @Inject(ABOUT_STATE_HOME_STATS_PORT) private readonly homeStatsPort: AboutStateHomeStatsPort,
    private readonly destroyRef: DestroyRef
  ) {
  }

  loadVisibleParkCount(): void {
    this.homeStatsPort.getHomeStats(anonymousHttpOptions())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (stats: HomeStatsModel): void => {
          this.visibleParkCountSignal.set(stats.parksCount);
        },
        error: (error: unknown): void => {
          console.error('Error loading the visible park count for the about page', error);
          this.visibleParkCountSignal.set(null);
        }
      });
  }
}
