import { inject, InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';

import { ImageCategory } from '@app/models/images/image-category';
import { ImageDto } from '@app/models/images/image-dto';
import { ImageOwnerType } from '@app/models/images/image-owner-type';
import { Park } from '@app/models/parks/park';
import { ParkItem } from '@app/models/parks/park-item';
import { AnonymousHttpOptions } from '@core/http/auth/anonymous-http-options';
import { ImagesApiService } from '@data-access/images/images-api.service';
import { ParkItemsApiService } from '@data-access/park-items/park-items-api.service';
import { ParksApiService } from '@data-access/parks/parks-api.service';
import { PagedResult } from '@shared/models/contracts';

export interface ParkItemImagesItemsPort {
  getParkItemById(id: string, options?: AnonymousHttpOptions): Observable<ParkItem>;
}

export interface ParkItemImagesParksPort {
  getParkById(id: string, options?: AnonymousHttpOptions): Observable<Park>;
}

export interface ParkItemImagesImagesPort {
  getImagesPage(ownerType: ImageOwnerType, ownerId: string, category: ImageCategory, page?: number, size?: number, options?: AnonymousHttpOptions): Observable<PagedResult<ImageDto>>;
}

export const PARK_ITEM_IMAGES_ITEMS_PORT = new InjectionToken<ParkItemImagesItemsPort>('PARK_ITEM_IMAGES_ITEMS_PORT', {
  providedIn: 'root',
  factory: () => inject(ParkItemsApiService)
});

export const PARK_ITEM_IMAGES_PARKS_PORT = new InjectionToken<ParkItemImagesParksPort>('PARK_ITEM_IMAGES_PARKS_PORT', {
  providedIn: 'root',
  factory: () => inject(ParksApiService)
});

export const PARK_ITEM_IMAGES_IMAGES_PORT = new InjectionToken<ParkItemImagesImagesPort>('PARK_ITEM_IMAGES_IMAGES_PORT', {
  providedIn: 'root',
  factory: () => inject(ImagesApiService)
});
