import { DestroyRef, Inject, Injectable, Signal, WritableSignal, computed, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subscription } from 'rxjs';

import {
  LiveOperationalScope,
  LiveOperationsDashboard,
  LiveQualityReplay,
  UpdateLiveOperationalControlRequest
} from '@app/models/admin/live-data/live-operations.models';
import { SignalScreenStateStore } from '@shared/state/signal-screen-state.store';
import {
  ADMIN_LIVE_OPERATIONS_DATA_PORT,
  AdminLiveOperationsDataPort
} from './admin-live-operations-data.port';

@Injectable()
export class AdminLiveOperationsFacade {
  private readonly store = new SignalScreenStateStore<LiveOperationsDashboard>();
  private readonly pendingScopeKeySignal: WritableSignal<string | null> = signal(null);
  private readonly replayPendingSignal: WritableSignal<boolean> = signal(false);
  private readonly feedbackKeySignal: WritableSignal<string | null> = signal(null);
  private readonly mutationErrorKeySignal: WritableSignal<string | null> = signal(null);
  private generation: number = 0;
  private loadSubscription: Subscription | null = null;

  readonly state = this.store.state;
  readonly dashboard: Signal<LiveOperationsDashboard | undefined> = computed(
    (): LiveOperationsDashboard | undefined => this.store.data()
  );
  readonly pendingScopeKey = this.pendingScopeKeySignal.asReadonly();
  readonly replayPending = this.replayPendingSignal.asReadonly();
  readonly feedbackKey = this.feedbackKeySignal.asReadonly();
  readonly mutationErrorKey = this.mutationErrorKeySignal.asReadonly();

  constructor(
    @Inject(ADMIN_LIVE_OPERATIONS_DATA_PORT)
    private readonly dataPort: AdminLiveOperationsDataPort,
    private readonly destroyRef: DestroyRef
  ) {
  }

  load(): void {
    const generation: number = ++this.generation;
    const previous: LiveOperationsDashboard | undefined = this.store.data();
    this.store.setLoading(previous);
    this.loadSubscription?.unsubscribe();
    this.loadSubscription = this.dataPort.getDashboard()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (dashboard: LiveOperationsDashboard): void => {
          if (generation === this.generation) {
            this.store.setReady(dashboard);
          }
        },
        error: (): void => {
          if (generation === this.generation) {
            this.store.setError('admin.liveOperations.errors.load', previous);
          }
        }
      });
  }

  update(
    scopeKey: string,
    request: UpdateLiveOperationalControlRequest
  ): void {
    if (this.pendingScopeKeySignal() !== null) {
      return;
    }

    this.pendingScopeKeySignal.set(scopeKey);
    this.clearFeedback();
    this.dataPort.updateControl(request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (): void => {
          this.pendingScopeKeySignal.set(null);
          this.feedbackKeySignal.set('admin.liveOperations.messages.updated');
          this.load();
        },
        error: (): void => {
          this.pendingScopeKeySignal.set(null);
          this.mutationErrorKeySignal.set('admin.liveOperations.errors.mutation');
        }
      });
  }

  replayQuarantine(): void {
    if (this.replayPendingSignal()) {
      return;
    }

    this.replayPendingSignal.set(true);
    this.clearFeedback();
    this.dataPort.replayQuarantine(100)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result: LiveQualityReplay): void => {
          this.replayPendingSignal.set(false);
          const feedbackKey: string = result.resolvedCount > 0
            ? result.stillBlockedCount > 0
              ? 'admin.liveOperations.messages.replayPartial'
              : 'admin.liveOperations.messages.replayed'
            : result.stillBlockedCount > 0
              ? 'admin.liveOperations.messages.replayBlocked'
              : 'admin.liveOperations.messages.replayNoChange';
          this.feedbackKeySignal.set(feedbackKey);
          this.load();
        },
        error: (): void => {
          this.replayPendingSignal.set(false);
          this.mutationErrorKeySignal.set('admin.liveOperations.errors.replay');
        }
      });
  }

  clearFeedback(): void {
    this.feedbackKeySignal.set(null);
    this.mutationErrorKeySignal.set(null);
  }
}
