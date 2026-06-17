import { HttpClient, HttpContext, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';

import { VideoDto } from '@app/models/videos/video-dto';
import { VideoSearchQuery } from '@app/models/videos/video-search-query';
import { VideoTagDto } from '@app/models/videos/video-tag-dto';
import { environment } from '../../../environments/environment';
import { PagedResult } from '@shared/models/contracts';
import { PagedCollectionResponse, unwrapCollection, unwrapPagedCollection } from '../shared/api-helpers';
import { VIDEOS_API_ENDPOINTS } from './videos-api-endpoints';

interface VideosHttpOptions {
  context?: HttpContext;
}

@Injectable({
  providedIn: 'root'
})
export class VideosApiService {
  constructor(private readonly http: HttpClient) {
  }

  getVideosPage(query: VideoSearchQuery = {}, options: VideosHttpOptions = {}): Observable<PagedResult<VideoDto>> {
    const url: string = `${environment.apiBaseUrl}${VIDEOS_API_ENDPOINTS.getVideos}`;
    const params: HttpParams = this.buildSearchParams(query);

    return this.http.get<VideoDto[] | PagedCollectionResponse<VideoDto>>(url, { ...options, params }).pipe(
      map((response: VideoDto[] | PagedCollectionResponse<VideoDto>) => unwrapPagedCollection<VideoDto>(response))
    );
  }

  getVideoById(id: string, options: VideosHttpOptions = {}): Observable<VideoDto> {
    const url: string = `${environment.apiBaseUrl}${VIDEOS_API_ENDPOINTS.getVideo(id)}`;
    return this.http.get<VideoDto>(url, options);
  }

  getVideoTags(options: VideosHttpOptions = {}): Observable<VideoTagDto[]> {
    const url: string = `${environment.apiBaseUrl}${VIDEOS_API_ENDPOINTS.getVideoTags}`;
    const params: HttpParams = new HttpParams()
      .set('page', '1')
      .set('size', '100');

    return this.http.get<VideoTagDto[] | PagedCollectionResponse<VideoTagDto>>(url, { ...options, params }).pipe(
      map((response: VideoTagDto[] | PagedCollectionResponse<VideoTagDto>) => unwrapCollection<VideoTagDto>(response))
    );
  }

  private buildSearchParams(query: VideoSearchQuery): HttpParams {
    let params: HttpParams = new HttpParams()
      .set('page', String(query.page ?? 1))
      .set('size', String(query.size ?? 24));

    params = this.appendOptionalParam(params, 'search', query.search);
    params = this.appendOptionalParam(params, 'hostingProvider', query.hostingProvider);
    params = this.appendOptionalParam(params, 'ownerType', query.ownerType);
    params = this.appendOptionalParam(params, 'ownerId', query.ownerId);
    params = this.appendOptionalParam(params, 'type', query.type);
    params = this.appendOptionalParam(params, 'tagId', query.tagId);
    params = this.appendOptionalParam(params, 'creatorName', query.creatorName);
    params = this.appendOptionalBooleanParam(params, 'isPublished', query.isPublished);
    params = this.appendOptionalParam(params, 'sortBy', query.sortBy);
    params = this.appendOptionalParam(params, 'sortDirection', query.sortDirection);

    return params;
  }

  private appendOptionalParam(params: HttpParams, key: string, value: string | number | null | undefined): HttpParams {
    if (value === null || value === undefined || String(value).trim() === '') {
      return params;
    }

    return params.set(key, String(value));
  }

  private appendOptionalBooleanParam(params: HttpParams, key: string, value: boolean | null | undefined): HttpParams {
    if (value === null || value === undefined) {
      return params;
    }

    return params.set(key, String(value));
  }
}
