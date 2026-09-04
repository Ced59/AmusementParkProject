import { Injectable } from '@angular/core';

import {
  PassportAnonymousDraft
} from '@features/profile/passport/anonymous-drafts/models/passport-anonymous-draft.models';
import { isSupportedPassportAnonymousDraft } from '@features/profile/passport/anonymous-drafts/models/passport-anonymous-draft-validation';
import type { PassportAnonymousDraftStorePort } from '@features/profile/passport/anonymous-drafts/state/passport-anonymous-draft-store.ports';

@Injectable({ providedIn: 'root' })
export class IndexedDbPassportAnonymousDraftStoreService implements PassportAnonymousDraftStorePort {
  private static readonly DatabaseName: string = 'amusement-park-passport';
  private static readonly DatabaseVersion: number = 1;
  private static readonly DraftStoreName: string = 'anonymous-visit-drafts';

  private databasePromise: Promise<IDBDatabase> | null = null;

  isAvailable(): boolean {
    return typeof globalThis.indexedDB !== 'undefined';
  }

  async list(): Promise<PassportAnonymousDraft[]> {
    const drafts: PassportAnonymousDraft[] = await this.execute<PassportAnonymousDraft[]>(
      'readonly',
      (store: IDBObjectStore): IDBRequest<PassportAnonymousDraft[]> => store.getAll()
    );
    return drafts
      .filter(isSupportedPassportAnonymousDraft)
      .sort((left: PassportAnonymousDraft, right: PassportAnonymousDraft): number =>
        right.updatedAtUtc.localeCompare(left.updatedAtUtc));
  }

  async get(draftId: string): Promise<PassportAnonymousDraft | null> {
    const normalizedDraftId: string = this.requireIdentifier(draftId);
    const draft: PassportAnonymousDraft | undefined =
      await this.execute<PassportAnonymousDraft | undefined>(
        'readonly',
        (store: IDBObjectStore): IDBRequest<PassportAnonymousDraft | undefined> =>
          store.get(normalizedDraftId)
      );
    return draft && isSupportedPassportAnonymousDraft(draft) ? draft : null;
  }

  async save(draft: PassportAnonymousDraft): Promise<void> {
    if (!isSupportedPassportAnonymousDraft(draft)) {
      throw new Error('passport-anonymous-draft.invalid-schema');
    }

    await this.writeUnlockedDraft(draft);
  }

  async compareAndSet(
    expectedDraft: PassportAnonymousDraft,
    updatedDraft: PassportAnonymousDraft
  ): Promise<boolean> {
    if (!isSupportedPassportAnonymousDraft(expectedDraft)
      || !isSupportedPassportAnonymousDraft(updatedDraft)
      || expectedDraft.id !== updatedDraft.id) {
      throw new Error('passport-anonymous-draft.invalid-comparison');
    }

    const database: IDBDatabase = await this.openDatabase();
    return new Promise<boolean>((resolve, reject): void => {
      const transaction: IDBTransaction = database.transaction(
        IndexedDbPassportAnonymousDraftStoreService.DraftStoreName,
        'readwrite'
      );
      const store: IDBObjectStore = transaction.objectStore(
        IndexedDbPassportAnonymousDraftStoreService.DraftStoreName
      );
      const readRequest: IDBRequest<PassportAnonymousDraft | undefined> = store.get(expectedDraft.id);
      let matches: boolean = false;
      readRequest.onsuccess = (): void => {
        const currentDraft: PassportAnonymousDraft | undefined = readRequest.result;
        matches = !!currentDraft
          && isSupportedPassportAnonymousDraft(currentDraft)
          && this.draftsAreEqual(currentDraft, expectedDraft);
        if (matches) {
          store.put(updatedDraft);
        }
      };
      readRequest.onerror = (): void => reject(readRequest.error ?? new Error(
        'passport-anonymous-draft.request-failed'
      ));
      transaction.oncomplete = (): void => resolve(matches);
      transaction.onerror = (): void => reject(transaction.error ?? new Error(
        'passport-anonymous-draft.transaction-failed'
      ));
      transaction.onabort = (): void => reject(transaction.error ?? new Error(
        'passport-anonymous-draft.transaction-aborted'
      ));
    });
  }

