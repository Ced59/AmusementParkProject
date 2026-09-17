import { DestroyRef } from '@angular/core';
import { of } from 'rxjs';

import { UserNotificationPage } from '@app/models/watchlists/user-notification.model';
import { UserNotificationsDataPort } from './user-notifications-data.port';
import { UserNotificationsFacade } from './user-notifications.facade';

describe('UserNotificationsFacade', () => {
  it('applies private centre filters through the data port', () => {
    const dataPort: UserNotificationsDataPort = buildPort();
    const facade: UserNotificationsFacade = createFacade(dataPort);

    facade.load();
    facade.setUnreadOnly(true);
    facade.setPark('park-1');
    facade.setEventType('ParkNameChanged');

    expect(dataPort.search).toHaveBeenLastCalledWith({
      page: 1,
      size: 12,
      unreadOnly: true,
      parkId: 'park-1',
      eventType: 'ParkNameChanged'
    });
    expect(facade.pagination.totalItems).toBe(1);
  });

  it('marks every notification as read and refreshes the first page', () => {
    const dataPort: UserNotificationsDataPort = buildPort();
    const facade: UserNotificationsFacade = createFacade(dataPort);
    facade.load(2);

    facade.markAllRead();

    expect(dataPort.markAllRead).toHaveBeenCalledOnce();
    expect(dataPort.search).toHaveBeenLastCalledWith(expect.objectContaining({ page: 1 }));
  });

  it('waits for the read mutation before continuing to the notification target', () => {
    const dataPort: UserNotificationsDataPort = buildPort();
    const facade: UserNotificationsFacade = createFacade(dataPort);
    const onSuccess: () => void = vi.fn();
    const notification = {
      notificationId: 'notification-1',
      status: 'Delivered' as const,
      version: 3
    } as Parameters<UserNotificationsFacade['markRead']>[0];

    facade.markRead(notification, onSuccess);

    expect(dataPort.markRead).toHaveBeenCalledWith('notification-1', 3);
    expect(onSuccess).toHaveBeenCalledOnce();
  });

  it('keeps the displayed subscription version when unsubscribing', () => {
    const dataPort: UserNotificationsDataPort = buildPort();
    const facade: UserNotificationsFacade = createFacade(dataPort);
    const notification = {
      notificationId: 'notification-1',
      subscriptionVersion: 4
    } as Parameters<UserNotificationsFacade['unsubscribe']>[0];

    facade.unsubscribe(notification);

    expect(dataPort.deleteSourceSubscription).toHaveBeenCalledWith('notification-1', 4);
  });
});

function createFacade(dataPort: UserNotificationsDataPort): UserNotificationsFacade {
  const destroyRef: Pick<DestroyRef, 'onDestroy'> = {
    onDestroy: vi.fn().mockReturnValue((): void => undefined)
  };
  return new UserNotificationsFacade(dataPort, destroyRef as DestroyRef);
}

function buildPort(): UserNotificationsDataPort {
  const page: UserNotificationPage = {
    items: [],
    page: 1,
    pageSize: 12,
    totalItems: 1,
    totalPages: 1,
    unreadCount: 1,
    retentionDays: 365,
    parkFilters: [{ parkId: 'park-1', parkName: 'Europa-Park' }]
  };
  return {
    search: vi.fn().mockReturnValue(of(page)),
    markRead: vi.fn().mockReturnValue(of(void 0)),
    dismiss: vi.fn().mockReturnValue(of(void 0)),
    markAllRead: vi.fn().mockReturnValue(of(void 0)),
    deleteSourceSubscription: vi.fn().mockReturnValue(of(void 0))
  };
}
