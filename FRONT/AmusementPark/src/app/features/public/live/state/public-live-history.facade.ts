import { HttpErrorResponse } from '@angular/common/http';
import { DestroyRef, Inject, Injectable, Signal, signal } from '@angular/core';
import { Subscription } from 'rxjs';

import { PublicLiveHistory } from '@app/models/live-data/public-live.models';
import { SsrRuntimeService } from '@core/ssr/ssr-runtime.service';
import {
  INITIAL_PUBLIC_LIVE_HISTORY_VIEW_STATE,
  PublicLiveHistoryViewState
} from '../models/public-live-history-view-state.model';
import { PUBLIC_LIVE_DATA_PORT, PublicLiveDataPort } from './public-live-data.port';

@Injectable()
export class PublicLiveHistoryFacade {
  private readonly stateSignal = signal<PublicLiveHistoryViewState>(INITIAL_PUBLIC_LIVE_HISTORY_VIEW_STATE);
  private currentItemId: string | null = null;
  private requestSubscription: Subscription | null = null;

  readonly state: Signal<PublicLiveHistoryViewState> = this.stateSignal.asReadonly();

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

    this.currentItemId = itemId;
    this.requestSubscription?.unsubscribe();
    this.stateSignal.set({ kind: 'loading', history: null });
    const subscription: Subscription = this.liveDataPort.getParkItemHistory(itemId).subscribe({
      next: (history: PublicLiveHistory) => {
        this.requestSubscription = null;
        this.stateSignal.set({ kind: 'ready', history });
      },
      error: (error: unknown) => {
        this.requestSubscription = null;
        this.stateSignal.set({
          kind: error instanceof HttpErrorResponse && error.status === 404 ? 'disabled' : 'error',
          history: null
        });
      }
    });
    this.requestSubscription = subscription.closed ? null : subscription;
  }

  retry(): void {
    if (this.currentItemId) {
      this.load(this.currentItemId);
    }
  }
}
