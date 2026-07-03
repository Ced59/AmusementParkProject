import { inject, InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';

import { ImageCategory } from '@app/models/images/image-category';
import { ImageDto } from '@app/models/images/image-dto';
import { ImageOwnerType } from '@app/models/images/image-owner-type';
import { ParkItemImageDto } from '@app/models/images/park-item-image-dto';
import { ParkDistanceResponse } from '@app/models/parks/park-distance';
import { ParkDetailSummary } from '@app/models/parks/park-detail-summary';
import { ParkOpeningHoursCalendar } from '@app/models/parks/park-opening-hours';
import { ParkWeatherForecast } from '@app/models/parks/park-weather';
import { HistoryTimeline } from '@app/models/history/history.models';
import { VideoDto } from '@app/models/videos/video-dto';
import { ParkItemVideoDto } from '@app/models/videos/park-item-video-dto';
import { VideoSearchQuery } from '@app/models/videos/video-search-query';
import { HistoryApiService } from '@data-access/history/history-api.service';
import { ImagesApiService } from '@data-access/images/images-api.service';
import { ParksApiService } from '@data-access/parks/parks-api.service';
import { VideosApiService } from '@data-access/videos/videos-api.service';
import { AnonymousHttpOptions } from '@core/http/auth/anonymous-http-options';
import { PagedResult } from '@shared/models/contracts';
import { ClosedEntityFilter } from '@app/models/shared/closed-entity-filter';

export interface ParkDetailHttpOptions extends AnonymousHttpOptions {
  closedFilter?: ClosedEntityFilter;
}

export interface ParkDetailParksPort {
  getParkDetailSummary(id: string, options?: ParkDetailHttpOptions): Observable<ParkDetailSummary>;
  getNearestParks(sourceParkId: string, limit?: number, maxDistanceKilometers?: number | null, options?: AnonymousHttpOptions): Observable<ParkDistanceResponse>;
  getParkWeather(id: string, days?: number, options?: AnonymousHttpOptions): Observable<ParkWeatherForecast>;
  getParkOpeningHours(id: string, from?: string | null, to?: string | null, options?: AnonymousHttpOptions): Observable<ParkOpeningHoursCalendar>;
}

export interface ParkDetailVideosPort {
  getVideosPage(query?: VideoSearchQuery, options?: AnonymousHttpOptions): Observable<PagedResult<VideoDto>>;
  getParkItemVideosByPark(parkId: string, query?: VideoSearchQuery, options?: AnonymousHttpOptions): Observable<PagedResult<ParkItemVideoDto>>;
}

export interface ParkDetailImagesPort {
  getImagesPage(ownerType: ImageOwnerType, ownerId: string, category: ImageCategory, page?: number, size?: number, options?: AnonymousHttpOptions): Observable<PagedResult<ImageDto>>;
  getParkItemImagesByPark(parkId: string, page?: number, size?: number, options?: AnonymousHttpOptions): Observable<PagedResult<ParkItemImageDto>>;
}

export interface ParkDetailHistoryPort {
  getParkTimeline(parkId: string, includeParkItems?: boolean, parkItemIds?: readonly string[], options?: AnonymousHttpOptions): Observable<HistoryTimeline>;
}

export const PARK_DETAIL_PARKS_PORT = new InjectionToken<ParkDetailParksPort>('PARK_DETAIL_PARKS_PORT', {
  providedIn: 'root',
  factory: () => inject(ParksApiService)
});

export const PARK_DETAIL_VIDEOS_PORT = new InjectionToken<ParkDetailVideosPort>('PARK_DETAIL_VIDEOS_PORT', {
  providedIn: 'root',
  factory: () => inject(VideosApiService)
});

export const PARK_DETAIL_IMAGES_PORT = new InjectionToken<ParkDetailImagesPort>('PARK_DETAIL_IMAGES_PORT', {
  providedIn: 'root',
  factory: () => inject(ImagesApiService)
});

export const PARK_DETAIL_HISTORY_PORT = new InjectionToken<ParkDetailHistoryPort>('PARK_DETAIL_HISTORY_PORT', {
  providedIn: 'root',
  factory: () => inject(HistoryApiService)
});
