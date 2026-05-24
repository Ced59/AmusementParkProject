import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { AttractionAccessCondition } from '@app/models/parks/attraction-access-condition';
import { AttractionDetails } from '@app/models/parks/attraction-details';
import { AttractionLocationPoint } from '@app/models/parks/attraction-location-point';
import { AttractionLocations } from '@app/models/parks/attraction-locations';
import { ParkItemAdminRow } from '@app/models/parks/park-item-admin-row';
import { ParkItem } from '@app/models/parks/park-item';
import { LocalizedItem } from '@app/models/shared/localized-item';
import { ApiResponse } from '@app/models/shared/api_reponse';
import {
  normalizeParkItem,
  normalizeParkItemAdminRows,
  PagedCollectionResponse,
  unwrapCollection
} from '../shared/api-helpers';
import { PARK_ITEMS_API_ENDPOINTS, ParkItemAdminListFilters } from './park-items-api-endpoints';
import { BulkAdministrationUpdateRequest, BulkAdministrationUpdateResult } from '@app/models/admin/admin-review-status';

interface AttractionAccessConditionWriteRequest {
  type: AttractionAccessCondition['type'];
  isCustom?: boolean | null;
  value?: number | null;
  unit?: AttractionAccessCondition['unit'];
  requiresAccompaniment?: boolean | null;
  minimumCompanionAge?: number | null;
  label?: LocalizedItem<string>[] | null;
  description?: LocalizedItem<string>[] | null;
  displayOrder?: number | null;
}

interface AttractionDetailsWriteRequest {
  manufacturerId?: string | null;
  model?: string | null;
  externalSource?: string | null;
  externalId?: string | null;
  sourceUrl?: string | null;
  status?: string | null;
  materialType?: string | null;
  seatingType?: string | null;
  launchType?: string | null;
  restraintType?: string | null;
  isLaunched?: boolean | null;
  openingDate?: string | null;
  closingDate?: string | null;
  openingDateText?: string | null;
  closingDateText?: string | null;
  durationInSeconds?: number | null;
  capacityPerHour?: number | null;
  heightInFeet?: number | null;
  heightInMeters?: number | null;
  lengthInFeet?: number | null;
  lengthInMeters?: number | null;
  speedInMph?: number | null;
  speedInKmH?: number | null;
  dropInMeters?: number | null;
  inversionCount?: number | null;
  trainCount?: number | null;
  carsPerTrain?: number | null;
  ridersPerVehicle?: number | null;
  hasSingleRider?: boolean | null;
  hasFastPass?: boolean | null;
  isAccessibleForReducedMobility?: boolean | null;
  isIndoor?: boolean | null;
  waterExposureLevel?: AttractionDetails['waterExposureLevel'];
  accessConditions?: AttractionAccessConditionWriteRequest[] | null;
}

interface AttractionLocationPointWriteRequest {
  latitude: number;
  longitude: number;
}

interface AttractionLocationsWriteRequest {
  entrance?: AttractionLocationPointWriteRequest | null;
  exit?: AttractionLocationPointWriteRequest | null;
  fastPassEntrance?: AttractionLocationPointWriteRequest | null;
  reducedMobilityEntrance?: AttractionLocationPointWriteRequest | null;
}

interface ParkItemWriteRequest {
  parkId: string;
  zoneId?: string | null;
  name: string;
  category: ParkItem['category'];
  type: ParkItem['type'];
  subtype?: string | null;
  latitude: number;
  longitude: number;
  descriptions: LocalizedItem<string>[];
  attractionDetails?: AttractionDetailsWriteRequest | null;
  attractionLocations?: AttractionLocationsWriteRequest | null;
  isVisible: boolean;
  adminReviewStatus?: string | null;
}

@Injectable({
  providedIn: 'root'
})
export class ParkItemsApiService {
  constructor(private readonly http: HttpClient) {
  }

  getParkItemsByParkId(parkId: string): Observable<ParkItem[]> {
    const url: string = `${environment.apiBaseUrl}${PARK_ITEMS_API_ENDPOINTS.getParkItemsByParkId(parkId)}`;
    return this.http.get<ParkItem[] | PagedCollectionResponse<ParkItem>>(url).pipe(
      map((response: ParkItem[] | PagedCollectionResponse<ParkItem>) => unwrapCollection<ParkItem>(response).map((item: ParkItem) => normalizeParkItem(item)))
    );
  }

  getParkItemsPaginated(
    page: number,
    size: number,
    parkId?: string | null,
    search?: string | null,
    filters: ParkItemAdminListFilters | null = null
  ): Observable<ApiResponse<ParkItemAdminRow>> {
    const url: string = `${environment.apiBaseUrl}${PARK_ITEMS_API_ENDPOINTS.getParkItemsPaginated(page, size, parkId, search, filters)}`;
    return this.http.get<ApiResponse<ParkItemAdminRow>>(url).pipe(
      map((response: ApiResponse<ParkItemAdminRow>) => ({
        ...response,
        data: normalizeParkItemAdminRows(response.data)
      }))
    );
  }

  getParkItemById(id: string): Observable<ParkItem> {
    const url: string = `${environment.apiBaseUrl}${PARK_ITEMS_API_ENDPOINTS.getParkItemById(id)}`;
    return this.http.get<ParkItem>(url).pipe(
      map((item: ParkItem) => normalizeParkItem(item))
    );
  }

