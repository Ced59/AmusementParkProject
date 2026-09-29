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
  private readonly stateSignal = signal<PublicLiveForecastViewState>(INITIAL_PUBLIC_LIVE_FORECAST_VIEW_STATE);
  private requestSubscription: Subscription | null = null;

  readonly state: Signal<PublicLiveForecastViewState> = this.stateSignal.asReadonly();

  constructor(
    @Inject(PUBLIC_LIVE_DATA_PORT) private readonly liveDataPort: PublicLiveDataPort,
    private readonly ssrRuntimeService: SsrRuntimeService,
    destroyRef: DestroyRef
  ) {
    destroyRef.onDestroy((): void => this.requestSubscription?.unsubscribe());
  }

  load(itemId: string): void {
    if (!this.ssrRuntimeService.isBrowserRuntime()) {
      return;
    }

    this.requestSubscription?.unsubscribe();
    this.stateSignal.set({ kind: 'loading', forecast: null });
    const subscription: Subscription = this.liveDataPort.getParkItemForecast(itemId).subscribe({
      next: (forecast: PublicLiveForecast) => {
        this.requestSubscription = null;
        this.stateSignal.set({ kind: 'ready', forecast });
      },
      error: () => {
        this.requestSubscription = null;
        this.stateSignal.set({ kind: 'unavailable', forecast: null });
      }
    });
    this.requestSubscription = subscription.closed ? null : subscription;
  }
}
