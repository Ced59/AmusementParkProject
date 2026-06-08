import { HttpClient, HttpContext } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { SearchApiResponse } from '@app/models/search/search-api-response';
import { SEARCH_API_ENDPOINTS } from './search-api-endpoints';

interface SearchHttpOptions {
  context?: HttpContext;
}

@Injectable({
  providedIn: 'root'
})
export class SearchApiService {
  constructor(private readonly http: HttpClient) {
  }

  getSearch(query: string, categories: string[], page: number, size: number, options: SearchHttpOptions = {}): Observable<SearchApiResponse> {
    const url: string = `${environment.apiBaseUrl}${SEARCH_API_ENDPOINTS.getSearch(query, categories, page, size)}`;
    return this.http.get<SearchApiResponse>(url, options);
  }
}
