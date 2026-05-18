import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { ParkExplorer } from '@app/models/parks/park-explorer';
import { Park } from '@app/models/parks/park';
import { ParkMapPoint } from '@app/models/parks/park-map-point';
import { ParksApiResponse } from '@app/models/parks/parks_api_response';
import { LocalizedItem } from '@app/models/shared/localized-item';
import { PagedCollectionResponse, unwrapCollection } from '../shared/api-helpers';
import { ParkRegionFilter } from '@shared/models/geo/world-region-filter.model';
import { PARKS_API_ENDPOINTS } from './parks-api-endpoints';

interface ParkWriteRequest {
  name?: string;
  countryCode?: string | null;
  type?: Park['type'] | null;
  founderId?: string | null;
  operatorId?: string | null;
  latitude: number;
  longitude: number;
  descriptions: LocalizedItem<string>[];
  isVisible: boolean;
  isFeaturedOnHome: boolean;
  featuredHomeOrder: number | null;
  isFeaturedOnHomeSponsored: boolean;
  websiteUrl?: string | null;
  street?: string | null;
  city?: string | null;
  postalCode?: string | null;
}

@Injectable({
  providedIn: 'root'
})
export class ParksApiService {
  private readonly jsonHttpOptions = {
    headers: new HttpHeaders({
      'Content-Type': 'application/json'
    })
  };

  constructor(private readonly http: HttpClient) {
  }

  getParksPaginated(page: number, size: number, visibleOnly: boolean = false, region: ParkRegionFilter | null = null): Observable<ParksApiResponse> {
    const url: string = `${environment.apiBaseUrl}${PARKS_API_ENDPOINTS.getParksPaginated(page, size, visibleOnly, region)}`;
    return this.http.get<ParksApiResponse>(url);
  }

  getRandomVisibleParks(limit: number = 4): Observable<Park[]> {
    const url: string = `${environment.apiBaseUrl}${PARKS_API_ENDPOINTS.getRandomVisibleParks(limit)}`;
    return this.http.get<Park[]>(url);
  }

  getVisibleParkMapPoints(query: string | null = null, region: ParkRegionFilter | null = null): Observable<ParkMapPoint[]> {
    const normalizedQuery: string | null = query?.trim() || null;
    const url: string = `${environment.apiBaseUrl}${PARKS_API_ENDPOINTS.getVisibleParkMapPoints(normalizedQuery, region)}`;
    return this.http.get<ParkMapPoint[]>(url);
  }

  getParkById(id: string): Observable<Park> {
    const url: string = `${environment.apiBaseUrl}${PARKS_API_ENDPOINTS.getParkById(id)}`;
    return this.http.get<Park>(url);
  }

  searchParks(query: string, page: number, size: number, visibleOnly: boolean = false, region: ParkRegionFilter | null = null): Observable<ParksApiResponse> {
    const url: string = `${environment.apiBaseUrl}${PARKS_API_ENDPOINTS.searchParks(query, page, size, visibleOnly, region)}`;
    return this.http.get<ParksApiResponse>(url);
  }

  getParksByLocation(latitude: number, longitude: number, radius: number): Observable<Park[]> {
    const url: string = `${environment.apiBaseUrl}${PARKS_API_ENDPOINTS.getParksByLocation(latitude, longitude, radius)}`;
    return this.http.get<Park[] | PagedCollectionResponse<Park>>(url).pipe(
      map((response: Park[] | PagedCollectionResponse<Park>) => unwrapCollection<Park>(response))
    );
  }

  updateParkVisibility(parkId: string, isVisible: boolean): Observable<Park> {
    const url: string = `${environment.apiBaseUrl}${PARKS_API_ENDPOINTS.updateParkVisibility(parkId)}`;
    return this.http.patch<Park>(url, { isVisible });
  }

  createPark(park: Park): Observable<Park> {
    const url: string = `${environment.apiBaseUrl}${PARKS_API_ENDPOINTS.createPark}`;
    return this.http.post<Park>(url, this.mapParkToWriteRequest(park), this.jsonHttpOptions);
  }

  updatePark(id: string, park: Park): Observable<Park> {
    const url: string = `${environment.apiBaseUrl}${PARKS_API_ENDPOINTS.updatePark(id)}`;
    return this.http.put<Park>(url, this.mapParkToWriteRequest(park), this.jsonHttpOptions);
  }

  getParkExplorer(parkId: string): Observable<ParkExplorer> {
    const url: string = `${environment.apiBaseUrl}${PARKS_API_ENDPOINTS.getParkExplorer(parkId)}`;
    return this.http.get<ParkExplorer>(url);
  }

  private mapParkToWriteRequest(park: Park): ParkWriteRequest {
    return {
      name: park.name,
      countryCode: park.countryCode ?? null,
      type: park.type ?? null,
      founderId: park.founderId ?? null,
      operatorId: park.operatorId ?? null,
      latitude: park.latitude,
      longitude: park.longitude,
      descriptions: park.descriptions ?? [],
      isVisible: park.isVisible ?? true,
      isFeaturedOnHome: park.isFeaturedOnHome ?? false,
      featuredHomeOrder: this.normalizeFeaturedHomeOrder(park.featuredHomeOrder),
      isFeaturedOnHomeSponsored: Boolean(park.isFeaturedOnHome) && Boolean(park.isFeaturedOnHomeSponsored),
      websiteUrl: park.webSiteUrl ?? null,
      street: park.street ?? null,
      city: park.city ?? null,
      postalCode: park.postalCode ?? null
    };
  }

  private normalizeFeaturedHomeOrder(value: number | null | undefined): number | null {
    if (value === null || value === undefined) {
      return null;
    }

    const normalizedValue: number = Number(value);
    return Number.isFinite(normalizedValue) && normalizedValue > 0 ? normalizedValue : null;
  }
}
