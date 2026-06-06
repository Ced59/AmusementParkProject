import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { ImageCategory } from '@app/models/images/image-category';
import { AdminImageBulkMetadataResult, AdminImageBulkMetadataUpdate } from '@app/models/images/admin-image-bulk-metadata-update';
import { AdminImageSearchQuery } from '@app/models/images/admin-image-search-query';
import { ImageDto } from '@app/models/images/image-dto';
import { ImageGeoLocation } from '@app/models/images/image-geo-location';
import { ImageOwnerType } from '@app/models/images/image-owner-type';
import { ImageTagDto } from '@app/models/images/image-tag-dto';
import { LinkImageToOwner } from '@app/models/images/link-image-to-owner';
import { UploadedImage } from '@app/models/images/uploaded-image';
import { LocalizedItemDto } from '@app/models/shared/localized-item-dto';
import { PagedResult } from '@shared/models/contracts';
import { UrlSecurityService } from '@shared/utils/security';
import {
  PagedCollectionResponse,
  unwrapPagedCollection,
  toImageCategoryApiValue,
  toImageOwnerTypeApiValue,
  unwrapCollection
} from '../shared/api-helpers';
import { IMAGES_API_ENDPOINTS } from './images-api-endpoints';

@Injectable({
  providedIn: 'root'
})
export class ImagesApiService {
  constructor(
    private readonly http: HttpClient,
    private readonly urlSecurityService: UrlSecurityService
  ) {
  }

  uploadImage(
    file: File,
    category: ImageCategory,
    withWatermark: boolean = true,
    description?: string
  ): Observable<UploadedImage> {
    const url: string = `${environment.apiBaseUrl}${IMAGES_API_ENDPOINTS.uploadImage}`;
    const formData: FormData = new FormData();

    formData.append('File', file);
    formData.append('Category', String(toImageCategoryApiValue(category)));
    formData.append('WithWatermark', String(withWatermark));

    if (description) {
      formData.append('Description', description);
    }

    return this.http.post<UploadedImage>(url, formData);
  }

  linkImage(request: LinkImageToOwner): Observable<ImageDto> {
    const url: string = `${environment.apiBaseUrl}${IMAGES_API_ENDPOINTS.linkImage}`;
    return this.http.post<ImageDto>(url, {
      ...request,
      ownerType: toImageOwnerTypeApiValue(request.ownerType)
    });
  }

  getImages(ownerType: ImageOwnerType, ownerId: string, category: ImageCategory, page: number = 1, size: number = 100): Observable<ImageDto[]> {
    const ownerTypeApiValue: number = toImageOwnerTypeApiValue(ownerType);
    const categoryApiValue: number = toImageCategoryApiValue(category);
    const url: string = `${environment.apiBaseUrl}${IMAGES_API_ENDPOINTS.getImages(
      String(ownerTypeApiValue),
      ownerId,
      String(categoryApiValue)
    )}`;
    const params: HttpParams = new HttpParams()
      .set('page', String(page))
      .set('size', String(size));

    return this.http.get<ImageDto[] | PagedCollectionResponse<ImageDto>>(url, { params }).pipe(
      map((response: ImageDto[] | PagedCollectionResponse<ImageDto>) => unwrapCollection<ImageDto>(response))
    );
  }

  getCurrentImage(ownerType: ImageOwnerType, ownerId: string, category: ImageCategory): Observable<ImageDto> {
    const url: string = `${environment.apiBaseUrl}${IMAGES_API_ENDPOINTS.getCurrentImage(
      String(toImageOwnerTypeApiValue(ownerType)),
      ownerId,
      String(toImageCategoryApiValue(category))
    )}`;
    return this.http.get<ImageDto>(url);
  }

  setCurrentImage(imageId: string): Observable<ImageDto> {
    const url: string = `${environment.apiBaseUrl}${IMAGES_API_ENDPOINTS.setCurrentImage(imageId)}`;
    return this.http.put<ImageDto>(url, {});
  }

  deleteImage(imageId: string): Observable<boolean> {
    const url: string = `${environment.apiBaseUrl}${IMAGES_API_ENDPOINTS.deleteImage(imageId)}`;
    return this.http.delete<boolean>(url);
  }

  buildImageUrl(imageId: string): string {
    return `${environment.imagesBaseUrl}/${imageId}`;
  }

