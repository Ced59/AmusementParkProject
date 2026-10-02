import { DOCUMENT } from '@angular/common';
import { DestroyRef, Inject, Injectable, Signal, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable, Subscription, forkJoin, map, of, switchMap } from 'rxjs';

import { PublicLiveTarget, PublicParkLiveItems } from '@app/models/live-data/public-live.models';
import { SsrRuntimeService } from '@core/ssr/ssr-runtime.service';
import { extractApiProblemDetails } from '@shared/utils/security/error-display.helpers';
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
  private expirationTimeoutId: number | null = null;
  private ageTimeoutId: number | null = null;
  private requestSubscription: Subscription | null = null;
  private isTemporarilySuspended: boolean = false;

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
    this.cancelScheduledExpiration();
    this.cancelScheduledAgeUpdate();
    this.requestSubscription?.unsubscribe();
    this.requestSubscription = null;
    this.isTemporarilySuspended = false;
    this.currentMode = mode;
    this.currentTargetId = targetId;
    this.stateSignal.set({
      ...INITIAL_PUBLIC_LIVE_VIEW_STATE,
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
      || (this.stateSignal().kind === 'disabled' && !this.isTemporarilySuspended)
      || this.document.hidden
      || !navigator.onLine
    ) {
      return;
    }

    const targetId: string = this.currentTargetId;
    const mode: PublicLiveDisplayMode = this.currentMode;
    const request: Observable<PublicLiveLoadResult | null> = this.liveDataPort.isPublicReadEnabled().pipe(
      switchMap((enabled: boolean) => {
        if (!enabled) {
          return of(null);
        }

        this.stateSignal.update((state: PublicLiveViewState) => ({
          ...state,
          kind: state.target ? 'ready' : 'loading',
          isRefreshing: state.target !== null,
          isOnline: navigator.onLine,
          refreshFailed: false
        }));

        return mode === 'park'
          ? forkJoin({
            target: this.liveDataPort.getPark(targetId),
            collection: this.liveDataPort.getParkItems(targetId)
          }).pipe(map((result: { target: PublicLiveTarget; collection: PublicParkLiveItems }) => ({
            target: result.target,
            items: result.collection.items
          })))
          : this.liveDataPort.getParkItem(targetId).pipe(
            map((target: PublicLiveTarget) => ({ target, items: [] }))
          );
      })
    );

    const subscription: Subscription = request.subscribe({
      next: (result: PublicLiveLoadResult | null) => {
        this.requestSubscription = null;
        this.isTemporarilySuspended = false;
        if (result === null) {
          this.cancelScheduledRefresh();
          this.cancelScheduledExpiration();
          this.cancelScheduledAgeUpdate();
          this.stateSignal.set({ ...INITIAL_PUBLIC_LIVE_VIEW_STATE, kind: 'disabled', mode, isOnline: navigator.onLine });
          return;
        }
        const now: number = Date.now();
        const target: PublicLiveTarget = this.ageTarget(result.target, now);
        const items: readonly PublicLiveTarget[] = result.items.map(
          (item: PublicLiveTarget) => this.ageTarget(item, now)
        );
        this.stateSignal.set({
          kind: 'ready',
          mode: this.currentMode,
          target,
          items,
          isRefreshing: false,
          isOnline: navigator.onLine,
          refreshFailed: false,
          lastSuccessfulRefreshUtc: new Date().toISOString()
        });
        this.scheduleNextExpiration(target, items);
        this.scheduleNextAgeUpdate(target, items);
        this.scheduleNextRefresh(target, items);
      },
      error: (error: unknown) => {
        this.requestSubscription = null;
        const endpointDisabled: boolean = error instanceof HttpErrorResponse && error.status === 404;
        const endpointTemporarilySuspended: boolean = endpointDisabled
          && extractApiProblemDetails(error)?.errorCode === 'live-data.public-read.temporarily-suspended';
        this.isTemporarilySuspended = endpointTemporarilySuspended;
        const fallbackState: PublicLiveViewState = this.stateSignal();
        this.stateSignal.set(endpointDisabled
          ? {
            ...INITIAL_PUBLIC_LIVE_VIEW_STATE,
            kind: 'disabled',
            mode: this.currentMode,
            isOnline: navigator.onLine,
            refreshFailed: false
          }
          : {
            ...fallbackState,
            kind: fallbackState.target ? 'ready' : 'error',
            mode: this.currentMode,
            isRefreshing: false,
            isOnline: navigator.onLine,
            refreshFailed: true
          });
        if (endpointDisabled) {
          this.cancelScheduledExpiration();
          this.cancelScheduledAgeUpdate();
          if (endpointTemporarilySuspended) {
            this.scheduleNextRefresh(null, []);
          }
          return;
        }

        this.scheduleNextRefresh(fallbackState.target, fallbackState.items);
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

  private scheduleNextExpiration(
    target: PublicLiveTarget | null,
    items: readonly PublicLiveTarget[]
  ): void {
    this.cancelScheduledExpiration();
    const now: number = Date.now();
    const nextExpiration: number | null = [target, ...items]
      .filter((candidate: PublicLiveTarget | null): candidate is PublicLiveTarget =>
        candidate !== null && candidate.availability === 'Current')
      .map((candidate: PublicLiveTarget) => candidate.expiresAtUtc
        ? Date.parse(candidate.expiresAtUtc)
        : Number.NaN)
      .filter((expiresAt: number) => Number.isFinite(expiresAt))
      .reduce<number | null>((earliest: number | null, expiresAt: number) =>
        earliest === null || expiresAt < earliest ? expiresAt : earliest, null);

    if (nextExpiration === null) {
      return;
    }

    const delay: number = Math.max(0, nextExpiration - now);
    this.expirationTimeoutId = window.setTimeout((): void => {
      this.expirationTimeoutId = null;
      this.expireCurrentObservations();
    }, Math.min(delay, 2_147_483_647));
  }

  private expireCurrentObservations(): void {
    const now: number = Date.now();
    const state: PublicLiveViewState = this.stateSignal();
    const target: PublicLiveTarget | null = this.expireTarget(
      state.target ? this.ageTarget(state.target, now) : null,
      now
    );
    const items: readonly PublicLiveTarget[] = state.items.map(
      (item: PublicLiveTarget) => this.expireTarget(this.ageTarget(item, now), now) ?? item
    );
    this.stateSignal.set({ ...state, target, items });
    this.scheduleNextExpiration(target, items);
  }

  private expireTarget(target: PublicLiveTarget | null, now: number): PublicLiveTarget | null {
    if (!target || target.availability !== 'Current' || !target.expiresAtUtc) {
      return target;
    }

    const expiresAt: number = Date.parse(target.expiresAtUtc);
    if (!Number.isFinite(expiresAt) || expiresAt > now) {
      return target;
    }

    return { ...target, availability: 'Expired', freshness: 'Expired' };
  }

  private scheduleNextAgeUpdate(
    target: PublicLiveTarget | null,
    items: readonly PublicLiveTarget[]
  ): void {
    this.cancelScheduledAgeUpdate();
    if (this.document.hidden) {
      return;
    }

    const now: number = Date.now();
    const nextAgeBoundary: number | null = [target, ...items]
      .map((candidate: PublicLiveTarget | null) => candidate?.observedAtUtc
        ? Date.parse(candidate.observedAtUtc)
        : Number.NaN)
      .filter((observedAt: number) => Number.isFinite(observedAt))
      .map((observedAt: number) => {
        const elapsed: number = Math.max(0, now - observedAt);
        return now + (60_000 - (elapsed % 60_000));
      })
      .reduce<number | null>((earliest: number | null, boundary: number) =>
        earliest === null || boundary < earliest ? boundary : earliest, null);

    if (nextAgeBoundary === null) {
      return;
    }

    this.ageTimeoutId = window.setTimeout((): void => {
      this.ageTimeoutId = null;
      this.advanceObservationAges();
    }, Math.max(1, nextAgeBoundary - now));
  }

  private advanceObservationAges(): void {
    const now: number = Date.now();
    const state: PublicLiveViewState = this.stateSignal();
    const target: PublicLiveTarget | null = state.target
      ? this.ageTarget(state.target, now)
      : null;
    const items: readonly PublicLiveTarget[] = state.items.map(
      (item: PublicLiveTarget) => this.ageTarget(item, now)
    );
    this.stateSignal.set({ ...state, target, items });
    this.scheduleNextAgeUpdate(target, items);
  }

  private ageTarget(target: PublicLiveTarget, now: number): PublicLiveTarget {
    if (!target.observedAtUtc) {
      return target;
    }

    const observedAt: number = Date.parse(target.observedAtUtc);
    if (!Number.isFinite(observedAt)) {
      return target;
    }

    const ageSeconds: number = Math.max(
      target.ageSeconds ?? 0,
      Math.floor(Math.max(0, now - observedAt) / 1000)
    );
    return ageSeconds === target.ageSeconds ? target : { ...target, ageSeconds };
  }

  private readonly handleVisibilityChange = (): void => {
    if (this.document.hidden) {
      this.cancelScheduledRefresh();
      this.cancelScheduledAgeUpdate();
      return;
    }

    this.advanceObservationAges();
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

  private cancelScheduledExpiration(): void {
    if (this.expirationTimeoutId !== null) {
      window.clearTimeout(this.expirationTimeoutId);
      this.expirationTimeoutId = null;
    }
  }

  private cancelScheduledAgeUpdate(): void {
    if (this.ageTimeoutId !== null) {
      window.clearTimeout(this.ageTimeoutId);
      this.ageTimeoutId = null;
    }
  }

  private dispose(): void {
    this.cancelScheduledRefresh();
    this.cancelScheduledExpiration();
    this.cancelScheduledAgeUpdate();
    this.requestSubscription?.unsubscribe();
    this.requestSubscription = null;
    if (this.ssrRuntimeService.isBrowserRuntime()) {
      this.document.removeEventListener('visibilitychange', this.handleVisibilityChange);
      window.removeEventListener('online', this.handleOnline);
      window.removeEventListener('offline', this.handleOffline);
    }
  }
}