  async delete(draftId: string): Promise<void> {
    const normalizedDraftId: string = this.requireIdentifier(draftId);
    await this.deleteUnlockedDraft(normalizedDraftId);
  }

  async deleteIfUnchanged(expectedDraft: PassportAnonymousDraft): Promise<boolean> {
    if (!isSupportedPassportAnonymousDraft(expectedDraft)) {
      throw new Error('passport-anonymous-draft.invalid-comparison');
    }

    const database: IDBDatabase = await this.openDatabase();
    return new Promise<boolean>((resolve, reject): void => {
      const transaction: IDBTransaction = database.transaction(
        IndexedDbPassportAnonymousDraftStoreService.DraftStoreName,
        'readwrite'
      );
      const store: IDBObjectStore = transaction.objectStore(
        IndexedDbPassportAnonymousDraftStoreService.DraftStoreName
      );
      const readRequest: IDBRequest<PassportAnonymousDraft | undefined> = store.get(expectedDraft.id);
      let canDelete: boolean = false;
      readRequest.onsuccess = (): void => {
        const currentDraft: PassportAnonymousDraft | undefined = readRequest.result;
        canDelete = !currentDraft
          || (isSupportedPassportAnonymousDraft(currentDraft)
            && this.draftsAreEqual(currentDraft, expectedDraft));
        if (currentDraft && canDelete) {
          store.delete(expectedDraft.id);
        }
      };
      readRequest.onerror = (): void => reject(readRequest.error ?? new Error(
        'passport-anonymous-draft.request-failed'
      ));
      transaction.oncomplete = (): void => resolve(canDelete);
      transaction.onerror = (): void => reject(transaction.error ?? new Error(
        'passport-anonymous-draft.transaction-failed'
      ));
      transaction.onabort = (): void => reject(transaction.error ?? new Error(
        'passport-anonymous-draft.transaction-aborted'
      ));
    });
  }

  async clear(): Promise<void> {
    await this.clearUnlockedDrafts();
  }

  private async writeUnlockedDraft(draft: PassportAnonymousDraft): Promise<void> {
    const database: IDBDatabase = await this.openDatabase();
    return new Promise<void>((resolve, reject): void => {
      const transaction: IDBTransaction = database.transaction(
        IndexedDbPassportAnonymousDraftStoreService.DraftStoreName,
        'readwrite'
      );
      const store: IDBObjectStore = transaction.objectStore(
        IndexedDbPassportAnonymousDraftStoreService.DraftStoreName
      );
      const readRequest: IDBRequest<PassportAnonymousDraft | undefined> = store.get(draft.id);
      readRequest.onsuccess = (): void => {
        const currentDraft: PassportAnonymousDraft | undefined = readRequest.result;
        if (currentDraft?.pendingImport) {
          transaction.abort();
          return;
        }

        store.put(draft);
      };
      this.resolveProtectedTransaction(transaction, resolve, reject);
    });
  }

  private async deleteUnlockedDraft(draftId: string): Promise<void> {
    const database: IDBDatabase = await this.openDatabase();
    return new Promise<void>((resolve, reject): void => {
      const transaction: IDBTransaction = database.transaction(
        IndexedDbPassportAnonymousDraftStoreService.DraftStoreName,
        'readwrite'
      );
      const store: IDBObjectStore = transaction.objectStore(
        IndexedDbPassportAnonymousDraftStoreService.DraftStoreName
      );
      const readRequest: IDBRequest<PassportAnonymousDraft | undefined> = store.get(draftId);
      readRequest.onsuccess = (): void => {
        if (readRequest.result?.pendingImport) {
          transaction.abort();
          return;
        }

        store.delete(draftId);
      };
      this.resolveProtectedTransaction(transaction, resolve, reject);
    });
  }

