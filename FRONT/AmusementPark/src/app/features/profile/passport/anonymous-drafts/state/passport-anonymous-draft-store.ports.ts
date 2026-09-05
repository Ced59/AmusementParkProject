import { inject, InjectionToken } from '@angular/core';

import { IndexedDbPassportAnonymousDraftStoreService } from '@data-access/passport/indexed-db-passport-anonymous-draft-store.service';
import { PassportAnonymousDraft } from '../models/passport-anonymous-draft.models';

export interface PassportAnonymousDraftStorePort {
  isAvailable(): boolean;
  list(): Promise<PassportAnonymousDraft[]>;
  get(draftId: string): Promise<PassportAnonymousDraft | null>;
  save(draft: PassportAnonymousDraft): Promise<void>;
  claimSecondVisitMilestone(): Promise<boolean>;
  compareAndSet(
    expectedDraft: PassportAnonymousDraft,
    updatedDraft: PassportAnonymousDraft
  ): Promise<boolean>;
  deleteIfUnchanged(expectedDraft: PassportAnonymousDraft): Promise<boolean>;
  delete(draftId: string): Promise<void>;
  clear(): Promise<void>;
}

export const PASSPORT_ANONYMOUS_DRAFT_STORE_PORT =
  new InjectionToken<PassportAnonymousDraftStorePort>(
    'PASSPORT_ANONYMOUS_DRAFT_STORE_PORT',
    { providedIn: 'root', factory: () => inject(IndexedDbPassportAnonymousDraftStoreService) }
  );
