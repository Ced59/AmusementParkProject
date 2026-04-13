import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { ImageCategory } from '@app/models/images/image-category';
import { ImageDto } from '@app/models/images/image-dto';
import { ImageGeoLocation } from '@app/models/images/image-geo-location';
import { ImageOwnerType } from '@app/models/images/image-owner-type';
import { ImageTagDto } from '@app/models/images/image-tag-dto';
import { LinkImageToOwner } from '@app/models/images/link-image-to-owner';
import { UploadedImage } from '@app/models/images/uploaded-image';
import { LocalizedItemDto } from '@app/models/shared/localized-item-dto';
import {
  PagedCollectionResponse,
  toImageCategoryApiValue,
  toImageOwnerTypeApiValue,
  unwrapCollection
} from '../shared/api-helpers';
import { IMAGES_API_ENDPOINTS } from './images-api-endpoints';

@Injectable({
  providedIn: 'root'
})
export class ImagesApiService {
  constructor(private readonly http: HttpClient) {
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

  getImages(ownerType: ImageOwnerType, ownerId: string, category: ImageCategory): Observable<ImageDto[]> {
    const url: string = `${environment.apiBaseUrl}${IMAGES_API_ENDPOINTS.getImages(ownerType, ownerId, category)}`;
    return this.http.get<ImageDto[] | PagedCollectionResponse<ImageDto>>(url).pipe(
      map((response: ImageDto[] | PagedCollectionResponse<ImageDto>) => unwrapCollection<ImageDto>(response))
    );
  }

  getCurrentImage(ownerType: ImageOwnerType, ownerId: string, category: ImageCategory): Observable<ImageDto> {
    const url: string = `${environment.apiBaseUrl}${IMAGES_API_ENDPOINTS.getCurrentImage(ownerType, ownerId, category)}`;
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

    if (/^data:/i.test(rawValue) || /^https?:\/\//i.test(rawValue)) {
      return rawValue;
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

  getAdminImages(): Observable<ImageDto[]> {
    const url: string = `${environment.apiBaseUrl}${IMAGES_API_ENDPOINTS.getAdminImages}`;
    return this.http.get<ImageDto[] | PagedCollectionResponse<ImageDto>>(url).pipe(
      map((response: ImageDto[] | PagedCollectionResponse<ImageDto>) => unwrapCollection<ImageDto>(response))
    );
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
    return this.http.get<ImageTagDto[] | PagedCollectionResponse<ImageTagDto>>(url).pipe(
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
}
