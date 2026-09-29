import { inject, InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';

import { PublicLiveForecast, PublicLiveHistory, PublicLiveTarget, PublicParkLiveItems } from '@app/models/live-data/public-live.models';
import { PublicLiveApiService } from '@data-access/live-data/public-live-api.service';

export interface PublicLiveDataPort {
  getPark(parkId: string): Observable<PublicLiveTarget>;
  getParkItem(itemId: string): Observable<PublicLiveTarget>;
  getParkItems(parkId: string): Observable<PublicParkLiveItems>;
  getParkItemHistory(itemId: string): Observable<PublicLiveHistory>;
  getParkItemForecast(itemId: string): Observable<PublicLiveForecast>;
}

export const PUBLIC_LIVE_DATA_PORT = new InjectionToken<PublicLiveDataPort>('PUBLIC_LIVE_DATA_PORT', {
  providedIn: 'root',
  factory: () => inject(PublicLiveApiService)
});
