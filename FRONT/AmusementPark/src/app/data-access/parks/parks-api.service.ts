import { HttpClient, HttpContext, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { ParkExplorer } from '@app/models/parks/park-explorer';
import { Park } from '@app/models/parks/park';
import { ParkMapPoint } from '@app/models/parks/park-map-point';
import { ParkDistanceResponse } from '@app/models/parks/park-distance';
import { ParksApiResponse } from '@app/models/parks/parks_api_response';
import { LocalizedItem } from '@app/models/shared/localized-item';
import { PagedCollectionResponse, unwrapCollection } from '../shared/api-helpers';
import { ParkRegionFilter } from '@shared/models/geo/world-region-filter.model';
import { PARKS_API_ENDPOINTS, ParkAdminListFilters } from './parks-api-endpoints';
import { BulkAdministrationUpdateRequest, BulkAdministrationUpdateResult } from '@app/models/admin/admin-review-status';

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
  adminReviewStatus?: string | null;
  isFeaturedOnHome: boolean;
  featuredHomeOrder: number | null;
  isFeaturedOnHomeSponsored: boolean;
  websiteUrl?: string | null;
  street?: string | null;
  city?: string | null;
  postalCode?: string | null;
}

interface ParksHttpOptions {
  context?: HttpContext;
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

  getParksPaginated(page: number, size: number, visibleOnly: boolean = false, region: ParkRegionFilter | null = null, filters: ParkAdminListFilters | null = null, options: ParksHttpOptions = {}): Observable<ParksApiResponse> {
    const url: string = `${environment.apiBaseUrl}${PARKS_API_ENDPOINTS.getParksPaginated(page, size, visibleOnly, region, filters)}`;
    return this.http.get<ParksApiResponse>(url, options);
  }

  getRandomVisibleParks(limit: number = 4, options: ParksHttpOptions = {}): Observable<Park[]> {
    const url: string = `${environment.apiBaseUrl}${PARKS_API_ENDPOINTS.getRandomVisibleParks(limit)}`;
    return this.http.get<Park[]>(url, options);
  }

  getVisibleParkMapPoints(query: string | null = null, region: ParkRegionFilter | null = null, options: ParksHttpOptions = {}): Observable<ParkMapPoint[]> {
    const normalizedQuery: string | null = query?.trim() || null;
    const url: string = `${environment.apiBaseUrl}${PARKS_API_ENDPOINTS.getVisibleParkMapPoints(normalizedQuery, region)}`;
    return this.http.get<ParkMapPoint[]>(url, options);
  }

  getParkById(id: string, options: ParksHttpOptions = {}): Observable<Park> {
    const url: string = `${environment.apiBaseUrl}${PARKS_API_ENDPOINTS.getParkById(id)}`;
    return this.http.get<Park>(url, options);
  }

  searchParks(query: string, page: number, size: number, visibleOnly: boolean = false, region: ParkRegionFilter | null = null, filters: ParkAdminListFilters | null = null, options: ParksHttpOptions = {}): Observable<ParksApiResponse> {
    const url: string = `${environment.apiBaseUrl}${PARKS_API_ENDPOINTS.searchParks(query, page, size, visibleOnly, region, filters)}`;
    return this.http.get<ParksApiResponse>(url, options);
  }

  getParkDistances(sourceParkId: string, targetParkIds: string[], options: ParksHttpOptions = {}): Observable<ParkDistanceResponse> {
    const url: string = `${environment.apiBaseUrl}${PARKS_API_ENDPOINTS.getParkDistances(sourceParkId, targetParkIds)}`;
    return this.http.get<ParkDistanceResponse>(url, options);
  }

  getNearestParks(sourceParkId: string, limit: number = 4, maxDistanceKilometers: number | null = null, options: ParksHttpOptions = {}): Observable<ParkDistanceResponse> {
    const url: string = `${environment.apiBaseUrl}${PARKS_API_ENDPOINTS.getNearestParks(sourceParkId, limit, maxDistanceKilometers)}`;
    return this.http.get<ParkDistanceResponse>(url, options);
  }

  getParksByLocation(latitude: number, longitude: number, radiusMeters: number, options: ParksHttpOptions = {}): Observable<Park[]> {
    const url: string = `${environment.apiBaseUrl}${PARKS_API_ENDPOINTS.getParksByLocation(latitude, longitude, radiusMeters)}`;
    return this.http.get<Park[] | PagedCollectionResponse<Park>>(url, options).pipe(
      map((response: Park[] | PagedCollectionResponse<Park>) => unwrapCollection<Park>(response))
    );
  }

  updateParkVisibility(parkId: string, isVisible: boolean): Observable<Park> {
    const url: string = `${environment.apiBaseUrl}${PARKS_API_ENDPOINTS.updateParkVisibility(parkId)}`;
    return this.http.patch<Park>(url, { isVisible });
  }

  updateParksBulkAdministration(request: BulkAdministrationUpdateRequest): Observable<BulkAdministrationUpdateResult> {
    const url: string = `${environment.apiBaseUrl}${PARKS_API_ENDPOINTS.updateParksBulkAdministration}`;
    return this.http.patch<BulkAdministrationUpdateResult>(url, request, this.jsonHttpOptions);
  }

  createPark(park: Park): Observable<Park> {
    const url: string = `${environment.apiBaseUrl}${PARKS_API_ENDPOINTS.createPark}`;
    return this.http.post<Park>(url, this.mapParkToWriteRequest(park), this.jsonHttpOptions);
  }

  updatePark(id: string, park: Park): Observable<Park> {
    const url: string = `${environment.apiBaseUrl}${PARKS_API_ENDPOINTS.updatePark(id)}`;
    return this.http.put<Park>(url, this.mapParkToWriteRequest(park), this.jsonHttpOptions);
  }

  getParkExplorer(parkId: string, options: ParksHttpOptions = {}): Observable<ParkExplorer> {
    const url: string = `${environment.apiBaseUrl}${PARKS_API_ENDPOINTS.getParkExplorer(parkId)}`;
    return this.http.get<ParkExplorer>(url, options);
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
      adminReviewStatus: park.adminReviewStatus ?? 'Validated',
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
