import { DOCUMENT } from '@angular/common';
import { DestroyRef, Inject, Injectable, Signal, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable, Subscription, forkJoin, map } from 'rxjs';

import { PublicLiveTarget, PublicParkLiveItems } from '@app/models/live-data/public-live.models';
import { SsrRuntimeService } from '@core/ssr/ssr-runtime.service';
import {
  INITIAL_PUBLIC_LIVE_VIEW_STATE,
  PublicLiveDisplayMode,
  PublicLiveViewState
} from '../models/public-live-view-state.model';
import { PUBLIC_LIVE_DATA_PORT, PublicLiveDataPort } from './public-live-data.port';
import { resolvePublicLiveRefreshDelay } from './public-live-refresh.policy';

interface PublicLiveLoadResult {
  readonly target: PublicLiveTarget;
  readonly items: readonly PublicLiveTarget[];
}

@Injectable()
export class PublicLiveStateFacade {
  private readonly stateSignal = signal<PublicLiveViewState>(INITIAL_PUBLIC_LIVE_VIEW_STATE);
  private currentMode: PublicLiveDisplayMode | null = null;
  private currentTargetId: string | null = null;
  private refreshTimeoutId: number | null = null;
  private requestSubscription: Subscription | null = null;

  readonly state: Signal<PublicLiveViewState> = this.stateSignal.asReadonly();

  constructor(
    @Inject(PUBLIC_LIVE_DATA_PORT) private readonly liveDataPort: PublicLiveDataPort,
    @Inject(DOCUMENT) private readonly document: Document,
    private readonly ssrRuntimeService: SsrRuntimeService,
    destroyRef: DestroyRef
  ) {
    if (this.ssrRuntimeService.isBrowserRuntime()) {
      this.document.addEventListener('visibilitychange', this.handleVisibilityChange);
      window.addEventListener('online', this.handleOnline);
      window.addEventListener('offline', this.handleOffline);
    }

    destroyRef.onDestroy((): void => this.dispose());
  }

  watchPark(parkId: string): void {
    this.watch('park', parkId);
  }

  watchParkItem(itemId: string): void {
    this.watch('item', itemId);
  }

  refresh(): void {
    this.loadCurrentTarget();
  }

  private watch(mode: PublicLiveDisplayMode, targetId: string): void {
    if (!this.ssrRuntimeService.isBrowserRuntime()) {
      return;
    }

    this.cancelScheduledRefresh();
    this.requestSubscription?.unsubscribe();
    this.requestSubscription = null;
    this.currentMode = mode;
    this.currentTargetId = targetId;
    this.stateSignal.set({
      ...INITIAL_PUBLIC_LIVE_VIEW_STATE,
      kind: 'loading',
      mode,
      isOnline: navigator.onLine
    });
    this.loadCurrentTarget();
  }

  private loadCurrentTarget(): void {
    if (
      !this.ssrRuntimeService.isBrowserRuntime()
      || !this.currentMode
      || !this.currentTargetId
      || this.requestSubscription
      || this.stateSignal().kind === 'disabled'
      || this.document.hidden
      || !navigator.onLine
    ) {
      return;
    }

    const previousState: PublicLiveViewState = this.stateSignal();
    this.stateSignal.update((state: PublicLiveViewState) => ({
      ...state,
      kind: state.target ? 'ready' : 'loading',
      isRefreshing: state.target !== null,
      isOnline: true,
      refreshFailed: false
    }));

    const request: Observable<PublicLiveLoadResult> = this.currentMode === 'park'
      ? forkJoin({
        target: this.liveDataPort.getPark(this.currentTargetId),
        collection: this.liveDataPort.getParkItems(this.currentTargetId)
      }).pipe(map((result: { target: PublicLiveTarget; collection: PublicParkLiveItems }) => ({
        target: result.target,
        items: result.collection.items
      })))
      : this.liveDataPort.getParkItem(this.currentTargetId).pipe(
        map((target: PublicLiveTarget) => ({ target, items: [] }))
      );

    const subscription: Subscription = request.subscribe({
      next: (result: PublicLiveLoadResult) => {
        this.requestSubscription = null;
        this.stateSignal.set({
          kind: 'ready',
          mode: this.currentMode,
          target: result.target,
          items: result.items,
          isRefreshing: false,
          isOnline: true,
          refreshFailed: false,
          lastSuccessfulRefreshUtc: new Date().toISOString()
        });
        this.scheduleNextRefresh(result.target, result.items);
      },
      error: (error: unknown) => {
        this.requestSubscription = null;
        const endpointDisabled: boolean = error instanceof HttpErrorResponse && error.status === 404;
        this.stateSignal.set(endpointDisabled
          ? {
            ...INITIAL_PUBLIC_LIVE_VIEW_STATE,
            kind: 'disabled',
            mode: this.currentMode,
            isOnline: navigator.onLine,
            refreshFailed: false
          }
          : {
            ...previousState,
            kind: previousState.target ? 'ready' : 'error',
            mode: this.currentMode,
            isRefreshing: false,
            isOnline: navigator.onLine,
            refreshFailed: true
          });
        if (endpointDisabled) {
          return;
        }

        this.scheduleNextRefresh(previousState.target, previousState.items);
      }
    });
    this.requestSubscription = subscription.closed ? null : subscription;
  }

  private scheduleNextRefresh(target: PublicLiveTarget | null, items: readonly PublicLiveTarget[]): void {
    this.cancelScheduledRefresh();
    if (this.document.hidden || !navigator.onLine) {
      return;
    }

    const delay: number = resolvePublicLiveRefreshDelay(target, items);
    this.refreshTimeoutId = window.setTimeout((): void => {
      this.refreshTimeoutId = null;
      this.loadCurrentTarget();
    }, delay);
  }

  private readonly handleVisibilityChange = (): void => {
    if (this.document.hidden) {
      this.cancelScheduledRefresh();
      return;
    }

    this.loadCurrentTarget();
  };

  private readonly handleOnline = (): void => {
    this.stateSignal.update((state: PublicLiveViewState) => ({ ...state, isOnline: true }));
    this.loadCurrentTarget();
  };

  private readonly handleOffline = (): void => {
    this.cancelScheduledRefresh();
    this.stateSignal.update((state: PublicLiveViewState) => ({
      ...state,
      isOnline: false,
      isRefreshing: false
    }));
  };

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
    if (this.ssrRuntimeService.isBrowserRuntime()) {
      this.document.removeEventListener('visibilitychange', this.handleVisibilityChange);
      window.removeEventListener('online', this.handleOnline);
      window.removeEventListener('offline', this.handleOffline);
    }
  }
}
