import { DestroyRef } from '@angular/core';
import { Subject, of } from 'rxjs';

import { TripNotificationState } from '@app/models/trips/trip-notification.models';
import { TripNotificationDataPort } from './trip-notification-data.port';
import { TripNotificationFacade } from './trip-notification.facade';

describe('TripNotificationFacade', () => {
  it('enables opt-in notifications with the current version', () => {
    const disabled: TripNotificationState = state(false, 0, 0);
    const enabled: TripNotificationState = state(true, 1, 0);
    const data: TripNotificationDataPort = {
      get: vi.fn().mockReturnValue(of(disabled)),
      setEnabled: vi.fn().mockReturnValue(of(enabled)),
      markRead: vi.fn()
    };
    const facade: TripNotificationFacade = createFacade(data);

    facade.load(' trip-1 ');
    facade.toggle();

    expect(data.setEnabled).toHaveBeenCalledWith('trip-1', {
      enabled: true,
      expectedVersion: 0
    });
    expect(facade.state()).toEqual(enabled);
  });

  it('marks unread changes as seen and keeps the returned server version', () => {
    const unread: TripNotificationState = state(true, 4, 3);
    const read: TripNotificationState = state(true, 5, 0);
    const data: TripNotificationDataPort = {
      get: vi.fn().mockReturnValue(of(unread)),
      setEnabled: vi.fn(),
      markRead: vi.fn().mockReturnValue(of(read))
    };
    const facade: TripNotificationFacade = createFacade(data);

    facade.load('trip-1');
    facade.markRead();

    expect(data.markRead).toHaveBeenCalledWith('trip-1', { expectedVersion: 4 });
    expect(facade.state()).toEqual(read);
  });

  it('ignores a refresh response that finishes after a newer mutation', () => {
    const disabled: TripNotificationState = state(false, 0, 0);
    const enabled: TripNotificationState = state(true, 1, 0);
    const staleRefresh: Subject<TripNotificationState> = new Subject<TripNotificationState>();
    const mutation: Subject<TripNotificationState> = new Subject<TripNotificationState>();
    const data: TripNotificationDataPort = {
      get: vi.fn()
        .mockReturnValueOnce(of(disabled))
        .mockReturnValueOnce(staleRefresh),
      setEnabled: vi.fn().mockReturnValue(mutation),
      markRead: vi.fn()
    };
    const facade: TripNotificationFacade = createFacade(data);

    facade.load('trip-1');
    facade.refresh();
    facade.toggle();
    mutation.next(enabled);
    mutation.complete();
    staleRefresh.next(disabled);
    staleRefresh.complete();

    expect(facade.state()).toEqual(enabled);
    expect(facade.loading()).toBe(false);
  });
});

function createFacade(data: TripNotificationDataPort): TripNotificationFacade {
  const destroyRef: DestroyRef = {
    onDestroy: (): (() => void) => (): void => undefined,
    destroyed: false
  } as unknown as DestroyRef;
  return new TripNotificationFacade(data, destroyRef);
}

function state(enabled: boolean, version: number, unreadCount: number): TripNotificationState {
  return { enabled, version, unreadCount, hasMoreUnread: false };
}
