import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  CaptureSocialShareEventRequest,
  CaptureSocialShareEventResponse
} from '@app/models/social-share/social-share.models';

@Injectable({
  providedIn: 'root'
})
export class SocialShareEventsApiService {
  private readonly baseUrl: string = `${environment.apiBaseUrl}social-share/events`;

  constructor(private readonly http: HttpClient) {
  }

  captureEvent(request: CaptureSocialShareEventRequest): Observable<CaptureSocialShareEventResponse> {
    return this.http.post<CaptureSocialShareEventResponse>(this.baseUrl, request);
  }
}
