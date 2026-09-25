import { DestroyRef } from '@angular/core';
import { Subject, of, throwError } from 'rxjs';

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

  it('keeps a mutation error visible after recovering the server state', () => {
    const disabled: TripNotificationState = state(false, 2, 0);
    const data: TripNotificationDataPort = {
      get: vi.fn().mockReturnValue(of(disabled)),
      setEnabled: vi.fn().mockReturnValue(throwError(() => new Error('conflict'))),
      markRead: vi.fn()
    };
    const facade: TripNotificationFacade = createFacade(data);

    facade.load('trip-1');
    facade.toggle();

    expect(data.get).toHaveBeenCalledTimes(2);
    expect(facade.state()).toEqual(disabled);
    expect(facade.error()).toBe(true);
    expect(facade.loading()).toBe(false);
  });

  it('lets a different trip supersede an in-flight load', () => {
    const tripB: Subject<TripNotificationState> = new Subject<TripNotificationState>();
    const stateA: TripNotificationState = state(false, 1, 0);
    const stateB: TripNotificationState = state(false, 3, 0);
    const enabledB: TripNotificationState = state(true, 4, 0);
    const data: TripNotificationDataPort = {
      get: vi.fn()
        .mockReturnValueOnce(of(stateA))
        .mockReturnValueOnce(tripB),
      setEnabled: vi.fn().mockReturnValue(of(enabledB)),
      markRead: vi.fn()
    };
    const facade: TripNotificationFacade = createFacade(data);

    facade.load('trip-a');
    facade.load('trip-b');
    expect(facade.state()).toBeNull();
    facade.toggle();
    expect(data.setEnabled).not.toHaveBeenCalled();

    tripB.next(stateB);
    tripB.complete();
    facade.toggle();

    expect(facade.state()).toEqual(enabledB);
    expect(facade.loading()).toBe(false);
    expect(data.setEnabled).toHaveBeenCalledWith('trip-b', {
      enabled: true,
      expectedVersion: 3
    });
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
