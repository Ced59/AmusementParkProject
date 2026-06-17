import { HttpClient, HttpContext, HttpParameterCodec, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';

import { CreateVideoTagRequest, UpdateVideoTagRequest } from '@app/models/videos/video-tag-write-request';
import { ResolvedVideoMetadataDto } from '@app/models/videos/resolved-video-metadata-dto';
import { VideoDto } from '@app/models/videos/video-dto';
import { VideoSearchQuery } from '@app/models/videos/video-search-query';
import { VideoTagDto } from '@app/models/videos/video-tag-dto';
import { VideoWriteRequest } from '@app/models/videos/video-write-request';
import { environment } from '../../../environments/environment';
import { PagedResult } from '@shared/models/contracts';
import { PagedCollectionResponse, unwrapCollection, unwrapPagedCollection } from '../shared/api-helpers';
import { VIDEOS_API_ENDPOINTS } from './videos-api-endpoints';

interface VideosHttpOptions {
  context?: HttpContext;
}

class StrictUriParameterCodec implements HttpParameterCodec {
  encodeKey(key: string): string {
    return encodeURIComponent(key);
  }

  encodeValue(value: string): string {
    return encodeURIComponent(value);
  }

  decodeKey(key: string): string {
    return decodeURIComponent(key);
  }

  decodeValue(value: string): string {
    return decodeURIComponent(value);
  }
}

const STRICT_URI_PARAMETER_CODEC = new StrictUriParameterCodec();

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

  getVideoById(id: string, options: VideosHttpOptions = {}, languageCode: string | null = null): Observable<VideoDto> {
    const url: string = `${environment.apiBaseUrl}${VIDEOS_API_ENDPOINTS.getVideo(id)}`;
    const params: HttpParams = languageCode?.trim()
      ? new HttpParams().set('languageCode', languageCode.trim())
      : new HttpParams();

    return this.http.get<VideoDto>(url, { ...options, params });
  }

  resolveVideoMetadata(videoUrl: string, options: VideosHttpOptions = {}): Observable<ResolvedVideoMetadataDto> {
    const url: string = `${environment.apiBaseUrl}${VIDEOS_API_ENDPOINTS.resolveMetadata}`;
    const params: HttpParams = new HttpParams({ encoder: STRICT_URI_PARAMETER_CODEC }).set('url', videoUrl);

    return this.http.get<ResolvedVideoMetadataDto>(url, { ...options, params });
  }

  createVideo(request: VideoWriteRequest, options: VideosHttpOptions = {}): Observable<VideoDto> {
    const url: string = `${environment.apiBaseUrl}${VIDEOS_API_ENDPOINTS.createVideo}`;
    return this.http.post<VideoDto>(url, request, options);
  }

  updateVideo(id: string, request: VideoWriteRequest, options: VideosHttpOptions = {}): Observable<VideoDto> {
    const url: string = `${environment.apiBaseUrl}${VIDEOS_API_ENDPOINTS.updateVideo(id)}`;
    return this.http.put<VideoDto>(url, request, options);
  }

  deleteVideo(id: string, options: VideosHttpOptions = {}): Observable<boolean> {
    const url: string = `${environment.apiBaseUrl}${VIDEOS_API_ENDPOINTS.deleteVideo(id)}`;
    return this.http.delete<boolean>(url, options);
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

  createVideoTag(request: CreateVideoTagRequest, options: VideosHttpOptions = {}): Observable<VideoTagDto> {
    const url: string = `${environment.apiBaseUrl}${VIDEOS_API_ENDPOINTS.createVideoTag}`;
    return this.http.post<VideoTagDto>(url, request, options);
  }

  updateVideoTag(id: string, request: UpdateVideoTagRequest, options: VideosHttpOptions = {}): Observable<VideoTagDto> {
    const url: string = `${environment.apiBaseUrl}${VIDEOS_API_ENDPOINTS.updateVideoTag(id)}`;
    return this.http.put<VideoTagDto>(url, request, options);
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
    params = this.appendOptionalParam(params, 'languageCode', query.languageCode);
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
