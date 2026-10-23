import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { NearbyDestination, NearbyDestinationKind } from '@app/models/parks/nearby-destination';
import { AnonymousHttpOptions } from '@core/http/auth/anonymous-http-options';

@Injectable({ providedIn: 'root' })
export class NearbyDestinationsApiService {
  private readonly http = inject(HttpClient);

  getNearby(sourceKind: NearbyDestinationKind, sourceId: string, limit: number, options: AnonymousHttpOptions): Observable<NearbyDestination[]> {
    const url = `${environment.apiBaseUrl}nearby-destinations/${sourceKind}/${encodeURIComponent(sourceId)}`;
    return this.http.get<NearbyDestination[]>(url, { ...options, params: new HttpParams().set('limit', limit) });
  }
}
