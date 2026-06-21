import { HttpClient, HttpResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

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
}
