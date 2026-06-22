import { inject, InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';

import { ImageCategory } from '@app/models/images/image-category';
import { ImageDto } from '@app/models/images/image-dto';
import { ImageOwnerType } from '@app/models/images/image-owner-type';
import { Park } from '@app/models/parks/park';
import { ParkItem } from '@app/models/parks/park-item';
import { ParkItemSiblingNavigation } from '@app/models/parks/park-item-sibling-navigation';
import { TechnicalPage } from '@app/models/technical-pages/technical-page';
import { VideoDto } from '@app/models/videos/video-dto';
import { VideoSearchQuery } from '@app/models/videos/video-search-query';
import { ImagesApiService } from '@data-access/images/images-api.service';
import { ManufacturersApiService } from '@data-access/manufacturers/manufacturers-api.service';
import { ParkItemsApiService } from '@data-access/park-items/park-items-api.service';
import { ParksApiService } from '@data-access/parks/parks-api.service';
import { ParkZonesApiService } from '@data-access/parks/park-zones-api.service';
import { TechnicalPagesApiService } from '@data-access/technical-pages/technical-pages-api.service';
import { VideosApiService } from '@data-access/videos/videos-api.service';
import { AnonymousHttpOptions } from '@core/http/auth/anonymous-http-options';
import { PagedResult } from '@shared/models/contracts';

export interface ParkItemDetailItemsPort {
  getParkItemById(id: string, options?: AnonymousHttpOptions): Observable<ParkItem>;
  getParkItemSiblingNavigation(id: string, options?: AnonymousHttpOptions): Observable<ParkItemSiblingNavigation>;
  getRelatedParkItems(id: string, limit?: number, options?: AnonymousHttpOptions): Observable<ParkItem[]>;
}

export interface ParkItemDetailParksPort {
  getParkById(id: string, options?: AnonymousHttpOptions): Observable<Park>;
}

export interface ParkItemDetailManufacturersPort {
  getAttractionManufacturerById(id: string): Observable<{ name?: string | null }>;
}

export interface ParkItemDetailZonesPort {
  getParkZoneById(id: string, options?: AnonymousHttpOptions): Observable<{ name?: string | null }>;
}

export interface ParkItemDetailImagesPort {
  getImages(ownerType: ImageOwnerType, ownerId: string, category: ImageCategory, page?: number, size?: number, options?: AnonymousHttpOptions): Observable<ImageDto[]>;
}

export interface ParkItemDetailVideosPort {
  getVideosPage(query?: VideoSearchQuery, options?: AnonymousHttpOptions): Observable<PagedResult<VideoDto>>;
}

export interface ParkItemDetailTechnicalPagesPort {
  getPublicLinkIndex(): Observable<TechnicalPage[]>;
}

export const PARK_ITEM_DETAIL_ITEMS_PORT = new InjectionToken<ParkItemDetailItemsPort>('PARK_ITEM_DETAIL_ITEMS_PORT', {
  providedIn: 'root',
  factory: () => inject(ParkItemsApiService)
});

export const PARK_ITEM_DETAIL_PARKS_PORT = new InjectionToken<ParkItemDetailParksPort>('PARK_ITEM_DETAIL_PARKS_PORT', {
  providedIn: 'root',
  factory: () => inject(ParksApiService)
});

export const PARK_ITEM_DETAIL_MANUFACTURERS_PORT = new InjectionToken<ParkItemDetailManufacturersPort>('PARK_ITEM_DETAIL_MANUFACTURERS_PORT', {
  providedIn: 'root',
  factory: () => inject(ManufacturersApiService)
});

export const PARK_ITEM_DETAIL_ZONES_PORT = new InjectionToken<ParkItemDetailZonesPort>('PARK_ITEM_DETAIL_ZONES_PORT', {
  providedIn: 'root',
  factory: () => inject(ParkZonesApiService)
});

export const PARK_ITEM_DETAIL_IMAGES_PORT = new InjectionToken<ParkItemDetailImagesPort>('PARK_ITEM_DETAIL_IMAGES_PORT', {
  providedIn: 'root',
  factory: () => inject(ImagesApiService)
});

export const PARK_ITEM_DETAIL_VIDEOS_PORT = new InjectionToken<ParkItemDetailVideosPort>('PARK_ITEM_DETAIL_VIDEOS_PORT', {
  providedIn: 'root',
  factory: () => inject(VideosApiService)
});

export const PARK_ITEM_DETAIL_TECHNICAL_PAGES_PORT = new InjectionToken<ParkItemDetailTechnicalPagesPort>('PARK_ITEM_DETAIL_TECHNICAL_PAGES_PORT', {
  providedIn: 'root',
  factory: () => inject(TechnicalPagesApiService)
});
