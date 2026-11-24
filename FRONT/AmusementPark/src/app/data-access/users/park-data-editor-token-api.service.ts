import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { CreatedParkDataEditorTokenResponse } from '@app/models/users/created-park-data-editor-token-response';
import { ParkDataEditorToken } from '@app/models/users/user-admin-responses';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ParkDataEditorTokenApiService {
  private readonly url: string = `${environment.apiBaseUrl}park-data-editor/tokens`;

  constructor(private readonly http: HttpClient) {
  }

  createToken(): Observable<CreatedParkDataEditorTokenResponse> {
    return this.http.post<CreatedParkDataEditorTokenResponse>(this.url, {
      label: 'Codex cloud editor',
      expiresInDays: 30
    });
  }

  listTokens(): Observable<ParkDataEditorToken[]> {
    return this.http.get<ParkDataEditorToken[]>(this.url);
  }

  revoke(tokenId: string): Observable<void> {
    return this.http.delete<void>(`${this.url}/${encodeURIComponent(tokenId)}`);
  }
}
