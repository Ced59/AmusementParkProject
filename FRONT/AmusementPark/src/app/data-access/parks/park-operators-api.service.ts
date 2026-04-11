import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { ParkOperator } from '../../models/parks/park-operator';
import { PagedCollectionResponse, unwrapCollection } from '../shared/api-helpers';
import { PARK_OPERATORS_API_ENDPOINTS } from './park-operators-api-endpoints';

@Injectable({
  providedIn: 'root'
})
export class ParkOperatorsApiService {
  private readonly jsonHttpOptions = {
    headers: new HttpHeaders({
      'Content-Type': 'application/json'
    })
  };

  constructor(private readonly http: HttpClient) {
  }

  getParkOperators(): Observable<ParkOperator[]> {
    const url: string = `${environment.apiBaseUrl}${PARK_OPERATORS_API_ENDPOINTS.getParkOperators}`;
    return this.http.get<ParkOperator[] | PagedCollectionResponse<ParkOperator>>(url).pipe(
      map((response: ParkOperator[] | PagedCollectionResponse<ParkOperator>) => unwrapCollection<ParkOperator>(response))
    );
  }

  getParkOperatorById(id: string): Observable<ParkOperator> {
    const url: string = `${environment.apiBaseUrl}${PARK_OPERATORS_API_ENDPOINTS.getParkOperatorById(id)}`;
    return this.http.get<ParkOperator>(url);
  }

  createParkOperator(parkOperator: ParkOperator): Observable<ParkOperator> {
    const url: string = `${environment.apiBaseUrl}${PARK_OPERATORS_API_ENDPOINTS.createParkOperator}`;
    return this.http.post<ParkOperator>(url, JSON.stringify(parkOperator), this.jsonHttpOptions);
  }

  updateParkOperator(id: string, parkOperator: ParkOperator): Observable<ParkOperator> {
    const url: string = `${environment.apiBaseUrl}${PARK_OPERATORS_API_ENDPOINTS.updateParkOperator(id)}`;
    return this.http.put<ParkOperator>(url, JSON.stringify(parkOperator), this.jsonHttpOptions);
  }
}
