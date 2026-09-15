import { inject, InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';

import {
  UserCollectionEntry,
  UserCollectionKind,
  UserCollectionTargetType
} from '@app/models/watchlists/user-collection-entry.model';
import { UserCollectionsApiService } from '@data-access/watchlists/user-collections-api.service';

export interface UserCollectionsDataPort {
  listMine(targetType?: UserCollectionTargetType, targetId?: string): Observable<UserCollectionEntry[]>;
  add(
    targetType: UserCollectionTargetType,
    targetId: string,
    kind: UserCollectionKind
  ): Observable<UserCollectionEntry>;
  delete(
    targetType: UserCollectionTargetType,
    targetId: string,
    kind: UserCollectionKind
  ): Observable<void>;
}

export const USER_COLLECTIONS_DATA_PORT = new InjectionToken<UserCollectionsDataPort>(
  'USER_COLLECTIONS_DATA_PORT',
  {
    providedIn: 'root',
    factory: (): UserCollectionsDataPort => inject(UserCollectionsApiService)
  }
);
