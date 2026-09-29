import { DestroyRef, Inject, Injectable, Signal, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize, Observable } from 'rxjs';

import {
  LiveAlertDashboard,
  LiveAlertNotification,
  LiveAlertSubscription
} from '@app/models/watchlists/live-alert.model';
import { LIVE_ALERTS_DATA_PORT, LiveAlertsDataPort } from './live-alerts-data.port';

@Injectable()
export class LiveAlertInboxFacade {
  private readonly dashboardSignal = signal<LiveAlertDashboard | null>(null);
  private readonly loadingSignal = signal<boolean>(false);
  private readonly mutatingIdSignal = signal<string | null>(null);
  private readonly errorSignal = signal<boolean>(false);

  readonly dashboard: Signal<LiveAlertDashboard | null> = this.dashboardSignal.asReadonly();
  readonly loading: Signal<boolean> = this.loadingSignal.asReadonly();
  readonly mutatingId: Signal<string | null> = this.mutatingIdSignal.asReadonly();
  readonly error: Signal<boolean> = this.errorSignal.asReadonly();

  constructor(
    @Inject(LIVE_ALERTS_DATA_PORT) private readonly dataPort: LiveAlertsDataPort,
    private readonly destroyRef: DestroyRef
  ) {
  }

  load(): void {
    this.loadingSignal.set(true);
    this.errorSignal.set(false);
    this.dataPort.getDashboard().pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize((): void => this.loadingSignal.set(false))
    ).subscribe({
      next: (dashboard: LiveAlertDashboard): void => this.dashboardSignal.set(dashboard),
      error: (): void => this.errorSignal.set(true)
    });
  }

  markRead(notification: LiveAlertNotification): void {
    if (notification.status !== 'Delivered') {
      return;
    }
    this.runMutation(
      notification.notificationId,
      this.dataPort.markRead(notification.notificationId, notification.version));
  }

  dismiss(notification: LiveAlertNotification): void {
    this.runMutation(
      notification.notificationId,
      this.dataPort.dismiss(notification.notificationId, notification.version));
  }

  remove(subscription: LiveAlertSubscription): void {
    this.runMutation(
      subscription.subscriptionId,
      this.dataPort.delete(subscription.subscriptionId, subscription.version));
  }

  private runMutation(id: string, request: Observable<void>): void {
    if (this.mutatingIdSignal()) {
      return;
    }

    this.mutatingIdSignal.set(id);
    this.errorSignal.set(false);
    request.pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize((): void => this.mutatingIdSignal.set(null))
    ).subscribe({
      next: (): void => this.load(),
      error: (): void => this.errorSignal.set(true)
    });
  }
}
