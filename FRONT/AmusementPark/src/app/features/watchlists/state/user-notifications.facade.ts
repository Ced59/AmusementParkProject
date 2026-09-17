import { DestroyRef, Inject, Injectable, Signal, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize, Observable } from 'rxjs';

import {
  UserNotification,
  UserNotificationPage,
  UserNotificationParkFilter,
  UserNotificationSearch
} from '@app/models/watchlists/user-notification.model';
import { FactualEventType } from '@app/models/watchlists/watch-subscription.model';
import { PaginationContract } from '@shared/models/contracts/pagination.model';
import {
  USER_NOTIFICATIONS_DATA_PORT,
  UserNotificationsDataPort
} from './user-notifications-data.port';

@Injectable()
export class UserNotificationsFacade {
  private static readonly PageSize: number = 12;
  private readonly pageSignal = signal<UserNotificationPage | null>(null);
  private readonly loadingSignal = signal<boolean>(false);
  private readonly errorSignal = signal<boolean>(false);
  private readonly mutatingIdSignal = signal<string | null>(null);
  private readonly unreadOnlySignal = signal<boolean>(false);
  private readonly parkIdSignal = signal<string | null>(null);
  private readonly eventTypeSignal = signal<FactualEventType | null>(null);
  private requestId: number = 0;

  readonly page: Signal<UserNotificationPage | null> = this.pageSignal.asReadonly();
  readonly loading: Signal<boolean> = this.loadingSignal.asReadonly();
  readonly error: Signal<boolean> = this.errorSignal.asReadonly();
  readonly mutatingId: Signal<string | null> = this.mutatingIdSignal.asReadonly();
  readonly unreadOnly: Signal<boolean> = this.unreadOnlySignal.asReadonly();
  readonly parkId: Signal<string | null> = this.parkIdSignal.asReadonly();
  readonly eventType: Signal<FactualEventType | null> = this.eventTypeSignal.asReadonly();

  constructor(
    @Inject(USER_NOTIFICATIONS_DATA_PORT) private readonly dataPort: UserNotificationsDataPort,
    private readonly destroyRef: DestroyRef
  ) {
  }

  get notifications(): UserNotification[] {
    return this.pageSignal()?.items ?? [];
  }

  get parkFilters(): UserNotificationParkFilter[] {
    return this.pageSignal()?.parkFilters ?? [];
  }

  get pagination(): PaginationContract {
    const page: UserNotificationPage | null = this.pageSignal();
    return {
      currentPage: page?.page ?? 1,
      itemsPerPage: page?.pageSize ?? UserNotificationsFacade.PageSize,
      totalItems: page?.totalItems ?? 0,
      totalPages: page?.totalPages ?? 0
    };
  }

  load(page: number = 1): void {
    const loadId: number = ++this.requestId;
    this.loadingSignal.set(true);
    this.errorSignal.set(false);
    const criteria: UserNotificationSearch = {
      page: Math.max(1, Math.trunc(page)),
      size: UserNotificationsFacade.PageSize,
      unreadOnly: this.unreadOnlySignal(),
      parkId: this.parkIdSignal(),
      eventType: this.eventTypeSignal()
    };
    this.dataPort.search(criteria)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize((): void => {
          if (loadId === this.requestId) {
            this.loadingSignal.set(false);
          }
        })
      )
      .subscribe({
        next: (result: UserNotificationPage): void => {
          if (loadId === this.requestId) {
            this.pageSignal.set(result);
          }
        },
        error: (): void => {
          if (loadId === this.requestId) {
            this.errorSignal.set(true);
          }
        }
      });
  }

  setUnreadOnly(unreadOnly: boolean): void {
    this.unreadOnlySignal.set(unreadOnly);
    this.load();
  }

  setPark(parkId: string): void {
    this.parkIdSignal.set(parkId.trim() || null);
    this.load();
  }

  setEventType(eventType: string): void {
    this.eventTypeSignal.set((eventType.trim() || null) as FactualEventType | null);
    this.load();
  }

  markRead(notification: UserNotification, onSuccess?: () => void): void {
    if (notification.status === 'Read') {
      onSuccess?.();
      return;
    }
    this.runMutation(
      notification.notificationId,
      this.dataPort.markRead(notification.notificationId, notification.version),
      (): void => {
        if (onSuccess) {
          onSuccess();
          return;
        }
        this.load(this.pageSignal()?.page ?? 1);
      }
    );
  }

  dismiss(notification: UserNotification): void {
    this.runMutation(
      notification.notificationId,
      this.dataPort.dismiss(notification.notificationId, notification.version),
      (): void => this.load(this.pageSignal()?.page ?? 1)
    );
  }

  markAllRead(): void {
    this.runMutation('all', this.dataPort.markAllRead(), (): void => this.load());
  }

  unsubscribe(notification: UserNotification): void {
    this.runMutation(
      notification.notificationId,
      this.dataPort.deleteSourceSubscription(notification.notificationId),
      (): void => this.load(this.pageSignal()?.page ?? 1)
    );
  }

  private runMutation(id: string, request: Observable<void>, onSuccess: () => void): void {
    if (this.mutatingIdSignal()) {
      return;
    }
    this.mutatingIdSignal.set(id);
    this.errorSignal.set(false);
    request.pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize((): void => this.mutatingIdSignal.set(null))
    ).subscribe({
      next: onSuccess,
      error: (): void => this.errorSignal.set(true)
    });
  }
}
