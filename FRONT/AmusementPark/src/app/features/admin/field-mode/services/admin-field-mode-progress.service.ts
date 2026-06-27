import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { catchError, map, Observable, of, tap } from 'rxjs';

import { environment } from '../../../../../environments/environment';
import { AdminFieldModeProcessedStatusPort } from '../state/admin-field-mode-data.ports';

interface AdminFieldModeProcessedItemsResponse {
  parkId: string;
  itemIds: string[];
}

interface AdminFieldModeProcessedItemResponse {
  parkId: string;
  itemId: string;
  isProcessed: boolean;
}

@Injectable({ providedIn: 'root' })
export class AdminFieldModeProgressService implements AdminFieldModeProcessedStatusPort {
  private readonly storagePrefix: string = 'admin.fieldMode.processedItemIds.';

  constructor(private readonly http: HttpClient) {
  }

  getProcessedItemIds(parkId: string): Observable<Set<string>> {
    const url: string = this.buildUrl(`admin/field-mode/parks/${encodeURIComponent(parkId)}/processed-items`);
    return this.http.get<AdminFieldModeProcessedItemsResponse>(url).pipe(
      map((response: AdminFieldModeProcessedItemsResponse) => new Set(response.itemIds ?? [])),
      tap((processedIds: Set<string>) => this.writeLocalProcessedItemIds(parkId, processedIds)),
      catchError(() => of(this.readLocalProcessedItemIds(parkId)))
    );
  }

  setProcessed(parkId: string, itemId: string, isProcessed: boolean): Observable<boolean> {
    const url: string = this.buildUrl(`admin/field-mode/parks/${encodeURIComponent(parkId)}/items/${encodeURIComponent(itemId)}/processed`);
    this.writeLocalProcessedItemId(parkId, itemId, isProcessed);
    return this.http.put<AdminFieldModeProcessedItemResponse>(url, { isProcessed }).pipe(
      map((response: AdminFieldModeProcessedItemResponse) => response.isProcessed),
      tap((savedProcessed: boolean) => this.writeLocalProcessedItemId(parkId, itemId, savedProcessed)),
      catchError(() => of(isProcessed))
    );
  }

  private buildUrl(path: string): string {
    const baseUrl: string = environment.apiBaseUrl.endsWith('/')
      ? environment.apiBaseUrl
      : `${environment.apiBaseUrl}/`;
    return `${baseUrl}${path.replace(/^\/+/, '')}`;
  }

  private readLocalProcessedItemIds(parkId: string): Set<string> {
    const storage: Storage | null = this.getStorage();
    if (!storage) {
      return new Set<string>();
    }

    try {
      const rawValue: string | null = storage.getItem(this.buildStorageKey(parkId));
      const values: unknown = rawValue ? JSON.parse(rawValue) : [];
      if (!Array.isArray(values)) {
        return new Set<string>();
      }

      return new Set(values.filter((value: unknown): value is string => typeof value === 'string' && value.trim().length > 0));
    } catch {
      return new Set<string>();
    }
  }

  private writeLocalProcessedItemIds(parkId: string, itemIds: Set<string>): void {
    const storage: Storage | null = this.getStorage();
    if (!storage) {
      return;
    }

    try {
      storage.setItem(this.buildStorageKey(parkId), JSON.stringify([...itemIds].sort()));
    } catch {
      // Local persistence is a field-mode fallback only.
    }
  }

  private writeLocalProcessedItemId(parkId: string, itemId: string, isProcessed: boolean): void {
    const itemIds: Set<string> = this.readLocalProcessedItemIds(parkId);
    if (isProcessed) {
      itemIds.add(itemId);
    } else {
      itemIds.delete(itemId);
    }

    this.writeLocalProcessedItemIds(parkId, itemIds);
  }

  private buildStorageKey(parkId: string): string {
    return `${this.storagePrefix}${parkId}`;
  }

  private getStorage(): Storage | null {
    try {
      return typeof globalThis !== 'undefined' ? globalThis.localStorage : null;
    } catch {
      return null;
    }
  }
}
