import { inject, InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';

import { ImageCategory } from '@app/models/images/image-category';
import { ImageDto } from '@app/models/images/image-dto';
import { ImageOwnerType } from '@app/models/images/image-owner-type';
import { ParkDetailSummary } from '@app/models/parks/park-detail-summary';
import { ParkItem } from '@app/models/parks/park-item';
import { VideoDto } from '@app/models/videos/video-dto';
import { VideoSearchQuery } from '@app/models/videos/video-search-query';
import { VideoTagDto } from '@app/models/videos/video-tag-dto';
import { AnonymousHttpOptions } from '@core/http/auth/anonymous-http-options';
import { ImagesApiService } from '@data-access/images/images-api.service';
import { ParkItemsApiService } from '@data-access/park-items/park-items-api.service';
import { ParksApiService } from '@data-access/parks/parks-api.service';
import { VideosApiService } from '@data-access/videos/videos-api.service';
import { PagedResult } from '@shared/models/contracts';

export interface ParkItemVideosItemsPort {
  getParkItemById(id: string, options?: AnonymousHttpOptions): Observable<ParkItem>;
}

export interface ParkItemVideosParksPort {
  getParkDetailSummary(id: string, options?: AnonymousHttpOptions): Observable<ParkDetailSummary>;
}

export interface ParkItemVideosImagesPort {
  getImages(ownerType: ImageOwnerType, ownerId: string, category: ImageCategory, page?: number, size?: number, options?: AnonymousHttpOptions): Observable<ImageDto[]>;
}

export interface ParkItemVideosVideosPort {
  getVideosPage(query?: VideoSearchQuery, options?: AnonymousHttpOptions): Observable<PagedResult<VideoDto>>;
  getVideoById(id: string, options?: AnonymousHttpOptions, languageCode?: string | null): Observable<VideoDto>;
  getVideoTags(options?: AnonymousHttpOptions): Observable<VideoTagDto[]>;
}

export const PARK_ITEM_VIDEOS_ITEMS_PORT = new InjectionToken<ParkItemVideosItemsPort>('PARK_ITEM_VIDEOS_ITEMS_PORT', {
  providedIn: 'root',
  factory: () => inject(ParkItemsApiService)
});

export const PARK_ITEM_VIDEOS_PARKS_PORT = new InjectionToken<ParkItemVideosParksPort>('PARK_ITEM_VIDEOS_PARKS_PORT', {
  providedIn: 'root',
  factory: () => inject(ParksApiService)
});

export const PARK_ITEM_VIDEOS_IMAGES_PORT = new InjectionToken<ParkItemVideosImagesPort>('PARK_ITEM_VIDEOS_IMAGES_PORT', {
  providedIn: 'root',
  factory: () => inject(ImagesApiService)
});

export const PARK_ITEM_VIDEOS_VIDEOS_PORT = new InjectionToken<ParkItemVideosVideosPort>('PARK_ITEM_VIDEOS_VIDEOS_PORT', {
  providedIn: 'root',
  factory: () => inject(VideosApiService)
});
