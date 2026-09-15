import { DestroyRef } from '@angular/core';
import { of, Subject } from 'rxjs';

import { UserCollectionEntry } from '@app/models/watchlists/user-collection-entry.model';
import { AuthService } from '@app/services/auth/auth.service';
import { SharedService } from '@app/services/shared/shared.service';
import { UserCollectionActionsFacade } from './user-collection-actions.facade';
import { UserCollectionsDataPort } from './user-collections-data.port';

describe('UserCollectionActionsFacade', () => {
  it('loads a target once and toggles its favorite idempotently', () => {
    const entry: UserCollectionEntry = buildEntry();
    const dataPort: UserCollectionsDataPort = {
      listMine: vi.fn().mockReturnValue(of([])),
      add: vi.fn().mockReturnValue(of(entry)),
      delete: vi.fn().mockReturnValue(of(void 0))
    };
    const authService: Pick<AuthService, 'isLoggedIn'> = {
      isLoggedIn: vi.fn().mockReturnValue(true)
    };
    const sharedService: Pick<SharedService, 'getLoginStatusListener'> = {
      getLoginStatusListener: vi.fn().mockReturnValue(new Subject<void>())
    };
    const destroyRef: Pick<DestroyRef, 'onDestroy'> = {
      onDestroy: vi.fn().mockReturnValue((): void => undefined)
    };
    const facade: UserCollectionActionsFacade = new UserCollectionActionsFacade(
      dataPort,
      authService as AuthService,
      sharedService as SharedService,
      destroyRef as DestroyRef
    );

    facade.configure('Park', 'park-1');
    facade.toggle('Favorite');

    expect(dataPort.listMine).toHaveBeenCalledWith('Park', 'park-1');
    expect(dataPort.add).toHaveBeenCalledWith('Park', 'park-1', 'Favorite');
    expect(facade.has('Favorite')).toBe(true);

    facade.toggle('Favorite');

    expect(dataPort.delete).toHaveBeenCalledWith('Park', 'park-1', 'Favorite');
    expect(facade.has('Favorite')).toBe(false);
  });
});

function buildEntry(): UserCollectionEntry {
  return {
    entryId: 'entry-1',
    targetType: 'Park',
    targetId: 'park-1',
    kind: 'Favorite',
    targetStatus: 'Available',
    targetName: 'Europa-Park',
    parentParkId: null,
    parentParkName: null,
    mainImageId: null,
    privateNote: null,
    priority: null,
    preferredStartsOn: null,
    preferredEndsOn: null,
    createdAtUtc: '2026-09-15T10:00:00Z',
    updatedAtUtc: '2026-09-15T10:00:00Z',
    version: 1
  };
}
