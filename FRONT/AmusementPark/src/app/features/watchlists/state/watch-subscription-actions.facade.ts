import { DestroyRef, Inject, Injectable, Signal, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize, Observable } from 'rxjs';

import { UserCollectionTargetType } from '@app/models/watchlists/user-collection-entry.model';
import {
  FactualEventType,
  WatchSubscription
} from '@app/models/watchlists/watch-subscription.model';
import { AuthService } from '@app/services/auth/auth.service';
import { SharedService } from '@app/services/shared/shared.service';
import {
  WATCH_SUBSCRIPTIONS_DATA_PORT,
  WatchSubscriptionsDataPort
} from './watch-subscriptions-data.port';

@Injectable()
export class WatchSubscriptionActionsFacade {
  private readonly targetTypeSignal = signal<UserCollectionTargetType>('Park');
  private readonly targetIdSignal = signal<string>('');
  private readonly subscriptionSignal = signal<WatchSubscription | null>(null);
  private readonly selectedEventTypesSignal = signal<FactualEventType[]>([]);
  private readonly authenticatedSignal = signal<boolean>(false);
  private readonly loadingSignal = signal<boolean>(false);
  private readonly mutatingSignal = signal<boolean>(false);
  private readonly expandedSignal = signal<boolean>(false);
  private readonly errorSignal = signal<boolean>(false);
  private requestId: number = 0;

  readonly authenticated: Signal<boolean> = this.authenticatedSignal.asReadonly();
  readonly subscription: Signal<WatchSubscription | null> = this.subscriptionSignal.asReadonly();
  readonly selectedEventTypes: Signal<FactualEventType[]> = this.selectedEventTypesSignal.asReadonly();
  readonly loading: Signal<boolean> = this.loadingSignal.asReadonly();
  readonly mutating: Signal<boolean> = this.mutatingSignal.asReadonly();
  readonly expanded: Signal<boolean> = this.expandedSignal.asReadonly();
  readonly error: Signal<boolean> = this.errorSignal.asReadonly();

  constructor(
    @Inject(WATCH_SUBSCRIPTIONS_DATA_PORT) private readonly dataPort: WatchSubscriptionsDataPort,
    private readonly authService: AuthService,
    sharedService: SharedService,
    private readonly destroyRef: DestroyRef
  ) {
    this.refreshAuthentication();
    sharedService.getLoginStatusListener()
      .pipe(takeUntilDestroyed(destroyRef))
      .subscribe((): void => {
        this.refreshAuthentication();
        if (this.authenticatedSignal() && this.targetIdSignal()) {
          this.load();
        } else {
          this.reset();
        }
      });
  }

  configure(
    targetType: UserCollectionTargetType,
    targetId: string,
    defaultEventTypes: readonly FactualEventType[]
  ): void {
    const normalizedTargetId: string = targetId.trim();
    const changed: boolean = targetType !== this.targetTypeSignal()
      || normalizedTargetId !== this.targetIdSignal();
    this.targetTypeSignal.set(targetType);
    this.targetIdSignal.set(normalizedTargetId);
    this.refreshAuthentication();
    if (!changed) {
      return;
    }

    this.requestId++;
    this.subscriptionSignal.set(null);
    this.selectedEventTypesSignal.set([...defaultEventTypes]);
    this.expandedSignal.set(false);
    this.errorSignal.set(false);
    if (this.authenticatedSignal() && normalizedTargetId) {
      this.load();
    }
  }

  toggleExpanded(): void {
    this.expandedSignal.update((expanded: boolean): boolean => !expanded);
  }

  isGroupSelected(eventTypes: readonly FactualEventType[]): boolean {
    const selected: Set<FactualEventType> = new Set(this.selectedEventTypesSignal());
    return eventTypes.every((eventType: FactualEventType): boolean => selected.has(eventType));
  }

  toggleGroup(eventTypes: readonly FactualEventType[]): void {
    if (this.mutatingSignal()) {
      return;
    }

    const current: Set<FactualEventType> = new Set(this.selectedEventTypesSignal());
    const remove: boolean = eventTypes.every((eventType: FactualEventType): boolean => current.has(eventType));
    eventTypes.forEach((eventType: FactualEventType): void => {
      if (remove) {
        current.delete(eventType);
      } else {
        current.add(eventType);
      }
    });
    this.selectedEventTypesSignal.set([...current]);
  }

  save(): void {
    const targetId: string = this.targetIdSignal();
    const eventTypes: FactualEventType[] = this.selectedEventTypesSignal();
    if (!targetId || eventTypes.length === 0 || this.mutatingSignal()) {
      return;
    }

    const current: WatchSubscription | null = this.subscriptionSignal();
    const request: Observable<WatchSubscription> = current
      ? this.dataPort.update(current.subscriptionId, {
        eventTypes,
        frequency: 'WebOnly',
        channels: [],
        expectedVersion: current.version
      })
      : this.dataPort.create({
        targetType: this.targetTypeSignal(),
        targetId,
        eventTypes,
        frequency: 'WebOnly',
        channels: []
      });
    this.runMutation(request);
  }

  togglePaused(): void {
    const current: WatchSubscription | null = this.subscriptionSignal();
    if (!current || this.mutatingSignal()) {
      return;
    }

    this.runMutation(this.dataPort.setPaused(
      current.subscriptionId,
      current.version,
      !current.isPaused
    ));
  }

  remove(): void {
    const current: WatchSubscription | null = this.subscriptionSignal();
    if (!current || this.mutatingSignal()) {
      return;
    }

    const mutationId: number = ++this.requestId;
    this.mutatingSignal.set(true);
    this.errorSignal.set(false);
    this.dataPort.delete(current.subscriptionId, current.version)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize((): void => {
          if (mutationId === this.requestId) {
            this.mutatingSignal.set(false);
          }
        })
      )
      .subscribe({
        next: (): void => {
          if (mutationId === this.requestId) {
            this.subscriptionSignal.set(null);
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

  private load(): void {
    const loadId: number = ++this.requestId;
    this.loadingSignal.set(true);
    this.dataPort.listMine(this.targetTypeSignal(), this.targetIdSignal())
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize((): void => {
          if (loadId === this.requestId) {
            this.loadingSignal.set(false);
          }
        })
      )
      .subscribe({
        next: (subscriptions: WatchSubscription[]): void => {
          if (loadId !== this.requestId) {
            return;
          }
          const subscription: WatchSubscription | null = subscriptions[0] ?? null;
          this.subscriptionSignal.set(subscription);
          if (subscription) {
            this.selectedEventTypesSignal.set([...subscription.eventTypes]);
          }
        },
        error: (): void => {
          if (loadId === this.requestId) {
            this.errorSignal.set(true);
          }
        }
      });
  }

  private runMutation(request: Observable<WatchSubscription>): void {
    const mutationId: number = ++this.requestId;
    this.mutatingSignal.set(true);
    this.errorSignal.set(false);
    request.pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize((): void => {
        if (mutationId === this.requestId) {
          this.mutatingSignal.set(false);
        }
      })
    ).subscribe({
      next: (subscription: WatchSubscription): void => {
        if (mutationId === this.requestId) {
          this.subscriptionSignal.set(subscription);
          this.selectedEventTypesSignal.set([...subscription.eventTypes]);
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

  private reset(): void {
    this.requestId++;
    this.subscriptionSignal.set(null);
    this.loadingSignal.set(false);
    this.mutatingSignal.set(false);
    this.expandedSignal.set(false);
  }

  private refreshAuthentication(): void {
    this.authenticatedSignal.set(this.authService.isLoggedIn());
  }
}
