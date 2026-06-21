import { HttpClient, HttpResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { ContextualBlockExportDocument } from '@shared/models/admin/contextual-block-export.models';
import { ContextualBlockPreviewResult } from '@shared/models/admin/contextual-block-preview.models';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ContextualBlocksApiService {
  constructor(private readonly http: HttpClient) {
  }

  downloadBlockExport(blockType: string, entityId: string): Observable<HttpResponse<Blob>> {
    const url: string = `${environment.apiBaseUrl}admin/contextual-blocks/${encodeURIComponent(blockType)}/${encodeURIComponent(entityId)}/export`;
    return this.http.get(url, {
      observe: 'response',
      responseType: 'blob'
    });
  }

  getBlockExportDocument<TBlock = unknown>(blockType: string, entityId: string): Observable<ContextualBlockExportDocument<TBlock>> {
    const url: string = `${environment.apiBaseUrl}admin/contextual-blocks/${encodeURIComponent(blockType)}/${encodeURIComponent(entityId)}/export`;
    return this.http.get<ContextualBlockExportDocument<TBlock>>(url);
  }

  previewBlock(blockType: string, entityId: string, document: unknown): Observable<ContextualBlockPreviewResult> {
    const url: string = `${environment.apiBaseUrl}admin/contextual-blocks/${encodeURIComponent(blockType)}/${encodeURIComponent(entityId)}/preview`;
    return this.http.post<ContextualBlockPreviewResult>(url, { document });
  }

  applyBlock(blockType: string, entityId: string, document: unknown): Observable<ContextualBlockPreviewResult> {
    const url: string = `${environment.apiBaseUrl}admin/contextual-blocks/${encodeURIComponent(blockType)}/${encodeURIComponent(entityId)}/apply`;
    return this.http.post<ContextualBlockPreviewResult>(url, { document });
  }
}
