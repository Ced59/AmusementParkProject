import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { ImageCategory } from '../../models/images/image-category';
import { ImageDto } from '../../models/images/image-dto';
import { ImageGeoLocation } from '../../models/images/image-geo-location';
import { ImageOwnerType } from '../../models/images/image-owner-type';
import { ImageTagDto } from '../../models/images/image-tag-dto';
import { LinkImageToOwner } from '../../models/images/link-image-to-owner';
import { UploadedImage } from '../../models/images/uploaded-image';
import { LocalizedItemDto } from '../../models/shared/localized-item-dto';
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
    if (!imagePathOrUrl) {
      return null;
    }

    if (/^https?:\/\//i.test(imagePathOrUrl)) {
      return imagePathOrUrl;
    }

    if (imagePathOrUrl.startsWith('/images/')) {
      const imageId: string = imagePathOrUrl.replace(/^\/images\//, '');
      return this.buildImageUrl(imageId);
    }

    const normalizedPath: string = imagePathOrUrl.replace(/^\/+/, '');
    return `${environment.apiBaseUrl}${normalizedPath}`;
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
