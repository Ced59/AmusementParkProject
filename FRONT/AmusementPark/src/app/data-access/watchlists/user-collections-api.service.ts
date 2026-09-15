import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  UserCollectionEntry,
  UserCollectionKind,
  UserCollectionTargetType
} from '@app/models/watchlists/user-collection-entry.model';
import { environment } from '../../../environments/environment';
import { USER_COLLECTIONS_API_ENDPOINTS } from './user-collections-api-endpoints';

@Injectable({ providedIn: 'root' })
export class UserCollectionsApiService {
  constructor(private readonly http: HttpClient) {
  }

  listMine(targetType?: UserCollectionTargetType, targetId?: string): Observable<UserCollectionEntry[]> {
    let params: HttpParams = new HttpParams();
    if (targetType) {
      params = params.set('targetType', targetType);
    }
    if (targetId) {
      params = params.set('targetId', targetId);
    }

    return this.http.get<UserCollectionEntry[]>(
      `${environment.apiBaseUrl}${USER_COLLECTIONS_API_ENDPOINTS.collection}`,
      { params }
    );
  }

  add(
    targetType: UserCollectionTargetType,
    targetId: string,
    kind: UserCollectionKind
  ): Observable<UserCollectionEntry> {
    return this.http.put<UserCollectionEntry>(
      `${environment.apiBaseUrl}${USER_COLLECTIONS_API_ENDPOINTS.entry(targetType, targetId, kind)}`,
      null
    );
  }

  delete(
    targetType: UserCollectionTargetType,
    targetId: string,
    kind: UserCollectionKind
  ): Observable<void> {
    return this.http.delete<void>(
      `${environment.apiBaseUrl}${USER_COLLECTIONS_API_ENDPOINTS.entry(targetType, targetId, kind)}`
    );
  }
}
