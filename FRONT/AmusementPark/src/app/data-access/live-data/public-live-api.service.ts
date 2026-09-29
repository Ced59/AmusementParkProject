import { HttpClient, HttpErrorResponse, HttpHeaders, HttpResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, catchError, map, of, throwError } from 'rxjs';

import { anonymousHttpOptions } from '@core/http/auth/anonymous-http-options';
import { PublicLiveTarget, PublicParkLiveItems } from '@app/models/live-data/public-live.models';
import { environment } from '../../../environments/environment';

interface CachedPublicLiveResponse {
  readonly body: unknown;
  readonly entityTag: string | null;
}

@Injectable({
  providedIn: 'root'
})
export class PublicLiveApiService {
  private readonly responseCache = new Map<string, CachedPublicLiveResponse>();

  constructor(private readonly http: HttpClient) {
  }

  getPark(parkId: string): Observable<PublicLiveTarget> {
    return this.getConditional<PublicLiveTarget>(
      `${environment.apiBaseUrl}public/live/parks/${encodeURIComponent(parkId)}`
    );
  }

  getParkItem(itemId: string): Observable<PublicLiveTarget> {
    return this.getConditional<PublicLiveTarget>(
      `${environment.apiBaseUrl}public/live/items/${encodeURIComponent(itemId)}`
    );
  }

  getParkItems(parkId: string): Observable<PublicParkLiveItems> {
    return this.getConditional<PublicParkLiveItems>(
      `${environment.apiBaseUrl}public/live/parks/${encodeURIComponent(parkId)}/items`
    );
  }

  private getConditional<TResponse>(url: string): Observable<TResponse> {
    const cachedResponse: CachedPublicLiveResponse | undefined = this.responseCache.get(url);
    const headers: HttpHeaders = cachedResponse?.entityTag
      ? new HttpHeaders({ 'If-None-Match': cachedResponse.entityTag })
      : new HttpHeaders();

    return this.http.get<TResponse>(url, {
      ...anonymousHttpOptions(),
      headers,
      observe: 'response'
    }).pipe(
      map((response: HttpResponse<TResponse>) => {
        if (response.body === null) {
          throw new Error('The live-data response body is missing.');
        }

        this.responseCache.set(url, {
          body: response.body,
          entityTag: response.headers.get('ETag')
        });
        return response.body;
      }),
      catchError((error: unknown) => {
        if (error instanceof HttpErrorResponse && error.status === 304 && cachedResponse) {
          return of(cachedResponse.body as TResponse);
        }

        return throwError(() => error);
      })
    );
  }
}