  resolveImageUrl(imagePathOrUrl?: string | null): string | null {
    const rawValue: string | undefined = imagePathOrUrl?.trim();

    if (!rawValue) {
      return null;
    }

    if (/^data:/i.test(rawValue) || /^blob:/i.test(rawValue) || /^https?:\/\//i.test(rawValue)) {
      return this.urlSecurityService.sanitizeImageUrl(rawValue);
    }

    if (/^[a-z][a-z0-9+.-]*:/i.test(rawValue)) {
      return null;
    }

    if (rawValue.startsWith('/images/')) {
      const imageId: string = rawValue.replace(/^\/images\//, '');
      return this.buildImageUrl(imageId);
    }

    if (rawValue.startsWith('images/')) {
      const imageId: string = rawValue.replace(/^images\//, '');
      return this.buildImageUrl(imageId);
    }

    if (!rawValue.includes('/')) {
      return this.buildImageUrl(rawValue);
    }

    const normalizedPath: string = rawValue.replace(/^\/+/, '');
    const normalizedApiBaseUrl: string = environment.apiBaseUrl.endsWith('/')
      ? environment.apiBaseUrl
      : `${environment.apiBaseUrl}/`;

    return `${normalizedApiBaseUrl}${normalizedPath}`;
  }

  buildAvatarFallbackDataUrl(size: number = 128): string {
    const svg: string = `<svg xmlns="http://www.w3.org/2000/svg" width="${size}" height="${size}" viewBox="0 0 128 128"><circle cx="64" cy="64" r="64" fill="#e5e7eb"/><circle cx="64" cy="46" r="22" fill="#9ca3af"/><path d="M24 110c8-18 24-28 40-28s32 10 40 28" fill="#9ca3af"/></svg>`;
    return `data:image/svg+xml;utf8,${encodeURIComponent(svg)}`;
  }

  resolveAvatarUrl(imagePathOrUrl?: string | null): string {
    return this.resolveImageUrl(imagePathOrUrl) ?? this.buildAvatarFallbackDataUrl();
  }

  getAdminImages(query: Partial<AdminImageSearchQuery> = {}): Observable<PagedResult<ImageDto>> {
    const url: string = `${environment.apiBaseUrl}${IMAGES_API_ENDPOINTS.getAdminImages}`;
    let params: HttpParams = new HttpParams()
      .set('page', String(query.page ?? 1))
      .set('size', String(query.size ?? 40));

    params = this.appendOptionalParam(params, 'search', query.search);
    params = this.appendOptionalParam(params, 'category', query.category);
    params = this.appendOptionalParam(params, 'ownerType', query.ownerType);
    params = this.appendOptionalParam(params, 'ownerId', query.ownerId);
    params = this.appendOptionalParam(params, 'tagId', query.tagId);
    params = this.appendOptionalBooleanParam(params, 'isPublished', query.isPublished);
    params = this.appendOptionalBooleanParam(params, 'hasOwner', query.hasOwner);
    params = this.appendOptionalBooleanParam(params, 'hasGeoLocation', query.hasGeoLocation);
    params = this.appendOptionalParam(params, 'sortBy', query.sortBy);
    params = this.appendOptionalParam(params, 'sortDirection', query.sortDirection);

    return this.http.get<ImageDto[] | PagedCollectionResponse<ImageDto>>(url, { params }).pipe(
      map((response: ImageDto[] | PagedCollectionResponse<ImageDto>) => unwrapPagedCollection<ImageDto>(response))
    );
  }

  updateAdminImagesBulkMetadata(request: AdminImageBulkMetadataUpdate): Observable<AdminImageBulkMetadataResult> {
    const url: string = `${environment.apiBaseUrl}${IMAGES_API_ENDPOINTS.getAdminImages}/bulk-metadata`;
    return this.http.patch<AdminImageBulkMetadataResult>(url, request);
  }

  updateAdminImage(id: string, request: {
    description?: string;
    geoLocation?: ImageGeoLocation | null;
    altTexts: LocalizedItemDto<string>[];
    captions: LocalizedItemDto<string>[];
    credits: LocalizedItemDto<string>[];
    tagIds: string[];
    isPublished: boolean;
  }): Observable<ImageDto> {
    const url: string = `${environment.apiBaseUrl}${IMAGES_API_ENDPOINTS.updateAdminImage(id)}`;
    return this.http.put<ImageDto>(url, request);
  }

  getAdminImageTags(): Observable<ImageTagDto[]> {
    const url: string = `${environment.apiBaseUrl}${IMAGES_API_ENDPOINTS.getAdminImageTags}`;
    const params: HttpParams = new HttpParams()
      .set('page', '1')
      .set('size', '100');

    return this.http.get<ImageTagDto[] | PagedCollectionResponse<ImageTagDto>>(url, { params }).pipe(
      map((response: ImageTagDto[] | PagedCollectionResponse<ImageTagDto>) => unwrapCollection<ImageTagDto>(response))
    );
  }

  createAdminImageTag(request: {
    slug: string;
    labels: LocalizedItemDto<string>[];
    descriptions: LocalizedItemDto<string>[];
  }): Observable<ImageTagDto> {
    const url: string = `${environment.apiBaseUrl}${IMAGES_API_ENDPOINTS.createAdminImageTag}`;
    return this.http.post<ImageTagDto>(url, request);
  }

  updateAdminImageTag(id: string, request: {
    slug: string;
    labels: LocalizedItemDto<string>[];
    descriptions: LocalizedItemDto<string>[];
    isActive: boolean;
  }): Observable<ImageTagDto> {
    const url: string = `${environment.apiBaseUrl}${IMAGES_API_ENDPOINTS.updateAdminImageTag(id)}`;
    return this.http.put<ImageTagDto>(url, request);
  }
  private appendOptionalParam(params: HttpParams, key: string, value: string | number | null | undefined): HttpParams {
    if (value === null || value === undefined || String(value).trim() === '') {
      return params;
    }

    return params.set(key, String(value));
  }

  private appendOptionalBooleanParam(params: HttpParams, key: string, value: boolean | null | undefined): HttpParams {
    if (value === null || value === undefined) {
      return params;
    }

    return params.set(key, String(value));
  }

}
