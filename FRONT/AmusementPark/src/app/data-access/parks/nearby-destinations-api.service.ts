import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map, Observable } from 'rxjs';
import { NearbyDestinationCardResponse } from '@app/models/parks/nearby-destination-card-response';
import { mapNearbyDestinationResponse } from './nearby-destination-response.mapper';
import { environment } from '../../../environments/environment';
import { NearbyDestination, NearbyDestinationKind } from '@app/models/parks/nearby-destination';
import { AnonymousHttpOptions } from '@core/http/auth/anonymous-http-options';

@Injectable({ providedIn: 'root' })
export class NearbyDestinationsApiService {
  private readonly http = inject(HttpClient);

  getNearby(sourceKind: NearbyDestinationKind, sourceId: string, limit: number, options: AnonymousHttpOptions): Observable<NearbyDestination[]> {
    const url = `${environment.apiBaseUrl}nearby-destinations/${sourceKind}/${encodeURIComponent(sourceId)}`;
    return this.http.get<(NearbyDestinationCardResponse | NearbyDestination)[]>(url, {
      ...options, params: new HttpParams().set('limit', limit).set('cardView', 'true')
    }).pipe(map(responses => responses.map(mapNearbyDestinationResponse)));
  }
}
