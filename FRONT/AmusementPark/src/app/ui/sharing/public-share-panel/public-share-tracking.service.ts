import { Injectable } from '@angular/core';
import { EMPTY } from 'rxjs';
import { catchError, take } from 'rxjs/operators';

import { SocialShareEventsApiService } from '@data-access/social-share/social-share-events-api.service';
import { CaptureSocialShareEventRequest } from '@app/models/social-share/social-share.models';

@Injectable({
  providedIn: 'root'
})
export class PublicShareTrackingService {
  constructor(private readonly eventsApiService: SocialShareEventsApiService) {
  }

  track(request: CaptureSocialShareEventRequest): void {
    this.eventsApiService.captureEvent(request).pipe(
      take(1),
      catchError(() => EMPTY)
    ).subscribe();
  }
}
