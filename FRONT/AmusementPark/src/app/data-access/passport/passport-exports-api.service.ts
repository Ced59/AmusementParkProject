import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { PassportExport, RequestPassportExport } from '@app/models/passport/passport-export.models';
import { environment } from '../../../environments/environment';
import { PASSPORT_EXPORTS_API_ENDPOINTS } from './passport-exports-api-endpoints';

@Injectable({
  providedIn: 'root'
})
export class PassportExportsApiService {
  constructor(private readonly http: HttpClient) {
  }

  requestExport(request: RequestPassportExport): Observable<PassportExport> {
    const url: string = `${environment.apiBaseUrl}${PASSPORT_EXPORTS_API_ENDPOINTS.request}`;
    return this.http.post<PassportExport>(url, request);
  }

  getExport(exportId: string): Observable<PassportExport> {
    const url: string = `${environment.apiBaseUrl}${PASSPORT_EXPORTS_API_ENDPOINTS.status(exportId)}`;
    return this.http.get<PassportExport>(url, { transferCache: false });
  }

  downloadExport(exportId: string): Observable<Blob> {
    const url: string = `${environment.apiBaseUrl}${PASSPORT_EXPORTS_API_ENDPOINTS.download(exportId)}`;
    return this.http.get(url, { responseType: 'blob', transferCache: false });
  }
}
