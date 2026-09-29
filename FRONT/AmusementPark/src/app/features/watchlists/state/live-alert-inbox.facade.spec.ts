import { DestroyRef } from '@angular/core';
import { of } from 'rxjs';

import { LiveAlertDashboard, LiveAlertNotification } from '@app/models/watchlists/live-alert.model';
import { LiveAlertInboxFacade } from './live-alert-inbox.facade';
import { LiveAlertsDataPort } from './live-alerts-data.port';

describe('LiveAlertInboxFacade', () => {
  it('marks an unread live notification with its displayed version then refreshes', () => {
    const notification: LiveAlertNotification = buildNotification();
    const dashboard: LiveAlertDashboard = {
      subscriptions: [], notifications: [notification], unreadCount: 1, retentionDays: 30
    };
    const dataPort: LiveAlertsDataPort = buildPort(dashboard);
    const facade: LiveAlertInboxFacade = createFacade(dataPort);
    facade.load();

    facade.markRead(notification);

    expect(dataPort.markRead).toHaveBeenCalledWith('notification-1', 3);
    expect(dataPort.getDashboard).toHaveBeenCalledTimes(2);
  });

  it('does not mark an already-read notification again', () => {
    const notification: LiveAlertNotification = { ...buildNotification(), status: 'Read' };
    const dataPort: LiveAlertsDataPort = buildPort({
      subscriptions: [], notifications: [notification], unreadCount: 0, retentionDays: 30
    });
    const facade: LiveAlertInboxFacade = createFacade(dataPort);

    facade.markRead(notification);

    expect(dataPort.markRead).not.toHaveBeenCalled();
  });
});

function createFacade(dataPort: LiveAlertsDataPort): LiveAlertInboxFacade {
  const destroyRef: Pick<DestroyRef, 'onDestroy'> = {
    onDestroy: vi.fn().mockReturnValue((): void => undefined)
  };
  return new LiveAlertInboxFacade(dataPort, destroyRef as DestroyRef);
}

function buildPort(dashboard: LiveAlertDashboard): LiveAlertsDataPort {
  return {
    getDashboard: vi.fn().mockReturnValue(of(dashboard)),
    create: vi.fn(),
    delete: vi.fn().mockReturnValue(of(void 0)),
    markRead: vi.fn().mockReturnValue(of(void 0)),
    dismiss: vi.fn().mockReturnValue(of(void 0))
  };
}

function buildNotification(): LiveAlertNotification {
  return {
    notificationId: 'notification-1', subscriptionId: 'alert-1', targetId: 'item-1', parkId: 'park-1',
    targetName: 'Silver Star', parkName: 'Europa-Park', mainImageId: null, type: 'WaitBelow',
    thresholdMinutes: 30, previousStatus: 'Open', currentStatus: 'Open', previousWaitMinutes: 40,
    currentWaitMinutes: 25, sourceId: 'source-1', sourceName: 'ThemeParks.wiki',
    attributionText: 'Powered by ThemeParks.wiki', attributionUrl: 'https://example.test',
    observedAtUtc: '2026-09-29T12:00:00Z', deliveredAtUtc: '2026-09-29T12:01:00Z',
    ageSecondsAtDelivery: 60, status: 'Delivered', readAtUtc: null,
    expiresAtUtc: '2026-10-29T12:01:00Z', version: 3
  };
}
