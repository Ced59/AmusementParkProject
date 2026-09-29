import { DestroyRef, Inject, Injectable, Signal, signal } from '@angular/core';
import { Subscription } from 'rxjs';

import { PublicLiveForecast } from '@app/models/live-data/public-live.models';
import { SsrRuntimeService } from '@core/ssr/ssr-runtime.service';
import {
  INITIAL_PUBLIC_LIVE_FORECAST_VIEW_STATE,
  PublicLiveForecastViewState
} from '../models/public-live-forecast-view-state.model';
import { PUBLIC_LIVE_DATA_PORT, PublicLiveDataPort } from './public-live-data.port';

@Injectable()
export class PublicLiveForecastFacade {
  private static readonly unavailableRetryDelayMs: number = 15 * 60 * 1000;
  private static readonly expiredResponseRetryDelayMs: number = 30 * 1000;

  private readonly stateSignal = signal<PublicLiveForecastViewState>(INITIAL_PUBLIC_LIVE_FORECAST_VIEW_STATE);
  private currentItemId: string | null = null;
  private refreshTimeoutId: number | null = null;
  private requestSubscription: Subscription | null = null;

  readonly state: Signal<PublicLiveForecastViewState> = this.stateSignal.asReadonly();

  constructor(
    @Inject(PUBLIC_LIVE_DATA_PORT) private readonly liveDataPort: PublicLiveDataPort,
    private readonly ssrRuntimeService: SsrRuntimeService,
    destroyRef: DestroyRef
  ) {
    destroyRef.onDestroy((): void => this.dispose());
  }

  load(itemId: string): void {
    if (!this.ssrRuntimeService.isBrowserRuntime()) {
      return;
    }

    this.currentItemId = itemId;
    this.loadCurrentForecast();
  }

  refresh(): void {
    if (!this.ssrRuntimeService.isBrowserRuntime() || !this.currentItemId) {
      return;
    }

    this.loadCurrentForecast();
  }

  private loadCurrentForecast(): void {
    if (!this.currentItemId) {
      return;
    }

    this.cancelScheduledRefresh();
    this.requestSubscription?.unsubscribe();
    this.stateSignal.set({ kind: 'loading', forecast: null });
    const subscription: Subscription = this.liveDataPort.getParkItemForecast(this.currentItemId).subscribe({
      next: (forecast: PublicLiveForecast) => {
        this.requestSubscription = null;
        this.stateSignal.set({ kind: 'ready', forecast });
        this.scheduleRefreshAt(forecast.forecastToUtc);
      },
      error: () => {
        this.requestSubscription = null;
        this.stateSignal.set({ kind: 'unavailable', forecast: null });
        this.scheduleRefreshAfter(PublicLiveForecastFacade.unavailableRetryDelayMs);
      }
    });
    this.requestSubscription = subscription.closed ? null : subscription;
  }

  private scheduleRefreshAt(timestampUtc: string): void {
    const timestamp: number = Date.parse(timestampUtc);
    const remainingDelay: number = timestamp - Date.now();
    const delay: number = Number.isFinite(timestamp) && remainingDelay > 0
      ? remainingDelay
      : PublicLiveForecastFacade.expiredResponseRetryDelayMs;
    this.scheduleRefreshAfter(delay);
  }

  private scheduleRefreshAfter(delay: number): void {
    this.cancelScheduledRefresh();
    this.refreshTimeoutId = window.setTimeout((): void => {
      this.refreshTimeoutId = null;
      this.loadCurrentForecast();
    }, Math.min(delay, 2_147_483_647));
  }

  private cancelScheduledRefresh(): void {
    if (this.refreshTimeoutId !== null) {
      window.clearTimeout(this.refreshTimeoutId);
      this.refreshTimeoutId = null;
    }
  }

  private dispose(): void {
    this.cancelScheduledRefresh();
    this.requestSubscription?.unsubscribe();
    this.requestSubscription = null;
  }
}
