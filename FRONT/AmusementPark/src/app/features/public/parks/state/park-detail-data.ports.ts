import { inject, InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';

import { ParkDistanceResponse } from '@app/models/parks/park-distance';
import { ParkDetailSummary } from '@app/models/parks/park-detail-summary';
import { ParkWeatherForecast } from '@app/models/parks/park-weather';
import { VideoDto } from '@app/models/videos/video-dto';
import { VideoSearchQuery } from '@app/models/videos/video-search-query';
import { ParksApiService } from '@data-access/parks/parks-api.service';
import { VideosApiService } from '@data-access/videos/videos-api.service';
import { AnonymousHttpOptions } from '@core/http/auth/anonymous-http-options';
import { PagedResult } from '@shared/models/contracts';

export interface ParkDetailParksPort {
  getParkDetailSummary(id: string, options?: AnonymousHttpOptions): Observable<ParkDetailSummary>;
  getNearestParks(sourceParkId: string, limit?: number, maxDistanceKilometers?: number | null, options?: AnonymousHttpOptions): Observable<ParkDistanceResponse>;
  getParkWeather(id: string, days?: number, options?: AnonymousHttpOptions): Observable<ParkWeatherForecast>;
}

export interface ParkDetailVideosPort {
  getVideosPage(query?: VideoSearchQuery, options?: AnonymousHttpOptions): Observable<PagedResult<VideoDto>>;
}

export const PARK_DETAIL_PARKS_PORT = new InjectionToken<ParkDetailParksPort>('PARK_DETAIL_PARKS_PORT', {
  providedIn: 'root',
  factory: () => inject(ParksApiService)
});

export const PARK_DETAIL_VIDEOS_PORT = new InjectionToken<ParkDetailVideosPort>('PARK_DETAIL_VIDEOS_PORT', {
  providedIn: 'root',
  factory: () => inject(VideosApiService)
});
