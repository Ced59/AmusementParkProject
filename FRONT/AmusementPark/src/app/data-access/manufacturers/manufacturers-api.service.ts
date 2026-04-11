import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { AttractionManufacturer } from '../../models/parks/attraction-manufacturer';
import { PagedCollectionResponse, unwrapCollection } from '../shared/api-helpers';
import { MANUFACTURERS_API_ENDPOINTS } from './manufacturers-api-endpoints';

@Injectable({
  providedIn: 'root'
})
export class ManufacturersApiService {
  private readonly jsonHttpOptions = {
    headers: new HttpHeaders({
      'Content-Type': 'application/json'
    })
  };

  constructor(private readonly http: HttpClient) {
  }

  getAttractionManufacturers(): Observable<AttractionManufacturer[]> {
    const url: string = `${environment.apiBaseUrl}${MANUFACTURERS_API_ENDPOINTS.getAttractionManufacturers}`;
    return this.http.get<AttractionManufacturer[] | PagedCollectionResponse<AttractionManufacturer>>(url).pipe(
      map((response: AttractionManufacturer[] | PagedCollectionResponse<AttractionManufacturer>) => unwrapCollection<AttractionManufacturer>(response))
    );
  }

  getAttractionManufacturerById(id: string): Observable<AttractionManufacturer> {
    const url: string = `${environment.apiBaseUrl}${MANUFACTURERS_API_ENDPOINTS.getAttractionManufacturerById(id)}`;
    return this.http.get<AttractionManufacturer>(url);
  }

  createAttractionManufacturer(manufacturer: AttractionManufacturer): Observable<AttractionManufacturer> {
    const url: string = `${environment.apiBaseUrl}${MANUFACTURERS_API_ENDPOINTS.createAttractionManufacturer}`;
    return this.http.post<AttractionManufacturer>(url, JSON.stringify(manufacturer), this.jsonHttpOptions);
  }

  updateAttractionManufacturer(id: string, manufacturer: AttractionManufacturer): Observable<AttractionManufacturer> {
    const url: string = `${environment.apiBaseUrl}${MANUFACTURERS_API_ENDPOINTS.updateAttractionManufacturer(id)}`;
    return this.http.put<AttractionManufacturer>(url, JSON.stringify(manufacturer), this.jsonHttpOptions);
  }
}
