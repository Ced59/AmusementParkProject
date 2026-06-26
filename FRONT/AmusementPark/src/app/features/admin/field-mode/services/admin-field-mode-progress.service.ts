import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';

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
  constructor(private readonly http: HttpClient) {
  }

  getProcessedItemIds(parkId: string): Observable<Set<string>> {
    const url: string = this.buildUrl(`admin/field-mode/parks/${encodeURIComponent(parkId)}/processed-items`);
    return this.http.get<AdminFieldModeProcessedItemsResponse>(url).pipe(
      map((response: AdminFieldModeProcessedItemsResponse) => new Set(response.itemIds ?? []))
    );
  }

  setProcessed(parkId: string, itemId: string, isProcessed: boolean): Observable<boolean> {
    const url: string = this.buildUrl(`admin/field-mode/parks/${encodeURIComponent(parkId)}/items/${encodeURIComponent(itemId)}/processed`);
    return this.http.put<AdminFieldModeProcessedItemResponse>(url, { isProcessed }).pipe(
      map((response: AdminFieldModeProcessedItemResponse) => response.isProcessed)
    );
  }

  private buildUrl(path: string): string {
    const baseUrl: string = environment.apiBaseUrl.endsWith('/')
      ? environment.apiBaseUrl
      : `${environment.apiBaseUrl}/`;
    return `${baseUrl}${path.replace(/^\/+/, '')}`;
  }
}
