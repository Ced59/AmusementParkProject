import { InjectionToken, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ImageCategory } from '@app/models/images/image-category';
import { ImageDto } from '@app/models/images/image-dto';
import { ImageGeoLocation } from '@app/models/images/image-geo-location';
import { ImageOwnerType } from '@app/models/images/image-owner-type';
import { ImageTagDto } from '@app/models/images/image-tag-dto';
import { LinkImageToOwner } from '@app/models/images/link-image-to-owner';
import { RemoteImageImport } from '@app/models/images/remote-image-import';
import { UploadedImage } from '@app/models/images/uploaded-image';
import { LocalizedItemDto } from '@app/models/shared/localized-item-dto';
import { ImagesApiService } from '@data-access/images/images-api.service';

export interface AdminContextualBlockPhotoAddImagesPort {
  uploadImage(file: File, category: ImageCategory, withWatermark?: boolean, description?: string): Observable<UploadedImage>;
  linkImage(request: LinkImageToOwner): Observable<ImageDto>;
  importRemoteImage(request: RemoteImageImport): Observable<ImageDto>;
  getAdminImageTags(): Observable<ImageTagDto[]>;
  createAdminImageTag(request: {
    slug: string;
    labels: LocalizedItemDto<string>[];
    descriptions: LocalizedItemDto<string>[];
  }): Observable<ImageTagDto>;
  updateAdminImage(id: string, request: {
    description?: string;
    geoLocation?: ImageGeoLocation | null;
    altTexts: LocalizedItemDto<string>[];
    captions: LocalizedItemDto<string>[];
    credits: LocalizedItemDto<string>[];
    tagIds: string[];
    isPublished: boolean;
    sourceUrl?: string | null;
  }): Observable<ImageDto>;
}

export const ADMIN_CONTEXTUAL_BLOCK_PHOTO_ADD_IMAGES_PORT = new InjectionToken<AdminContextualBlockPhotoAddImagesPort>(
  'ADMIN_CONTEXTUAL_BLOCK_PHOTO_ADD_IMAGES_PORT',
  {
    providedIn: 'root',
    factory: () => inject(ImagesApiService)
  }
);
