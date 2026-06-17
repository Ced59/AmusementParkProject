import { inject, InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';

import { ParkDetailSummary } from '@app/models/parks/park-detail-summary';
import { VideoDto } from '@app/models/videos/video-dto';
import { VideoSearchQuery } from '@app/models/videos/video-search-query';
import { VideoTagDto } from '@app/models/videos/video-tag-dto';
import { AnonymousHttpOptions } from '@core/http/auth/anonymous-http-options';
import { ParksApiService } from '@data-access/parks/parks-api.service';
import { VideosApiService } from '@data-access/videos/videos-api.service';
import { PagedResult } from '@shared/models/contracts';

export interface ParkVideosParksPort {
  getParkDetailSummary(id: string, options?: AnonymousHttpOptions): Observable<ParkDetailSummary>;
}

export interface ParkVideosVideosPort {
  getVideosPage(query?: VideoSearchQuery, options?: AnonymousHttpOptions): Observable<PagedResult<VideoDto>>;
  getVideoById(id: string, options?: AnonymousHttpOptions): Observable<VideoDto>;
  getVideoTags(options?: AnonymousHttpOptions): Observable<VideoTagDto[]>;
}

export const PARK_VIDEOS_PARKS_PORT = new InjectionToken<ParkVideosParksPort>('PARK_VIDEOS_PARKS_PORT', {
  providedIn: 'root',
  factory: () => inject(ParksApiService)
});

export const PARK_VIDEOS_VIDEOS_PORT = new InjectionToken<ParkVideosVideosPort>('PARK_VIDEOS_VIDEOS_PORT', {
  providedIn: 'root',
  factory: () => inject(VideosApiService)
});