  private async clearUnlockedDrafts(): Promise<void> {
    const database: IDBDatabase = await this.openDatabase();
    return new Promise<void>((resolve, reject): void => {
      const transaction: IDBTransaction = database.transaction(
        IndexedDbPassportAnonymousDraftStoreService.DraftStoreName,
        'readwrite'
      );
      const store: IDBObjectStore = transaction.objectStore(
        IndexedDbPassportAnonymousDraftStoreService.DraftStoreName
      );
      const readRequest: IDBRequest<PassportAnonymousDraft[]> = store.getAll();
      readRequest.onsuccess = (): void => {
        if (readRequest.result.some((draft: PassportAnonymousDraft): boolean => !!draft.pendingImport)) {
          transaction.abort();
          return;
        }

        store.clear();
      };
      this.resolveProtectedTransaction(transaction, resolve, reject);
    });
  }

  private resolveProtectedTransaction(
    transaction: IDBTransaction,
    resolve: () => void,
    reject: (reason?: unknown) => void
  ): void {
    transaction.oncomplete = (): void => resolve();
    transaction.onerror = (): void => reject(transaction.error ?? new Error(
      'passport-anonymous-draft.transaction-failed'
    ));
    transaction.onabort = (): void => reject(new Error(
      'passport-anonymous-draft.import-locked'
    ));
  }

  private draftsAreEqual(left: PassportAnonymousDraft, right: PassportAnonymousDraft): boolean {
    return JSON.stringify(left) === JSON.stringify(right);
  }

  private async execute<TResult>(
    mode: IDBTransactionMode,
    operation: (store: IDBObjectStore) => IDBRequest<TResult>
  ): Promise<TResult> {
    const database: IDBDatabase = await this.openDatabase();
    return new Promise<TResult>((resolve, reject): void => {
      const transaction: IDBTransaction = database.transaction(
        IndexedDbPassportAnonymousDraftStoreService.DraftStoreName,
        mode
      );
      const request: IDBRequest<TResult> = operation(transaction.objectStore(
        IndexedDbPassportAnonymousDraftStoreService.DraftStoreName
      ));
      let result: TResult = undefined as TResult;
      request.onsuccess = (): void => {
        result = request.result;
      };
      request.onerror = (): void => reject(request.error ?? new Error(
        'passport-anonymous-draft.request-failed'
      ));
      transaction.oncomplete = (): void => resolve(result);
      transaction.onerror = (): void => reject(transaction.error ?? new Error(
        'passport-anonymous-draft.transaction-failed'
      ));
      transaction.onabort = (): void => reject(transaction.error ?? new Error(
        'passport-anonymous-draft.transaction-aborted'
      ));
    });
  }

  private openDatabase(): Promise<IDBDatabase> {
    if (!this.isAvailable()) {
      return Promise.reject(new Error('passport-anonymous-draft.storage-unavailable'));
    }

    if (!this.databasePromise) {
      this.databasePromise = new Promise<IDBDatabase>((resolve, reject): void => {
        const request: IDBOpenDBRequest = globalThis.indexedDB.open(
          IndexedDbPassportAnonymousDraftStoreService.DatabaseName,
          IndexedDbPassportAnonymousDraftStoreService.DatabaseVersion
        );
        request.onupgradeneeded = (): void => {
          const database: IDBDatabase = request.result;
          if (!database.objectStoreNames.contains(
            IndexedDbPassportAnonymousDraftStoreService.DraftStoreName
          )) {
            const store: IDBObjectStore = database.createObjectStore(
              IndexedDbPassportAnonymousDraftStoreService.DraftStoreName,
              { keyPath: 'id' }
            );
            store.createIndex('updatedAtUtc', 'updatedAtUtc');
          }
        };
        request.onsuccess = (): void => {
          const database: IDBDatabase = request.result;
          database.onversionchange = (): void => {
            database.close();
            this.databasePromise = null;
          };
          resolve(database);
        };
        request.onerror = (): void => {
          this.databasePromise = null;
          reject(request.error ?? new Error('passport-anonymous-draft.open-failed'));
        };
        request.onblocked = (): void => {
          this.databasePromise = null;
          reject(new Error('passport-anonymous-draft.open-blocked'));
        };
      });
    }

    return this.databasePromise;
  }

  private requireIdentifier(value: string): string {
    const normalizedValue: string = value.trim();
    if (!normalizedValue) {
      throw new Error('passport-anonymous-draft.identifier-required');
    }

    return normalizedValue;
  }
}
