import { inject, InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';

import { Park } from '@app/models/parks/park';
import { ParkItem } from '@app/models/parks/park-item';
import { VideoDto } from '@app/models/videos/video-dto';
import { VideoSearchQuery } from '@app/models/videos/video-search-query';
import { VideoTagDto } from '@app/models/videos/video-tag-dto';
import { AnonymousHttpOptions } from '@core/http/auth/anonymous-http-options';
import { ParkItemsApiService } from '@data-access/park-items/park-items-api.service';
import { ParksApiService } from '@data-access/parks/parks-api.service';
import { VideosApiService } from '@data-access/videos/videos-api.service';
import { PagedResult } from '@shared/models/contracts';

export interface ParkItemVideosItemsPort {
  getParkItemById(id: string, options?: AnonymousHttpOptions): Observable<ParkItem>;
}

export interface ParkItemVideosParksPort {
  getParkById(id: string, options?: AnonymousHttpOptions): Observable<Park>;
}

export interface ParkItemVideosVideosPort {
  getVideosPage(query?: VideoSearchQuery, options?: AnonymousHttpOptions): Observable<PagedResult<VideoDto>>;
  getVideoById(id: string, options?: AnonymousHttpOptions): Observable<VideoDto>;
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

export const PARK_ITEM_VIDEOS_VIDEOS_PORT = new InjectionToken<ParkItemVideosVideosPort>('PARK_ITEM_VIDEOS_VIDEOS_PORT', {
  providedIn: 'root',
  factory: () => inject(VideosApiService)
});
