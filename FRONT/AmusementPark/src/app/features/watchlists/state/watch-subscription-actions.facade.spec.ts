import { DestroyRef } from '@angular/core';
import { of, Subject } from 'rxjs';

import { WatchSubscription } from '@app/models/watchlists/watch-subscription.model';
import { AuthService } from '@app/services/auth/auth.service';
import { SharedService } from '@app/services/shared/shared.service';
import { WatchSubscriptionActionsFacade } from './watch-subscription-actions.facade';
import { WatchSubscriptionsDataPort } from './watch-subscriptions-data.port';

describe('WatchSubscriptionActionsFacade', () => {
  it('creates an explicit Web subscription from the selected groups', () => {
    const subscription: WatchSubscription = buildSubscription();
    const dataPort: WatchSubscriptionsDataPort = {
      listMine: vi.fn().mockReturnValue(of([])),
      create: vi.fn().mockReturnValue(of(subscription)),
      update: vi.fn(),
      setPaused: vi.fn(),
      delete: vi.fn()
    };
    const facade: WatchSubscriptionActionsFacade = createFacade(dataPort);

    facade.configure('Park', 'park-1', ['ParkNameChanged', 'OpeningCalendarChanged']);
    facade.save();

    expect(dataPort.create).toHaveBeenCalledWith({
      targetType: 'Park',
      targetId: 'park-1',
      eventTypes: ['ParkNameChanged', 'OpeningCalendarChanged'],
      frequency: 'WebOnly',
      channels: []
    });
    expect(facade.subscription()?.subscriptionId).toBe('subscription-1');
  });

  it('does not save an implicit subscription without a selected group', () => {
    const dataPort: WatchSubscriptionsDataPort = {
      listMine: vi.fn().mockReturnValue(of([])),
      create: vi.fn(),
      update: vi.fn(),
      setPaused: vi.fn(),
      delete: vi.fn()
    };
    const facade: WatchSubscriptionActionsFacade = createFacade(dataPort);

    facade.configure('ParkItem', 'item-1', ['Renamed']);
    facade.toggleGroup(['Renamed']);
    facade.save();

    expect(dataPort.create).not.toHaveBeenCalled();
  });
});

function createFacade(dataPort: WatchSubscriptionsDataPort): WatchSubscriptionActionsFacade {
  const authService: Pick<AuthService, 'isLoggedIn'> = {
    isLoggedIn: vi.fn().mockReturnValue(true)
  };
  const sharedService: Pick<SharedService, 'getLoginStatusListener'> = {
    getLoginStatusListener: vi.fn().mockReturnValue(new Subject<void>())
  };
  const destroyRef: Pick<DestroyRef, 'onDestroy'> = {
    onDestroy: vi.fn().mockReturnValue((): void => undefined)
  };
  return new WatchSubscriptionActionsFacade(
    dataPort,
    authService as AuthService,
    sharedService as SharedService,
    destroyRef as DestroyRef
  );
}

function buildSubscription(): WatchSubscription {
  return {
    subscriptionId: 'subscription-1',
    targetType: 'Park',
    targetId: 'park-1',
    targetName: 'Europa-Park',
    parentParkId: null,
    parentParkName: null,
    mainImageId: null,
    eventTypes: ['ParkNameChanged', 'OpeningCalendarChanged'],
    frequency: 'WebOnly',
    channels: [],
    isPaused: false,
    createdAtUtc: '2026-09-15T10:00:00Z',
    updatedAtUtc: '2026-09-15T10:00:00Z',
    version: 1
  };
}
