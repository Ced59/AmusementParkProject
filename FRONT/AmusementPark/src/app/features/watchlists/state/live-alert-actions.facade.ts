import { DestroyRef, Inject, Injectable, Signal, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';

import { PublicLiveTarget } from '@app/models/live-data/public-live.models';
import { LiveAlertSubscription, LiveAlertType } from '@app/models/watchlists/live-alert.model';
import { AuthService } from '@app/services/auth/auth.service';
import { SharedService } from '@app/services/shared/shared.service';
import { resolvePublicLiveWaitMinutes } from '@features/public/live/utils/public-live-view.helpers';
import { LIVE_ALERTS_DATA_PORT, LiveAlertsDataPort } from './live-alerts-data.port';

@Injectable()
export class LiveAlertActionsFacade {
  private readonly targetSignal = signal<PublicLiveTarget | null>(null);
  private readonly subscriptionsSignal = signal<readonly LiveAlertSubscription[]>([]);
  private readonly authenticatedSignal = signal<boolean>(false);
  private readonly expandedSignal = signal<boolean>(false);
  private readonly loadingSignal = signal<boolean>(false);
  private readonly mutatingSignal = signal<boolean>(false);
  private readonly errorSignal = signal<boolean>(false);
  private readonly selectedTypeSignal = signal<LiveAlertType>('WaitBelow');
  private readonly thresholdSignal = signal<number>(30);
  private readonly durationSignal = signal<number>(180);
  private requestId: number = 0;

  readonly subscriptions: Signal<readonly LiveAlertSubscription[]> = this.subscriptionsSignal.asReadonly();
  readonly authenticated: Signal<boolean> = this.authenticatedSignal.asReadonly();
  readonly expanded: Signal<boolean> = this.expandedSignal.asReadonly();
  readonly loading: Signal<boolean> = this.loadingSignal.asReadonly();
  readonly mutating: Signal<boolean> = this.mutatingSignal.asReadonly();
  readonly error: Signal<boolean> = this.errorSignal.asReadonly();
  readonly selectedType: Signal<LiveAlertType> = this.selectedTypeSignal.asReadonly();
  readonly threshold: Signal<number> = this.thresholdSignal.asReadonly();
  readonly duration: Signal<number> = this.durationSignal.asReadonly();

  constructor(
    @Inject(LIVE_ALERTS_DATA_PORT) private readonly dataPort: LiveAlertsDataPort,
    private readonly authService: AuthService,
    sharedService: SharedService,
    private readonly destroyRef: DestroyRef
  ) {
    this.refreshAuthentication();
    sharedService.getLoginStatusListener()
      .pipe(takeUntilDestroyed(destroyRef))
      .subscribe((): void => {
        this.refreshAuthentication();
        this.subscriptionsSignal.set([]);
        if (this.authenticatedSignal() && this.targetSignal()) {
          this.load();
        }
      });
  }

  configure(target: PublicLiveTarget): void {
    const changed: boolean = this.targetSignal()?.targetId !== target.targetId;
    this.targetSignal.set(target);
    this.refreshAuthentication();
    if (!changed) {
      return;
    }

    const waitMinutes: number | null = resolvePublicLiveWaitMinutes(target);
    this.selectedTypeSignal.set(target.status === 'Open' ? 'WaitBelow' : 'Reopened');
    this.thresholdSignal.set(Math.max(5, Math.min(300, (waitMinutes ?? 40) - 10)));
    this.subscriptionsSignal.set([]);
    this.expandedSignal.set(false);
    this.errorSignal.set(false);
    if (this.authenticatedSignal()) {
      this.load();
    }
  }

  toggleExpanded(): void {
    this.expandedSignal.update((value: boolean): boolean => !value);
  }

  selectType(type: LiveAlertType): void {
    if (!this.mutatingSignal()) {
      this.selectedTypeSignal.set(type);
    }
  }

  setThreshold(value: number): void {
    if (Number.isFinite(value)) {
      this.thresholdSignal.set(Math.max(5, Math.min(300, Math.round(value))));
    }
  }

  setDuration(minutes: number): void {
    if ([60, 180, 360, 720].includes(minutes)) {
      this.durationSignal.set(minutes);
    }
  }

  create(): void {
    const target: PublicLiveTarget | null = this.targetSignal();
    if (!target || this.mutatingSignal()) {
      return;
    }

    const type: LiveAlertType = this.selectedTypeSignal();
    const thresholdMinutes: number | null = type === 'WaitBelow' || type === 'WaitAbove'
      ? this.thresholdSignal()
      : null;
    const mutationId: number = ++this.requestId;
    this.mutatingSignal.set(true);
    this.errorSignal.set(false);
    this.dataPort.create({
      targetId: target.targetId,
      type,
      thresholdMinutes,
      durationMinutes: this.durationSignal()
    }).pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize((): void => {
        if (mutationId === this.requestId) {
          this.mutatingSignal.set(false);
        }
      })
    ).subscribe({
      next: (subscription: LiveAlertSubscription): void => {
        if (mutationId === this.requestId) {
          this.subscriptionsSignal.update((items: readonly LiveAlertSubscription[]): readonly LiveAlertSubscription[] => [
            ...items.filter((item: LiveAlertSubscription): boolean => item.subscriptionId !== subscription.subscriptionId),
            subscription
          ]);
          this.expandedSignal.set(false);
        }
      },
      error: (): void => {
        if (mutationId === this.requestId) {
          this.errorSignal.set(true);
        }
      }
    });
  }

  remove(subscription: LiveAlertSubscription): void {
    if (this.mutatingSignal()) {
      return;
    }

    const mutationId: number = ++this.requestId;
    this.mutatingSignal.set(true);
    this.errorSignal.set(false);
    this.dataPort.delete(subscription.subscriptionId, subscription.version).pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize((): void => {
        if (mutationId === this.requestId) {
          this.mutatingSignal.set(false);
        }
      })
    ).subscribe({
      next: (): void => {
        if (mutationId === this.requestId) {
          this.subscriptionsSignal.update((items: readonly LiveAlertSubscription[]): readonly LiveAlertSubscription[] =>
            items.filter((item: LiveAlertSubscription): boolean => item.subscriptionId !== subscription.subscriptionId));
        }
      },
      error: (): void => {
        if (mutationId === this.requestId) {
          this.errorSignal.set(true);
        }
      }
    });
  }

  private load(): void {
    const target: PublicLiveTarget | null = this.targetSignal();
    if (!target) {
      return;
    }

    const loadId: number = ++this.requestId;
    this.loadingSignal.set(true);
    this.dataPort.getDashboard(target.targetId).pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize((): void => {
        if (loadId === this.requestId) {
          this.loadingSignal.set(false);
        }
      })
    ).subscribe({
      next: (dashboard): void => {
        if (loadId === this.requestId) {
          this.subscriptionsSignal.set(dashboard.subscriptions);
        }
      },
      error: (): void => {
        if (loadId === this.requestId) {
          this.errorSignal.set(true);
        }
      }
    });
  }

  private refreshAuthentication(): void {
    this.authenticatedSignal.set(this.authService.isLoggedIn());
  }
}
