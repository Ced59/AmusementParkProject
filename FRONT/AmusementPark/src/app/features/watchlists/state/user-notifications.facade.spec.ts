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

  it('records privacy-safe pilot interactions without changing the notification list', () => {
    const dataPort: UserNotificationsDataPort = buildPort();
    const facade: UserNotificationsFacade = createFacade(dataPort);

    facade.recordCenterOpened();
    facade.recordSourceOpened('notification-1');
    facade.reportMisleading('notification-1');

    expect(dataPort.capturePilotInteraction).toHaveBeenCalledWith('NotificationCenterOpened', null);
    expect(dataPort.capturePilotInteraction).toHaveBeenCalledWith('SourceOpened', 'notification-1');
    expect(dataPort.capturePilotInteraction).toHaveBeenCalledWith('MisleadingAlertReported', 'notification-1');
    expect(facade.reportedNotificationIds().has('notification-1')).toBe(true);
  });

  it('restores an existing misleading report from the private notification state', () => {
    const notification = {
      notificationId: 'notification-1',
      isReportedMisleading: true
    } as UserNotificationPage['items'][number];
    const dataPort: UserNotificationsDataPort = buildPort();
    vi.mocked(dataPort.search).mockReturnValue(of(buildPage([notification], 1, 1, 1)));
    const facade: UserNotificationsFacade = createFacade(dataPort);

    facade.load();

    expect(facade.reportedNotificationIds().has('notification-1')).toBe(true);
  });

  it('returns to the last populated page after dismissing the final item', () => {
    const notification = {
      notificationId: 'notification-13',
      status: 'Delivered' as const,
      version: 1
    } as UserNotificationPage['items'][number];
    const dataPort: UserNotificationsDataPort = buildPort();
    vi.mocked(dataPort.search)
      .mockReturnValueOnce(of(buildPage([notification], 2, 13, 2)))
      .mockReturnValueOnce(of(buildPage([], 2, 12, 1)))
      .mockReturnValueOnce(of(buildPage([], 1, 12, 1)));
    const facade: UserNotificationsFacade = createFacade(dataPort);
    facade.load(2);

    facade.dismiss(notification);

    expect(dataPort.search).toHaveBeenLastCalledWith(expect.objectContaining({ page: 1 }));
    expect(facade.pagination.currentPage).toBe(1);
  });
});

function createFacade(dataPort: UserNotificationsDataPort): UserNotificationsFacade {
  const destroyRef: Pick<DestroyRef, 'onDestroy'> = {
    onDestroy: vi.fn().mockReturnValue((): void => undefined)
  };
  return new UserNotificationsFacade(dataPort, destroyRef as DestroyRef);
}

function buildPort(): UserNotificationsDataPort {
  const page: UserNotificationPage = buildPage([], 1, 1, 1);
  return {
    search: vi.fn().mockReturnValue(of(page)),
    markRead: vi.fn().mockReturnValue(of(void 0)),
    dismiss: vi.fn().mockReturnValue(of(void 0)),
    markAllRead: vi.fn().mockReturnValue(of(void 0)),
    deleteSourceSubscription: vi.fn().mockReturnValue(of(void 0)),
    capturePilotInteraction: vi.fn().mockReturnValue(of(void 0))
  };
}

function buildPage(
  items: UserNotificationPage['items'],
  page: number,
  totalItems: number,
  totalPages: number
): UserNotificationPage {
  return {
    items,
    page,
    pageSize: 12,
    totalItems,
    totalPages,
    unreadCount: 1,
    retentionDays: 365,
    parkFilters: [{ parkId: 'park-1', parkName: 'Europa-Park' }]
  };
}