  createParkItem(item: ParkItem): Observable<ParkItem> {
    const url: string = `${environment.apiBaseUrl}${PARK_ITEMS_API_ENDPOINTS.createParkItem}`;
    return this.http.post<ParkItem>(url, this.mapParkItemToWriteRequest(item)).pipe(
      map((createdItem: ParkItem) => normalizeParkItem(createdItem))
    );
  }

  updateParkItem(id: string, item: ParkItem): Observable<ParkItem> {
    const url: string = `${environment.apiBaseUrl}${PARK_ITEMS_API_ENDPOINTS.updateParkItem(id)}`;
    return this.http.put<ParkItem>(url, this.mapParkItemToWriteRequest(item)).pipe(
      map((updatedItem: ParkItem) => normalizeParkItem(updatedItem))
    );
  }

  deleteParkItem(id: string): Observable<boolean> {
    const url: string = `${environment.apiBaseUrl}${PARK_ITEMS_API_ENDPOINTS.deleteParkItem(id)}`;
    return this.http.delete<boolean>(url);
  }

  updateParkItemsBulkAdministration(request: BulkAdministrationUpdateRequest): Observable<BulkAdministrationUpdateResult> {
    const url: string = `${environment.apiBaseUrl}${PARK_ITEMS_API_ENDPOINTS.updateParkItemsBulkAdministration}`;
    return this.http.patch<BulkAdministrationUpdateResult>(url, request);
  }

  private mapParkItemToWriteRequest(item: ParkItem): ParkItemWriteRequest {
    return {
      parkId: item.parkId,
      zoneId: item.zoneId ?? null,
      name: item.name,
      category: item.category,
      type: item.type,
      subtype: item.subtype ?? null,
      latitude: item.latitude,
      longitude: item.longitude,
      descriptions: item.descriptions ?? [],
      attractionDetails: this.mapAttractionDetails(item.attractionDetails),
      attractionLocations: this.mapAttractionLocations(item.attractionLocations),
      isVisible: item.isVisible ?? true,
      adminReviewStatus: item.adminReviewStatus ?? 'Validated'
    };
  }

  private mapAttractionDetails(details: AttractionDetails | null | undefined): AttractionDetailsWriteRequest | null {
    if (!details) {
      return null;
    }

    return {
      manufacturerId: details.manufacturerId ?? null,
      model: details.model ?? null,
      externalSource: details.externalSource ?? null,
      externalId: details.externalId ?? null,
      sourceUrl: details.sourceUrl ?? null,
      status: details.status ?? null,
      materialType: details.materialType ?? null,
      seatingType: details.seatingType ?? null,
      launchType: details.launchType ?? null,
      restraintType: details.restraintType ?? null,
      isLaunched: details.isLaunched ?? null,
      openingDate: details.openingDate ?? null,
      closingDate: details.closingDate ?? null,
      openingDateText: details.openingDateText ?? null,
      closingDateText: details.closingDateText ?? null,
      durationInSeconds: details.durationInSeconds ?? null,
      capacityPerHour: details.capacityPerHour ?? null,
      heightInFeet: details.heightInFeet ?? null,
      heightInMeters: details.heightInMeters ?? null,
      lengthInFeet: details.lengthInFeet ?? null,
      lengthInMeters: details.lengthInMeters ?? null,
      speedInMph: details.speedInMph ?? null,
      speedInKmH: details.speedInKmH ?? null,
      dropInMeters: details.dropInMeters ?? null,
      inversionCount: details.inversionCount ?? null,
      trainCount: details.trainCount ?? null,
      carsPerTrain: details.carsPerTrain ?? null,
      ridersPerVehicle: details.ridersPerVehicle ?? null,
      hasSingleRider: details.hasSingleRider ?? null,
      hasFastPass: details.hasFastPass ?? null,
      isAccessibleForReducedMobility: details.isAccessibleForReducedMobility ?? null,
      isIndoor: details.isIndoor ?? null,
      waterExposureLevel: details.waterExposureLevel ?? null,
      accessConditions: details.accessConditions?.map((condition: AttractionAccessCondition) => ({
        type: condition.type,
        isCustom: condition.isCustom ?? null,
        value: condition.value ?? null,
        unit: condition.unit ?? null,
        requiresAccompaniment: condition.requiresAccompaniment ?? null,
        minimumCompanionAge: condition.minimumCompanionAge ?? null,
        label: condition.label ?? null,
        description: condition.description ?? null,
        displayOrder: condition.displayOrder ?? null
      })) ?? null
    };
  }

  private mapAttractionLocations(locations: AttractionLocations | null | undefined): AttractionLocationsWriteRequest | null {
    if (!locations) {
      return null;
    }

    return {
      entrance: this.mapLocationPoint(locations.entrance),
      exit: this.mapLocationPoint(locations.exit),
      fastPassEntrance: this.mapLocationPoint(locations.fastPassEntrance),
      reducedMobilityEntrance: this.mapLocationPoint(locations.reducedMobilityEntrance)
    };
  }
  
  private mapLocationPoint(point: AttractionLocationPoint | null | undefined): AttractionLocationPointWriteRequest | null {
    if (!point || point.latitude == null || point.longitude == null) {
      return null;
    }

    return {
      latitude: point.latitude,
      longitude: point.longitude
    };
  }
}
