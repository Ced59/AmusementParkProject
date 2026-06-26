import { InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';

import { BulkAdministrationUpdateResult } from '@app/models/admin/admin-review-status';
import { ImageCategory } from '@app/models/images/image-category';
import { ImageDto } from '@app/models/images/image-dto';
import { ImageGeoLocation } from '@app/models/images/image-geo-location';
import { ImageOwnerType } from '@app/models/images/image-owner-type';
import { ImageTagDto } from '@app/models/images/image-tag-dto';
import { LinkImageToOwner } from '@app/models/images/link-image-to-owner';
import { UploadedImage } from '@app/models/images/uploaded-image';
import { Park } from '@app/models/parks/park';
import { ParkItem } from '@app/models/parks/park-item';
import { ParkItemAdminRow } from '@app/models/parks/park-item-admin-row';
import { ParksApiResponse } from '@app/models/parks/parks_api_response';
import { ApiResponse } from '@app/models/shared/api_reponse';
import { ClosedEntityFilter } from '@app/models/shared/closed-entity-filter';
import { LocalizedItemDto } from '@app/models/shared/localized-item-dto';
import { ParkItemAdminListFilters, ParkItemAdminListSort, ParkItemsByParkIdFilters } from '@data-access/park-items/park-items-api-endpoints';
import { PagedResult } from '@shared/models/contracts';

export interface AdminFieldModeParksApiServicePort {
  getParksPaginated(page: number, size: number): Observable<ParksApiResponse>;
  searchParks(query: string, page: number, size: number): Observable<ParksApiResponse>;
  getParkById(id: string): Observable<Park>;
}

export interface AdminFieldModeParkItemsApiServicePort {
  getParkItemsByParkIdPage(
    parkId: string,
    page: number,
    size: number,
    filters: ParkItemsByParkIdFilters | null,
    options: { closedFilter?: ClosedEntityFilter } | undefined
  ): Observable<PagedResult<ParkItem>>;
  getParkItemsPaginated(
    page: number,
    size: number,
    parkId?: string | null,
    search?: string | null,
    filters?: ParkItemAdminListFilters | null,
    sort?: ParkItemAdminListSort | null,
    options?: { closedFilter?: ClosedEntityFilter } | undefined
  ): Observable<ApiResponse<ParkItemAdminRow>>;
  getParkItemById(id: string): Observable<ParkItem>;
  updateParkItem(id: string, item: ParkItem): Observable<ParkItem>;
}

export interface AdminFieldModeImagesApiServicePort {
  getImagesPage(ownerType: ImageOwnerType, ownerId: string, category: ImageCategory, page: number, size: number): Observable<PagedResult<ImageDto>>;
  uploadImage(file: File, category: ImageCategory, withWatermark: boolean, description?: string): Observable<UploadedImage>;
  linkImage(request: LinkImageToOwner): Observable<ImageDto>;
  updateAdminImage(id: string, request: {
    category?: ImageCategory | null;
    ownerType?: ImageOwnerType | null;
    ownerId?: string | null;
    isCurrent?: boolean | null;
    description?: string;
    geoLocation?: ImageGeoLocation | null;
    altTexts: LocalizedItemDto<string>[];
    captions: LocalizedItemDto<string>[];
    credits: LocalizedItemDto<string>[];
    tagIds: string[];
    isPublished: boolean;
    sourceUrl?: string | null;
  }): Observable<ImageDto>;
  getAdminImageTags(): Observable<ImageTagDto[]>;
  createAdminImageTag(request: {
    slug: string;
    labels: LocalizedItemDto<string>[];
    descriptions: LocalizedItemDto<string>[];
  }): Observable<ImageTagDto>;
}

export interface AdminFieldModeGeolocationPort {
  getCurrentPosition(options?: PositionOptions): Promise<GeolocationPosition>;
}

export const ADMIN_FIELD_MODE_PARKS_API_SERVICE_PORT = new InjectionToken<AdminFieldModeParksApiServicePort>('ADMIN_FIELD_MODE_PARKS_API_SERVICE_PORT');
export const ADMIN_FIELD_MODE_PARK_ITEMS_API_SERVICE_PORT = new InjectionToken<AdminFieldModeParkItemsApiServicePort>('ADMIN_FIELD_MODE_PARK_ITEMS_API_SERVICE_PORT');
export const ADMIN_FIELD_MODE_IMAGES_API_SERVICE_PORT = new InjectionToken<AdminFieldModeImagesApiServicePort>('ADMIN_FIELD_MODE_IMAGES_API_SERVICE_PORT');
export const ADMIN_FIELD_MODE_GEOLOCATION_PORT = new InjectionToken<AdminFieldModeGeolocationPort>('ADMIN_FIELD_MODE_GEOLOCATION_PORT');

export type AdminFieldModeLocationUpdateResult = BulkAdministrationUpdateResult | ParkItem;
