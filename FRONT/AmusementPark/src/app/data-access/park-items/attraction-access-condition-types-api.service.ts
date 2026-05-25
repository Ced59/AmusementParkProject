import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  AttractionAccessConditionTypeDefinition,
  UpsertAttractionAccessConditionTypeDefinitionRequest
} from '@app/models/parks/attraction-access-condition-type-definition';

@Injectable({ providedIn: 'root' })
export class AttractionAccessConditionTypesApiService {
  constructor(private readonly http: HttpClient) {
  }

  getAll(): Observable<AttractionAccessConditionTypeDefinition[]> {
    return this.http.get<AttractionAccessConditionTypeDefinition[]>(`${environment.apiBaseUrl}attraction-access-condition-types`);
  }

  upsert(request: UpsertAttractionAccessConditionTypeDefinitionRequest): Observable<AttractionAccessConditionTypeDefinition> {
    return this.http.post<AttractionAccessConditionTypeDefinition>(`${environment.apiBaseUrl}attraction-access-condition-types`, request);
  }
}
