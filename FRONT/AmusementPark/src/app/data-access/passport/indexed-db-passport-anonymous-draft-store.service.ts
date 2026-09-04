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

    await this.execute<IDBValidKey>(
      'readwrite',
      (store: IDBObjectStore): IDBRequest<IDBValidKey> => store.put(draft)
    );
  }

  async delete(draftId: string): Promise<void> {
    const normalizedDraftId: string = this.requireIdentifier(draftId);
    await this.execute<undefined>(
      'readwrite',
      (store: IDBObjectStore): IDBRequest<undefined> => store.delete(normalizedDraftId)
    );
  }

  async clear(): Promise<void> {
    await this.execute<undefined>(
      'readwrite',
      (store: IDBObjectStore): IDBRequest<undefined> => store.clear()
    );
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
