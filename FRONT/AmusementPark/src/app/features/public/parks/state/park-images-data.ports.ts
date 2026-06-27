import { inject, InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';

import { ImageCategory } from '@app/models/images/image-category';
import { ImageDto } from '@app/models/images/image-dto';
import { ImageOwnerType } from '@app/models/images/image-owner-type';
import { ImageTagDto } from '@app/models/images/image-tag-dto';
import { ParkItemImageDto } from '@app/models/images/park-item-image-dto';
import { ParkDetailSummary } from '@app/models/parks/park-detail-summary';
import { ImagesApiService } from '@data-access/images/images-api.service';
import { ParksApiService } from '@data-access/parks/parks-api.service';
import { AnonymousHttpOptions } from '@core/http/auth/anonymous-http-options';
import { PagedResult } from '@shared/models/contracts';

export interface ParkImagesParksPort {
  getParkDetailSummary(id: string, options?: AnonymousHttpOptions): Observable<ParkDetailSummary>;
}

export interface ParkImagesImagesPort {
  getImagesPage(ownerType: ImageOwnerType, ownerId: string, category: ImageCategory, page?: number, size?: number, options?: AnonymousHttpOptions): Observable<PagedResult<ImageDto>>;
  getParkItemImagesByPark(parkId: string, page?: number, size?: number, options?: AnonymousHttpOptions): Observable<PagedResult<ParkItemImageDto>>;
  getImageTags(options?: AnonymousHttpOptions): Observable<ImageTagDto[]>;
}

export const PARK_IMAGES_PARKS_PORT = new InjectionToken<ParkImagesParksPort>('PARK_IMAGES_PARKS_PORT', {
  providedIn: 'root',
  factory: () => inject(ParksApiService)
});

export const PARK_IMAGES_IMAGES_PORT = new InjectionToken<ParkImagesImagesPort>('PARK_IMAGES_IMAGES_PORT', {
  providedIn: 'root',
  factory: () => inject(ImagesApiService)
});
