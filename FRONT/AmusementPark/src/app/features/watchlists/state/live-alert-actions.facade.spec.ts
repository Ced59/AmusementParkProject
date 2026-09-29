import { DestroyRef } from '@angular/core';
import { of, Subject } from 'rxjs';

import { PublicLiveTarget } from '@app/models/live-data/public-live.models';
import { LiveAlertDashboard, LiveAlertSubscription } from '@app/models/watchlists/live-alert.model';
import { AuthService } from '@app/services/auth/auth.service';
import { SharedService } from '@app/services/shared/shared.service';
import { LiveAlertActionsFacade } from './live-alert-actions.facade';
import { LiveAlertsDataPort } from './live-alerts-data.port';

describe('LiveAlertActionsFacade', () => {
  it('creates an explicit temporary threshold alert for the current attraction', () => {
    const subscription: LiveAlertSubscription = buildSubscription();
    const dataPort: LiveAlertsDataPort = buildPort(subscription);
    const facade: LiveAlertActionsFacade = createFacade(dataPort);

    facade.configure(buildTarget());
    facade.selectType('WaitAbove');
    facade.setThreshold(55);
    facade.setDuration(60);
    facade.create();

    expect(dataPort.getDashboard).toHaveBeenCalledWith('item-1');
    expect(dataPort.create).toHaveBeenCalledWith({
      targetId: 'item-1',
      type: 'WaitAbove',
      thresholdMinutes: 55,
      durationMinutes: 60
    });
    expect(facade.subscriptions()).toEqual([subscription]);
  });

  it('does not load private alerts for a signed-out visitor', () => {
    const dataPort: LiveAlertsDataPort = buildPort(buildSubscription());
    const authService: Pick<AuthService, 'isLoggedIn'> = {
      isLoggedIn: vi.fn().mockReturnValue(false)
    };
    const facade: LiveAlertActionsFacade = createFacade(dataPort, authService);

    facade.configure(buildTarget());

    expect(dataPort.getDashboard).not.toHaveBeenCalled();
    expect(facade.authenticated()).toBe(false);
  });
});

function createFacade(
  dataPort: LiveAlertsDataPort,
  authService: Pick<AuthService, 'isLoggedIn'> = {
    isLoggedIn: vi.fn().mockReturnValue(true)
  }
): LiveAlertActionsFacade {
  const sharedService: Pick<SharedService, 'getLoginStatusListener'> = {
    getLoginStatusListener: vi.fn().mockReturnValue(new Subject<void>())
  };
  const destroyRef: Pick<DestroyRef, 'onDestroy'> = {
    onDestroy: vi.fn().mockReturnValue((): void => undefined)
  };
  return new LiveAlertActionsFacade(
    dataPort,
    authService as AuthService,
    sharedService as SharedService,
    destroyRef as DestroyRef
  );
}

function buildPort(subscription: LiveAlertSubscription): LiveAlertsDataPort {
  const dashboard: LiveAlertDashboard = {
    subscriptions: [],
    notifications: [],
    unreadCount: 0,
    retentionDays: 30
  };
  return {
    getDashboard: vi.fn().mockReturnValue(of(dashboard)),
    create: vi.fn().mockReturnValue(of(subscription)),
    delete: vi.fn().mockReturnValue(of(void 0)),
    markRead: vi.fn().mockReturnValue(of(void 0)),
    dismiss: vi.fn().mockReturnValue(of(void 0))
  };
}

function buildSubscription(): LiveAlertSubscription {
  return {
    subscriptionId: 'alert-1', targetId: 'item-1', parkId: 'park-1',
    targetName: 'Silver Star', parkName: 'Europa-Park', mainImageId: null,
    type: 'WaitAbove', thresholdMinutes: 55,
    createdAtUtc: '2026-09-29T12:00:00Z', expiresAtUtc: '2026-09-29T13:00:00Z',
    lastObservedAtUtc: '2026-09-29T12:00:00Z', lastStatus: 'Open', lastWaitMinutes: 40,
    cooldownMinutes: 30, hysteresisMinutes: 5, version: 1
  };
}

function buildTarget(): PublicLiveTarget {
  return {
    targetId: 'item-1', targetType: 'ParkItem', displayName: 'Silver Star',
    parkId: 'park-1', parkDisplayName: 'Europa-Park', availability: 'Current', status: 'Open',
    queues: [{ kind: 'Standby', waitTimeMinutes: 40, isEstimated: false, availability: 'Available',
      returnStartUtc: null, returnEndUtc: null, currentGroupStart: null, currentGroupEnd: null,
      nextAllocationUtc: null, priceMinorUnits: null, currencyCode: null }],
    asOfUtc: '2026-09-29T12:00:00Z', observedAtUtc: '2026-09-29T12:00:00Z',
    receivedAtUtc: '2026-09-29T12:00:00Z', ageSeconds: 20, freshness: 'Fresh',
    expiresAtUtc: '2026-09-29T12:05:00Z', source: null, confidence: 'High'
  };
}
