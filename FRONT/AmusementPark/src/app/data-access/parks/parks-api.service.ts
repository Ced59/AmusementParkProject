import { HttpClient, HttpContext, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { finalize, map, Observable, shareReplay } from 'rxjs';

import { environment } from '../../../environments/environment';
import { ParkExplorer } from '@app/models/parks/park-explorer';
import { Park } from '@app/models/parks/park';
import { ParkDetailSummary } from '@app/models/parks/park-detail-summary';
import { ParkWeatherForecast, ParkWeatherHistoricalComparisons } from '@app/models/parks/park-weather';
import { ParkOpeningHoursCalendar, ParkOpeningHoursSchedule } from '@app/models/parks/park-opening-hours';
import { ParkMapItems } from '@app/models/parks/park-map-items';
import { ParkMapPoint } from '@app/models/parks/park-map-point';
import { ParkDistanceResponse } from '@app/models/parks/park-distance';
import { ParksApiResponse } from '@app/models/parks/parks_api_response';
import { LocalizedItem } from '@app/models/shared/localized-item';
import { PagedCollectionResponse, unwrapCollection } from '../shared/api-helpers';
import { ParkRegionFilter } from '@shared/models/geo/world-region-filter.model';
import { PARKS_API_ENDPOINTS, ParkAdminListFilters, ParkAdminListSort } from './parks-api-endpoints';
import { BulkAdministrationUpdateRequest, BulkAdministrationUpdateResult } from '@app/models/admin/admin-review-status';
import { ClosedEntityFilter } from '@app/models/shared/closed-entity-filter';

interface ParkWriteRequest {
  name?: string;
  countryCode?: string | null;
  type?: Park['type'] | null;
  status?: Park['status'] | null;
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
  closedFilter?: ClosedEntityFilter;
  sort?: ParkAdminListSort;
}

@Injectable({
  providedIn: 'root'
})
export class ParksApiService {
  private readonly parkDetailSummaryRequests = new Map<string, Observable<ParkDetailSummary>>();
  private readonly nearestParkRequests = new Map<string, Observable<ParkDistanceResponse>>();

  private readonly jsonHttpOptions = {
    headers: new HttpHeaders({
      'Content-Type': 'application/json'
    })
  };

  constructor(private readonly http: HttpClient) {
  }

  getParksPaginated(page: number, size: number, visibleOnly: boolean = false, region: ParkRegionFilter | null = null, filters: ParkAdminListFilters | null = null, options: ParksHttpOptions = {}): Observable<ParksApiResponse> {
    const url: string = `${environment.apiBaseUrl}${PARKS_API_ENDPOINTS.getParksPaginated(page, size, visibleOnly, region, filters, options.sort ?? null, options.closedFilter)}`;
    return this.http.get<ParksApiResponse>(url, options);
  }

  getRandomVisibleParks(limit: number = 4, options: ParksHttpOptions = {}): Observable<Park[]> {
    const url: string = `${environment.apiBaseUrl}${PARKS_API_ENDPOINTS.getRandomVisibleParks(limit)}`;
    return this.http.get<Park[]>(url, options);
  }

  getVisibleParkMapPoints(query: string | null = null, region: ParkRegionFilter | null = null, options: ParksHttpOptions = {}): Observable<ParkMapPoint[]> {
    const normalizedQuery: string | null = query?.trim() || null;
    const url: string = `${environment.apiBaseUrl}${PARKS_API_ENDPOINTS.getVisibleParkMapPoints(normalizedQuery, region, options.closedFilter)}`;
    return this.http.get<ParkMapPoint[]>(url, options);
  }

  getParkById(id: string, options: ParksHttpOptions = {}): Observable<Park> {
    const url: string = `${environment.apiBaseUrl}${PARKS_API_ENDPOINTS.getParkById(id)}`;
    return this.http.get<Park>(url, options);
  }

  getParkWeather(id: string, days: number = 7, options: ParksHttpOptions = {}): Observable<ParkWeatherForecast> {
    const url: string = `${environment.apiBaseUrl}${PARKS_API_ENDPOINTS.getParkWeather(id, days)}`;
    return this.http.get<ParkWeatherForecast>(url, options);
  }

  getParkWeatherHistoricalComparisons(id: string, days: number = 7, years: number = 10, options: ParksHttpOptions = {}): Observable<ParkWeatherHistoricalComparisons> {
    const url: string = `${environment.apiBaseUrl}${PARKS_API_ENDPOINTS.getParkWeatherHistoricalComparisons(id, days, years)}`;
    return this.http.get<ParkWeatherHistoricalComparisons>(url, options);
  }

  getParkOpeningHours(id: string, from?: string | null, to?: string | null, options: ParksHttpOptions = {}): Observable<ParkOpeningHoursCalendar> {
    const url: string = `${environment.apiBaseUrl}${PARKS_API_ENDPOINTS.getParkOpeningHours(id, from, to)}`;
    return this.http.get<ParkOpeningHoursCalendar>(url, options);
  }

  getAdminParkOpeningHours(id: string): Observable<ParkOpeningHoursSchedule> {
    const url: string = `${environment.apiBaseUrl}${PARKS_API_ENDPOINTS.getAdminParkOpeningHours(id)}`;
    return this.http.get<ParkOpeningHoursSchedule>(url);
  }

  upsertAdminParkOpeningHours(id: string, schedule: ParkOpeningHoursSchedule): Observable<ParkOpeningHoursSchedule> {
    const url: string = `${environment.apiBaseUrl}${PARKS_API_ENDPOINTS.upsertAdminParkOpeningHours(id)}`;
    return this.http.put<ParkOpeningHoursSchedule>(url, schedule, this.jsonHttpOptions);
  }

  getParkDetailSummary(id: string, options: ParksHttpOptions = {}): Observable<ParkDetailSummary> {
    const url: string = `${environment.apiBaseUrl}${PARKS_API_ENDPOINTS.getParkDetailSummary(id, options.closedFilter)}`;
    const pendingRequest: Observable<ParkDetailSummary> | undefined = this.parkDetailSummaryRequests.get(url);

    if (pendingRequest) {
      return pendingRequest;
    }

    const request: Observable<ParkDetailSummary> = this.http.get<ParkDetailSummary>(url, options).pipe(
      finalize((): void => {
        this.parkDetailSummaryRequests.delete(url);
      }),
      shareReplay({ bufferSize: 1, refCount: false })
    );

    this.parkDetailSummaryRequests.set(url, request);
    return request;
  }

  getParkMapItems(id: string, options: ParksHttpOptions = {}): Observable<ParkMapItems> {
    const url: string = `${environment.apiBaseUrl}${PARKS_API_ENDPOINTS.getParkMapItems(id, options.closedFilter)}`;
    return this.http.get<ParkMapItems>(url, options);
  }

  searchParks(query: string, page: number, size: number, visibleOnly: boolean = false, region: ParkRegionFilter | null = null, filters: ParkAdminListFilters | null = null, options: ParksHttpOptions = {}): Observable<ParksApiResponse> {
    const url: string = `${environment.apiBaseUrl}${PARKS_API_ENDPOINTS.searchParks(query, page, size, visibleOnly, region, filters, options.sort ?? null, options.closedFilter)}`;
    return this.http.get<ParksApiResponse>(url, options);
  }

  getParkDistances(sourceParkId: string, targetParkIds: string[], options: ParksHttpOptions = {}): Observable<ParkDistanceResponse> {
    const url: string = `${environment.apiBaseUrl}${PARKS_API_ENDPOINTS.getParkDistances(sourceParkId, targetParkIds)}`;
    return this.http.get<ParkDistanceResponse>(url, options);
  }

  getNearestParks(sourceParkId: string, limit: number = 4, maxDistanceKilometers: number | null = null, options: ParksHttpOptions = {}): Observable<ParkDistanceResponse> {
    const url: string = `${environment.apiBaseUrl}${PARKS_API_ENDPOINTS.getNearestParks(sourceParkId, limit, maxDistanceKilometers)}`;
    const pendingRequest: Observable<ParkDistanceResponse> | undefined = this.nearestParkRequests.get(url);

    if (pendingRequest) {
      return pendingRequest;
    }

    const request: Observable<ParkDistanceResponse> = this.http.get<ParkDistanceResponse>(url, options).pipe(
      finalize((): void => {
        this.nearestParkRequests.delete(url);
      }),
      shareReplay({ bufferSize: 1, refCount: false })
    );

    this.nearestParkRequests.set(url, request);
    return request;
  }

  getParksByLocation(latitude: number, longitude: number, radiusMeters: number, options: ParksHttpOptions = {}): Observable<Park[]> {
    const url: string = `${environment.apiBaseUrl}${PARKS_API_ENDPOINTS.getParksByLocation(latitude, longitude, radiusMeters, options.closedFilter)}`;
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
    const url: string = `${environment.apiBaseUrl}${PARKS_API_ENDPOINTS.getParkExplorer(parkId, options.closedFilter)}`;
    return this.http.get<ParkExplorer>(url, options);
  }

  private mapParkToWriteRequest(park: Park): ParkWriteRequest {
    return {
      name: park.name,
      countryCode: park.countryCode ?? null,
      type: park.type ?? null,
      status: park.status ?? 'Operating',
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
