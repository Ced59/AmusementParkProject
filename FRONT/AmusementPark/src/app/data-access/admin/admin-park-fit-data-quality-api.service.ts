import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';

import {
  ParkFitDataQuality,
  ParkFitDataQualityPage
} from '@app/models/admin/park-fit/park-fit-data-quality.models';
import {
  PagedCollectionResponse,
  unwrapPagedCollection
} from '@app/data-access/shared/api-helpers';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AdminParkFitDataQualityApiService {
  private readonly baseUrl: string = `${environment.apiBaseUrl}admin/park-fit/data-quality`;

  constructor(private readonly http: HttpClient) {
  }

  getPage(page: number, size: number): Observable<ParkFitDataQualityPage> {
    const params: HttpParams = new HttpParams()
      .set('page', page)
      .set('size', size);

    return this.http.get<PagedCollectionResponse<ParkFitDataQuality>>(this.baseUrl, { params })
      .pipe(map((response: PagedCollectionResponse<ParkFitDataQuality>) =>
        unwrapPagedCollection(response)));
  }
}
